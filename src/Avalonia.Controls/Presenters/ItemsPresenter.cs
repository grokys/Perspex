using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Presents items inside an <see cref="Avalonia.Controls.ItemsControl"/>.
    /// </summary>
    public class ItemsPresenter : Control, ILogicalScrollable
    {
        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<Panel?>> ItemsPanelProperty =
            ItemsControl.ItemsPanelProperty.AddOwner<ItemsPresenter>();

        private PanelContainerGenerator? _generator;
        private ILogicalScrollable? _logicalScrollable;
        private EventHandler? _scrollInvalidated;

        event EventHandler? ILogicalScrollable.ScrollInvalidated
        {
            add => _scrollInvalidated += value;
            remove => _scrollInvalidated -= value;
        }

        /// <summary>
        /// Gets or sets a template which creates the <see cref="Panel"/> used to display the items.
        /// </summary>
        public ITemplate<Panel?> ItemsPanel
        {
            get => GetValue(ItemsPanelProperty);
            set => SetValue(ItemsPanelProperty, value);
        }

        /// <summary>
        /// Gets the panel used to display the items.
        /// </summary>
        public Panel? Panel { get; private set; }

        /// <summary>
        /// Gets the owner <see cref="ItemsControl"/>.
        /// </summary>
        internal ItemsControl? ItemsControl { get; private set; }
        
        bool ILogicalScrollable.CanHorizontallyScroll 
        {
            get => _logicalScrollable?.CanHorizontallyScroll ?? false;
            set
            {
                if (_logicalScrollable is not null)
                    _logicalScrollable.CanHorizontallyScroll = value;
            }
        }

        bool ILogicalScrollable.CanVerticallyScroll 
        {
            get => _logicalScrollable?.CanVerticallyScroll ?? false;
            set
            {
                if (_logicalScrollable is not null)
                    _logicalScrollable.CanVerticallyScroll = value;
            }
        }

        Vector IScrollable.Offset 
        {
            get => _logicalScrollable?.Offset ?? default;
            set
            {
                if (_logicalScrollable is not null)
                    _logicalScrollable.Offset = value;
            }
        }

        bool ILogicalScrollable.IsLogicalScrollEnabled => _logicalScrollable?.IsLogicalScrollEnabled ?? false;
        Size ILogicalScrollable.ScrollSize => _logicalScrollable?.ScrollSize ?? default;
        Size ILogicalScrollable.PageScrollSize => _logicalScrollable?.PageScrollSize ?? default;
        Size IScrollable.Extent => _logicalScrollable?.Extent ?? default;
        Size IScrollable.Viewport => _logicalScrollable?.Viewport ?? default;


        public override sealed void ApplyTemplate()
        {
            if (Panel is null && ItemsControl is not null)
            {
                if (_logicalScrollable is not null)
                {
                    _logicalScrollable.ScrollInvalidated -= OnLogicalScrollInvalidated;
                }

                Panel = ItemsPanel.Build();

                if (Panel is null)
                {
                    return;
                }

                Panel.TemplatedParent = TemplatedParent;
                Panel.IsItemsHost = true;
                LogicalChildren.Add(Panel);
                VisualChildren.Add(Panel);

                if (Panel is VirtualizingPanel v)
                    v.Attach(ItemsControl);
                else
                    CreateSimplePanelGenerator();

                _logicalScrollable = Panel as ILogicalScrollable;

                if (_logicalScrollable is not null)
                {
                    _logicalScrollable.ScrollInvalidated += OnLogicalScrollInvalidated;
                }
            }
        }

        bool ILogicalScrollable.BringIntoView(Control target, Rect targetRect) =>
            _logicalScrollable?.BringIntoView(target, targetRect) ?? false;
        Control? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control? from) =>
            _logicalScrollable?.GetControlInDirection(direction, from);
        void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e) => _scrollInvalidated?.Invoke(this, e);

        internal void ScrollIntoView(int index)
        {
            if (Panel is VirtualizingPanel v)
                v.ScrollIntoView(index);
            else if (index >= 0 && index < Panel?.Children.Count)
                Panel.Children[index].BringIntoView();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TemplatedParentProperty)
            {
                ResetState();
                ItemsControl = null;

                if (change.NewValue is ItemsControl itemsControl)
                {
                    ItemsControl = itemsControl;
                    ItemsControl.RegisterItemsPresenter(this);
                }
            }
            else if (change.Property == ItemsPanelProperty)
            {
                ResetState();
                InvalidateMeasure();
            }
        }

        internal void Refresh()
        {
            if (Panel is VirtualizingPanel v)
                v.Refresh();
            else
                _generator?.Refresh();
        }

        private void ResetState()
        {
            _generator?.Dispose();
            _generator = null;
            LogicalChildren.Clear();
            VisualChildren.Clear();
            (Panel as VirtualizingPanel)?.Detach();
            Panel = null;
        }

        private void CreateSimplePanelGenerator()
        {
            Debug.Assert(Panel is not VirtualizingPanel);

            if (ItemsControl is null || Panel is null)
                return;

            _generator?.Dispose();
            _generator = new(this);
        }

        internal Control? ContainerFromIndex(int index)
        {
            if (Panel is VirtualizingPanel v)
                return v.ContainerFromIndex(index);
            return index >= 0 && index < Panel?.Children.Count ? Panel.Children[index] : null;
        }

        internal IEnumerable<Control>? GetRealizedContainers()
        {
            if (Panel is VirtualizingPanel v)
                return v.GetRealizedContainers();
            return Panel?.Children;
        }
        
        internal static bool ControlMatchesTextSearch(Control control, string textSearchTerm)
        {
            if (control is AvaloniaObject ao && ao.IsSet(TextSearch.TextProperty))
            {
                var searchText = ao.GetValue(TextSearch.TextProperty);
                
                if (searchText?.StartsWith(textSearchTerm, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }
            }
            return control is IContentControl cc && 
                   cc.Content?.ToString()?.StartsWith(textSearchTerm, StringComparison.OrdinalIgnoreCase) == true;
        }
        
        internal int GetIndexFromTextSearch(string textSearch)
        {
            if (Panel is VirtualizingPanel v)
                return v.GetIndexFromTextSearch(textSearch);
            
            var matchingControl = Panel?.Children.FirstOrDefault(c => ControlMatchesTextSearch(c, textSearch));
            return matchingControl != null ? Panel!.Children.IndexOf(matchingControl) : -1;
        }

        internal int IndexFromContainer(Control container)
        {
            if (Panel is VirtualizingPanel v)
                return v.IndexFromContainer(container);
            return Panel?.Children.IndexOf(container) ?? -1;
        }

        private void OnLogicalScrollInvalidated(object? sender, EventArgs e) => _scrollInvalidated?.Invoke(this, e);
    }
}
