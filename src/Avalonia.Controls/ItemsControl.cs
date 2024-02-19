using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Displays a collection of items.
    /// </summary>
    [PseudoClasses(":empty", ":singleitem")]
    public class ItemsControl : TemplatedControl, IChildIndexProvider
    {
        /// <summary>
        /// The default value for the <see cref="ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<Panel?> DefaultPanel =
            new(() => new StackPanel());

        /// <summary>
        /// Defines the <see cref="ItemContainerTheme"/> property.
        /// </summary>
        public static readonly StyledProperty<ControlTheme?> ItemContainerThemeProperty =
            AvaloniaProperty.Register<ItemsControl, ControlTheme?>(nameof(ItemContainerTheme));

        /// <summary>
        /// Defines the <see cref="ItemCount"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsControl, int> ItemCountProperty =
            AvaloniaProperty.RegisterDirect<ItemsControl, int>(nameof(ItemCount), o => o.ItemCount);

        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<Panel?>> ItemsPanelProperty =
            AvaloniaProperty.Register<ItemsControl, ITemplate<Panel?>>(nameof(ItemsPanel), DefaultPanel);

        /// <summary>
        /// Defines the <see cref="ItemsSource"/> property.
        /// </summary>
        public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
            AvaloniaProperty.Register<ItemsControl, IEnumerable?>(nameof(ItemsSource));

        /// <summary>
        /// Defines the <see cref="ItemTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
            AvaloniaProperty.Register<ItemsControl, IDataTemplate?>(nameof(ItemTemplate));

        /// <summary>
        /// Defines the <see cref="DisplayMemberBinding" /> property
        /// </summary>
        public static readonly StyledProperty<IBinding?> DisplayMemberBindingProperty =
            AvaloniaProperty.Register<ItemsControl, IBinding?>(nameof(DisplayMemberBinding));

        private static readonly AttachedProperty<ControlTheme?> AppliedItemContainerTheme =
            AvaloniaProperty.RegisterAttached<ItemsControl, Control, ControlTheme?>("AppliedItemContainerTheme");

        /// <summary>
        /// Gets or sets the <see cref="IBinding"/> to use for binding to the display member of each item.
        /// </summary>
        [AssignBinding]
        [InheritDataTypeFromItems(nameof(ItemsSource))]
        public IBinding? DisplayMemberBinding
        {
            get => GetValue(DisplayMemberBindingProperty);
            set => SetValue(DisplayMemberBindingProperty, value);
        }

        /// <summary>
        /// Gets a list of the <see cref="ItemFilter"/> objects owned by the <see cref="ItemsSourceView"/> of this <see cref="ItemsControl"/>.
        /// </summary>
        /// <remarks>
        /// This property is provided as a XAML shortcut to <c>ItemsView.Filters</c>.
        /// </remarks>
        public AvaloniaList<ItemFilter> Filters => ItemsView.Filters;

        /// <summary>
        /// Gets a list of the <see cref="ItemSorter"/> objects owned by the <see cref="ItemsSourceView"/> of this <see cref="ItemsControl"/>.
        /// </summary>
        /// <remarks>
        /// This property is provided as a XAML shortcut to <c>ItemsView.Sorters</c>.
        /// </remarks>
        public AvaloniaList<ItemSorter> Sorters => ItemsView.Sorters;

        private readonly ItemCollection _items;
        private int _itemCount;
        private ItemContainerGenerator? _itemContainerGenerator;
        private EventHandler<ChildIndexChangedEventArgs>? _childIndexChanged;
        private IDataTemplate? _displayMemberItemTemplate;
        private ItemsPresenter? _itemsPresenter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsControl"/> class.
        /// </summary>
        public ItemsControl()
        {
            _items = new(this);
            UpdatePseudoClasses();
            _items.CollectionChanged += OnItemsViewCollectionChanged;
        }

        /// <summary>
        /// Gets the <see cref="ItemContainerGenerator"/> for the control.
        /// </summary>
        public ItemContainerGenerator ItemContainerGenerator
        {
#pragma warning disable CS0612 // Type or member is obsolete
            get => _itemContainerGenerator ??= CreateItemContainerGenerator();
#pragma warning restore CS0612 // Type or member is obsolete
        }

        /// <summary>
        /// Gets the items to display.
        /// </summary>
        /// <remarks>
        /// You use either the <see cref="Items"/> or the <see cref="ItemsSource"/> property to
        /// specify the collection that should be used to generate the content of your
        /// <see cref="ItemsControl"/>. When the <see cref="ItemsSource"/> property is set, the
        /// <see cref="Items"/> collection is made read-only and fixed-size.
        ///
        /// When <see cref="ItemsSource"/> is in use, setting the <see cref="ItemsSource"/>
        /// property to null removes the collection and restores usage to <see cref="Items"/>,
        /// which will be an empty <see cref="ItemCollection"/>.
        /// </remarks>
        [Content]
        public ItemCollection Items => _items;

        /// <summary>
        /// Gets or sets the <see cref="ControlTheme"/> that is applied to the container element generated for each item.
        /// </summary>
        public ControlTheme? ItemContainerTheme
        {
            get => GetValue(ItemContainerThemeProperty);
            set => SetValue(ItemContainerThemeProperty, value);
        }

        /// <summary>
        /// Gets the number of items being displayed by the <see cref="ItemsControl"/>.
        /// </summary>
        public int ItemCount
        {
            get => _itemCount;
            private set
            {
                if (SetAndRaise(ItemCountProperty, ref _itemCount, value))
                {
                    UpdatePseudoClasses();
                    _childIndexChanged?.Invoke(this, ChildIndexChangedEventArgs.TotalCountChanged);
                }
            }
        }

        /// <summary>
        /// Gets or sets the panel used to display the items.
        /// </summary>
        public ITemplate<Panel?> ItemsPanel
        {
            get => GetValue(ItemsPanelProperty);
            set => SetValue(ItemsPanelProperty, value);
        }

        /// <summary>
        /// Gets or sets a collection used to generate the content of the <see cref="ItemsControl"/>.
        /// </summary>
        /// <remarks>
        /// A common scenario is to use an <see cref="ItemsControl"/> such as a 
        /// <see cref="ListBox"/> to display a data collection, or to bind an
        /// <see cref="ItemsControl"/> to a collection object. To bind an <see cref="ItemsControl"/>
        /// to a collection object, use the <see cref="ItemsSource"/> property.
        /// 
        /// When the <see cref="ItemsSource"/> property is set, the <see cref="Items"/> collection
        /// is made read-only and fixed-size.
        ///
        /// When <see cref="ItemsSource"/> is in use, setting the property to null removes the
        /// collection and restores usage to <see cref="Items"/>, which will be an empty 
        /// <see cref="ItemCollection"/>.
        /// </remarks>
        public IEnumerable? ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to display the items in the control.
        /// </summary>
        [InheritDataTypeFromItems(nameof(ItemsSource))]
        public IDataTemplate? ItemTemplate
        {
            get => GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        /// <summary>
        /// Gets the items presenter control.
        /// </summary>
        public ItemsPresenter? Presenter { get; private set; }

        /// <summary>
        /// Gets the <see cref="Panel"/> specified by <see cref="ItemsPanel"/>.
        /// </summary>
        public Panel? ItemsPanelRoot => Presenter?.Panel;

        /// <summary>
        /// Gets a read-only view of the items in the <see cref="ItemsControl"/>.
        /// </summary>
        public ItemsSourceView ItemsView => _items;

        private protected bool WrapFocus { get; set; }

        event EventHandler<ChildIndexChangedEventArgs>? IChildIndexProvider.ChildIndexChanged
        {
            add => _childIndexChanged += value;
            remove => _childIndexChanged -= value;
        }

        /// <summary>
        /// Occurs each time a container is prepared for use.
        /// </summary>
        /// <remarks>
        /// The prepared element might be newly created or an existing container that is being re-
        /// used.
        /// </remarks>
        public event EventHandler<ContainerPreparedEventArgs>? ContainerPrepared;

        /// <summary>
        /// Occurs for each realized container when the index for the item it represents has changed.
        /// </summary>
        /// <remarks>
        /// This event is raised for each realized container where the index for the item it
        /// represents has changed. For example, when another item is added or removed in the data
        /// source, the index for items that come after in the ordering will be impacted.
        /// </remarks>
        public event EventHandler<ContainerIndexChangedEventArgs>? ContainerIndexChanged;

        /// <summary>
        /// Occurs each time a container is cleared.
        /// </summary>
        /// <remarks>
        /// This event is raised immediately each time an container is cleared, such as when it
        /// falls outside the range of realized items or the corresponding item is removed.
        /// </remarks>
        public event EventHandler<ContainerClearingEventArgs>? ContainerClearing;

        /// <summary>
        /// Gets a default recycle key that can be used when an <see cref="ItemsControl"/> supports
        /// a single container type.
        /// </summary>
        protected static object DefaultRecycleKey { get; } = new object();

        /// <summary>
        /// Returns the container for the item at the specified index.
        /// </summary>
        /// <param name="index">The index of the item to retrieve.</param>
        /// <returns>
        /// The container for the item at the specified index within the item collection, if the
        /// item has a container; otherwise, null.
        /// </returns>
        public Control? ContainerFromIndex(int index) => Presenter?.ContainerFromIndex(index);

        /// <summary>
        /// Returns the container corresponding to the specified item.
        /// </summary>
        /// <param name="item">The item to retrieve the container for.</param>
        /// <returns>
        /// A container that corresponds to the specified item, if the item has a container and
        /// exists in the collection; otherwise, null.
        /// </returns>
        public Control? ContainerFromItem(object item)
        {
            var index = _items.IndexOf(item);
            return index >= 0 ? ContainerFromIndex(index) : null;
        }

        /// <summary>
        /// Returns the index to the item that has the specified, generated container.
        /// </summary>
        /// <param name="container">The generated container to retrieve the item index for.</param>
        /// <returns>
        /// The index to the item that corresponds to the specified generated container, or -1 if 
        /// <paramref name="container"/> is not found.
        /// </returns>
        public int IndexFromContainer(Control container) => Presenter?.IndexFromContainer(container) ?? -1;

        /// <summary>
        /// Returns the item that corresponds to the specified, generated container.
        /// </summary>
        /// <param name="container">The control that corresponds to the item to be returned.</param>
        /// <returns>
        /// The contained item, or the container if it does not contain an item.
        /// </returns>
        public object? ItemFromContainer(Control container)
        {
            var index = IndexFromContainer(container);
            return index >= 0 && index < _items.Count ? _items[index] : null;
        }

        /// <summary>
        /// Gets the currently realized containers.
        /// </summary>
        public IEnumerable<Control> GetRealizedContainers() => Presenter?.GetRealizedContainers() ?? Array.Empty<Control>();

        /// <summary>
        /// Scrolls the specified item into view.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        public void ScrollIntoView(int index) => Presenter?.ScrollIntoView(index);

        /// <summary>
        /// Scrolls the specified item into view.
        /// </summary>
        /// <param name="item">The item.</param>
        public void ScrollIntoView(object item) => ScrollIntoView(ItemsView.IndexOf(item));

        /// <summary>
        /// Returns the <see cref="ItemsControl"/> that owns the specified container control.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>
        /// The owning <see cref="ItemsControl"/> or null if the control is not an items container.
        /// </returns>
        [Obsolete("Typo, use ItemsControlFromItemContainer instead")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        public static ItemsControl? ItemsControlFromItemContaner(Control container) =>
            ItemsControlFromItemContainer(container);

        /// <summary>
        /// Returns the <see cref="ItemsControl"/> that owns the specified container control.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>
        /// The owning <see cref="ItemsControl"/> or null if the control is not an items container.
        /// </returns>
        public static ItemsControl? ItemsControlFromItemContainer(Control container)
        {
            var c = container.Parent as Control;

            while (c is not null)
            {
                if (c is ItemsControl itemsControl)
                {
                    return itemsControl.IndexFromContainer(container) >= 0 ? itemsControl : null;
                }

                c = c.Parent as Control;
            }

            return null;
        }

        /// <summary>
        /// Creates or a container that can be used to display an item.
        /// </summary>
        protected internal virtual Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return new ContentPresenter();
        }

        /// <summary>
        /// Prepares the specified element to display the specified item.
        /// </summary>
        /// <param name="container">The element that's used to display the specified item.</param>
        /// <param name="item">The item to display.</param>
        /// <param name="index">The index of the item to display.</param>
        protected internal virtual void PrepareContainerForItemOverride(Control container, object? item, int index)
        {
            if (container == item)
                return;

            var itemTemplate = GetEffectiveItemTemplate();

            if (container is HeaderedContentControl hcc)
            {
                SetIfUnset(hcc, HeaderedContentControl.ContentProperty, item);

                if (item is IHeadered headered)
                    SetIfUnset(hcc, HeaderedContentControl.HeaderProperty, headered.Header);
                else if (item is not Visual)
                    SetIfUnset(hcc, HeaderedContentControl.HeaderProperty, item);

                if (itemTemplate is not null)
                    SetIfUnset(hcc, HeaderedContentControl.HeaderTemplateProperty, itemTemplate);
            }
            else if (container is ContentControl cc)
            {
                SetIfUnset(cc, ContentControl.ContentProperty, item);
                if (itemTemplate is not null)
                    SetIfUnset(cc, ContentControl.ContentTemplateProperty, itemTemplate);
            }
            else if (container is ContentPresenter p)
            {
                SetIfUnset(p, ContentPresenter.ContentProperty, item);
                if (itemTemplate is not null)
                    SetIfUnset(p, ContentPresenter.ContentTemplateProperty, itemTemplate);
            }
            else if (container is ItemsControl ic)
            {
                if (itemTemplate is not null)
                    SetIfUnset(ic, ItemTemplateProperty, itemTemplate);
                if (ItemContainerTheme is { } ict)
                    SetIfUnset(ic, ItemContainerThemeProperty, ict);
            }

            // These conditions are separate because HeaderedItemsControl and
            // HeaderedSelectingItemsControl also need to run the ItemsControl preparation.
            if (container is HeaderedItemsControl hic)
            {
                SetIfUnset(hic, HeaderedItemsControl.HeaderProperty, item);
                SetIfUnset(hic, HeaderedItemsControl.HeaderTemplateProperty, itemTemplate);
                hic.PrepareItemContainer(this);
            }
            else if (container is HeaderedSelectingItemsControl hsic)
            {
                SetIfUnset(hsic, HeaderedSelectingItemsControl.HeaderProperty, item);
                SetIfUnset(hsic, HeaderedSelectingItemsControl.HeaderTemplateProperty, itemTemplate);
                hsic.PrepareItemContainer(this);
            }
        }

        /// <summary>
        /// Called when a container has been fully prepared to display an item.
        /// </summary>
        /// <param name="container">The container control.</param>
        /// <param name="item">The item being displayed.</param>
        /// <param name="index">The index of the item being displayed.</param>
        /// <remarks>
        /// This method will be called when a container has been fully prepared and added to the
        /// logical and visual trees, but may be called before a layout pass has completed. It is
        /// called immediately before the <see cref="ContainerPrepared"/> event is raised.
        /// </remarks>
        protected internal virtual void ContainerForItemPreparedOverride(Control container, object? item, int index)
        {
        }

        /// <summary>
        /// Called when the index for a container changes due to an insertion or removal in the
        /// items collection.
        /// </summary>
        /// <param name="container">The container whose index changed.</param>
        /// <param name="oldIndex">The old index.</param>
        /// <param name="newIndex">The new index.</param>
        protected virtual void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
        {
        }

        /// <summary>
        /// Undoes the effects of the <see cref="PrepareContainerForItemOverride(Control, object?, int)"/> method.
        /// </summary>
        /// <param name="container">The container element.</param>
        protected internal virtual void ClearContainerForItemOverride(Control container)
        {
            if (container is HeaderedContentControl hcc)
            {
                hcc.ClearValue(HeaderedContentControl.ContentProperty);
                hcc.ClearValue(HeaderedContentControl.HeaderProperty);
                hcc.ClearValue(HeaderedContentControl.HeaderTemplateProperty);
            }
            else if (container is ContentControl cc)
            {
                cc.ClearValue(ContentControl.ContentProperty);
                cc.ClearValue(ContentControl.ContentTemplateProperty);
            }
            else if (container is ContentPresenter p)
            {
                p.ClearValue(ContentPresenter.ContentProperty);
                p.ClearValue(ContentPresenter.ContentTemplateProperty);
            }
            else if (container is ItemsControl ic)
            {
                ic.ClearValue(ItemTemplateProperty);
                ic.ClearValue(ItemContainerThemeProperty);
            }
            
            if (container is HeaderedItemsControl hic)
            {
                hic.ClearValue(HeaderedItemsControl.HeaderProperty);
                hic.ClearValue(HeaderedItemsControl.HeaderTemplateProperty);
            }
            else if (container is HeaderedSelectingItemsControl hsic)
            {
                hsic.ClearValue(HeaderedSelectingItemsControl.HeaderProperty);
                hsic.ClearValue(HeaderedSelectingItemsControl.HeaderTemplateProperty);
            }

            // Feels like we should be clearing the HeaderedItemsControl.Items binding here, but looking at
            // the WPF source it seems that this isn't done there.
        }

        /// <summary>
        /// Determines whether the specified item can be its own container.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="recycleKey">
        /// When the method returns, contains a key that can be used to locate a previously
        /// recycled container of the correct type, or null if the item cannot be recycled.
        /// If the item is its own container then by definition it cannot be recycled, so
        /// <paramref name="recycleKey"/> shoud be set to null.
        /// </param>
        /// <returns>
        /// true if the item needs a container; otherwise false if the item can itself be used
        /// as a container.
        /// </returns>
        protected internal virtual bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            return NeedsContainer<Control>(item, out recycleKey);
        }

        /// <summary>
        /// A default implementation of <see cref="NeedsContainerOverride(object, int, out object?)"/>
        /// that returns true and sets the recycle key to <see cref="DefaultRecycleKey"/> if the item
        /// is not a <typeparamref name="T"/> .
        /// </summary>
        /// <typeparam name="T">The container type.</typeparam>
        /// <param name="item">The item.</param>
        /// <param name="recycleKey">
        /// When the method returns, contains <see cref="DefaultRecycleKey"/> if
        /// <paramref name="item"/> is not of type <typeparamref name="T"/>; otherwise null.
        /// </param>
        /// <returns>
        /// true if <paramref name="item"/> is of type <typeparamref name="T"/>; otherwise false.
        /// </returns>
        protected bool NeedsContainer<T>(object? item, out object? recycleKey) where T : Control
        {
            if (item is T)
            {
                recycleKey = null;
                return false;
            }
            else
            {
                recycleKey = DefaultRecycleKey;
                return true;
            }
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _itemsPresenter = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            // If the focus is coming from a child control, set the tab once active element to
            // the focused control. This ensures that tabbing back into the control will focus
            // the last focused control when TabNavigationMode == Once.
            if (e.Source != this && e.Source is IInputElement ie)
                KeyboardNavigation.SetTabOnceActiveElement(this, ie);
        }

        /// <summary>
        /// Handles directional navigation within the <see cref="ItemsControl"/>.
        /// </summary>
        /// <param name="e">The key events.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled)
            {
                var focus = FocusManager.GetFocusManager(this);
                var direction = e.Key.ToNavigationDirection();
                var container = Presenter?.Panel as INavigableContainer;

                if (focus == null ||
                    container == null ||
                    focus.GetFocusedElement() == null ||
                    direction == null ||
                    direction.Value.IsTab())
                {
                    return;
                }

                Visual? current = focus.GetFocusedElement() as Visual;

                while (current != null)
                {
                    if (current.VisualParent == container && current is IInputElement inputElement)
                    {
                        var next = GetNextControl(container, direction.Value, inputElement, WrapFocus);

                        if (next != null)
                        {
                            next.Focus(NavigationMethod.Directional, e.KeyModifiers);
                            e.Handled = true;
                        }

                        break;
                    }

                    current = current.VisualParent;
                }
            }

            base.OnKeyDown(e);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ItemsControlAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ItemContainerThemeProperty && _itemContainerGenerator is not null)
            {
                RefreshContainers();
            }
            else if (change.Property == ItemsSourceProperty)
            {
                _items.SetItemsSource(change.GetNewValue<IEnumerable?>());
            }
            else if (change.Property == ItemTemplateProperty)
            {
                if (change.NewValue is not null && DisplayMemberBinding is not null)
                    throw new InvalidOperationException("Cannot set both DisplayMemberBinding and ItemTemplate.");
                RefreshContainers();
            }
            else if (change.Property == DisplayMemberBindingProperty)
            {
                if (change.NewValue is not null && ItemTemplate is not null)
                    throw new InvalidOperationException("Cannot set both DisplayMemberBinding and ItemTemplate.");
                _displayMemberItemTemplate = null;
                RefreshContainers();
            }
        }

        /// <summary>
        /// Refreshes the containers displayed by the control.
        /// </summary>
        /// <remarks>
        /// Causes all containers to be unrealized and re-realized.
        /// </remarks>
        protected void RefreshContainers() => Presenter?.Refresh();

        /// <summary>
        /// Called when the <see cref="INotifyCollectionChanged.CollectionChanged"/> event is
        /// raised on <see cref="ItemsView"/>.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private protected virtual void OnItemsViewCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_items.IsReadOnly)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        AddControlItemsToLogicalChildren(e.NewItems);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        RemoveControlItemsFromLogicalChildren(e.OldItems);
                        break;
                }
            }

            ItemCount = ItemsView.Count;
        }

        /// <summary>
        /// Creates the <see cref="ItemContainerGenerator"/>
        /// </summary>
        /// <remarks>
        /// This method is only present for backwards compatibility with 0.10.x in order for
        /// TreeView to be able to create a <see cref="TreeItemContainerGenerator"/>. Can be
        /// removed in 12.0.
        /// </remarks>
        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        private protected virtual ItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator(this);
        }

        internal void AddLogicalChild(Control c)
        {
            if (!LogicalChildren.Contains(c))
                LogicalChildren.Add(c);
        }

        internal void RemoveLogicalChild(Control c) => LogicalChildren.Remove(c);

        /// <summary>
        /// Called by <see cref="ItemsPresenter"/> to register with the <see cref="ItemsControl"/>.
        /// </summary>
        /// <param name="presenter">The items presenter.</param>
        /// <remarks>
        /// ItemsPresenters can be within nested templates or in popups and so are not necessarily
        /// created immediately when the ItemsControl control's template is instantiated. Instead
        /// they register themselves using this method.
        /// </remarks>
        internal void RegisterItemsPresenter(ItemsPresenter presenter)
        {
            Presenter = presenter;
            _childIndexChanged?.Invoke(this, ChildIndexChangedEventArgs.ChildIndexesReset);
        }

        internal void PrepareItemContainer(Control container, object? item, int index)
        {
            // If the container has no theme set, or we've already applied our ItemContainerTheme
            // (and it hasn't changed since) then we're in control of the container's Theme and may
            // need to update it.
            if (!container.IsSet(ThemeProperty) || container.GetValue(AppliedItemContainerTheme) == container.Theme)
            {
                var itemContainerTheme = ItemContainerTheme;

                if (itemContainerTheme?.TargetType?.IsAssignableFrom(GetStyleKey(container)) == true)
                {
                    // We have an ItemContainerTheme and it matches the container. Set the Theme
                    // property, and mark the container as having had ItemContainerTheme applied.
                    container.SetCurrentValue(ThemeProperty, itemContainerTheme);
                    container.SetValue(AppliedItemContainerTheme, itemContainerTheme);
                }
                else
                {
                    // Otherwise clear the theme and the AppliedItemContainerTheme property.
                    container.ClearValue(ThemeProperty);
                    container.ClearValue(AppliedItemContainerTheme);
                }
            }

            if (item is not Control)
                container.DataContext = item;

            PrepareContainerForItemOverride(container, item, index);
        }

        internal void ItemContainerPrepared(Control container, object? item, int index)
        {
            ContainerForItemPreparedOverride(container, item, index);
            _childIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(container, index));
            ContainerPrepared?.Invoke(this, new(container, index));
        }

        internal void ItemContainerIndexChanged(Control container, int oldIndex, int newIndex)
        {
            ContainerIndexChangedOverride(container, oldIndex, newIndex);
            _childIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(container, newIndex));
            ContainerIndexChanged?.Invoke(this, new(container, oldIndex, newIndex));
        }

        internal void ClearItemContainer(Control container)
        {
            ClearContainerForItemOverride(container);
            ContainerClearing?.Invoke(this, new(container));
        }

        private void AddControlItemsToLogicalChildren(IEnumerable? items)
        {
            if (items is null)
                return;

            List<ILogical>? toAdd = null;

            foreach (var i in items)
            {
                if (i is Control control && !LogicalChildren.Contains(control))
                {
                    toAdd ??= new();
                    toAdd.Add(control);
                }
            }

            if (toAdd is not null)
                LogicalChildren.AddRange(toAdd);
        }

        private void SetIfUnset<T>(AvaloniaObject target, StyledProperty<T> property, T value)
        {
            if (!target.IsSet(property))
                target.SetCurrentValue(property, value);
        }

        private void RemoveControlItemsFromLogicalChildren(IEnumerable? items)
        {
            if (items is null)
                return;

            List<ILogical>? toRemove = null;

            foreach (var i in items)
            {
                if (i is Control control)
                {
                    toRemove ??= new();
                    toRemove.Add(control);
                }
            }

            if (toRemove is not null)
                LogicalChildren.RemoveAll(toRemove);
        }

        private IDataTemplate? GetEffectiveItemTemplate()
        {
            if (ItemTemplate is { } itemTemplate)
                return itemTemplate;

            if (_displayMemberItemTemplate is null && DisplayMemberBinding is { } binding)
            {
                _displayMemberItemTemplate = new FuncDataTemplate<object?>((_, _) =>
                    new TextBlock
                    {
                        [!TextBlock.TextProperty] = binding,
                    });
            }

            return _displayMemberItemTemplate;
        }

        private void UpdatePseudoClasses()
        {
            PseudoClasses.Set(":empty", ItemCount == 0);
            PseudoClasses.Set(":singleitem", ItemCount == 1);
        }

        protected static IInputElement? GetNextControl(
            INavigableContainer container,
            NavigationDirection direction,
            IInputElement? from,
            bool wrap)
        {
            var current = from;

            for (;;)
            {
                var result = container.GetControl(direction, current, wrap);

                if (result is null)
                {
                    return null;
                }

                if (result.Focusable &&
                    result.IsEffectivelyEnabled &&
                    result.IsEffectivelyVisible)
                {
                    return result;
                }

                current = result;

                if (current == from)
                {
                    return null;
                }

                switch (direction)
                {
                    //We did not find an enabled first item. Move downwards until we find one.
                    case NavigationDirection.First:
                        direction = NavigationDirection.Down;
                        from = result;
                        break;

                    //We did not find an enabled last item. Move upwards until we find one.
                    case NavigationDirection.Last:
                        direction = NavigationDirection.Up;
                        from = result;
                        break;

                }
            }
        }

        int IChildIndexProvider.GetChildIndex(ILogical child)
        {
            return child is Control container ? IndexFromContainer(container) : -1;
        }

        bool IChildIndexProvider.TryGetTotalCount(out int count)
        {
            count = ItemsView.Count;
            return true;
        }
    }
}
