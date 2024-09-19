

#nullable enable

using System;
using System.Collections;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Utilities;
using Avalonia.VisualTree;
namespace Avalonia
{
    /// <summary>
    /// Base class for controls that provides rendering and related visual properties.
    /// </summary>
    /// <remarks>
    /// The <see cref="Visual"/> class represents elements that have a visual on-screen
    /// representation and stores all the information needed for an 
    /// <see cref="IRenderer"/> to render the control. To traverse the visual tree, use the
    /// extension methods defined in <see cref="VisualExtensions"/>.
    /// </remarks>
    [UsableDuringInitialization]
    public partial class Visual : StyledElement, IAvaloniaListItemValidator<Visual>
    {
        internal static int RootedVisualChildrenCount { get; private set; }

        /// <summary>
        /// Defines the <see cref="Bounds"/> property.
        /// </summary>
        public static readonly DirectProperty<Visual, Rect> BoundsProperty =
            AvaloniaProperty.RegisterDirect<Visual, Rect>(nameof(Bounds), o => o.Bounds);
        
        /// <summary>
        /// Defines the <see cref="ClipToBounds"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ClipToBoundsProperty =
            AvaloniaProperty.Register<Visual, bool>(nameof(ClipToBounds));

        /// <summary>
        /// Defines the <see cref="Clip"/> property.
        /// </summary>
        public static readonly StyledProperty<Geometry?> ClipProperty =
            AvaloniaProperty.Register<Visual, Geometry?>(nameof(Clip));
        
        /// <summary>
        /// Defines the <see cref="IsVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsVisibleProperty =
            AvaloniaProperty.Register<Visual, bool>(nameof(IsVisible), true);

        /// <summary>
        /// Defines the <see cref="Opacity"/> property.
        /// </summary>
        public static readonly StyledProperty<double> OpacityProperty =
            AvaloniaProperty.Register<Visual, double>(nameof(Opacity), 1);

        /// <summary>
        /// Defines the <see cref="OpacityMask"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> OpacityMaskProperty =
            AvaloniaProperty.Register<Visual, IBrush?>(nameof(OpacityMask));
        
        /// <summary>
        /// Defines the <see cref="Effect"/> property.
        /// </summary>
        public static readonly StyledProperty<IEffect?> EffectProperty =
            AvaloniaProperty.Register<Visual, IEffect?>(nameof(Effect));

        /// <summary>
        /// Defines the <see cref="HasMirrorTransform"/> property.
        /// </summary>
        public static readonly DirectProperty<Visual, bool> HasMirrorTransformProperty =
            AvaloniaProperty.RegisterDirect<Visual, bool>(nameof(HasMirrorTransform), o => o.HasMirrorTransform);

        /// <summary>
        /// Defines the <see cref="RenderTransform"/> property.
        /// </summary>
        public static readonly StyledProperty<ITransform?> RenderTransformProperty =
            AvaloniaProperty.Register<Visual, ITransform?>(nameof(RenderTransform));

        /// <summary>
        /// Defines the <see cref="RenderTransformOrigin"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativePoint> RenderTransformOriginProperty =
            AvaloniaProperty.Register<Visual, RelativePoint>(nameof(RenderTransformOrigin), defaultValue: RelativePoint.Center);

