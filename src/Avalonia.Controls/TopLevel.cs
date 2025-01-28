using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Reactive;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.Utilities;
using Avalonia.Input.Platform;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for top-level widgets.
    /// </summary>
    /// <remarks>
    /// This class acts as a base for top level widget.
    /// It handles scheduling layout, styling and rendering as well as
    /// tracking the widget's <see cref="ClientSize"/>.
    /// </remarks>
    [TemplatePart("PART_TransparencyFallback", typeof(Border))]
    public abstract class TopLevel : ContentControl,
        IInputRoot,
        ILayoutRoot,
        IRenderRoot,
        ICloseable,
        IStyleHost,
        ILogicalRoot,
        ITextInputMethodRoot
    {
        /// <summary>
        /// Defines the <see cref="ClientSize"/> property.
        /// </summary>
        public static readonly DirectProperty<TopLevel, Size> ClientSizeProperty =
            AvaloniaProperty.RegisterDirect<TopLevel, Size>(nameof(ClientSize), o => o.ClientSize);

        /// <summary>
        /// Defines the <see cref="FrameSize"/> property.
        /// </summary>
        public static readonly DirectProperty<TopLevel, Size?> FrameSizeProperty =
            AvaloniaProperty.RegisterDirect<TopLevel, Size?>(nameof(FrameSize), o => o.FrameSize);

        /// <summary>
        /// Defines the <see cref="IInputRoot.PointerOverElement"/> property.
        /// </summary>
        public static readonly StyledProperty<IInputElement?> PointerOverElementProperty =
            AvaloniaProperty.Register<TopLevel, IInputElement?>(nameof(IInputRoot.PointerOverElement));

        /// <summary>
        /// Defines the <see cref="TransparencyLevelHint"/> property.
        /// </summary>
        public static readonly StyledProperty<IReadOnlyList<WindowTransparencyLevel>> TransparencyLevelHintProperty =
            AvaloniaProperty.Register<TopLevel, IReadOnlyList<WindowTransparencyLevel>>(nameof(TransparencyLevelHint), Array.Empty<WindowTransparencyLevel>());

        /// <summary>
        /// Defines the <see cref="ActualTransparencyLevel"/> property.
        /// </summary>
        public static readonly DirectProperty<TopLevel, WindowTransparencyLevel> ActualTransparencyLevelProperty =
            AvaloniaProperty.RegisterDirect<TopLevel, WindowTransparencyLevel>(nameof(ActualTransparencyLevel), 
                o => o.ActualTransparencyLevel, 
                unsetValue: WindowTransparencyLevel.None);        

        /// <summary>
        /// Defines the <see cref="TransparencyBackgroundFallback"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> TransparencyBackgroundFallbackProperty =
            AvaloniaProperty.Register<TopLevel, IBrush>(nameof(TransparencyBackgroundFallback), Brushes.White);

        /// <inheritdoc cref="ThemeVariantScope.ActualThemeVariantProperty" />
        public static readonly StyledProperty<ThemeVariant> ActualThemeVariantProperty =
            ThemeVariantScope.ActualThemeVariantProperty.AddOwner<TopLevel>();
        
        /// <inheritdoc cref="ThemeVariantScope.RequestedThemeVariantProperty" />
        public static readonly StyledProperty<ThemeVariant?> RequestedThemeVariantProperty =
            ThemeVariantScope.RequestedThemeVariantProperty.AddOwner<TopLevel>();

        /// <summary>
        /// Defines the SystemBarColor attached property.
        /// </summary>
        public static readonly AttachedProperty<SolidColorBrush?> SystemBarColorProperty =
            AvaloniaProperty.RegisterAttached<TopLevel, Control, SolidColorBrush?>(
                "SystemBarColor",
                inherits: true);

        /// <summary>
        /// Defines the AutoSafeAreaPadding attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> AutoSafeAreaPaddingProperty =
            AvaloniaProperty.RegisterAttached<TopLevel, Control, bool>(
                "AutoSafeAreaPadding",
                defaultValue: true);

        /// <summary>
        /// Defines the <see cref="BackRequested"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> BackRequestedEvent = 
            RoutedEvent.Register<TopLevel, RoutedEventArgs>(nameof(BackRequested), RoutingStrategies.Bubble);

        private static readonly WeakEvent<IResourceHost, ResourcesChangedEventArgs>
            ResourcesChangedWeakEvent = WeakEvent.Register<IResourceHost, ResourcesChangedEventArgs>(
                (s, h) => s.ResourcesChanged += h,
                (s, h) => s.ResourcesChanged -= h
            );

        private readonly IInputManager? _inputManager;
        private readonly IToolTipService? _tooltipService;
        private readonly IAccessKeyHandler? _accessKeyHandler;
        private readonly IKeyboardNavigationHandler? _keyboardNavigationHandler;
        private readonly IGlobalStyles? _globalStyles;
        private readonly IThemeVariantHost? _applicationThemeHost;
        private readonly PointerOverPreProcessor? _pointerOverPreProcessor;
        private readonly IDisposable? _pointerOverPreProcessorSubscription;
        private readonly IDisposable? _backGestureSubscription;
        private readonly Dictionary<AvaloniaProperty, Action> _platformImplBindings = new();
        private Size _clientSize;
        private Size? _frameSize;
        private WindowTransparencyLevel _actualTransparencyLevel;
        private ILayoutManager? _layoutManager;
        private Border? _transparencyFallbackBorder;
        private TargetWeakEventSubscriber<TopLevel, ResourcesChangedEventArgs>? _resourcesChangesSubscriber;
        private IStorageProvider? _storageProvider;
        private Screens? _screens;
        private LayoutDiagnosticBridge? _layoutDiagnosticBridge;
        
        /// <summary>
        /// Initializes static members of the <see cref="TopLevel"/> class.
        /// </summary>
        static TopLevel()
        {
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<TopLevel>(KeyboardNavigationMode.Cycle);
            Avalonia.Styling.Container.SizingProperty.OverrideDefaultValue<TopLevel>(ContainerSizing.WidthAndHeight);

            AffectsMeasure<TopLevel>(ClientSizeProperty);

            SystemBarColorProperty.Changed.AddClassHandler<Control>((view, e) =>
            {
                if (e.NewValue is SolidColorBrush colorBrush)
                {
                    if (view.Parent is TopLevel tl && tl.InsetsManager is { } insetsManager)
                    {
                        insetsManager.SystemBarColor = colorBrush.Color;
                    }

                    if (view is TopLevel topLevel && topLevel.InsetsManager is { } insets)
                    {
                        insets.SystemBarColor = colorBrush.Color;
                    }
                }
            });

            AutoSafeAreaPaddingProperty.Changed.AddClassHandler<Control>((view, e) =>
            {
                var topLevel = view as TopLevel ?? view.Parent as TopLevel;
                topLevel?.InvalidateChildInsetsPadding();
            });

            PointerOverElementProperty.Changed.AddClassHandler<TopLevel>((topLevel, e) =>
            {
                if (e.OldValue is InputElement oldInputElement)
                {
                    oldInputElement.PropertyChanged -= topLevel.PointerOverElementOnPropertyChanged;
                }

                if (e.NewValue is InputElement newInputElement)
                {
                    topLevel.PlatformImpl?.SetCursor(newInputElement.Cursor?.PlatformImpl);
                    newInputElement.PropertyChanged += topLevel.PointerOverElementOnPropertyChanged;
                }
            });

            ToolTip.ServiceEnabledProperty.Changed.Subscribe(OnToolTipServiceEnabledChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TopLevel"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific window implementation.</param>
        public TopLevel(ITopLevelImpl impl)
            : this(impl, AvaloniaLocator.Current)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TopLevel"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific window implementation.</param>
        /// <param name="dependencyResolver">
        /// The dependency resolver to use. If null the default dependency resolver will be used.
        /// </param>
        public TopLevel(ITopLevelImpl impl, IAvaloniaDependencyResolver? dependencyResolver)
        {
            PlatformImpl = impl ?? throw new InvalidOperationException(
                "Could not create window implementation: maybe no windowing subsystem was initialized?");

            _actualTransparencyLevel = PlatformImpl.TransparencyLevel;

            dependencyResolver ??= AvaloniaLocator.Current;

            _accessKeyHandler = TryGetService<IAccessKeyHandler>(dependencyResolver);
            _inputManager = TryGetService<IInputManager>(dependencyResolver);
            _tooltipService = TryGetService<IToolTipService>(dependencyResolver);
            _keyboardNavigationHandler = TryGetService<IKeyboardNavigationHandler>(dependencyResolver);
            _globalStyles = TryGetService<IGlobalStyles>(dependencyResolver);
            _applicationThemeHost = TryGetService<IThemeVariantHost>(dependencyResolver);

            Renderer = new CompositingRenderer(this, impl.Compositor, () => PlatformImpl.Surfaces ?? []);
            Renderer.SceneInvalidated += SceneInvalidated;

            impl.SetInputRoot(this);

            impl.Closed = HandleClosed;
            impl.Input = HandleInput;
            impl.Paint = HandlePaint;
            impl.Resized = HandleResized;
            impl.ScalingChanged = HandleScalingChanged;
            impl.TransparencyLevelChanged = HandleTransparencyLevelChanged;

            CreatePlatformImplBinding(TransparencyLevelHintProperty, hint => PlatformImpl.SetTransparencyLevelHint(hint ?? Array.Empty<WindowTransparencyLevel>()));
            CreatePlatformImplBinding(ActualThemeVariantProperty, variant =>
            {
                variant ??= ThemeVariant.Default;
                PlatformImpl?.SetFrameThemeVariant((PlatformThemeVariant?)variant ?? PlatformThemeVariant.Light);
            });

            _keyboardNavigationHandler?.SetOwner(this);
            _accessKeyHandler?.SetOwner(this);

            if (_globalStyles is object)
            {
                _globalStyles.GlobalStylesAdded += ((IStyleHost)this).StylesAdded;
                _globalStyles.GlobalStylesRemoved += ((IStyleHost)this).StylesRemoved;
            }
            if (_applicationThemeHost is { })
            {
                SetValue(ActualThemeVariantProperty, _applicationThemeHost.ActualThemeVariant, BindingPriority.Template);
                _applicationThemeHost.ActualThemeVariantChanged += GlobalActualThemeVariantChanged;
            }

            ClientSize = impl.ClientSize;

            if (((IStyleHost)this).StylingParent is IResourceHost applicationResources)
            {
                _resourcesChangesSubscriber = new TargetWeakEventSubscriber<TopLevel, ResourcesChangedEventArgs>(
                    this, static (target, _, _, e) =>
                    {
                        ((ILogical)target).NotifyResourcesChanged(e);
                    });

                ResourcesChangedWeakEvent.Subscribe(applicationResources, _resourcesChangesSubscriber);
            }

            impl.LostFocus += PlatformImpl_LostFocus;

            _pointerOverPreProcessor = new PointerOverPreProcessor(this);
            _pointerOverPreProcessorSubscription = _inputManager?.PreProcess.Subscribe(_pointerOverPreProcessor);

            if(impl.TryGetFeature<ISystemNavigationManagerImpl>() is {} systemNavigationManager)
            {
                systemNavigationManager.BackRequested += (_, e) =>
                {
                    e.RoutedEvent = BackRequestedEvent;
                    Dispatcher.UIThread.Send(_ => RaiseEvent(e));
                };
            }

            _backGestureSubscription = _inputManager?.PreProcess.Subscribe(e =>
            {
                bool backRequested = false;

                if (e is RawKeyEventArgs rawKeyEventArgs && rawKeyEventArgs.Type == RawKeyEventType.KeyDown)
                {
                    var keymap = PlatformSettings?.HotkeyConfiguration.Back;

                    if (keymap != null)
                    {
                        var keyEvent = new KeyEventArgs()
                        {
                            KeyModifiers = (KeyModifiers)rawKeyEventArgs.Modifiers,
                            Key = rawKeyEventArgs.Key,
                            PhysicalKey = rawKeyEventArgs.PhysicalKey,
                            KeyDeviceType= rawKeyEventArgs.KeyDeviceType,
                            KeySymbol = rawKeyEventArgs.KeySymbol
                        };

                        backRequested = keymap.Any( key => key.Matches(keyEvent));
                    }
                }
                else if(e is RawPointerEventArgs pointerEventArgs)
                {
                    backRequested = pointerEventArgs.Type == RawPointerEventType.XButton1Down;
                }

                if (backRequested)
                {
                    var backRequestedEventArgs = new RoutedEventArgs(BackRequestedEvent);
                    Dispatcher.UIThread.Send(_ => RaiseEvent(backRequestedEventArgs));

                    e.Handled = backRequestedEventArgs.Handled;
                }
            });
        }
        /// <summary>
        /// Fired when the window is opened.
        /// </summary>
        public event EventHandler? Opened;

        /// <summary>
        /// Fired when the window is closed.
        /// </summary>
        public event EventHandler? Closed;

        /// <summary>
        /// Gets or sets a method called when the TopLevel's scaling changes.
        /// </summary>
        public event EventHandler? ScalingChanged;
        
        /// <summary>
        /// Gets or sets the client size of the window.
        /// </summary>
        public Size ClientSize
        {
            get => _clientSize;
            protected set => SetAndRaise(ClientSizeProperty, ref _clientSize, value);
        }

        /// <summary>
        /// Gets or sets the total size of the window.
        /// </summary>
        public Size? FrameSize
        {
            get => _frameSize;
            protected set => SetAndRaise(FrameSizeProperty, ref _frameSize, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="WindowTransparencyLevel"/> that the TopLevel should use when possible.
        /// Accepts multiple values which are applied in a fallback order.
        /// For instance, with "Mica, Blur" Mica will be applied only on platforms where it is possible,
        /// and Blur will be used on the rest of them. Default value is an empty array or "None".    
        /// </summary>
        public IReadOnlyList<WindowTransparencyLevel> TransparencyLevelHint
        {
            get => GetValue(TransparencyLevelHintProperty);
            set => SetValue(TransparencyLevelHintProperty, value);
        }

        /// <summary>
        /// Gets the achieved <see cref="WindowTransparencyLevel"/> that the platform was able to provide.
        /// </summary>
        public WindowTransparencyLevel ActualTransparencyLevel
        {
            get => _actualTransparencyLevel;
            private set => SetAndRaise(ActualTransparencyLevelProperty, ref _actualTransparencyLevel, value);
        }        

        /// <summary>
        /// Gets or sets the <see cref="IBrush"/> that transparency will blend with when transparency is not supported.
        /// By default this is a solid white brush.
        /// </summary>
        public IBrush TransparencyBackgroundFallback
        {
            get => GetValue(TransparencyBackgroundFallbackProperty);
            set => SetValue(TransparencyBackgroundFallbackProperty, value);
        }

        /// <inheritdoc cref="ThemeVariantScope.RequestedThemeVariant"/>
        public ThemeVariant? RequestedThemeVariant
        {
            get => GetValue(RequestedThemeVariantProperty);
            set => SetValue(RequestedThemeVariantProperty, value);
        }

        /// <summary>
        /// Occurs when physical Back Button is pressed or a back navigation has been requested.
        /// </summary>
        public event EventHandler<RoutedEventArgs> BackRequested
        {
            add => AddHandler(BackRequestedEvent, value);
            remove => RemoveHandler(BackRequestedEvent, value);
        }

        internal ILayoutManager LayoutManager
        {
            get
            {
                if (_layoutManager is null)
                {
                    _layoutManager = CreateLayoutManager();

                    if (_layoutManager is LayoutManager typedLayoutManager)
                    {
                        _layoutDiagnosticBridge = new LayoutDiagnosticBridge(Renderer.Diagnostics, typedLayoutManager);
                        _layoutDiagnosticBridge.SetupBridge();
                    }
                }

                return _layoutManager;
            }
        }

        ILayoutManager ILayoutRoot.LayoutManager => LayoutManager;

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        public ITopLevelImpl? PlatformImpl { get; private set; }

        /// <summary>
        /// Trys to get the platform handle for the TopLevel-derived control.
        /// </summary>
        /// <returns>
        /// An <see cref="IPlatformHandle"/> describing the window handle, or null if the handle
        /// could not be retrieved.
        /// </returns>
        public IPlatformHandle? TryGetPlatformHandle() => PlatformImpl?.Handle;

        private protected void CreatePlatformImplBinding<TValue>(StyledProperty<TValue> property, Action<TValue> onValue)
        {
            _platformImplBindings.TryGetValue(property, out var actions);
            _platformImplBindings[property] = actions + UpdatePlatformImpl;

            UpdatePlatformImpl(); // execute the action now to handle the default value, which may have been overridden

            void UpdatePlatformImpl()
            {
                if (PlatformImpl is not null)
                {
                    onValue(GetValue(property));
                }
            }
        }

        /// <summary>
        /// Gets the renderer for the window.
        /// </summary>
        internal CompositingRenderer Renderer { get; }

        internal IHitTester HitTester => HitTesterOverride ?? Renderer;

        // This property setter is here purely for lazy unit tests
        // that don't want to set up a proper hit-testable visual tree
        // and should be removed after fixing those tests
        internal IHitTester? HitTesterOverride;

        IRenderer IRenderRoot.Renderer => Renderer;
        IHitTester IRenderRoot.HitTester => HitTester;

        /// <summary>
        /// Gets a value indicating whether the renderer should draw specific diagnostics.
        /// </summary>
        public RendererDiagnostics RendererDiagnostics => Renderer.Diagnostics;

        internal PixelPoint? LastPointerPosition => _pointerOverPreProcessor?.LastPosition;
        
        /// <summary>
        /// Gets the access key handler for the window.
        /// </summary>
        internal IAccessKeyHandler? AccessKeyHandler => _accessKeyHandler;

        /// <summary>
        /// Gets or sets the keyboard navigation handler for the window.
        /// </summary>
        IKeyboardNavigationHandler? IInputRoot.KeyboardNavigationHandler => _keyboardNavigationHandler;

        /// <inheritdoc/>
        IInputElement? IInputRoot.PointerOverElement
        {
            get => GetValue(PointerOverElementProperty);
            set => SetValue(PointerOverElementProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether access keys are shown in the window.
        /// </summary>
        bool IInputRoot.ShowAccessKeys
        {
            get => GetValue(AccessText.ShowAccessKeyProperty);
            set => SetValue(AccessText.ShowAccessKeyProperty, value);
        }

        /// <summary>
        /// Helper for setting the color of the platform's system bars.
        /// </summary>
        /// <param name="control">The main view attached to the toplevel, or the toplevel.</param>
        /// <param name="color">The color to set.</param>
        public static void SetSystemBarColor(Control control, SolidColorBrush? color)
        {
            control.SetValue(SystemBarColorProperty, color);
        }

        /// <summary>
        /// Helper for getting the color of the platform's system bars.
        /// </summary>
        /// <param name="control">The main view attached to the toplevel, or the toplevel.</param>
        /// <returns>The current color of the platform's system bars.</returns>
        public static SolidColorBrush? GetSystemBarColor(Control control)
        {
            return control.GetValue(SystemBarColorProperty);
        }

        /// <summary>
        /// Enabled or disables whenever TopLevel should automatically adjust paddings depending on the safe area.
        /// </summary>
        /// <param name="control">The main view attached to the toplevel, or the toplevel.</param>
        /// <param name="value">Value to be set.</param>
        public static void SetAutoSafeAreaPadding(Control control, bool value)
        {
            control.SetValue(AutoSafeAreaPaddingProperty, value);
        }

        /// <summary>
        /// Gets if auto safe area padding is enabled.
        /// </summary>
        /// <param name="control">The main view attached to the toplevel, or the toplevel.</param>
        public static bool GetAutoSafeAreaPadding(Control control)
        {
            return control.GetValue(AutoSafeAreaPaddingProperty);
        }

        /// <inheritdoc/>
        double ILayoutRoot.LayoutScaling => PlatformImpl?.RenderScaling ?? 1;

        /// <inheritdoc/>
        public double RenderScaling => PlatformImpl?.RenderScaling ?? 1;

        IStyleHost IStyleHost.StylingParent => _globalStyles!;

        /// <summary>
        /// File System storage service used for file pickers and bookmarks.
        /// </summary>
        public IStorageProvider StorageProvider => _storageProvider
            ??= AvaloniaLocator.Current.GetService<IStorageProviderFactory>()?.CreateProvider(this)
            ?? PlatformImpl?.TryGetFeature<IStorageProvider>()
            ?? new NoopStorageProvider();

        public IInsetsManager? InsetsManager => PlatformImpl?.TryGetFeature<IInsetsManager>();
        public IInputPane? InputPane => PlatformImpl?.TryGetFeature<IInputPane>();
        public ILauncher Launcher => PlatformImpl?.TryGetFeature<ILauncher>() ?? new NoopLauncher();

        /// <summary>
        /// Gets platform screens implementation.
        /// </summary>
        public Screens? Screens => _screens ??=
            PlatformImpl?.TryGetFeature<IScreenImpl>() is { } screenImpl ? new Screens(screenImpl) : null;

        /// <summary>
        /// Gets the platform's clipboard implementation
        /// </summary>
        public IClipboard? Clipboard => PlatformImpl?.TryGetFeature<IClipboard>();

        /// <inheritdoc />
        public IFocusManager? FocusManager => AvaloniaLocator.Current.GetService<IFocusManager>();

        /// <inheritdoc />
        public IPlatformSettings? PlatformSettings => AvaloniaLocator.Current.GetService<IPlatformSettings>();

        /// <inheritdoc/>
        Point IRenderRoot.PointToClient(PixelPoint p)
        {
            return PlatformImpl?.PointToClient(p) ?? default;
        }

        /// <inheritdoc/>
        PixelPoint IRenderRoot.PointToScreen(Point p)
        {
            return PlatformImpl?.PointToScreen(p) ?? default;
        }

        /// <summary>
        /// Gets the <see cref="TopLevel" /> for which the given <see cref="Visual"/> is hosted in.
        /// </summary>
        /// <param name="visual">The visual to query its TopLevel</param>
        /// <returns>The TopLevel</returns>
        public static TopLevel? GetTopLevel(Visual? visual)
        {
            return visual?.VisualRoot as TopLevel;
        }

        /// <summary>
        /// Requests a <see cref="PlatformInhibitionType"/> to be inhibited.
        /// The behavior remains inhibited until the return value is disposed.
        /// The available set of <see cref="PlatformInhibitionType"/>s depends on the platform.
        /// If a behavior is inhibited on a platform where this type is not supported the request will have no effect.
        /// </summary>
        public async Task<IDisposable> RequestPlatformInhibition(PlatformInhibitionType type, string reason)
        {
            var platformBehaviorInhibition = PlatformImpl?.TryGetFeature<IPlatformBehaviorInhibition>();
            if (platformBehaviorInhibition == null)
            {
                return Disposable.Create(() => { });
            }

            switch (type)
            {
                case PlatformInhibitionType.AppSleep:
                    await platformBehaviorInhibition.SetInhibitAppSleep(true, reason);
                    return Disposable.Create(() => platformBehaviorInhibition.SetInhibitAppSleep(false, reason).Wait());
                default:
                    return Disposable.Create(() => { });
            }
        }
        
        /// <summary>
        /// Enqueues a callback to be called on the next animation tick
        /// </summary>
        public void RequestAnimationFrame(Action<TimeSpan> action)
        {
            Dispatcher.UIThread.VerifyAccess();
            MediaContext.Instance.RequestAnimationFrame(action);
        }
        
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ContentProperty)
            {
                InvalidateChildInsetsPadding();
            }
            else if (_platformImplBindings.TryGetValue(change.Property, out var bindingAction))
            {
                bindingAction();
            }
        }

        private IDisposable? _insetsPaddings;

        private void InvalidateChildInsetsPadding()
        {
            if (Content is Control child
                && InsetsManager is {} insetsManager)
            {
                insetsManager.SafeAreaChanged -= InsetsManagerOnSafeAreaChanged;
                _insetsPaddings?.Dispose();

                if (child.GetValue(AutoSafeAreaPaddingProperty))
                {
                    insetsManager.SafeAreaChanged += InsetsManagerOnSafeAreaChanged;
                    _insetsPaddings = child.SetValue(
                        PaddingProperty,
                        insetsManager.SafeAreaPadding,
                        BindingPriority.Style); // lower priority, so it can be redefined by user
                }

                void InsetsManagerOnSafeAreaChanged(object? sender, SafeAreaChangedArgs e)
                {
                    InvalidateChildInsetsPadding();
                }
            }
        }

        /// <summary>
        /// Creates the layout manager for this <see cref="TopLevel" />.
        /// </summary>
        private protected virtual ILayoutManager CreateLayoutManager() => new LayoutManager(this);

        /// <summary>
        /// Handles a paint notification from <see cref="ITopLevelImpl.Resized"/>.
        /// </summary>
        /// <param name="rect">The dirty area.</param>
        private void HandlePaint(Rect rect)
        {
            Renderer.Paint(rect);
        }

        protected void StartRendering() => MediaContext.Instance.AddTopLevel(this, LayoutManager, Renderer);

        protected void StopRendering() => MediaContext.Instance.RemoveTopLevel(this);

        /// <summary>
        /// Handles a closed notification from <see cref="ITopLevelImpl.Closed"/>.
        /// </summary>
        private protected virtual void HandleClosed()
        {
            Renderer.SceneInvalidated -= SceneInvalidated;
            // We need to wait for the renderer to complete any in-flight operations
            Renderer.Dispose();
            StopRendering();
            
            Debug.Assert(PlatformImpl != null);
            // The PlatformImpl is completely invalid at this point
            PlatformImpl = null;
            
            if (_globalStyles is object)
            {
                _globalStyles.GlobalStylesAdded -= ((IStyleHost)this).StylesAdded;
                _globalStyles.GlobalStylesRemoved -= ((IStyleHost)this).StylesRemoved;
            }
            if (_applicationThemeHost is { })
            {
                _applicationThemeHost.ActualThemeVariantChanged -= GlobalActualThemeVariantChanged;
            }
            
            _layoutDiagnosticBridge?.Dispose();
            _layoutDiagnosticBridge = null;

            _pointerOverPreProcessor?.OnCompleted();
            _pointerOverPreProcessorSubscription?.Dispose();
            _backGestureSubscription?.Dispose();

            var logicalArgs = new LogicalTreeAttachmentEventArgs(this, this, null);
            ((ILogical)this).NotifyDetachedFromLogicalTree(logicalArgs);

            var visualArgs = new VisualTreeAttachmentEventArgs(this, this);
            OnDetachedFromVisualTreeCore(visualArgs);
            
            OnClosed(EventArgs.Empty);

            LayoutManager.Dispose();
            _platformImplBindings.Clear();
        }

        /// <summary>
        /// Handles a resize notification from <see cref="ITopLevelImpl.Resized"/>.
        /// </summary>
        /// <param name="clientSize">The new client size.</param>
        /// <param name="reason">The reason for the resize.</param>
        internal virtual void HandleResized(Size clientSize, WindowResizeReason reason)
        {
            ClientSize = clientSize;
            Width = clientSize.Width;
            Height = clientSize.Height;
            LayoutManager.ExecuteLayoutPass();
            Renderer.Resized(clientSize);
        }

        /// <summary>
        /// Handles a window scaling change notification from 
        /// <see cref="ITopLevelImpl.ScalingChanged"/>.
        /// </summary>
        /// <param name="scaling">The window scaling.</param>
        private void HandleScalingChanged(double scaling)
        {
            LayoutHelper.InvalidateSelfAndChildrenMeasure(this);
            Dispatcher.UIThread.Send(_ => ScalingChanged?.Invoke(this, EventArgs.Empty));
        }

        private void HandleTransparencyLevelChanged(WindowTransparencyLevel transparencyLevel)
        {
            if (_transparencyFallbackBorder != null)
            {
                if (transparencyLevel == WindowTransparencyLevel.None)
                {
                    _transparencyFallbackBorder.Background = TransparencyBackgroundFallback;
                }
                else
                {
                    _transparencyFallbackBorder.Background = null;
                }
            }

            ActualTransparencyLevel = transparencyLevel;
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            throw new InvalidOperationException(
                $"Control '{GetType().Name}' is a top level control and cannot be added as a child.");
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (PlatformImpl is null)
                return;

            _transparencyFallbackBorder = e.NameScope.Find<Border>("PART_TransparencyFallback");
            HandleTransparencyLevelChanged(PlatformImpl.TransparencyLevel);
        }

        /// <summary>
        /// Raises the <see cref="Opened"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnOpened(EventArgs e)
        {
            Dispatcher.UIThread.Send(_ => Opened?.Invoke(this, e));
        }

        /// <summary>
        /// Raises the <see cref="Closed"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnClosed(EventArgs e)
        {
            Dispatcher.UIThread.Send(_ => Closed?.Invoke(this, e));
        }

        /// <summary>
        /// Tries to get a service from an <see cref="IAvaloniaDependencyResolver"/>, logging a
        /// warning if not found.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="resolver">The resolver.</param>
        /// <returns>The service.</returns>
        private T? TryGetService<T>(IAvaloniaDependencyResolver resolver) where T : class
        {
            var result = resolver.GetService<T>();

            if (result == null)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                    this,
                    "Could not create {Service} : maybe Application.RegisterServices() wasn't called?",
                    typeof(T));
            }

            return result;
        }

        /// <summary>
        /// Handles input from <see cref="ITopLevelImpl.Input"/>.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void HandleInput(RawInputEventArgs e)
        {
            if (PlatformImpl != null)
            {
                Dispatcher.UIThread.Send(static state =>
                {
                    var (topLevel, e) = (ValueTuple<TopLevel, RawInputEventArgs>)state!;
                    if (e is RawPointerEventArgs pointerArgs)
                    {
                        var hitTestElement = topLevel.InputHitTest(pointerArgs.Position, enabledElementsOnly: false);

                        pointerArgs.InputHitTestResult = (hitTestElement, FirstEnabledAncestor(hitTestElement));
                    }

                    topLevel._inputManager?.ProcessInput(e);
                }, (this, e));
            }
            else
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                    this,
                    "PlatformImpl is null, couldn't handle input.");
            }
        }

        private static IInputElement? FirstEnabledAncestor(IInputElement? hitTestElement)
        {
            var candidate = hitTestElement;
            while (candidate?.IsEffectivelyEnabled == false)
            {
                candidate = (candidate as Visual)?.Parent as IInputElement;
            }

            return candidate;
        }

        private void PointerOverElementOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == CursorProperty && sender is InputElement inputElement)
            {
                PlatformImpl?.SetCursor(inputElement.Cursor?.PlatformImpl);
            }
        }

        private void GlobalActualThemeVariantChanged(object? sender, EventArgs e)
        {
            SetValue(ActualThemeVariantProperty, ((IThemeVariantHost)sender!).ActualThemeVariant, BindingPriority.Template);
        }

        private void SceneInvalidated(object? sender, SceneInvalidatedEventArgs e)
        {
            _pointerOverPreProcessor?.SceneInvalidated(e.DirtyRect);
            UpdateToolTip(e.DirtyRect);
        }

        private static void OnToolTipServiceEnabledChanged(AvaloniaPropertyChangedEventArgs<bool> args)
        {
            if (args.GetNewValue<bool>()
                && args.Priority != BindingPriority.Inherited 
                && args.Sender is Visual visual 
                && GetTopLevel(visual) is { } topLevel)
            {
                topLevel.UpdateToolTip(visual.Bounds.Translate((Vector)visual.TranslatePoint(default, topLevel)!));
            }
        }

        private void UpdateToolTip(Rect dirtyRect)
        {
            if (_tooltipService != null && IsPointerOver && _pointerOverPreProcessor?.LastPosition is { } lastPos)
            {
                var clientPoint = this.PointToClient(lastPos);
                if (dirtyRect.Contains(clientPoint))
                {
                    _tooltipService.Update(this, HitTester.HitTestFirst(clientPoint, this, null));
                }
            }
        }

        void PlatformImpl_LostFocus()
        {
            var focused = (Visual?)FocusManager?.GetFocusedElement();
            if (focused == null)
                return;
            while (focused.VisualParent != null)
                focused = focused.VisualParent;

            if (focused == this)
                KeyboardDevice.Instance?.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);
        }

        protected override bool BypassFlowDirectionPolicies => true;

        protected internal override void InvalidateMirrorTransform()
        {
            // Do nothing becuase TopLevel should't apply MirrorTransform on himself.
        }

        ITextInputMethodImpl? ITextInputMethodRoot.InputMethod => PlatformImpl?.TryGetFeature<ITextInputMethodImpl>();

        /// <summary>
        /// Provides layout pass timing from the layout manager to the renderer, for diagnostics purposes.
        /// </summary>
        private sealed class LayoutDiagnosticBridge : IDisposable
        {
            private readonly RendererDiagnostics _diagnostics;
            private readonly LayoutManager _layoutManager;
            private bool _isHandling;

            public LayoutDiagnosticBridge(RendererDiagnostics diagnostics, LayoutManager layoutManager)
            {
                _diagnostics = diagnostics;
                _layoutManager = layoutManager;

                diagnostics.PropertyChanged += OnDiagnosticsPropertyChanged;
            }

            public void SetupBridge()
            {
                var needsHandling = (_diagnostics.DebugOverlays & RendererDebugOverlays.LayoutTimeGraph) != 0;
                if (needsHandling != _isHandling)
                {
                    _isHandling = needsHandling;
                    _layoutManager.LayoutPassTimed = needsHandling
                        ? timing => _diagnostics.LastLayoutPassTiming = timing
                        : null;
                }
            }

            private void OnDiagnosticsPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(RendererDiagnostics.DebugOverlays))
                {
                    SetupBridge();
                }
            }

            public void Dispose()
            {
                _diagnostics.PropertyChanged -= OnDiagnosticsPropertyChanged;
                _layoutManager.LayoutPassTimed = null;
            }
        }
    }
}
