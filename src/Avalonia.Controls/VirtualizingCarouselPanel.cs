using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    /// <summary>
    /// A panel used by <see cref="Carousel"/> to display the current item.
    /// </summary>
    public class VirtualizingCarouselPanel : VirtualizingPanel, ILogicalScrollable
    {
        private static readonly AttachedProperty<object?> RecycleKeyProperty =
            AvaloniaProperty.RegisterAttached<VirtualizingStackPanel, Control, object?>("RecycleKey");

        private static readonly object s_itemIsItsOwnContainer = new object();
        private Size _extent;
        private Vector _offset;
        private Size _viewport;
        private Dictionary<object, Stack<Control>>? _recyclePool;
        private Control? _realized;
        private int _realizedIndex = -1;
        private Control? _transitionFrom;
        private int _transitionFromIndex = -1;
        private CancellationTokenSource? _transition;
        private EventHandler? _scrollInvalidated;

        bool ILogicalScrollable.CanHorizontallyScroll { get; set; }
        bool ILogicalScrollable.CanVerticallyScroll { get; set; }
        bool ILogicalScrollable.IsLogicalScrollEnabled => true;
        Size ILogicalScrollable.ScrollSize => new(1, 1);
        Size ILogicalScrollable.PageScrollSize => new(1, 1);
        Size IScrollable.Extent => Extent;
        Size IScrollable.Viewport => Viewport;

        Vector IScrollable.Offset 
        {
            get => _offset;
            set
            {
                if ((int)_offset.X != value.X)
                    InvalidateMeasure();
                _offset = value;
            }
        }

        private Size Extent
        {
            get => _extent;
            set
            {
                if (_extent != value)
                {
                    _extent = value;
                    _scrollInvalidated?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private Size Viewport
        {
            get => _viewport;
            set
            {
                if (_viewport != value)
                {
                    _viewport = value;
                    _scrollInvalidated?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        event EventHandler? ILogicalScrollable.ScrollInvalidated
        {
            add => _scrollInvalidated += value;
            remove => _scrollInvalidated -= value;
        }

        bool ILogicalScrollable.BringIntoView(Control target, Rect targetRect) => false;
        Control? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control? from) => null;
        void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e) => _scrollInvalidated?.Invoke(this, e);

        protected override Size MeasureOverride(Size availableSize)
        {
            var items = Items;
            var index = (int)_offset.X;

            if (index != _realizedIndex)
            {
                if (_realized is not null)
                {
                    var cancelTransition = _transition is not null;

                    // Cancel any already running transition, and recycle the element we're transitioning from.
                    if (cancelTransition)
                    {
                        _transition!.Cancel();
                        _transition = null;
                        if (_transitionFrom is not null)
                            RecycleElement(_transitionFrom);
                        _transitionFrom = null;
                        _transitionFromIndex = -1;
                    }

                    if (cancelTransition || GetTransition() is null)
                    {
                        // If don't have a transition or we've just canceled a transition then recycle the element
                        // we're moving from.
                        RecycleElement(_realized);
                    }
                    else
                    {
                        // We have a transition to do: record the current element as the element we're transitioning
                        // from and we'll start the transition in the arrange pass.
                        _transitionFrom = _realized;
                        _transitionFromIndex = _realizedIndex;
                    }

                    _realized = null;
                    _realizedIndex = -1;
                }
                
                // Get or create an element for the new item.
                if (index >= 0 && index < items.Count)
                {
                    _realized = GetOrCreateElement(items, index);
                    _realizedIndex = index;
                }
            }

            if (_realized is null)
            {
                Extent = Viewport = new(0, 0);
                _transitionFrom = null;
                _transitionFromIndex = -1;
                return default;
            }

            _realized.Measure(availableSize);
            Extent = new(items.Count, 1);
            Viewport = new(1, 1);

            return _realized.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);

            if (_transition is null &&
                _transitionFrom is not null &&
                _realized is { } to &&
                GetTransition() is { } transition)
            {
                _transition = new CancellationTokenSource();

                var forward = (_realizedIndex > _transitionFromIndex);
                if (Items.Count > 2)
                {
                    forward = forward || (_transitionFromIndex == Items.Count - 1 && _realizedIndex == 0);
                    forward = forward && !(_transitionFromIndex == 0 && _realizedIndex == Items.Count - 1);
                }

                transition.Start(_transitionFrom, to, forward, _transition.Token)
                    .ContinueWith(TransitionFinished, DispatcherTaskScheduler.UIThread);
            }

            return result;
        }

        protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap) => null;

        protected internal override Control? ContainerFromIndex(int index)
        {
            if (index < 0 || index >= Items.Count)
                return null;
            if (index == _realizedIndex)
                return _realized;
            if (Items[index] is Control c && c.GetValue(RecycleKeyProperty) == s_itemIsItsOwnContainer)
                return c;
            return null;
        }

        protected internal override IEnumerable<Control>? GetRealizedContainers()
        {
            return _realized is not null ? new[] { _realized } : null;
        }

        protected internal override int IndexFromContainer(Control container)
        {
            return container == _realized ? _realizedIndex : -1;
        }

        protected internal override Control? ScrollIntoView(int index)
        {
            return null;
        }

        protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(items, e);

            void Add(int index, int count)
            {
                if (index <= _realizedIndex)
                    _realizedIndex += count;
            }

            void Remove(int index, int count)
            {
                var end = index + (count - 1);

                if (_realized is not null && index <= _realizedIndex && end >= _realizedIndex)
                {
                    RecycleElement(_realized);
                    _realized = null;
                    _realizedIndex = -1;
                }
                else if (index < _realizedIndex)
                {
                    _realizedIndex -= count;
                }
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Add(e.NewStartingIndex, e.NewItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex < 0)
                    {
                        goto case NotifyCollectionChangedAction.Reset;
                    }

                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    Add(e.NewStartingIndex, e.NewItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex < 0)
                    {
                        goto case NotifyCollectionChangedAction.Reset;
                    }

                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    var insertIndex = e.NewStartingIndex;

                    if (e.NewStartingIndex > e.OldStartingIndex)
                    {
                        insertIndex -= e.OldItems.Count - 1;
                    }

                    Add(insertIndex, e.NewItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    if (_realized is not null)
                    {
                        RecycleElement(_realized);
                        _realized = null;
                        _realizedIndex = -1;
                    }
                    break;
            }

            InvalidateMeasure();
        }

        private Control GetOrCreateElement(IReadOnlyList<object?> items, int index)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var e = GetRealizedElement(index);

            if (e is null)
            {
                var item = items[index];
                var generator = ItemContainerGenerator!;

                if (generator.NeedsContainer(item, index, out var recycleKey))
                {
                    e = GetRecycledElement(item, index, recycleKey) ??
                        CreateElement(item, index, recycleKey);
                }
                else
                {
                    e = GetItemAsOwnContainer(item, index);
                }
            }

            return e;
        }

        private Control? GetRealizedElement(int index)
        {
            return _realizedIndex == index ? _realized : null;
        }

        private Control GetItemAsOwnContainer(object? item, int index)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var controlItem = (Control)item!;
            var generator = ItemContainerGenerator!;

            if (!controlItem.IsSet(RecycleKeyProperty))
            {
                generator.PrepareItemContainer(controlItem, controlItem, index);
                AddInternalChild(controlItem);
                controlItem.SetValue(RecycleKeyProperty, s_itemIsItsOwnContainer);
                generator.ItemContainerPrepared(controlItem, item, index);
            }

            controlItem.IsVisible = true;
            return controlItem;
        }

        private Control? GetRecycledElement(object? item, int index, object? recycleKey)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            if (recycleKey is null)
                return null;

            var generator = ItemContainerGenerator!;

            if (_recyclePool?.TryGetValue(recycleKey, out var recyclePool) == true && recyclePool.Count > 0)
            {
                var recycled = recyclePool.Pop();
                recycled.IsVisible = true;
                generator.PrepareItemContainer(recycled, item, index);
                generator.ItemContainerPrepared(recycled, item, index);
                return recycled;
            }

            return null;
        }

        private Control CreateElement(object? item, int index, object? recycleKey)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var generator = ItemContainerGenerator!;
            var container = generator.CreateContainer(item, index, recycleKey);

            container.SetValue(RecycleKeyProperty, recycleKey);
            generator.PrepareItemContainer(container, item, index);
            AddInternalChild(container);
            generator.ItemContainerPrepared(container, item, index);

            return container;
        }

        private void RecycleElement(Control element)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var recycleKey = element.GetValue(RecycleKeyProperty);
            Debug.Assert(recycleKey is not null);

            if (recycleKey == s_itemIsItsOwnContainer)
            {
                element.IsVisible = false;
            }
            else
            {
                ItemContainerGenerator.ClearItemContainer(element);
                _recyclePool ??= new();

                if (!_recyclePool.TryGetValue(recycleKey, out var pool))
                {
                    pool = new();
                    _recyclePool.Add(recycleKey, pool);
                }

                pool.Push(element);
                element.IsVisible = false;
            }
        }

        private IPageTransition? GetTransition() => (ItemsControl as Carousel)?.PageTransition;

        private void TransitionFinished(Task task)
        {
            if (task.IsCanceled)
                return;

            if (_transitionFrom is not null)
                RecycleElement(_transitionFrom);
            _transition = null;
            _transitionFrom = null;
            _transitionFromIndex = -1;
        }
    }
}
