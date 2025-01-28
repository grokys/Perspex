using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.PropertyStore;
using Avalonia.Styling;

namespace Avalonia
{
    /// <summary>
    /// Extends an <see cref="Animatable"/> with the following features:
    /// 
    /// - An inherited <see cref="DataContext"/>.
    /// - Implements <see cref="IStyleable"/> to allow styling to work on the styled element.
    /// - Implements <see cref="ILogical"/> to form part of a logical tree.
    /// - A collection of class strings for custom styling.
    /// </summary>
    public class StyledElement : Animatable, 
        IDataContextProvider, 
        ILogical,
        IThemeVariantHost,
        IStyleHost,
        ISetLogicalParent,
        ISetInheritanceParent,
        ISupportInitialize,
        INamed,
        IAvaloniaListItemValidator<ILogical>,
#pragma warning disable CS0618 // Type or member is obsolete
        IStyleable
#pragma warning restore CS0618 // Type or member is obsolete
    {
        /// <summary>
        /// Defines the <see cref="DataContext"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> DataContextProperty =
            AvaloniaProperty.Register<StyledElement, object?>(
                nameof(DataContext),
                defaultValue: null,
                inherits: true,
                defaultBindingMode: BindingMode.OneWay,
                validate: null,
                coerce: null,
                enableDataValidation: false,
                notifying: DataContextNotifying);

        /// <summary>
        /// Defines the <see cref="Name"/> property.
        /// </summary>
        public static readonly DirectProperty<StyledElement, string?> NameProperty =
            AvaloniaProperty.RegisterDirect<StyledElement, string?>(nameof(Name), o => o.Name, (o, v) => o.Name = v);
        
        /// <summary>
        /// Defines the <see cref="Parent"/> property.
        /// </summary>
        public static readonly DirectProperty<StyledElement, StyledElement?> ParentProperty =
            AvaloniaProperty.RegisterDirect<StyledElement, StyledElement?>(nameof(Parent), o => o.Parent);

        /// <summary>
        /// Defines the <see cref="TemplatedParent"/> property.
        /// </summary>
        public static readonly DirectProperty<StyledElement, AvaloniaObject?> TemplatedParentProperty =
            AvaloniaProperty.RegisterDirect<StyledElement, AvaloniaObject?>(
                nameof(TemplatedParent),
                o => o.TemplatedParent);
        
        /// <summary>
        /// Defines the <see cref="Theme"/> property.
        /// </summary>
        public static readonly StyledProperty<ControlTheme?> ThemeProperty =
            AvaloniaProperty.Register<StyledElement, ControlTheme?>(nameof(Theme));

        private static readonly ControlTheme s_invalidTheme = new ControlTheme();
        private int _initCount;
        private string? _name;
        private Classes? _classes;
        private ILogicalRoot? _logicalRoot;
        private IAvaloniaList<ILogical>? _logicalChildren;
        private IResourceDictionary? _resources;
        private Styles? _styles;
        private bool _stylesApplied;
        private bool _themeApplied;
        private bool _templatedParentThemeApplied;
        private AvaloniaObject? _templatedParent;
        private bool _dataContextUpdating;
        private ControlTheme? _implicitTheme;

        /// <summary>
        /// Initializes static members of the <see cref="StyledElement"/> class.
        /// </summary>
        static StyledElement()
        {
            DataContextProperty.Changed.AddClassHandler<StyledElement>((x,e) => x.OnDataContextChangedCore(e));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StyledElement"/> class.
        /// </summary>
        public StyledElement()
        {
            _logicalRoot = this as ILogicalRoot;
        }

        /// <summary>
        /// Raised when the styled element is attached to a rooted logical tree.
        /// </summary>
        public event EventHandler<LogicalTreeAttachmentEventArgs>? AttachedToLogicalTree;

        /// <summary>
        /// Raised when the styled element is detached from a rooted logical tree.
        /// </summary>
        public event EventHandler<LogicalTreeAttachmentEventArgs>? DetachedFromLogicalTree;

        /// <summary>
        /// Occurs when the <see cref="DataContext"/> property changes.
        /// </summary>
        /// <remarks>
        /// This event will be raised when the <see cref="DataContext"/> property has changed and
        /// all subscribers to that change have been notified.
        /// </remarks>
        public event EventHandler? DataContextChanged;

        /// <summary>
        /// Occurs when the styled element has finished initialization.
        /// </summary>
        /// <remarks>
        /// The Initialized event indicates that all property values on the styled element have been set.
        /// When loading the styled element from markup, it occurs when 
        /// <see cref="ISupportInitialize.EndInit"/> is called *and* the styled element
        /// is attached to a rooted logical tree. When the styled element is created by code and
        /// <see cref="ISupportInitialize"/> is not used, it is called when the styled element is attached
        /// to the visual tree.
        /// </remarks>
        public event EventHandler? Initialized;

        /// <summary>
        /// Occurs when a resource in this styled element or a parent styled element has changed.
        /// </summary>
        public event EventHandler<ResourcesChangedEventArgs>? ResourcesChanged;

        /// <inheritdoc />
        public event EventHandler? ActualThemeVariantChanged;
        
        /// <summary>
        /// Gets or sets the name of the styled element.
        /// </summary>
        /// <remarks>
        /// An element's name is used to uniquely identify an element within the element's name
        /// scope. Once the element is added to a logical tree, its name cannot be changed.
        /// </remarks>
        public string? Name
        {
            get => _name;

            set
            {
                if (_stylesApplied)
                {
                    throw new InvalidOperationException("Cannot set Name : styled element already styled.");
                }

                _name = value;
            }
        }

        /// <summary>
        /// Gets or sets the styled element's classes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Classes can be used to apply user-defined styling to styled elements, or to allow styled elements
        /// that share a common purpose to be easily selected.
        /// </para>
        /// <para>
        /// Even though this property can be set, the setter is only intended for use in object
        /// initializers. Assigning to this property does not change the underlying collection,
        /// it simply clears the existing collection and adds the contents of the assigned
        /// collection.
        /// </para>
        /// </remarks>
        public Classes Classes => _classes ??= new();

        /// <summary>
        /// Gets or sets the control's data context.
        /// </summary>
        /// <remarks>
        /// The data context is an inherited property that specifies the default object that will
        /// be used for data binding.
        /// </remarks>
        public object? DataContext
        {
            get { return GetValue(DataContextProperty); }
            set { SetValue(DataContextProperty, value); }
        }

        /// <summary>
        /// Gets a value that indicates whether the element has finished initialization.
        /// </summary>
        /// <remarks>
        /// For more information about when IsInitialized is set, see the <see cref="Initialized"/>
        /// event.
        /// </remarks>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Gets the styles for the styled element.
        /// </summary>
        /// <remarks>
        /// Styles for the entire application are added to the Application.Styles collection, but
        /// each styled element may in addition define its own styles which are applied to the styled element
        /// itself and its children.
        /// </remarks>
        public Styles Styles => _styles ??= new Styles(this);

        /// <summary>
        /// Gets the type by which the element is styled.
        /// </summary>
        /// <remarks>
        /// Usually controls are styled by their own type, but there are instances where you want
        /// an element to be styled by its base type, e.g. creating SpecialButton that
        /// derives from Button and adds extra functionality but is still styled as a regular
        /// Button. To change the style for a control class, override the <see cref="StyleKeyOverride"/>
        /// property
        /// </remarks>
        public Type StyleKey => StyleKeyOverride;

        /// <summary>
        /// Gets or sets the styled element's resource dictionary.
        /// </summary>
        public IResourceDictionary Resources
        {
            get => _resources ??= new ResourceDictionary(this);
            set
            {
                value = value ?? throw new ArgumentNullException(nameof(value));
                _resources?.RemoveOwner(this);
                _resources = value;
                _resources.AddOwner(this);
            }
        }

        /// <summary>
        /// Gets the styled element whose lookless template this styled element is part of.
        /// </summary>
        public AvaloniaObject? TemplatedParent
        {
            get => _templatedParent;
            internal set => SetAndRaise(TemplatedParentProperty, ref _templatedParent, value);
        }

        /// <summary>
        /// Gets or sets the theme to be applied to the element.
        /// </summary>
        public ControlTheme? Theme
        {
            get => GetValue(ThemeProperty);
            set => SetValue(ThemeProperty, value);
        }

        /// <summary>
        /// Gets the styled element's logical children.
        /// </summary>
        protected internal IAvaloniaList<ILogical> LogicalChildren
        {
            get
            {
                if (_logicalChildren == null)
                {
                    var list = new AvaloniaList<ILogical>
                    {
                        ResetBehavior = ResetBehavior.Remove,
                        Validator = this
                    };
                    list.CollectionChanged += LogicalChildrenCollectionChanged;
                    _logicalChildren = list;
                }

                return _logicalChildren;
            }
        }

        /// <summary>
        /// Gets the <see cref="Classes"/> collection in a form that allows adding and removing
        /// pseudoclasses.
        /// </summary>
        protected IPseudoClasses PseudoClasses => Classes;

        /// <summary>
        /// Gets the type by which the element is styled.
        /// </summary>
        /// <remarks>
        /// Usually controls are styled by their own type, but there are instances where you want
        /// an element to be styled by its base type, e.g. creating SpecialButton that
        /// derives from Button and adds extra functionality but is still styled as a regular
        /// Button. Override this property to change the style for a control class, returning the
        /// type that you wish the elements to be styled as.
        /// </remarks>
        protected virtual Type StyleKeyOverride => GetType();

        /// <summary>
        /// Gets a value indicating whether the element is attached to a rooted logical tree.
        /// </summary>
        bool ILogical.IsAttachedToLogicalTree => _logicalRoot != null;

        /// <summary>
        /// Gets the styled element's logical parent.
        /// </summary>
        public StyledElement? Parent { get; private set; }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1030:StyledProperty accessors should not have side effects", Justification = "False positive?")]
        public ThemeVariant ActualThemeVariant => GetValue(ThemeVariant.ActualThemeVariantProperty);
        
        /// <summary>
        /// Gets the styled element's logical parent.
        /// </summary>
        ILogical? ILogical.LogicalParent => Parent;

        /// <summary>
        /// Gets the styled element's logical children.
        /// </summary>
        IAvaloniaReadOnlyList<ILogical> ILogical.LogicalChildren => LogicalChildren;

        /// <inheritdoc/>
        bool IResourceNode.HasResources => (_resources?.HasResources ?? false) ||
            (((IResourceNode?)_styles)?.HasResources ?? false);

        /// <inheritdoc/>
        IAvaloniaReadOnlyList<string> IStyleable.Classes => Classes;

        /// <inheritdoc/>
        bool IStyleHost.IsStylesInitialized => _styles != null;

        /// <inheritdoc/>
        IStyleHost? IStyleHost.StylingParent => (IStyleHost?)InheritanceParent;

        /// <inheritdoc/>
        public virtual void BeginInit()
        {
            ++_initCount;
        }

        /// <inheritdoc/>
        public virtual void EndInit()
        {
            if (_initCount == 0)
            {
                throw new InvalidOperationException("BeginInit was not called.");
            }

            if (--_initCount == 0 && _logicalRoot is not null)
            {
                ApplyStyling();
                InitializeIfNeeded();
            }
        }

        /// <summary>
        /// Applies styling to the control if the control is initialized and styling is not
        /// already applied.
        /// </summary>
        /// <remarks>
        /// The styling system will automatically apply styling when required, so it should not
        /// usually be necessary to call this method manually.
        /// </remarks>
        /// <returns>
        /// A value indicating whether styling is now applied to the control.
        /// </returns>
        public bool ApplyStyling()
        {
            if (_initCount == 0 && (!_stylesApplied || !_themeApplied || !_templatedParentThemeApplied))
            {
                GetValueStore().BeginStyling();

                try
                {
                    if (!_themeApplied)
                    {
                        ApplyControlTheme();
                        _themeApplied = true;
                    }

                    if (!_templatedParentThemeApplied)
                    {
                        ApplyTemplatedParentControlTheme();
                        _templatedParentThemeApplied = true;
                    }

                    if (!_stylesApplied)
                    {
                        ApplyStyles(this);
                        _stylesApplied = true;
                    }
                }
                finally
                {
                    GetValueStore().EndStyling();
                }
            }

            return _stylesApplied;
        }

        protected void InitializeIfNeeded()
        {
            if (_initCount == 0 && !IsInitialized)
            {
                IsInitialized = true;
                OnInitialized();
                Initialized?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc/>
        void ILogical.NotifyAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            OnAttachedToLogicalTreeCore(e);
        }

        /// <inheritdoc/>
        void ILogical.NotifyDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            OnDetachedFromLogicalTreeCore(e);
        }

        /// <inheritdoc/>
        void ILogical.NotifyResourcesChanged(ResourcesChangedEventArgs e) => NotifyResourcesChanged(e);

        /// <inheritdoc/>
        void IResourceHost.NotifyHostedResourcesChanged(ResourcesChangedEventArgs e) => NotifyResourcesChanged(e);

        /// <inheritdoc/>
        public bool TryGetResource(object key, ThemeVariant? theme, out object? value)
        {
            value = null;
            return (_resources?.TryGetResource(key, theme, out value) ?? false) ||
                   (_styles?.TryGetResource(key, theme, out value) ?? false);
        }

        /// <summary>
        /// Sets the styled element's logical parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        void ISetLogicalParent.SetParent(ILogical? parent)
        {
            var old = Parent;

            if (parent != old)
            {
                if (old != null && parent != null)
                {
                    throw new InvalidOperationException("The Control already has a parent.");
                }

                if (InheritanceParent == null || parent == null)
                {
                    InheritanceParent = parent as AvaloniaObject;
                }

                Parent = (StyledElement?)parent;

                if (_logicalRoot != null)
                {
                    var e = new LogicalTreeAttachmentEventArgs(_logicalRoot, this, old!);
                    OnDetachedFromLogicalTreeCore(e);
                }

                var newRoot = FindLogicalRoot(this);

                if (newRoot is object)
                {
                    var e = new LogicalTreeAttachmentEventArgs(newRoot, this, parent!);
                    OnAttachedToLogicalTreeCore(e);
                }
                else if (parent is null)
                {
                    // If we were attached to the logical tree, we piggyback on the tree traversal
                    // there to raise resources changed notifications. If we're being removed from
                    // the logical tree, then traverse the tree raising notifications now.
                    //
                    // We don't raise resources changed notifications if we're being attached to a 
                    // non-rooted control beacuse it's unlikely that dynamic resources need to be 
                    // correct until the control is added to the tree, and it causes a *lot* of
                    // notifications.
                    NotifyResourcesChanged();
                }

                RaisePropertyChanged(ParentProperty, old, Parent);
            }
        }

        /// <summary>
        /// Sets the styled element's inheritance parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        void ISetInheritanceParent.SetParent(AvaloniaObject? parent)
        {
            InheritanceParent = parent;
        }

        void IStyleHost.StylesAdded(IReadOnlyList<IStyle> styles)
        {
            if (HasSettersOrAnimations(styles))
                InvalidateStyles(recurse: true);
        }

        void IStyleHost.StylesRemoved(IReadOnlyList<IStyle> styles)
        {
            if (FlattenStyles(styles) is { } allStyles)
                DetachStyles(allStyles);
        }

        protected virtual void LogicalChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    SetLogicalParent(e.NewItems!);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    ClearLogicalParent(e.OldItems!);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    ClearLogicalParent(e.OldItems!);
                    SetLogicalParent(e.NewItems!);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException("Reset should not be signaled on LogicalChildren collection");
            }
        }

