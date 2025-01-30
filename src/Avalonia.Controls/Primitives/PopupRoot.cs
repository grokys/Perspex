using System;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Diagnostics;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// The root window of a <see cref="Popup"/>.
    /// </summary>
    public sealed class PopupRoot : WindowBase, IHostedVisualTreeRoot, IDisposable, IStyleHost, IPopupHost
    {
        /// <summary>
        /// Defines the <see cref="Transform"/> property.
        /// </summary>
        public static readonly StyledProperty<Transform?> TransformProperty =
            AvaloniaProperty.Register<PopupRoot, Transform?>(nameof(Transform));

        /// <summary>
        /// Defines the <see cref="WindowManagerAddShadowHint"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> WindowManagerAddShadowHintProperty =
            Popup.WindowManagerAddShadowHintProperty.AddOwner<PopupRoot>();

        private PopupPositionRequest? _popupPositionRequest;
        private Size _popupSize;
        private bool _needsUpdate;

        /// <summary>
        /// Initializes static members of the <see cref="PopupRoot"/> class.
        /// </summary>
        static PopupRoot()
        {
            BackgroundProperty.OverrideDefaultValue(typeof(PopupRoot), Brushes.White);            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupRoot"/> class.
        /// </summary>
        public PopupRoot(TopLevel parent, IPopupImpl impl)
            : this(parent, impl,null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupRoot"/> class.
        /// </summary>
        /// <param name="parent">The popup parent.</param>
        /// <param name="impl">The popup implementation.</param>
        /// <param name="dependencyResolver">
        /// The dependency resolver to use. If null the default dependency resolver will be used.
        /// </param>
        public PopupRoot(TopLevel parent, IPopupImpl impl, IAvaloniaDependencyResolver? dependencyResolver)
            : base(impl, dependencyResolver)
        {
            ParentTopLevel = parent;
            impl.SetWindowManagerAddShadowHint(WindowManagerAddShadowHint);
            impl.SetTopmost(Topmost);
        }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        public new IPopupImpl? PlatformImpl => (IPopupImpl?)base.PlatformImpl;               

        /// <summary>
        /// Gets or sets a transform that will be applied to the popup.
        /// </summary>
        public Transform? Transform
        {
            get => GetValue(TransformProperty);
            set => SetValue(TransformProperty, value);
        }

        /// <summary>
        /// Gets or sets a hint to the window manager that a shadow should be added to the popup.
        /// </summary>
        public bool WindowManagerAddShadowHint
        {
            get => GetValue(WindowManagerAddShadowHintProperty);
            set => SetValue(WindowManagerAddShadowHintProperty, value);
        }

        /// <summary>
        /// Gets the parent control in the event route.
        /// </summary>
        /// <remarks>
        /// Popup events are passed to their parent window. This facilitates this.
        /// </remarks>
        internal override Interactive? InteractiveParent => (Interactive?)Parent;

        /// <summary>
        /// Gets the control that is hosting the popup root.
        /// </summary>
        Visual? IHostedVisualTreeRoot.Host
        {
            get
            {
                // If the parent is attached to a visual tree, then return that. However the parent
                // will possibly be a standalone Popup (i.e. a Popup not attached to a visual tree,
                // created by e.g. a ContextMenu): if this is the case, return the ParentTopLevel
                // if set. This helps to allow the focus manager to restore the focus to the outer
                // scope when the popup is closed.
                var parentVisual = Parent as Visual;
                if (parentVisual?.IsAttachedToVisualTree == true)
                    return parentVisual;
                return ParentTopLevel ?? parentVisual;
            }
        }

        /// <summary>
        /// Gets the styling parent of the popup root.
        /// </summary>
        IStyleHost? IStyleHost.StylingParent => Parent;

        public TopLevel ParentTopLevel { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            PlatformImpl?.Dispose();
        }

        private void UpdatePosition()
        {
            if (_needsUpdate && _popupPositionRequest is not null)
            {
                _needsUpdate = false;
                PlatformImpl?.PopupPositioner?
                    .Update(ParentTopLevel, _popupPositionRequest, _popupSize, FlowDirection);
            }
        }

        [Unstable(ObsoletionMessages.MayBeRemovedInAvalonia12)]
        public void ConfigurePosition(Visual target, PlacementMode placement, Point offset,
            PopupAnchor anchor = PopupAnchor.None,
            PopupGravity gravity = PopupGravity.None,
            PopupPositionerConstraintAdjustment constraintAdjustment = PopupPositionerConstraintAdjustment.All,
            Rect? rect = null)
        {
            ((IPopupHost)this).ConfigurePosition(new PopupPositionRequest(target, placement, offset, anchor, gravity,
                constraintAdjustment, rect, null));
        }

        void IPopupHost.ConfigurePosition(PopupPositionRequest request)
        {
            _popupPositionRequest = request;
            _needsUpdate = true;
            UpdatePosition();
        }

        public void SetChild(Control? control) => Content = control;

        public void TakeFocus() => PlatformImpl?.TakeFocus();

        Visual IPopupHost.HostedVisualTreeRoot => this;
        
        protected override Size MeasureOverride(Size availableSize)
        {
            var maxAutoSize = PlatformImpl?.MaxAutoSizeHint ?? Size.Infinity;
            var constraint = availableSize;

            if (double.IsInfinity(constraint.Width))
            {
                constraint = constraint.WithWidth(maxAutoSize.Width);
            }

            if (double.IsInfinity(constraint.Height))
            {
                constraint = constraint.WithHeight(maxAutoSize.Height);
            }

            var measured = base.MeasureOverride(constraint);
            var width = measured.Width;
            var height = measured.Height;
            var widthCache = Width;
            var heightCache = Height;

            if (!double.IsNaN(widthCache))
            {
                width = widthCache;
            }

            width = Math.Min(width, MaxWidth);
            width = Math.Max(width, MinWidth);

            if (!double.IsNaN(heightCache))
            {
                height = heightCache;
            }

            height = Math.Min(height, MaxHeight);
            height = Math.Max(height, MinHeight);

            return new Size(width, height);
        }

        protected sealed override Size ArrangeSetBounds(Size size)
        {
            if (_popupSize != size)
            {
                _popupSize = size;
                _needsUpdate = true;
                UpdatePosition();
            }

            return ClientSize;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new PopupRootAutomationPeer(this);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == WindowManagerAddShadowHintProperty)
            {
                PlatformImpl?.SetWindowManagerAddShadowHint(change.GetNewValue<bool>());
            }
            else if (change.Property == TopmostProperty)
            {
                PlatformImpl?.SetTopmost(Topmost);
            }
        }
    }
}
