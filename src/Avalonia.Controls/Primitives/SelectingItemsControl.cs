using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Controls.Selection;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Threading;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// An <see cref="ItemsControl"/> that maintains a selection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SelectingItemsControl"/> provides a base class for <see cref="ItemsControl"/>s
    /// that maintain a selection (single or multiple). By default only its 
    /// <see cref="SelectedIndex"/> and <see cref="SelectedItem"/> properties are visible; the
    /// current multiple <see cref="Selection"/> and <see cref="SelectedItems"/> together with the
    /// <see cref="SelectionMode"/> properties are protected, however a derived class can expose
    /// these if it wishes to support multiple selection.
    /// </para>
    /// <para>
    /// <see cref="SelectingItemsControl"/> maintains a selection respecting the current 
    /// <see cref="SelectionMode"/> but it does not react to user input; this must be handled in a
    /// derived class. It does, however, respond to <see cref="IsSelectedChangedEvent"/> events
    /// from items and updates the selection accordingly.
    /// </para>
    /// </remarks>
    public class SelectingItemsControl : ItemsControl
    {
        /// <summary>
        /// Defines the <see cref="AutoScrollToSelectedItem"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AutoScrollToSelectedItemProperty =
            AvaloniaProperty.Register<SelectingItemsControl, bool>(
                nameof(AutoScrollToSelectedItem),
                defaultValue: true);

        /// <summary>
        /// Defines the <see cref="SelectedIndex"/> property.
        /// </summary>
        public static readonly DirectProperty<SelectingItemsControl, int> SelectedIndexProperty =
            AvaloniaProperty.RegisterDirect<SelectingItemsControl, int>(
                nameof(SelectedIndex),
                o => o.SelectedIndex,
                (o, v) => o.SelectedIndex = v,
                unsetValue: -1,
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="SelectedItem"/> property.
        /// </summary>
        public static readonly DirectProperty<SelectingItemsControl, object?> SelectedItemProperty =
            AvaloniaProperty.RegisterDirect<SelectingItemsControl, object?>(
                nameof(SelectedItem),
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v,
                defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);

        /// <summary>
        /// Defines the <see cref="SelectedValue"/> property
        /// </summary>
        public static readonly StyledProperty<object?> SelectedValueProperty =
            AvaloniaProperty.Register<SelectingItemsControl, object?>(nameof(SelectedValue),
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="SelectedValueBinding"/> property
        /// </summary>
        public static readonly StyledProperty<IBinding?> SelectedValueBindingProperty =
            AvaloniaProperty.Register<SelectingItemsControl, IBinding?>(nameof(SelectedValueBinding));

        /// <summary>
        /// Defines the <see cref="SelectedItems"/> property.
        /// </summary>
        protected static readonly DirectProperty<SelectingItemsControl, IList?> SelectedItemsProperty =
            AvaloniaProperty.RegisterDirect<SelectingItemsControl, IList?>(
                nameof(SelectedItems),
                o => o.SelectedItems,
                (o, v) => o.SelectedItems = v);

        /// <summary>
        /// Defines the <see cref="Selection"/> property.
        /// </summary>
        protected static readonly DirectProperty<SelectingItemsControl, ISelectionModel> SelectionProperty =
            AvaloniaProperty.RegisterDirect<SelectingItemsControl, ISelectionModel>(
                nameof(Selection),
                o => o.Selection,
                (o, v) => o.Selection = v);

        /// <summary>
        /// Defines the <see cref="SelectionMode"/> property.
        /// </summary>
        protected static readonly StyledProperty<SelectionMode> SelectionModeProperty =
            AvaloniaProperty.Register<SelectingItemsControl, SelectionMode>(
                nameof(SelectionMode));

        /// <summary>
        /// Defines the IsSelected attached property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSelectedProperty =
            AvaloniaProperty.RegisterAttached<SelectingItemsControl, Control, bool>(
                "IsSelected",
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="IsTextSearchEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsTextSearchEnabledProperty =
            AvaloniaProperty.Register<SelectingItemsControl, bool>(nameof(IsTextSearchEnabled), false);

        /// <summary>
        /// Event that should be raised by containers when their selection state changes to notify
        /// the parent <see cref="SelectingItemsControl"/> that their selection state has changed.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> IsSelectedChangedEvent =
            RoutedEvent.Register<SelectingItemsControl, RoutedEventArgs>(
                "IsSelectedChanged",
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="SelectionChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<SelectionChangedEventArgs> SelectionChangedEvent =
            RoutedEvent.Register<SelectingItemsControl, SelectionChangedEventArgs>(
                nameof(SelectionChanged),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="WrapSelection"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> WrapSelectionProperty =
            AvaloniaProperty.Register<SelectingItemsControl, bool>(nameof(WrapSelection), defaultValue: false);
        private string _textSearchTerm = string.Empty;
        private DispatcherTimer? _textSearchTimer;
        private ISelectionModel? _selection;
        private int _oldSelectedIndex;
        private object? _oldSelectedItem;
        private IList? _oldSelectedItems;
        private bool _ignoreContainerSelectionChanged;
        private UpdateState? _updateState;
        private bool _hasScrolledToSelectedItem;
        private BindingHelper? _bindingHelper;
        private bool _isSelectionChangeActive;

        public SelectingItemsControl()
        {
            ((ItemCollection)ItemsView).SourceChanged += OnItemsViewSourceChanged;
        }

        /// <summary>
        /// Initializes static members of the <see cref="SelectingItemsControl"/> class.
        /// </summary>
        static SelectingItemsControl()
        {
            IsSelectedChangedEvent.AddClassHandler<SelectingItemsControl>((x, e) => x.ContainerSelectionChanged(e));
        }

        /// <summary>
        /// Occurs when the control's selection changes.
        /// </summary>
        public event EventHandler<SelectionChangedEventArgs>? SelectionChanged
        {
            add => AddHandler(SelectionChangedEvent, value);
            remove => RemoveHandler(SelectionChangedEvent, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically scroll to newly selected items.
        /// </summary>
        public bool AutoScrollToSelectedItem
        {
            get => GetValue(AutoScrollToSelectedItemProperty);
            set => SetValue(AutoScrollToSelectedItemProperty, value);
        }

        /// <summary>
        /// Gets or sets the index of the selected item.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                // When a Begin/EndInit/DataContext update is in place we return the value to be
                // updated here, even though it's not yet active and the property changed notification
                // has not yet been raised. If we don't do this then the old value will be written back
                // to the source when two-way bound, and the update value will be lost.
                if (_updateState is not null)
                {
                    return _updateState.SelectedIndex.HasValue ?
                        _updateState.SelectedIndex.Value :
                        TryGetExistingSelection()?.SelectedIndex ?? -1;
                }

                return Selection.SelectedIndex;
            }
            set
            {
                if (_updateState is object)
                {
                    _updateState.SelectedIndex = value;
                }
                else
                {
                    Selection.SelectedIndex = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        public object? SelectedItem
        {
            get
            {
                // See SelectedIndex getter for more information.
                if (_updateState is not null)
                {
                    return _updateState.SelectedItem.HasValue ?
                        _updateState.SelectedItem.Value :
                        TryGetExistingSelection()?.SelectedItem;
                }

                return Selection.SelectedItem;
            }
            set
            {
                if (_updateState is object)
                {
                    _updateState.SelectedItem = value;
                }
                else
                {
                    Selection.SelectedItem = value;
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="IBinding"/> instance used to obtain the 
        /// <see cref="SelectedValue"/> property
        /// </summary>
        [AssignBinding]
        [InheritDataTypeFromItems(nameof(ItemsSource))]
        public IBinding? SelectedValueBinding
        {
            get => GetValue(SelectedValueBindingProperty);
            set => SetValue(SelectedValueBindingProperty, value);
        }

        /// <summary>
        /// Gets or sets the value of the selected item, obtained using 
        /// <see cref="SelectedValueBinding"/>
        /// </summary>
        public object? SelectedValue
        {
            get => GetValue(SelectedValueProperty);
            set => SetValue(SelectedValueProperty, value);
        }

        /// <summary>
        /// Gets or sets the selected items.
        /// </summary>
        /// <remarks>
        /// By default returns a collection that can be modified in order to manipulate the control
        /// selection, however this property will return null if <see cref="Selection"/> is
        /// re-assigned; you should only use _either_ Selection or SelectedItems.
        /// </remarks>
        protected IList? SelectedItems
        {
            get
            {
                // See SelectedIndex setter for more information.
                if (_updateState?.SelectedItems.HasValue == true)
                {
                    return _updateState.SelectedItems.Value;
                }

                else if (Selection is InternalSelectionModel ism)
                {
                    var result = ism.WritableSelectedItems;
                    _oldSelectedItems = result;
                    return result;
                }

                return null;
            }
            set
            {
                if (_updateState is object)
                {
                    _updateState.SelectedItems = new Optional<IList?>(value);
                }
                else if (Selection is InternalSelectionModel i)
                {
                    i.WritableSelectedItems = value;
                }
                else
                {
                    throw new InvalidOperationException("Cannot set both Selection and SelectedItems.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the model that holds the current selection.
        /// </summary>
        [AllowNull]
        protected ISelectionModel Selection
        {
            get => _updateState?.Selection.HasValue == true ?
                    _updateState.Selection.Value :
                    GetOrCreateSelectionModel();
            set
            {
                value ??= CreateDefaultSelectionModel();

                if (_updateState is object)
                {
                    _updateState.Selection = new Optional<ISelectionModel>(value);
                }
                else if (_selection != value)
                {
                    if (value.Source != null && value.Source != ItemsView.Source)
                    {
                        throw new ArgumentException(
                            "The supplied ISelectionModel already has an assigned Source but this " +
                            "collection is different to the Items on the control.");
                    }

                    var oldSelection = _selection?.SelectedItems.ToArray();
                    DeinitializeSelectionModel(_selection);
                    _selection = value;

                    if (oldSelection?.Length > 0)
                    {
                        RaiseEvent(new SelectionChangedEventArgs(
                            SelectionChangedEvent,
                            oldSelection,
                            Array.Empty<object>()));
                    }

                    InitializeSelectionModel(_selection);

                    if (_oldSelectedItems != SelectedItems)
                    {
                        RaisePropertyChanged(SelectedItemsProperty, _oldSelectedItems, SelectedItems);
                        _oldSelectedItems = SelectedItems;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that specifies whether a user can jump to a value by typing.
        /// </summary>
        public bool IsTextSearchEnabled
        {
            get => GetValue(IsTextSearchEnabledProperty);
            set => SetValue(IsTextSearchEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets a value which indicates whether to wrap around when the first
        /// or last item is reached.
        /// </summary>
        public bool WrapSelection
        {
            get => GetValue(WrapSelectionProperty);
            set => SetValue(WrapSelectionProperty, value);
        }

        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        /// <remarks>
        /// Note that the selection mode only applies to selections made via user interaction.
        /// Multiple selections can be made programmatically regardless of the value of this property.
        /// </remarks>
        protected SelectionMode SelectionMode
        {
            get => GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="SelectionMode.AlwaysSelected"/> is set.
        /// </summary>
        protected bool AlwaysSelected => SelectionMode.HasAllFlags(SelectionMode.AlwaysSelected);

        /// <inheritdoc/>
        public override void BeginInit()
        {
            base.BeginInit();
            BeginUpdating();
        }

        /// <inheritdoc/>
        public override void EndInit()
        {
            base.EndInit();
            EndUpdating();
        }

        /// <summary>
        /// Gets the value of the <see cref="IsSelectedProperty"/> on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The value of the attached property.</returns>
        public static bool GetIsSelected(Control control) => control.GetValue(IsSelectedProperty);

        /// <summary>
        /// Gets the value of the <see cref="IsSelectedProperty"/> on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>The value of the attached property.</returns>
        public static void SetIsSelected(Control control, bool value) => control.SetValue(IsSelectedProperty, value);

        /// <summary>
        /// Tries to get the container that was the source of an event.
        /// </summary>
        /// <param name="eventSource">The control that raised the event.</param>
        /// <returns>The container or null if the event did not originate in a container.</returns>
        protected Control? GetContainerFromEventSource(object? eventSource)
        {
            for (var current = eventSource as Visual; current != null; current = current.VisualParent)
            {
                if (current is Control control && control.Parent == this &&
                    IndexFromContainer(control) != -1)
                {
                    return control;
                }
            }

            return null;
        }

        private protected override void OnItemsViewCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsViewCollectionChanged(sender!, e);

            //Do not change SelectedIndex during initialization
            if (_updateState is not null)
            {
                return;
            }

            if (AlwaysSelected && SelectedIndex == -1 && ItemCount > 0)
            {
                SelectedIndex = 0;
            }
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            AutoScrollToSelectedItemIfNecessary(GetAnchorIndex());
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            void ExecuteScrollWhenLayoutUpdated(object? sender, EventArgs e)
            {
                LayoutUpdated -= ExecuteScrollWhenLayoutUpdated;

                AutoScrollToSelectedItemIfNecessary(GetAnchorIndex());
            }

            if (AutoScrollToSelectedItem)
            {
                LayoutUpdated += ExecuteScrollWhenLayoutUpdated;
            }
        }

        internal int GetAnchorIndex()
        {
            var selection = _updateState is not null ? TryGetExistingSelection() : Selection;
            return selection?.AnchorIndex ?? -1;
        }

        private ISelectionModel? TryGetExistingSelection()
            => _updateState?.Selection.HasValue == true ? _updateState.Selection.Value : _selection;

        protected internal override void PrepareContainerForItemOverride(Control container, object? item, int index)
        {
            // Ensure that the selection model is created at this point so that accessing it in 
            // ContainerForItemPreparedOverride doesn't cause it to be initialized (which can
            // make containers become deselected when they're synced with the empty selection
            // mode).
            GetOrCreateSelectionModel();

            base.PrepareContainerForItemOverride(container, item, index);
        }

        protected internal override void ContainerForItemPreparedOverride(Control container, object? item, int index)
        {
            base.ContainerForItemPreparedOverride(container, item, index);

            // Once the container has been full prepared and added to the tree, any bindings from
            // styles or item container themes are guaranteed to be applied. 
            if (!container.IsSet(IsSelectedProperty))
            {
                // The IsSelected property is not set on the container: update the container
                // selection based on the current selection as understood by this control.
                MarkContainerSelected(container, Selection.IsSelected(index));
            }
            else
            {
                // The IsSelected property is set on the container: there is a style or item
                // container theme which has bound the IsSelected property. Update our selection
                // based on the selection state of the container.
                var containerIsSelected = GetIsSelected(container);
                UpdateSelection(index, containerIsSelected, toggleModifier: true);
            }

            if (Selection.AnchorIndex == index)
                KeyboardNavigation.SetTabOnceActiveElement(this, container);
        }

        /// <inheritdoc />
        protected override void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
        {
            base.ContainerIndexChangedOverride(container, oldIndex, newIndex);
            MarkContainerSelected(container, Selection.IsSelected(newIndex));
        }

        /// <inheritdoc />
        protected internal override void ClearContainerForItemOverride(Control element)
        {
            base.ClearContainerForItemOverride(element);

            try
            {
                _ignoreContainerSelectionChanged = true;
                element.ClearValue(IsSelectedProperty);
            }
            finally
            {
                _ignoreContainerSelectionChanged = false;
            }
        }

        /// <inheritdoc/>
        protected override void OnDataContextBeginUpdate()
        {
            base.OnDataContextBeginUpdate();
            BeginUpdating();
        }

        /// <inheritdoc/>
        protected override void OnDataContextEndUpdate()
        {
            base.OnDataContextEndUpdate();
            EndUpdating();
        }

        /// <summary>
        /// Called to update the validation state for properties for which data validation is
        /// enabled.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="state">The current data binding state.</param>
        /// <param name="error">The current data binding error, if any.</param>
        protected override void UpdateDataValidation(
            AvaloniaProperty property,
            BindingValueType state,
            Exception? error)
        {
            if (property == SelectedItemProperty)
            {
                DataValidationErrors.SetError(this, error);
            }
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (_selection is object)
            {
                _selection.Source = ItemsView;
            }
        }

        /// <inheritdoc />
        protected override void OnTextInput(TextInputEventArgs e)
        {
            if (!e.Handled)
            {
                if (!IsTextSearchEnabled)
                    return;

                StopTextSearchTimer();

                _textSearchTerm += e.Text;

                bool Match(Control container)
                {
                    if (container is AvaloniaObject ao && ao.IsSet(TextSearch.TextProperty))
                    {
                        var searchText = ao.GetValue(TextSearch.TextProperty);

                        if (searchText?.StartsWith(_textSearchTerm, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            return true;
                        }
                    }

                    return container is IContentControl control &&
                           control.Content?.ToString()?.StartsWith(_textSearchTerm, StringComparison.OrdinalIgnoreCase) == true;
                }

                var container = GetRealizedContainers().FirstOrDefault(Match);

                if (container != null)
                {
                    SelectedIndex = IndexFromContainer(container);
                }

                StartTextSearchTimer();

                e.Handled = true;
            }

            base.OnTextInput(e);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == AutoScrollToSelectedItemProperty)
            {
                AutoScrollToSelectedItemIfNecessary(GetAnchorIndex());
            }
            else if (change.Property == SelectionModeProperty && _selection is object)
            {
                var newValue = change.GetNewValue<SelectionMode>();
                _selection.SingleSelect = !newValue.HasAllFlags(SelectionMode.Multiple);
            }
            else if (change.Property == WrapSelectionProperty)
            {
                WrapFocus = WrapSelection;
            }
            else if (change.Property == SelectedValueProperty)
            {
                if (_isSelectionChangeActive)
                    return;

                if (_updateState is not null)
                {
                    _updateState.SelectedValue = change.NewValue;
                    return;
                }

                SelectItemWithValue(change.NewValue);
            }
            else if (change.Property == SelectedValueBindingProperty)
            {
                var idx = SelectedIndex;

                // If no selection is active, don't do anything as SelectedValue is already null
                if (idx == -1)
                {
                    return;
                }

                var value = change.GetNewValue<IBinding?>();
                if (value is null)
                {
                    // Clearing SelectedValueBinding makes the SelectedValue the item itself
                    SetCurrentValue(SelectedValueProperty, SelectedItem);
                    return;
                }

                var selectedItem = SelectedItem;

                try
                {
                    _isSelectionChangeActive = true;

                    if (_bindingHelper is null)
                    {
                        _bindingHelper = new BindingHelper(value);
                    }
                    else
                    {
                        _bindingHelper.UpdateBinding(value);
                    }

                    // Re-evaluate SelectedValue with the new binding
                    SetCurrentValue(SelectedValueProperty, _bindingHelper.Evaluate(selectedItem));
                }
                finally
                {
                    _isSelectionChangeActive = false;
                }
            }
        }

        /// <summary>
        /// Moves the selection in the specified direction relative to the current selection.
        /// </summary>
        /// <param name="direction">The direction to move.</param>
        /// <param name="wrap">Whether to wrap when the selection reaches the first or last item.</param>
        /// <param name="rangeModifier">Whether the range modifier is enabled (i.e. shift key).</param>
        /// <returns>True if the selection was moved; otherwise false.</returns>
        protected bool MoveSelection(
            NavigationDirection direction,
            bool wrap = false,
            bool rangeModifier = false)
        {
            var focused = FocusManager.GetFocusManager(this)?.GetFocusedElement();
            var from = GetContainerFromEventSource(focused) ?? ContainerFromIndex(Selection.AnchorIndex);
            return MoveSelection(from, direction, wrap, rangeModifier);
        }

        /// <summary>
        /// Moves the selection in the specified direction relative to the specified container.
        /// </summary>
        /// <param name="from">The container which serves as a starting point for the movement.</param>
        /// <param name="direction">The direction to move.</param>
        /// <param name="wrap">Whether to wrap when the selection reaches the first or last item.</param>
        /// <param name="rangeModifier">Whether the range modifier is enabled (i.e. shift key).</param>
        /// <returns>True if the selection was moved; otherwise false.</returns>
        protected bool MoveSelection(
            Control? from,
            NavigationDirection direction,
            bool wrap = false,
            bool rangeModifier = false)
        {
            if (Presenter?.Panel is not INavigableContainer container)
                return false;

            if (from is null)
            {
                direction = direction switch
                {
                    NavigationDirection.Down => NavigationDirection.First,
                    NavigationDirection.Up => NavigationDirection.Last,
                    NavigationDirection.Right => NavigationDirection.First,
                    NavigationDirection.Left => NavigationDirection.Last,
                    _ => direction,
                };
            }

            if (GetNextControl(container, direction, from, wrap) is Control next)
            {
                var index = IndexFromContainer(next);

                if (index != -1)
                {
                    UpdateSelection(index, true, rangeModifier);
                    next.Focus();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the selection for an item based on user interaction.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <param name="select">Whether the item should be selected or unselected.</param>
        /// <param name="rangeModifier">Whether the range modifier is enabled (i.e. shift key).</param>
        /// <param name="toggleModifier">Whether the toggle modifier is enabled (i.e. ctrl key).</param>
        /// <param name="rightButton">Whether the event is a right-click.</param>
        /// <param name="fromFocus">Wheter the event is a focus event</param>
        protected void UpdateSelection(
            int index,
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false,
            bool rightButton = false,
            bool fromFocus = false)
        {
            if (index < 0 || index >= ItemCount)
            {
                return;
            }

            var mode = SelectionMode;
            var multi = mode.HasAllFlags(SelectionMode.Multiple);
            var toggle = toggleModifier || mode.HasAllFlags(SelectionMode.Toggle);
            var range = multi && rangeModifier;

            if (!select)
            {
                Selection.Deselect(index);
            }
            else if (rightButton)
            {
                if (Selection.IsSelected(index) == false)
                {
                    SelectedIndex = index;
                }
            }
            else if (range)
            {
                using var operation = Selection.BatchUpdate();
                Selection.Clear();
                Selection.SelectRange(Selection.AnchorIndex, index);
            }
            else if (!fromFocus && toggle)
            {
                if (multi)
                {
                    if (Selection.IsSelected(index))
                    {
                        Selection.Deselect(index);
                    }
                    else
                    {
                        Selection.Select(index);
                    }
                }
                else
                {
                    SelectedIndex = (SelectedIndex == index) ? -1 : index;
                }
            }
            else if (!toggle)
            {
                using var operation = Selection.BatchUpdate();
                Selection.Clear();
                Selection.Select(index);
            }
        }

        /// <summary>
        /// Updates the selection for a container based on user interaction.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="select">Whether the container should be selected or unselected.</param>
        /// <param name="rangeModifier">Whether the range modifier is enabled (i.e. shift key).</param>
        /// <param name="toggleModifier">Whether the toggle modifier is enabled (i.e. ctrl key).</param>
        /// <param name="rightButton">Whether the event is a right-click.</param>
        /// <param name="fromFocus">Wheter the event is a focus event</param>
        protected void UpdateSelection(
            Control container,
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false,
            bool rightButton = false,
            bool fromFocus = false)
        {
            var index = IndexFromContainer(container);

            if (index != -1)
            {
                UpdateSelection(index, select, rangeModifier, toggleModifier, rightButton, fromFocus);
            }
        }

        /// <summary>
        /// Updates the selection based on an event that may have originated in a container that
        /// belongs to the control.
        /// </summary>
        /// <param name="eventSource">The control that raised the event.</param>
        /// <param name="select">Whether the container should be selected or unselected.</param>
        /// <param name="rangeModifier">Whether the range modifier is enabled (i.e. shift key).</param>
        /// <param name="toggleModifier">Whether the toggle modifier is enabled (i.e. ctrl key).</param>
        /// <param name="rightButton">Whether the event is a right-click.</param>
        /// <param name="fromFocus">Wheter the event is a focus event</param>
        /// <returns>
        /// True if the event originated from a container that belongs to the control; otherwise
        /// false.
        /// </returns>
        protected bool UpdateSelectionFromEventSource(
            object? eventSource,
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false,
            bool rightButton = false,
            bool fromFocus = false)
        {
            var container = GetContainerFromEventSource(eventSource);

            if (container != null)
            {
                UpdateSelection(container, select, rangeModifier, toggleModifier, rightButton, fromFocus);
                return true;
            }

            return false;
        }

        private ISelectionModel GetOrCreateSelectionModel()
        {
            if (_selection is null)
            {
                _selection = CreateDefaultSelectionModel();
                InitializeSelectionModel(_selection);
            }

            return _selection;
        }

        private void OnItemsViewSourceChanged(object? sender, EventArgs e)
        {
            if (_selection is not null && _updateState is null)
                _selection.Source = ItemsView;
        }

        /// <summary>
        /// Called when <see cref="INotifyPropertyChanged.PropertyChanged"/> is raised on
        /// <see cref="Selection"/>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnSelectionModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISelectionModel.AnchorIndex))
            {
                _hasScrolledToSelectedItem = false;

                var anchorIndex = GetAnchorIndex();
                KeyboardNavigation.SetTabOnceActiveElement(this, ContainerFromIndex(anchorIndex));
                AutoScrollToSelectedItemIfNecessary(anchorIndex);
            }
            else if (e.PropertyName == nameof(ISelectionModel.SelectedIndex) && _oldSelectedIndex != SelectedIndex)
            {
                RaisePropertyChanged(SelectedIndexProperty, _oldSelectedIndex, SelectedIndex);
                _oldSelectedIndex = SelectedIndex;
            }
            else if (e.PropertyName == nameof(ISelectionModel.SelectedItem) && _oldSelectedItem != SelectedItem)
            {
                RaisePropertyChanged(SelectedItemProperty, _oldSelectedItem, SelectedItem);
                _oldSelectedItem = SelectedItem;
            }
            else if (e.PropertyName == nameof(InternalSelectionModel.WritableSelectedItems) &&
                     _oldSelectedItems != (Selection as InternalSelectionModel)?.SelectedItems)
            {
                RaisePropertyChanged(SelectedItemsProperty, _oldSelectedItems, SelectedItems);
                _oldSelectedItems = SelectedItems;
            }
            else if (e.PropertyName == nameof(ISelectionModel.Source))
            {
                ClearValue(SelectedValueProperty);
            }
        }

        /// <summary>
        /// Called when <see cref="ISelectionModel.SelectionChanged"/> event is raised on
        /// <see cref="Selection"/>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnSelectionModelSelectionChanged(object? sender, SelectionModelSelectionChangedEventArgs e)
        {
            void Mark(int index, bool selected)
            {
                var container = ContainerFromIndex(index);

                if (container != null)
                {
                    MarkContainerSelected(container, selected);
                }
            }

            foreach (var i in e.SelectedIndexes)
            {
                Mark(i, true);
            }

            foreach (var i in e.DeselectedIndexes)
            {
                Mark(i, false);
            }

            if (!_isSelectionChangeActive)
            {
                UpdateSelectedValueFromItem();
            }

            var route = BuildEventRoute(SelectionChangedEvent);

            if (route.HasHandlers)
            {
                var ev = new SelectionChangedEventArgs(
                    SelectionChangedEvent,
                    e.DeselectedItems.ToArray(),
                    e.SelectedItems.ToArray());
                RaiseEvent(ev);
            }
        }

        /// <summary>
        /// Called when <see cref="ISelectionModel.LostSelection"/> event is raised on
        /// <see cref="Selection"/>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnSelectionModelLostSelection(object? sender, EventArgs e)
        {
            if (AlwaysSelected && ItemsView.Count > 0)
            {
                SelectedIndex = 0;
            }
        }

        private void SelectItemWithValue(object? value)
        {
            if (ItemCount == 0 || _isSelectionChangeActive)
                return;

            try
            {
                _isSelectionChangeActive = true;
                var si = FindItemWithValue(value);
                if (si != AvaloniaProperty.UnsetValue)
                {
                    SelectedItem = si;
                }
                else
                {
                    SelectedItem = null;
                }
            }
            finally
            {
                _isSelectionChangeActive = false;
            }
        }

        private object? FindItemWithValue(object? value)
        {
            if (ItemCount == 0 || value is null)
            {
                return AvaloniaProperty.UnsetValue;
            }

            var items = ItemsView;
            var binding = SelectedValueBinding;

            if (binding is null)
            {
                // No SelectedValueBinding set, SelectedValue is the item itself
                // Still verify the value passed in is in the Items list
                var index = items!.IndexOf(value);

                if (index >= 0)
                {
                    return value;
                }
                else
                {
                    return AvaloniaProperty.UnsetValue;
                }
            }

            _bindingHelper ??= new BindingHelper(binding);

            // Matching UWP behavior, if duplicates are present, return the first item matching
            // the SelectedValue provided
            foreach (var item in items!)
            {
                var itemValue = _bindingHelper.Evaluate(item);

                if (Equals(itemValue, value))
                {
                    return item;
                }
            }

            return AvaloniaProperty.UnsetValue;
        }

        private void UpdateSelectedValueFromItem()
        {
            if (_isSelectionChangeActive)
                return;

            var binding = SelectedValueBinding;
            var item = SelectedItem;

            if (binding is null || item is null)
            {
                // No SelectedValueBinding, SelectedValue is Item itself
                try
                {
                    _isSelectionChangeActive = true;
                    SetCurrentValue(SelectedValueProperty, item);
                }
                finally
                {
                    _isSelectionChangeActive = false;
                }
                return;
            }

            _bindingHelper ??= new BindingHelper(binding);

            try
            {
                _isSelectionChangeActive = true;
                SetCurrentValue(SelectedValueProperty, _bindingHelper.Evaluate(item));
            }
            finally
            {
                _isSelectionChangeActive = false;
            }
        }

        private void AutoScrollToSelectedItemIfNecessary(int anchorIndex)
        {
            if (AutoScrollToSelectedItem &&
                !_hasScrolledToSelectedItem &&
                Presenter is object &&
                anchorIndex >= 0 &&
                IsAttachedToVisualTree)
            {
                Dispatcher.UIThread.Post(state =>
                {
                    ScrollIntoView((int)state!);
                    _hasScrolledToSelectedItem = true;
                }, anchorIndex);
            }
        }

        /// <summary>
        /// Called when a container raises the <see cref="IsSelectedChangedEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        private void ContainerSelectionChanged(RoutedEventArgs e)
        {
            if (!_ignoreContainerSelectionChanged &&
                e.Source is Control control &&
                control.Parent == this &&
                IndexFromContainer(control) is var index &&
                index >= 0)
            {
                if (GetIsSelected(control))
                    Selection.Select(index);
                else
                    Selection.Deselect(index);
            }

            if (e.Source != this)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Sets the <see cref="IsSelectedProperty"/> on the specified container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="selected">Whether the control is selected</param>
        /// <returns>The previous selection state.</returns>
        private void MarkContainerSelected(Control container, bool selected)
        {
            _ignoreContainerSelectionChanged = true;

            try
            {
                container.SetCurrentValue(IsSelectedProperty, selected);
            }
            finally
            {
                _ignoreContainerSelectionChanged = false;
            }
        }

        private void UpdateContainerSelection()
        {
            if (Presenter?.Panel is { } panel)
            {
                foreach (var container in panel.Children)
                {
                    MarkContainerSelected(
                        container,
                        Selection.IsSelected(IndexFromContainer(container)));
                }
            }
        }

        private ISelectionModel CreateDefaultSelectionModel()
        {
            return new InternalSelectionModel
            {
                SingleSelect = !SelectionMode.HasAllFlags(SelectionMode.Multiple),
            };
        }

        private void InitializeSelectionModel(ISelectionModel model)
        {
            if (_updateState is null)
            {
                model.Source = ItemsView;
            }

            model.PropertyChanged += OnSelectionModelPropertyChanged;
            model.SelectionChanged += OnSelectionModelSelectionChanged;
            model.LostSelection += OnSelectionModelLostSelection;

            if (model.SingleSelect)
            {
                SelectionMode &= ~SelectionMode.Multiple;
            }
            else
            {
                SelectionMode |= SelectionMode.Multiple;
            }

            _oldSelectedIndex = model.SelectedIndex;
            _oldSelectedItem = model.SelectedItem;

            if (_updateState is null && AlwaysSelected && model.Count == 0)
            {
                model.SelectedIndex = 0;
            }

            UpdateContainerSelection();

            if (SelectedIndex != -1)
            {
                RaiseEvent(new SelectionChangedEventArgs(
                    SelectionChangedEvent,
                    Array.Empty<object>(),
                    Selection.SelectedItems.ToArray()));
            }
        }

        private void DeinitializeSelectionModel(ISelectionModel? model)
        {
            if (model is object)
            {
                model.PropertyChanged -= OnSelectionModelPropertyChanged;
                model.SelectionChanged -= OnSelectionModelSelectionChanged;
            }
        }

        private void BeginUpdating()
        {
            _updateState ??= new UpdateState();
            _updateState.UpdateCount++;
        }

        private void EndUpdating()
        {
            if (_updateState is object && --_updateState.UpdateCount == 0)
            {
                var state = _updateState;
                _updateState = null;

                if (state.Selection.HasValue)
                {
                    Selection = state.Selection.Value;
                }

                if (_selection is InternalSelectionModel s)
                {
                    s.Update(ItemsView, state.SelectedItems);
                }
                else
                {
                    if (state.SelectedItems.HasValue)
                    {
                        SelectedItems = state.SelectedItems.Value;
                    }

                    Selection.Source = ItemsView;
                }

                if (state.SelectedValue.HasValue)
                {
                    var item = FindItemWithValue(state.SelectedValue.Value);
                    if (item != AvaloniaProperty.UnsetValue)
                        state.SelectedItem = item;
                }

                // SelectedIndex vs SelectedItem:
                // - If only one has a value, use it
                // - If both have a value, prefer the one having a "non-empty" value, e.g. not -1 nor null
                // - If both have a "non-empty" value, prefer the index
                if (state.SelectedIndex.HasValue)
                {
                    var selectedIndex = state.SelectedIndex.Value;
                    if (selectedIndex >= 0 || !state.SelectedItem.HasValue)
                        SelectedIndex = selectedIndex;
                    else
                        SelectedItem = state.SelectedItem.Value;
                }
                else if (state.SelectedItem.HasValue)
                {
                    SelectedItem = state.SelectedItem.Value;
                }

                if (AlwaysSelected && SelectedIndex == -1 && ItemCount > 0)
                {
                    SelectedIndex = 0;
                }
            }
        }

        private void StartTextSearchTimer()
        {
            _textSearchTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _textSearchTimer.Tick += TextSearchTimer_Tick;
            _textSearchTimer.Start();
        }

        private void StopTextSearchTimer()
        {
            if (_textSearchTimer == null)
            {
                return;
            }

            _textSearchTimer.Tick -= TextSearchTimer_Tick;
            _textSearchTimer.Stop();

            _textSearchTimer = null;
        }

        private void TextSearchTimer_Tick(object? sender, EventArgs e)
        {
            _textSearchTerm = string.Empty;
            StopTextSearchTimer();
        }

        // When in a BeginInit..EndInit block, or when the DataContext is updating, we need to
        // defer changes to the selection model because we have no idea in which order properties
        // will be set. Consider:
        //
        // - Both Items and SelectedItem are bound
        // - The DataContext changes
        // - The binding for SelectedItem updates first, producing an item
        // - Items is searched to find the index of the new selected item
        // - However Items isn't yet updated; the item is not found
        // - SelectedIndex is incorrectly set to -1
        //
        // This logic cannot be encapsulated in SelectionModel because the selection model can also
        // be bound, consider:
        //
        // - Both Items and Selection are bound
        // - The DataContext changes
        // - The binding for Items updates first
        // - The new items are assigned to Selection.Source
        // - The binding for Selection updates, producing a new SelectionModel
        // - Both the old and new SelectionModels have the incorrect Source
        private class UpdateState
        {
            public int UpdateCount { get; set; }
            public Optional<ISelectionModel> Selection { get; set; }
            public Optional<IList?> SelectedItems { get; set; }
            public Optional<int> SelectedIndex { get; set; }
            public Optional<object?> SelectedItem { get; set; }
            public Optional<object?> SelectedValue { get; set; }
        }

        /// <summary>
        /// Helper class for evaluating a binding from an Item and IBinding instance
        /// </summary>
        private class BindingHelper : StyledElement
        {
            public BindingHelper(IBinding binding)
            {
                UpdateBinding(binding);
            }

            public static readonly StyledProperty<object> ValueProperty =
                AvaloniaProperty.Register<BindingHelper, object>("Value");

            public object? Evaluate(object? dataContext)
            {
                // Only update the DataContext if necessary
                if (!Equals(dataContext, DataContext))
                    DataContext = dataContext;

                return GetValue(ValueProperty);
            }

            public void UpdateBinding(IBinding binding)
            {
                _lastBinding = binding;
                var ib = binding.Initiate(this, ValueProperty);
                if (ib is null)
                {
                    throw new InvalidOperationException("Unable to create binding");
                }

                BindingOperations.Apply(this, ValueProperty, ib, null);
            }

            private IBinding? _lastBinding;
        }
    }
}
