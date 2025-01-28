using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for panels that can be used to virtualize items for an <see cref="ItemsControl"/>.
    /// </summary>
    /// <remarks>
    /// Panels should implement the abstract members of this class to provide virtualization of
    /// items in a <see cref="ItemsControl"/>. Derived panels can manage scrolling by implementing
    /// <see cref="ILogicalScrollable"/> or by listening to the 
    /// <see cref="Layoutable.EffectiveViewportChanged"/> event.
    /// 
    /// The methods on the <see cref="ItemContainerGenerator"/> should be used to create, prepare and
    /// clear containers for items.
    /// </remarks>
    public abstract class VirtualizingPanel : Panel, INavigableContainer
    {
        private ItemsControl? _itemsControl;

        /// <summary>
        /// Gets the <see cref="ItemContainerGenerator"/> for this <see cref="VirtualizingPanel"/>.
        /// </summary>
        public ItemContainerGenerator? ItemContainerGenerator => _itemsControl?.ItemContainerGenerator;

        /// <summary>
        /// Gets the items to display.
        /// </summary>
        protected IReadOnlyList<object?> Items => (IReadOnlyList<object?>?)ItemsControl?.ItemsView ?? 
            Array.Empty<object?>();

        /// <summary>
        /// Gets the <see cref="ItemsControl"/> that the panel is displaying items for.
        /// </summary>
        protected ItemsControl? ItemsControl 
        {
            get => _itemsControl;
            private set
            {
                if (_itemsControl != value)
                {
                    var oldValue = _itemsControl;
                    _itemsControl= value;
                    OnItemsControlChanged(oldValue);
                }
            }
        }

        IInputElement? INavigableContainer.GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
        {
            return GetControl(direction, from, wrap);
        }

        /// <summary>
        /// Scrolls the specified item into view.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>
        /// The element with the specified index, or null if the element could not be brought into view.
        /// </returns>
        protected internal abstract Control? ScrollIntoView(int index);

        /// <summary>
        /// Returns the container for the item at the specified index.
        /// </summary>
        /// <param name="index">The index of the item to retrieve.</param>
        /// <returns>
        /// The container for the item at the specified index within the item collection, if the
        /// item is realized; otherwise, null.
        /// </returns>
        /// <remarks>
        /// Note for implementors: if the item at the specified index is an ItemIsOwnContainer
        /// item that has previously been realized, then the item should be returned even if it
        /// currently falls outside the realized viewport.
        /// </remarks>
        protected internal abstract Control? ContainerFromIndex(int index);

        /// <summary>
        /// Returns the index to the item that has the specified realized container.
        /// </summary>
        /// <param name="container">The generated container to retrieve the item index for.</param>
        /// <returns>
        /// The index to the item that corresponds to the specified realized container, or -1 if 
        /// <paramref name="container"/> is not found.
        /// </returns>
        protected internal abstract int IndexFromContainer(Control container);

        /// <summary>
        /// Gets the currently realized containers.
        /// </summary>
        protected internal abstract IEnumerable<Control>? GetRealizedContainers();

        /// <summary>
        /// Gets the next control in the specified direction.
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        /// <param name="from">The control from which movement begins.</param>
        /// <param name="wrap">Whether to wrap around when the first or last item is reached.</param>
        /// <returns>The control.</returns>
        protected abstract IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap);

        /// <summary>
        /// Called when the <see cref="ItemsControl"/> that owns the panel changes.
        /// </summary>
        /// <param name="oldValue">
        /// The old value of the <see cref="ItemsControl"/> property.
        /// </param>
        protected virtual void OnItemsControlChanged(ItemsControl? oldValue)
        {
        }

        /// <summary>
        /// Called when the <see cref="ItemsControl.Items"/> collection of the owner
        /// <see cref="ItemsControl"/> changes.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// This method is called a <see cref="INotifyCollectionChanged"/> event is raised by
        /// the items, or when the <see cref="ItemsControl.Items"/> property is assigned a
        /// new collection, in which case the <see cref="NotifyCollectionChangedAction"/> will
        /// be <see cref="NotifyCollectionChangedAction.Reset"/>.
        /// </remarks>
        protected virtual void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
        {
        }

        /// <summary>
        /// Adds the specified <see cref="Control"/> to the <see cref="Panel.Children"/> collection
        /// of a <see cref="VirtualizingPanel"/> element.
        /// </summary>
        /// <param name="control">The control to add to the collection.</param>
        protected void AddInternalChild(Control control)
        {
            var itemsControl = EnsureItemsControl();
            itemsControl.AddLogicalChild(control);
            Children.Add(control);
        }

        /// <summary>
        /// Adds the specified <see cref="Control"/> to the <see cref="Panel.Children"/> collection
        /// of a <see cref="VirtualizingPanel"/> element at the specified index position.
        /// </summary>
        /// <param name="index">
        /// The index position within the collection at which the child element is inserted.
        /// </param>
        /// <param name="control">The control to add to the collection.</param>
        protected void InsertInternalChild(int index, Control control)
        {
            var itemsControl = EnsureItemsControl();
            itemsControl.AddLogicalChild(control);
            Children.Insert(index, control);
        }

        /// <summary>
        /// Removes a child element from the <see cref="Panel.Children"/> collection.
        /// </summary>
        /// <param name="child">The child to remove/</param>
        protected void RemoveInternalChild(Control child)
        {
            var itemsControl = EnsureItemsControl();
            itemsControl.RemoveLogicalChild(child);
            Children.Remove(child);
        }

        /// <summary>
        /// Removes child elements from the <see cref="Panel.Children"/> collection.
        /// </summary>
        /// <param name="index">
        /// The beginning index position within the collection at which the first child element is
        /// removed.
        /// </param>
        /// <param name="count">The number of child elements to remove.</param>
        protected void RemoveInternalChildRange(int index, int count)
        {
            var itemsControl = EnsureItemsControl();
            
            for (var i = index; i < count; ++i)
            {
                var c = Children[i];
                itemsControl.RemoveLogicalChild(c);
            }

            Children.RemoveRange(index, count);
        }

        /// <summary>
        /// Gets the index of a content control item contained within this panel that matches the given text search
        /// </summary>
        /// <param name="textSearchTerm">The beginning of a string (case-insensitive) to search for in the panel</param>
        /// <returns>The index of the first IContentControl item contained in the panel</returns>
        protected internal int GetIndexFromTextSearch(string textSearchTerm)
        {
            var matchingControl = Items.FirstOrDefault(i => i is Control c && ItemsPresenter.ControlMatchesTextSearch(c, textSearchTerm));
            if (matchingControl != null)
            {
                return Items.IndexOf(matchingControl);
            }

            var matchingObject = Items.FirstOrDefault(i =>
                i?.ToString()?.StartsWith(textSearchTerm, StringComparison.OrdinalIgnoreCase) == true);
            return matchingObject != null ? Items.IndexOf(matchingObject) : -1;
        }

        private protected override void InvalidateMeasureOnChildrenChanged()
        {
            // Don't invalidate measure when children are added or removed: the panel is responsible
            // for managing its children.
        }

        internal void Attach(ItemsControl itemsControl)
        {
            if (ItemsControl is not null)
                throw new InvalidOperationException("The VirtualizingPanel is already attached to an ItemsControl");

            ItemsControl = itemsControl;
            ItemsControl.ItemsView.PostCollectionChanged += OnItemsControlItemsChanged;
        }

        internal void Detach()
        {
            var itemsControl = EnsureItemsControl();
            itemsControl.ItemsView.PostCollectionChanged -= OnItemsControlItemsChanged;
            ItemsControl = null;
            Children.Clear();
        }

        internal void Refresh() => OnItemsControlItemsChanged(null, CollectionUtils.ResetEventArgs);

        private ItemsControl EnsureItemsControl()
        {
            if (ItemsControl is null)
                ThrowNotAttached();
            return ItemsControl;
        }

        private void OnItemsControlItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnItemsChanged(Items, e);
        }

        [DoesNotReturn]
        private static void ThrowNotAttached()
        {
            throw new InvalidOperationException("The VirtualizingPanel does not belong to an ItemsControl.");
        }
    }
}