        /// <summary>
        /// Defines the <see cref="FlowDirection"/> property.
        /// </summary>
        public static readonly AttachedProperty<FlowDirection> FlowDirectionProperty =
            AvaloniaProperty.RegisterAttached<Visual, Visual, FlowDirection>(
                nameof(FlowDirection),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="VisualParent"/> property.
        /// </summary>
        public static readonly DirectProperty<Visual, Visual?> VisualParentProperty =
            AvaloniaProperty.RegisterDirect<Visual, Visual?>(nameof(VisualParent), o => o._visualParent);

        /// <summary>
        /// Defines the <see cref="ZIndex"/> property.
        /// </summary>
        public static readonly StyledProperty<int> ZIndexProperty =
            AvaloniaProperty.Register<Visual, int>(nameof(ZIndex));
        
        private static readonly WeakEvent<IAffectsRender, EventArgs> InvalidatedWeakEvent =
            WeakEvent.Register<IAffectsRender>(
                (s, h) => s.Invalidated += h,
                (s, h) => s.Invalidated -= h);

        private Rect _bounds;
        private IRenderRoot? _visualRoot;
        private Visual? _visualParent;
        private bool _hasMirrorTransform;
        private TargetWeakEventSubscriber<Visual, EventArgs>? _affectsRenderWeakSubscriber;

        /// <summary>
        /// Initializes static members of the <see cref="Visual"/> class.
        /// </summary>
        static Visual()
        {
            AffectsRender<Visual>(
                BoundsProperty,
                ClipProperty,
                ClipToBoundsProperty,
                IsVisibleProperty,
                OpacityProperty,
                OpacityMaskProperty,
                EffectProperty,
                HasMirrorTransformProperty);
            RenderTransformProperty.Changed.Subscribe(RenderTransformChanged);
            ZIndexProperty.Changed.Subscribe(ZIndexChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Visual"/> class.
        /// </summary>
        public Visual()
        {
            // Disable transitions until we're added to the visual tree.
            DisableTransitions();

            var visualChildren = new AvaloniaList<Visual>();
            visualChildren.ResetBehavior = ResetBehavior.Remove;
            visualChildren.Validator = this;
            visualChildren.CollectionChanged += VisualChildrenChanged;
            VisualChildren = visualChildren;
        }

        /// <summary>
        /// Raised when the control is attached to a rooted visual tree.
        /// </summary>
        public event EventHandler<VisualTreeAttachmentEventArgs>? AttachedToVisualTree;

        /// <summary>
        /// Raised when the control is detached from a rooted visual tree.
        /// </summary>
        public event EventHandler<VisualTreeAttachmentEventArgs>? DetachedFromVisualTree;

        /// <summary>
        /// Gets the bounds of the control relative to its parent.
        /// </summary>
        public Rect Bounds
        {
            get { return _bounds; }
            protected set { SetAndRaise(BoundsProperty, ref _bounds, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the control should be clipped to its bounds.
        /// </summary>
        public bool ClipToBounds
        {
            get { return GetValue(ClipToBoundsProperty); }
            set { SetValue(ClipToBoundsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the geometry clip for this visual.
        /// </summary>
        public Geometry? Clip
        {
            get { return GetValue(ClipProperty); }
            set { SetValue(ClipProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether this control and all its parents are visible.
        /// </summary>
        public bool IsEffectivelyVisible { get; private set; } = true;
        
        /// <summary>
        /// Updates the <see cref="IsEffectivelyVisible"/> property based on the parent's
        /// <see cref="IsEffectivelyVisible"/>.
        /// </summary>
        /// <param name="parentState">The effective visibility of the parent control.</param>
        private void UpdateIsEffectivelyVisible(bool parentState)
        {
            var isEffectivelyVisible = parentState && IsVisible;

            if (IsEffectivelyVisible == isEffectivelyVisible)
                return;

            IsEffectivelyVisible = isEffectivelyVisible;

            // PERF-SENSITIVE: This is called on entire hierarchy and using foreach or LINQ
            // will cause extra allocations and overhead.
            
            var children = VisualChildren;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < children.Count; ++i)
            {
                var child = children[i];
                child.UpdateIsEffectivelyVisible(isEffectivelyVisible);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this control is visible.
        /// </summary>
        public bool IsVisible
        {
            get { return GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the opacity of the control.
        /// </summary>
        public double Opacity
        {
            get { return GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }

        /// <summary>
        /// Gets or sets the opacity mask of the control.
        /// </summary>
        public IBrush? OpacityMask
        {
            get { return GetValue(OpacityMaskProperty); }
            set { SetValue(OpacityMaskProperty, value); }
        }
        
        /// <summary>
        /// Gets or sets the effect of the control.
        /// </summary>
        public IEffect? Effect
        {
            get => GetValue(EffectProperty);
            set => SetValue(EffectProperty, value);
        }


        /// <summary>
        /// Gets or sets a value indicating whether to apply mirror transform on this control.
        /// </summary>
        public bool HasMirrorTransform 
        { 
            get { return _hasMirrorTransform; }
            protected set { SetAndRaise(HasMirrorTransformProperty, ref _hasMirrorTransform, value); }
        }

        /// <summary>
        /// Gets or sets the render transform of the control.
        /// </summary>
        public ITransform? RenderTransform
        {
            get { return GetValue(RenderTransformProperty); }
            set { SetValue(RenderTransformProperty, value); }
        }

        /// <summary>
        /// Gets or sets the transform origin of the control.
        /// </summary>
        public RelativePoint RenderTransformOrigin
        {
            get { return GetValue(RenderTransformOriginProperty); }
            set { SetValue(RenderTransformOriginProperty, value); }
        }

        /// <summary>
        /// Gets or sets the text flow direction.
        /// </summary>
        public FlowDirection FlowDirection
        {
            get => GetValue(FlowDirectionProperty);
            set => SetValue(FlowDirectionProperty, value);
        }

        /// <summary>
        /// Gets or sets the Z index of the control.
        /// </summary>
        /// <remarks>
        /// Controls with a higher <see cref="ZIndex"/> will appear in front of controls with
        /// a lower ZIndex. If two controls have the same ZIndex then the control that appears
        /// later in the containing element's children collection will appear on top.
        /// </remarks>
        public int ZIndex
        {
            get { return GetValue(ZIndexProperty); }
            set { SetValue(ZIndexProperty, value); }
        }

        /// <summary>
        /// Gets the control's child visuals.
        /// </summary>
        protected internal IAvaloniaList<Visual> VisualChildren
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the root of the visual tree, if the control is attached to a visual tree.
        /// </summary>
        protected internal IRenderRoot? VisualRoot => _visualRoot ?? (this as IRenderRoot);

        internal RenderOptions RenderOptions { get; set; }

        internal bool HasNonUniformZIndexChildren { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this control is attached to a visual root.
        /// </summary>
        internal bool IsAttachedToVisualTree => VisualRoot != null;

        /// <summary>
        /// Gets the control's parent visual.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1032", Justification = "GetVisualParent extension method is supposed to be used instead.")]
        internal Visual? VisualParent => _visualParent;

        /// <summary>
        /// Gets a value indicating whether control bypass FlowDirecton policies.
        /// </summary>
        /// <remarks>
        /// Related to FlowDirection system and returns false as default, so if 
        /// <see cref="FlowDirection"/> is RTL then control will get a mirror presentation. 
        /// For controls that want to avoid this behavior, override this property and return true.
        /// </remarks>
        protected virtual bool BypassFlowDirectionPolicies => false;

        /// <summary>
        /// Gets the value of the attached <see cref="FlowDirectionProperty"/> on a control.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <returns>The flow direction.</returns>
        public static FlowDirection GetFlowDirection(Visual visual)
        {
            return visual.GetValue(FlowDirectionProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FlowDirectionProperty"/> on a control.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFlowDirection(Visual visual, FlowDirection value)
        {
            visual.SetValue(FlowDirectionProperty, value);
        }

        /// <summary>
        /// Invalidates the visual and queues a repaint.
        /// </summary>
        public void InvalidateVisual()
        {
            VisualRoot?.Renderer.AddDirty(this);
        }

        /// <summary>
        /// Renders the visual to a <see cref="DrawingContext"/>.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public virtual void Render(DrawingContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }
        }

        /// <summary>
        /// Indicates that a property change should cause <see cref="InvalidateVisual"/> to be
        /// called.
        /// </summary>
        /// <typeparam name="T">The control which the property affects.</typeparam>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// This method should be called in a control's static constructor with each property
        /// on the control which when changed should cause a redraw. This is similar to WPF's
        /// FrameworkPropertyMetadata.AffectsRender flag.
        /// </remarks>
        protected static void AffectsRender<T>(params AvaloniaProperty[] properties)
            where T : Visual
        {
            var invalidateObserver = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
                static e =>
                {
                    if (e.Sender is T sender)
                    {
                        sender.InvalidateVisual();
                    }
                });
            
            
            var invalidateAndSubscribeObserver = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
                static e =>
                {
                    if (e.Sender is T sender)
                    {
                        if (e.OldValue is IAffectsRender oldValue)
                        {
                            if (sender._affectsRenderWeakSubscriber != null)
                            {
                                InvalidatedWeakEvent.Unsubscribe(oldValue, sender._affectsRenderWeakSubscriber);
                            }
                        }

                        if (e.NewValue is IAffectsRender newValue)
                        {
                            if (sender._affectsRenderWeakSubscriber == null)
                            {
                                sender._affectsRenderWeakSubscriber = new TargetWeakEventSubscriber<Visual, EventArgs>(
                                    sender, static (target, _, _, _) =>
                                    {
                                        target.InvalidateVisual();
                                    });
                            }
                            InvalidatedWeakEvent.Subscribe(newValue, sender._affectsRenderWeakSubscriber);
                        }

                        sender.InvalidateVisual();
                    }
                });

            foreach (var property in properties)
            {
                if (property.CanValueAffectRender())
                {
                    property.Changed.Subscribe(invalidateAndSubscribeObserver);
                }
                else
                {
                    property.Changed.Subscribe(invalidateObserver);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsVisibleProperty)
            {
                UpdateIsEffectivelyVisible(VisualParent?.IsEffectivelyVisible ?? true);
            } 
            else if (change.Property == FlowDirectionProperty)
            {
                InvalidateMirrorTransform();

                foreach (var child in VisualChildren)
                {
                    child.InvalidateMirrorTransform();
                }
            }
        }
 
        protected override void LogicalChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            base.LogicalChildrenCollectionChanged(sender, e);
            VisualRoot?.Renderer.RecalculateChildren(this);
        }

        /// <summary>
        /// Calls the <see cref="OnAttachedToVisualTree(VisualTreeAttachmentEventArgs)"/> method 
        /// for this control and all of its visual descendants.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnAttachedToVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.Visual)?.Log(this, "Attached to visual tree");

            _visualRoot = e.Root;
            RootedVisualChildrenCount++;
            if (_visualParent is null)
            {
                throw new InvalidOperationException("Visual was attached to the root without being added to the visual parent first.");
            }

            if (RenderTransform is IMutableTransform mutableTransform)
            {
                mutableTransform.Changed += RenderTransformChanged;
            }

            EnableTransitions();
            if (_visualRoot.Renderer is IRendererWithCompositor compositingRenderer)
            {
                AttachToCompositor(compositingRenderer.Compositor);
            }
            InvalidateMirrorTransform();
            UpdateIsEffectivelyVisible(_visualParent.IsEffectivelyVisible);
            OnAttachedToVisualTree(e);
            AttachedToVisualTree?.Invoke(this, e);
            InvalidateVisual();
            
            _visualRoot.Renderer.RecalculateChildren(_visualParent);
            
            if (ZIndex != 0)
                _visualParent.HasNonUniformZIndexChildren = true;
            
            var visualChildren = VisualChildren;
            var visualChildrenCount = visualChildren.Count;

            for (var i = 0; i < visualChildrenCount; i++)
            {
                if (visualChildren[i] is { } child && child._visualRoot != e.Root) // child may already have been attached within an event handler
                {
                    child.OnAttachedToVisualTreeCore(e);
                }
            }
        }

        /// <summary>
        /// Calls the <see cref="OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs)"/> method 
        /// for this control and all of its visual descendants.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnDetachedFromVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.Visual)?.Log(this, "Detached from visual tree");

            _visualRoot = null;
            RootedVisualChildrenCount--;

            if (RenderTransform is IMutableTransform mutableTransform)
            {
                mutableTransform.Changed -= RenderTransformChanged;
            }

            DisableTransitions();
            UpdateIsEffectivelyVisible(true);
            OnDetachedFromVisualTree(e);
            DetachFromCompositor();

            DetachedFromVisualTree?.Invoke(this, e);
            e.Root.Renderer.AddDirty(this);

            var visualChildren = VisualChildren;
            var visualChildrenCount = visualChildren.Count;

            for (var i = 0; i < visualChildrenCount; i++)
            {
                if (visualChildren[i] is { } child)
                {
                    child.OnDetachedFromVisualTreeCore(e);
                }
            }
        }

        /// <summary>
        /// Called when the control is added to a rooted visual tree.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
        }

        /// <summary>
        /// Called when the control is removed from a rooted visual tree.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
        }

        /// <summary>
        /// Called when the control's visual parent changes.
        /// </summary>
        /// <param name="oldParent">The old visual parent.</param>
        /// <param name="newParent">The new visual parent.</param>
        protected virtual void OnVisualParentChanged(Visual? oldParent, Visual? newParent)
        {
            RaisePropertyChanged(VisualParentProperty, oldParent, newParent);
        }

        /// <summary>
        /// Called when a visual's <see cref="RenderTransform"/> changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void RenderTransformChanged(AvaloniaPropertyChangedEventArgs<ITransform?> e)
        {
            var sender = e.Sender as Visual;

            if (sender?.VisualRoot != null)
            {
                var (oldValue, newValue) = e.GetOldAndNewValue<ITransform?>();

                if (oldValue is Transform oldTransform)
                {
                    oldTransform.Changed -= sender.RenderTransformChanged;
                }

                if (newValue is Transform newTransform)
                {
                    newTransform.Changed += sender.RenderTransformChanged;
                }
                
                sender.InvalidateVisual();
            }
        }

        /// <summary>
        /// Ensures a visual child is not null and not already parented.
        /// </summary>
        /// <param name="item">The visual child.</param>
        void IAvaloniaListItemValidator<Visual>.Validate(Visual item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item), $"Cannot add null to {nameof(VisualChildren)}.");
            }

            if (item.VisualParent is { } parent)
            {
                throw new InvalidOperationException(
                    $"The control {item.DebugDisplay} already has a visual parent {parent.GetDebugDisplay(false)} " +
                    $"while trying to add it as a child of {GetDebugDisplay(false)}.");
            }
        }

        /// <summary>
        /// Called when the <see cref="ZIndex"/> property changes on any control.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void ZIndexChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var sender = e.Sender as Visual;
            var parent = sender?.VisualParent;
            if (sender?.ZIndex != 0 && parent is Visual parentVisual)
                parentVisual.HasNonUniformZIndexChildren = true;
            
            sender?.InvalidateVisual();
            parent?.VisualRoot?.Renderer.RecalculateChildren(parent);
        }

        /// <summary>
        /// Called when the <see cref="RenderTransform"/>'s <see cref="Transform.Changed"/> event
        /// is fired.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void RenderTransformChanged(object? sender, EventArgs e)
        {
            InvalidateVisual();
        }

        /// <summary>
        /// Sets the visual parent of the Visual.
        /// </summary>
        /// <param name="value">The visual parent.</param>
        private void SetVisualParent(Visual? value)
        {
            if (_visualParent == value)
            {
                return;
            }

            var old = _visualParent;
            _visualParent = value;

            if (_visualRoot != null)
            {
                var e = new VisualTreeAttachmentEventArgs(old!, _visualRoot);
                OnDetachedFromVisualTreeCore(e);
            }

            if (_visualParent is IRenderRoot || _visualParent?.IsAttachedToVisualTree == true)
            {
                var root = this.FindAncestorOfType<IRenderRoot>() ??
                    throw new AvaloniaInternalException("Visual is atached to visual tree but root could not be found.");
                var e = new VisualTreeAttachmentEventArgs(_visualParent, root);
                OnAttachedToVisualTreeCore(e);
            }

            OnVisualParentChanged(old, value);
        }

        /// <summary>
        /// Called when the <see cref="VisualChildren"/> collection changes.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void VisualChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    SetVisualParent(e.NewItems!, this);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    SetVisualParent(e.OldItems!, null);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    SetVisualParent(e.OldItems!, null);
                    SetVisualParent(e.NewItems!, this);
                    break;
            }
        }
        
        private static void SetVisualParent(IList children, Visual? parent)
        {
            var count = children.Count;

            for (var i = 0; i < count; i++)
            {
                var visual = (Visual) children[i]!;
                
                visual.SetVisualParent(parent);
            }
        }

        internal override void OnTemplatedParentControlThemeChanged()
        {
            base.OnTemplatedParentControlThemeChanged();

            var count = VisualChildren.Count;
            var templatedParent = TemplatedParent;

            for (var i = 0; i < count; ++i)
            {
                if (VisualChildren[i] is StyledElement child &&
                    child.TemplatedParent == templatedParent)
                {
                    child.OnTemplatedParentControlThemeChanged();
                }
            }
        }

        /// <summary>
        /// Computes the <see cref="HasMirrorTransform"/> value according to the 
        /// <see cref="FlowDirection"/> and <see cref="BypassFlowDirectionPolicies"/>
        /// </summary>
        protected internal virtual void InvalidateMirrorTransform()
        {
            var flowDirection = this.FlowDirection;
            var parentFlowDirection = FlowDirection.LeftToRight;

            bool bypassFlowDirectionPolicies = BypassFlowDirectionPolicies;
            bool parentBypassFlowDirectionPolicies = false;

            var parent = VisualParent;
            if (parent != null)
            {
                parentFlowDirection = parent.FlowDirection;
                parentBypassFlowDirectionPolicies = parent.BypassFlowDirectionPolicies;
            }

            bool thisShouldBeMirrored = flowDirection == FlowDirection.RightToLeft && !bypassFlowDirectionPolicies;
            bool parentShouldBeMirrored = parentFlowDirection == FlowDirection.RightToLeft && !parentBypassFlowDirectionPolicies;

            bool shouldApplyMirrorTransform = thisShouldBeMirrored != parentShouldBeMirrored;

            HasMirrorTransform = shouldApplyMirrorTransform;
        }
    }
}