        /// <summary>
        /// Notifies child controls that a change has been made to resources that apply to them.
        /// </summary>
        /// <param name="e">The event args.</param>
        internal virtual void NotifyChildResourcesChanged(ResourcesChangedEventArgs e)
        {
            if (_logicalChildren is object)
            {
                var count = _logicalChildren.Count;

                if (count > 0)
                {
                    e ??= ResourcesChangedEventArgs.Empty;

                    for (var i = 0; i < count; ++i)
                    {
                        _logicalChildren[i].NotifyResourcesChanged(e);
                    }
                }
            }
        }

        /// <summary>
        /// Called when the styled element is added to a rooted logical tree.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
        }

        /// <summary>
        /// Called when the styled element is removed from a rooted logical tree.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
        }

        /// <summary>
        /// Called when the <see cref="DataContext"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnDataContextChanged(EventArgs e)
        {
            DataContextChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the <see cref="DataContext"/> begins updating.
        /// </summary>
        protected virtual void OnDataContextBeginUpdate()
        {
        }

        /// <summary>
        /// Called when the <see cref="DataContext"/> finishes updating.
        /// </summary>
        protected virtual void OnDataContextEndUpdate()
        {
        }

        /// <summary>
        /// Called when the control finishes initialization.
        /// </summary>
        protected virtual void OnInitialized()
        {
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ThemeProperty)
            {
                OnControlThemeChanged();
            }
            else if (change.Property == ThemeVariant.RequestedThemeVariantProperty)
            {
                if (change.GetNewValue<ThemeVariant>() is {} themeVariant && themeVariant != ThemeVariant.Default)
                    SetValue(ThemeVariant.ActualThemeVariantProperty, themeVariant);
                else
                    ClearValue(ThemeVariant.ActualThemeVariantProperty);
            }
            else if (change.Property == ThemeVariant.ActualThemeVariantProperty)
            {
                ActualThemeVariantChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private protected virtual void OnControlThemeChanged()
        {
            var values = GetValueStore();
            values.BeginStyling();

            try 
            { 
                values.RemoveFrames(FrameType.Theme);
            }
            finally 
            { 
                values.EndStyling();
                _themeApplied = false;
            }
        }

        internal virtual void OnTemplatedParentControlThemeChanged()
        {
            var values = GetValueStore();
            values.BeginStyling();
            try 
            { 
                values.RemoveFrames(FrameType.TemplatedParentTheme); 
            }
            finally 
            { 
                values.EndStyling();
                _templatedParentThemeApplied = false;
            }
        }

        internal ControlTheme? GetEffectiveTheme()
        {
            var theme = Theme;

            // Explicitly set Theme property takes precedence.
            if (theme is not null)
                return theme;

            // If the Theme property is not set, try to find a ControlTheme resource with our StyleKey.
            if (_implicitTheme is null)
            {
                var key = GetStyleKey(this);

                if (this.TryFindResource(key, out var value) && value is ControlTheme t)
                    _implicitTheme = t;
                else
                    _implicitTheme = s_invalidTheme;
            }

            if (_implicitTheme != s_invalidTheme)
                return _implicitTheme;

            return null;
        }

        internal virtual void InvalidateStyles(bool recurse)
        {
            var values = GetValueStore();
            values.BeginStyling();
            try { values.RemoveFrames(FrameType.Style); }
            finally { values.EndStyling(); }

            _stylesApplied = false;

            if (recurse && GetInheritanceChildren() is { } children)
            {
                var childCount = children.Count;
                for (var i = 0; i < childCount; ++i)
                    (children[i] as StyledElement)?.InvalidateStyles(recurse);
            }
        }

        /// <summary>
        /// Internal getter for <see cref="IStyleable.StyleKey"/> so that we only need to suppress the obsolete
        /// warning in one place.
        /// </summary>
        /// <param name="e">The element</param>
        /// <remarks>
        /// <see cref="IStyleable"/> is obsolete and will be removed in a future version, but for backwards
        /// compatibility we need to support code which overrides <see cref="IStyleable.StyleKey"/>.
        /// </remarks>
        internal static Type GetStyleKey(StyledElement e)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return ((IStyleable)e).StyleKey;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private static void DataContextNotifying(AvaloniaObject o, bool updateStarted)
        {
            if (o is StyledElement element)
            {
                DataContextNotifying(element, updateStarted);
            }
        }

        private static void DataContextNotifying(StyledElement element, bool updateStarted)
        {
            if (updateStarted)
            {
                if (!element._dataContextUpdating)
                {
                    element._dataContextUpdating = true;
                    element.OnDataContextBeginUpdate();

                    var logicalChildren = element.LogicalChildren;
                    var logicalChildrenCount = logicalChildren.Count;

                    for (var i = 0; i < logicalChildrenCount; i++)
                    {
                        if (element.LogicalChildren[i] is StyledElement s &&
                            s.InheritanceParent == element &&
                            !s.IsSet(DataContextProperty))
                        {
                            DataContextNotifying(s, updateStarted);
                        }
                    }
                }
            }
            else
            {
                if (element._dataContextUpdating)
                {
                    element.OnDataContextEndUpdate();
                    element._dataContextUpdating = false;
                }
            }
        }

        private static ILogicalRoot? FindLogicalRoot(IStyleHost? e)
        {
            while (e != null)
            {
                if (e is ILogicalRoot root)
                {
                    return root;
                }

                e = e.StylingParent;
            }

            return null;
        }

        void IAvaloniaListItemValidator<ILogical>.Validate(ILogical item)
        {
            if (item is null)
            {
                throw new ArgumentException($"Cannot add null to {nameof(LogicalChildren)}.");
            }
        }

        private void ApplyControlTheme()
        {
            if (GetEffectiveTheme() is { } theme)
                ApplyControlTheme(theme, FrameType.Theme);
        }

        private void ApplyTemplatedParentControlTheme()
        {
            if ((TemplatedParent as StyledElement)?.GetEffectiveTheme() is { } parentTheme)
            {
                ApplyControlTheme(parentTheme, FrameType.TemplatedParentTheme);
            }
        }

        private void ApplyControlTheme(ControlTheme theme, FrameType type)
        {
            Debug.Assert(type is FrameType.Theme or FrameType.TemplatedParentTheme);

            if (theme.BasedOn is ControlTheme basedOn)
                ApplyControlTheme(basedOn, type);

            theme.TryAttach(this, type);

            if (theme.HasChildren)
            {
                var children = theme.Children;
                for (var i = 0; i < children.Count; i++)
                {
                    ApplyStyle(children[i], null, type);
                }
            }
        }

        private void ApplyStyles(IStyleHost host)
        {
            var parent = host.StylingParent;
            if (parent != null)
                ApplyStyles(parent);
            
            if (host.IsStylesInitialized)
            {
                var styles = host.Styles;
                for (var i = 0; i < styles.Count; ++i)
                {
                    ApplyStyle(styles[i], host, FrameType.Style);
                }
            }
        }

        private void ApplyStyle(IStyle style, IStyleHost? host, FrameType type)
        {
            if (style is Styling.ContainerQuery m)
                m.TryAttach(this, host, type);

            if (style is Style s)
                s.TryAttach(this, host, type);

            var children = style.Children;
            for (var i = 0; i < children.Count; i++)
            {
                ApplyStyle(children[i], host, type);
            }
        }

        private void ReevaluateImplicitTheme()
        {
            // We only need to check if the theme has changed when Theme isn't set (i.e. when we
            // have an implicit theme).
            if (Theme is not null)
                return;

            // Refetch the implicit theme.
            var oldImplicitTheme = _implicitTheme == s_invalidTheme ? null : _implicitTheme;
            _implicitTheme = null;
            GetEffectiveTheme();

            var newImplicitTheme = _implicitTheme == s_invalidTheme ? null : _implicitTheme;

            // If the implicit theme has changed, detach the existing theme.
            if (newImplicitTheme != oldImplicitTheme)
            {
                OnControlThemeChanged();
                _themeApplied = false;
            }
        }

        private void OnAttachedToLogicalTreeCore(LogicalTreeAttachmentEventArgs e)
        {
            if (this.GetLogicalParent() == null && !(this is ILogicalRoot))
            {
                throw new InvalidOperationException(
                    $"AttachedToLogicalTreeCore called for '{GetType().Name}' but control has no logical parent.");
            }

            // This method can be called when a control is already attached to the logical tree
            // in the following scenario:
            // - ListBox gets assigned Items containing ListBoxItem
            // - ListBox makes ListBoxItem a logical child
            // - ListBox template gets applied; making its Panel get attached to logical tree
            // - That AttachedToLogicalTree signal travels down to the ListBoxItem
            if (_logicalRoot == null)
            {
                _logicalRoot = e.Root;

                ReevaluateImplicitTheme();
                ApplyStyling();
                NotifyResourcesChanged(propagate: false);

                OnAttachedToLogicalTree(e);
                AttachedToLogicalTree?.Invoke(this, e);
            }

            var logicalChildren = LogicalChildren;
            var logicalChildrenCount = logicalChildren.Count;

            for (var i = 0; i < logicalChildrenCount; i++)
            {
                if (logicalChildren[i] is StyledElement child && child._logicalRoot != e.Root) // child may already have been attached within an event handler
                {
                    child.OnAttachedToLogicalTreeCore(e);
                }
            }
        }

        private void OnDetachedFromLogicalTreeCore(LogicalTreeAttachmentEventArgs e)
        {
            if (_logicalRoot != null)
            {
                _logicalRoot = null;
                InvalidateStyles(recurse: false);
                OnDetachedFromLogicalTree(e);
                DetachedFromLogicalTree?.Invoke(this, e);

                var logicalChildren = LogicalChildren;
                var logicalChildrenCount = logicalChildren.Count;

                for (var i = 0; i < logicalChildrenCount; i++)
                {
                    if (logicalChildren[i] is StyledElement child)
                    {
                        child.OnDetachedFromLogicalTreeCore(e);
                    }
                }

#if DEBUG
                if (((INotifyCollectionChangedDebug)Classes).GetCollectionChangedSubscribers()?.Length > 0)
                {
                    Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                        this,
                        "{Type} detached from logical tree but still has class listeners",
                        GetType());
                }
#endif
            }
        }

        private void OnDataContextChangedCore(AvaloniaPropertyChangedEventArgs e)
        {
            OnDataContextChanged(EventArgs.Empty);
        }

        private void SetLogicalParent(IList children)
        {
            var count = children.Count;

            for (var i = 0; i < count; i++)
            {
                var logical = (ILogical) children[i]!;
                
                if (logical.LogicalParent is null)
                {
                    ((ISetLogicalParent)logical).SetParent(this);
                }
            }
        }

        private void ClearLogicalParent(IList children)
        {
            var count = children.Count;

            for (var i = 0; i < count; i++)
            {
                var logical = (ILogical) children[i]!;
                
                if (logical.LogicalParent == this)
                {
                    ((ISetLogicalParent)logical).SetParent(null);
                }
            }
        }

        private void DetachStyles(IReadOnlyList<Style> styles)
        {
            var values = GetValueStore();
            values.BeginStyling();
            try { values.RemoveFrames(styles); }
            finally { values.EndStyling(); }

            if (_logicalChildren is not null)
            {
                var childCount = _logicalChildren.Count;

                for (var i = 0; i < childCount; ++i)
                {
                    (_logicalChildren[i] as StyledElement)?.DetachStyles(styles);
                }
            }
        }

        private void NotifyResourcesChanged(
            ResourcesChangedEventArgs? e = null,
            bool propagate = true)
        {
            if (ResourcesChanged is object)
            {
                e ??= ResourcesChangedEventArgs.Empty;
                ResourcesChanged(this, e);
            }

            if (propagate)
            {
                e ??= ResourcesChangedEventArgs.Empty;
                NotifyChildResourcesChanged(e);
            }
        }

        internal override void BuildDebugDisplay(StringBuilder builder, bool includeContent)
        {
            base.BuildDebugDisplay(builder, includeContent);
            DebugDisplayHelper.AppendOptionalValue(builder, nameof(Name), Name, includeContent);
        }

        private static IReadOnlyList<Style>? FlattenStyles(IReadOnlyList<IStyle> styles)
        {
            List<Style>? result = null;

            static void FlattenStyle(IStyle style, ref List<Style>? result)
            {
                if (style is Style s)
                    (result ??= new()).Add(s);
                FlattenStyles(style.Children, ref result);
            }

            static void FlattenStyles(IReadOnlyList<IStyle> styles, ref List<Style>? result)
            {
                var count = styles.Count;
                for (var i = 0; i < count; ++i)
                    FlattenStyle(styles[i], ref result);
            }

            FlattenStyles(styles, ref result);
            return result;
        }

        private static bool HasSettersOrAnimations(IReadOnlyList<IStyle> styles)
        {
            static bool StyleHasSettersOrAnimations(IStyle style)
            {
                if (style is StyleBase s && s.HasSettersOrAnimations)
                    return true;
                return HasSettersOrAnimations(style.Children);
            }

            var count = styles.Count;

            for (var i = 0; i < count; ++i)
            {
                if (StyleHasSettersOrAnimations(styles[i]))
                    return true;
            }

            return false;
        }

        private static IReadOnlyList<StyleBase> RecurseStyles(IReadOnlyList<IStyle> styles)
        {
            var result = new List<StyleBase>();
            RecurseStyles(styles, result);
            return result;
        }

        private static void RecurseStyles(IReadOnlyList<IStyle> styles, List<StyleBase> result)
        {
            var count = styles.Count;

            for (var i = 0; i < count; ++i)
            {
                var s = styles[i];
                if (s is StyleBase style)
                    result.Add(style);
                else if (s is IReadOnlyList<IStyle> children)
                    RecurseStyles(children, result);
            }
        }
    }
}
