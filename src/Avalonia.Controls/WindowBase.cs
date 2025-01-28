using System;
using System.ComponentModel;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for top-level windows.
    /// </summary>
    /// <remarks>
    /// This class acts as a base for top level windows such as <see cref="Window"/> and
    /// <see cref="PopupRoot"/>. It handles scheduling layout, styling and rendering as well as
    /// tracking the window <see cref="TopLevel.ClientSize"/> and <see cref="IsActive"/> state.
    /// </remarks>
    public class WindowBase : TopLevel
    {
        /// <summary>
        /// Defines the <see cref="IsActive"/> property.
        /// </summary>
        public static readonly DirectProperty<WindowBase, bool> IsActiveProperty =
            AvaloniaProperty.RegisterDirect<WindowBase, bool>(nameof(IsActive), o => o.IsActive);

        /// <summary>
        /// Defines the <see cref="Owner"/> property.
        /// </summary>
        public static readonly DirectProperty<WindowBase, WindowBase?> OwnerProperty =
            AvaloniaProperty.RegisterDirect<WindowBase, WindowBase?>(nameof(Owner), o => o.Owner);

        public static readonly StyledProperty<bool> TopmostProperty =
            AvaloniaProperty.Register<WindowBase, bool>(nameof(Topmost));

        private bool _hasExecutedInitialLayoutPass;
        private bool _isActive;
        private int _ignoreVisibilityChanges;
        private WindowBase? _owner;
        
        protected bool IgnoreVisibilityChanges => _ignoreVisibilityChanges > 0;

        static WindowBase()
        {
            IsVisibleProperty.OverrideDefaultValue<WindowBase>(false);
        }

        public WindowBase(IWindowBaseImpl impl) : this(impl, AvaloniaLocator.Current)
        {
            CreatePlatformImplBinding(TopmostProperty, topmost => PlatformImpl!.SetTopmost(topmost));
            
            FrameSize = impl.FrameSize;
        }

        public WindowBase(IWindowBaseImpl impl, IAvaloniaDependencyResolver? dependencyResolver) : base(impl, dependencyResolver)
        {
            impl.Activated = HandleActivated;
            impl.Deactivated = HandleDeactivated;
            impl.PositionChanged = HandlePositionChanged;
        }

        private protected IDisposable FreezeVisibilityChangeHandling()
        {
            return new IgnoreVisibilityChangesDisposable(this);
        }

        /// <summary>
        /// Fired when the window is activated.
        /// </summary>
        public event EventHandler? Activated;

        /// <summary>
        /// Fired when the window is deactivated.
        /// </summary>
        public event EventHandler? Deactivated;

        /// <summary>
        /// Fired when the window position is changed.
        /// </summary>
        public event EventHandler<PixelPointEventArgs>? PositionChanged;

        /// <summary>
        /// Occurs when the window is resized.
        /// </summary>
        /// <remarks>
        /// Although this event is similar to the <see cref="Control.SizeChanged"/> event, they are
        /// conceptually different:
        /// 
        /// - <see cref="Resized"/> is a window-level event, fired when a resize notification arrives
        ///   from the platform windowing subsystem. The event args contain details of the source of
        ///   the resize event in the <see cref="WindowResizedEventArgs.Reason"/> property. This
        ///   event is raised before layout has been run on the window's content.
        /// - <see cref="Control.SizeChanged"/> is a layout-level event, fired when a layout pass
        ///   completes on a control. <see cref="Control.SizeChanged"/> is present on all controls
        ///   and is fired when the control's size changes for any reason, including a
        ///   <see cref="Resized"/> event in the case of a Window.
        /// </remarks>
        public event EventHandler<WindowResizedEventArgs>? Resized;

        public new IWindowBaseImpl? PlatformImpl => (IWindowBaseImpl?) base.PlatformImpl;

        /// <summary>
        /// Gets a value that indicates whether the window is active.
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            private set => SetAndRaise(IsActiveProperty, ref _isActive, value);
        }

        /// <inheritdoc cref="TopLevel.Screens"/>
        public new Screens Screens => base.Screens
            ?? throw new InvalidOperationException("Windowing backend wasn't properly initialized.");

        /// <summary>
        /// Gets or sets the owner of the window.
        /// </summary>
        public WindowBase? Owner
        {
            get => _owner;
            protected set => SetAndRaise(OwnerProperty, ref _owner, value);
        }

        /// <summary>
        /// Gets or sets whether this window appears on top of all other windows
        /// </summary>
        public bool Topmost
        {
            get => GetValue(TopmostProperty);
            set => SetValue(TopmostProperty, value);
        }

        /// <summary>
        /// Gets the scaling factor for Window positioning and sizing.
        /// </summary>
        public double DesktopScaling => DesktopScalingOverride ?? PlatformImpl?.DesktopScaling ?? 1;

        private protected double? DesktopScalingOverride { get; set; }
        
        /// <summary>
        /// Activates the window.
        /// </summary>
        public void Activate()
        {
            PlatformImpl?.Activate();
        }

        /// <summary>
        /// Hides the popup.
        /// </summary>
        public virtual void Hide()
        {
            using (FreezeVisibilityChangeHandling())
            {
                StopRendering();
                PlatformImpl?.Hide();
                IsVisible = false;
            }
        }

        /// <summary>
        /// Shows the window.
        /// </summary>
        public virtual void Show()
        {
            using (FreezeVisibilityChangeHandling())
            {
                EnsureInitialized();
                ApplyStyling();
                IsVisible = true;

                if (!_hasExecutedInitialLayoutPass)
                {
                    LayoutManager.ExecuteInitialLayoutPass();
                    _hasExecutedInitialLayoutPass = true;
                }

                PlatformImpl?.Show(true, false);
                StartRendering();
                OnOpened(EventArgs.Empty);
            }
        }


        /// <summary>
        /// Ensures that the window is initialized.
        /// </summary>
        protected void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                var init = (ISupportInitialize)this;
                init.BeginInit();
                init.EndInit();
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsVisibleProperty)
            {
                IsVisibleChanged(change);
            }
        }

        /// <inheritdoc/>
        protected override void OnClosed(EventArgs e)
        {
            // Window must manually raise Loaded/Unloaded events as it is a visual root and
            // does not raise OnAttachedToVisualTreeCore/OnDetachedFromVisualTreeCore events
            OnUnloadedCore();

            base.OnClosed(e);
        }

        /// <inheritdoc/>
        protected override void OnOpened(EventArgs e)
        {
            FrameSize = PlatformImpl?.FrameSize;
            
            // Window must manually raise Loaded/Unloaded events as it is a visual root and
            // does not raise OnAttachedToVisualTreeCore/OnDetachedFromVisualTreeCore events
            ScheduleOnLoadedCore();

            base.OnOpened(e);
        }

        /// <summary>
        /// Raises the <see cref="Resized"/> event.
        /// </summary>
        /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
        protected virtual void OnResized(WindowResizedEventArgs e) => Resized?.Invoke(this, e);

        private protected override void HandleClosed()
        {
            using (FreezeVisibilityChangeHandling())
            {
                IsVisible = false;

                if (this is IFocusScope scope)
                {
                    ((FocusManager?)FocusManager)?.RemoveFocusRoot(scope);
                }

                base.HandleClosed();
            }
        }

        /// <summary>
        /// Handles a resize notification from <see cref="ITopLevelImpl.Resized"/>.
        /// </summary>
        /// <param name="clientSize">The new client size.</param>
        /// <param name="reason">The reason for the resize.</param>
        internal override void HandleResized(Size clientSize, WindowResizeReason reason)
        {
            FrameSize = PlatformImpl?.FrameSize;

            var clientSizeChanged = ClientSize != clientSize;

            ClientSize = clientSize;
            OnResized(new WindowResizedEventArgs(clientSize, reason));

            if (clientSizeChanged)
            {
                LayoutManager.ExecuteLayoutPass();
                Renderer.Resized(clientSize);
            }
        }

        /// <summary>
        /// Overrides the core measure logic for windows.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The measured size.</returns>
        /// <remarks>
        /// The layout logic for top-level windows is different than for other controls because
        /// they don't have a parent, meaning that many layout properties handled by the default
        /// MeasureCore (such as margins and alignment) make no sense.
        /// </remarks>
        protected override Size MeasureCore(Size availableSize)
        {
            ApplyStyling();
            ApplyTemplate();

            var constraint = LayoutHelper.ApplyLayoutConstraints(this, availableSize);

            Avalonia.Styling.Container.GetQueryProvider(this)?.SetSize(constraint.Width, constraint.Height, Avalonia.Styling.Container.GetSizing(this));

            return MeasureOverride(constraint);
        }

        /// <summary>
        /// Overrides the core arrange logic for windows.
        /// </summary>
        /// <param name="finalRect">The final arrange rect.</param>
        /// <remarks>
        /// The layout logic for top-level windows is different than for other controls because
        /// they don't have a parent, meaning that many layout properties handled by the default
        /// ArrangeCore (such as margins and alignment) make no sense.
        /// </remarks>
        protected override void ArrangeCore(Rect finalRect)
        {
            var constraint = ArrangeSetBounds(finalRect.Size);
            var arrangeSize = ArrangeOverride(constraint);
            Bounds = new Rect(arrangeSize);
        }

        /// <summary>
        /// Called during the arrange pass to set the size of the window.
        /// </summary>
        /// <param name="size">The requested size of the window.</param>
        /// <returns>The actual size of the window.</returns>
        protected virtual Size ArrangeSetBounds(Size size) => size;

        /// <summary>
        /// Handles a window position change notification from 
        /// <see cref="IWindowBaseImpl.PositionChanged"/>.
        /// </summary>
        /// <param name="pos">The window position.</param>
        private void HandlePositionChanged(PixelPoint pos)
        {
            PositionChanged?.Invoke(this, new PixelPointEventArgs(pos));
        }

        /// <summary>
        /// Handles an activated notification from <see cref="IWindowBaseImpl.Activated"/>.
        /// </summary>
        private void HandleActivated()
        {
            Activated?.Invoke(this, EventArgs.Empty);

            var scope = this as IFocusScope;

            if (scope != null)
            {
                ((FocusManager?)FocusManager)?.SetFocusScope(scope);
            }

            IsActive = true;
        }

        /// <summary>
        /// Handles a deactivated notification from <see cref="IWindowBaseImpl.Deactivated"/>.
        /// </summary>
        private void HandleDeactivated()
        {
            IsActive = false;

            Deactivated?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void IsVisibleChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_ignoreVisibilityChanges == 0)
            {
                if ((bool)e.NewValue!)
                {
                    Show();
                }
                else
                {
                    Hide();
                }
            }
        }
        
        private readonly struct IgnoreVisibilityChangesDisposable : IDisposable
        {
            private readonly WindowBase _windowBase;

            public IgnoreVisibilityChangesDisposable(WindowBase windowBase)
            {
                _windowBase = windowBase;
                _windowBase._ignoreVisibilityChanges++;
            }
            
            public void Dispose()
            {
                _windowBase._ignoreVisibilityChanges--;
            }
        }
    }
}
