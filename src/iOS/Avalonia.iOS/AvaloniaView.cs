using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Rendering.Composition;
using CoreAnimation;
using Foundation;
using ObjCRuntime;
using UIKit;
using IInsetsManager = Avalonia.Controls.Platform.IInsetsManager;

namespace Avalonia.iOS
{
    /// <summary>
    /// Root view container for Avalonia content, that can be embedded into iOS visual tree.
    /// </summary>
    public partial class AvaloniaView : UIView, ITextInputMethodImpl
    {
        internal IInputRoot InputRoot
            => _inputRoot ?? throw new InvalidOperationException($"{nameof(IWindowImpl.SetInputRoot)} must have been called");
        internal TopLevel TopLevel => _topLevel;

        private readonly TopLevelImpl _topLevelImpl;
        private readonly EmbeddableControlRoot _topLevel;
        private readonly InputHandler _input;
        private TextInputMethodClient? _client;
        private IAvaloniaViewController? _controller;
        private IInputRoot? _inputRoot;
        private Metal.MetalRenderTarget? _currentRenderTarget;
        private (PixelSize size, double scaling) _latestLayoutProps;

        public AvaloniaView()
        {
            _topLevelImpl = new TopLevelImpl(this);
            _input = new InputHandler(this, _topLevelImpl);
            _topLevel = new EmbeddableControlRoot(_topLevelImpl);

            _topLevel.Prepare();

            _topLevel.StartRendering();

            InitLayerSurface();

            // Remote touch handling
            if (OperatingSystem.IsTvOS())
            {
                AddGestureRecognizer(new UISwipeGestureRecognizer(_input.Handle)
                {
                    Direction = UISwipeGestureRecognizerDirection.Up
                });
                AddGestureRecognizer(new UISwipeGestureRecognizer(_input.Handle)
                {
                    Direction = UISwipeGestureRecognizerDirection.Right
                });
                AddGestureRecognizer(new UISwipeGestureRecognizer(_input.Handle)
                {
                    Direction = UISwipeGestureRecognizerDirection.Down
                });
                AddGestureRecognizer(new UISwipeGestureRecognizer(_input.Handle)
                {
                    Direction = UISwipeGestureRecognizerDirection.Left
                });
            }
            else if (OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst())
            {
#if !TVOS
                MultipleTouchEnabled = true;
#endif
            }
        }

        [SuppressMessage("Interoperability", "CA1422:Validate platform compatibility")]
        private void InitLayerSurface()
        {
            var l = Layer;
            l.ContentsScale = UIScreen.MainScreen.Scale;
            l.Opaque = true;
#if !MACCATALYST
            if (l is CAEAGLLayer eaglLayer)
            {
                eaglLayer.DrawableProperties = new NSDictionary(
                    OpenGLES.EAGLDrawableProperty.RetainedBacking, false,
                    OpenGLES.EAGLDrawableProperty.ColorFormat, OpenGLES.EAGLColorFormat.RGBA8
                );
                _topLevelImpl.Surfaces = new[] { new Eagl.EaglLayerSurface(eaglLayer) };
            }
            else
#endif
            if (l is CAMetalLayer metalLayer)
            {
                metalLayer.Opaque = false;
                _topLevelImpl.Surfaces = new[] { new Metal.MetalPlatformSurface(metalLayer, this) };
            }
        }

        /// <inheritdoc />
        public override bool CanBecomeFirstResponder => true;

        /// <inheritdoc />
        public override bool CanResignFirstResponder => true;

        /// <inheritdoc />
        [ObsoletedOSPlatform("ios17.0", "Use the 'UITraitChangeObservable' protocol instead.")]
        [ObsoletedOSPlatform("maccatalyst17.0", "Use the 'UITraitChangeObservable' protocol instead.")]
        [ObsoletedOSPlatform("tvos17.0", "Use the 'UITraitChangeObservable' protocol instead.")]
        [SupportedOSPlatform("ios")]
        [SupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public override void TraitCollectionDidChange(UITraitCollection? previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);
            
            var settings = AvaloniaLocator.Current.GetRequiredService<IPlatformSettings>() as PlatformSettings;
            settings?.TraitCollectionDidChange();
        }

        /// <inheritdoc />
        public override void TintColorDidChange()
        {
            base.TintColorDidChange();
            
            var settings = AvaloniaLocator.Current.GetRequiredService<IPlatformSettings>() as PlatformSettings;
            settings?.TraitCollectionDidChange();
        }

        public void InitWithController<TController>(TController controller)
            where TController : UIViewController, IAvaloniaViewController
        {
            _controller = controller;
            _topLevelImpl._insetsManager.InitWithController(controller);
        }
        
        internal class TopLevelImpl : ITopLevelImpl
        {
            private readonly AvaloniaView _view;
            private readonly INativeControlHostImpl _nativeControlHost;
            internal readonly InsetsManager _insetsManager;
            private readonly IStorageProvider? _storageProvider;
            private readonly IClipboard? _clipboard;
            private readonly IInputPane? _inputPane;
            private IDisposable? _paddingInsets;

            public AvaloniaView View => _view;

            public TopLevelImpl(AvaloniaView view)
            {
                _view = view;
                _nativeControlHost = new NativeControlHostImpl(view);
#if TVOS
                _storageProvider = null;
                _clipboard = null;
                _inputPane = null;
#else
                _storageProvider = new Storage.IOSStorageProvider(view);
                _clipboard = new ClipboardImpl();
                _inputPane = UIKitInputPane.Instance;
#endif
                _insetsManager = new InsetsManager();
                _insetsManager.DisplayEdgeToEdgeChanged += (_, edgeToEdge) =>
                {
                    // iOS doesn't add any paddings/margins to the application by itself.
                    // Application is fully responsible for safe area paddings.
                    // So, unlikely to android, we need to "fake" safe area insets when edge to edge is disabled.
                    _paddingInsets?.Dispose();
                    if (!edgeToEdge && view._controller is { } controller)
                    {
                        _paddingInsets = view._topLevel.SetValue(
                            TemplatedControl.PaddingProperty,
                            controller.SafeAreaPadding,
                            BindingPriority.Style); // lower priority, so it can be redefined by user
                    }
                };
            }

            public void Dispose()
            {
                // No-op
            }

            public Compositor Compositor => Platform.Compositor
                ?? throw new InvalidOperationException("iOS backend wasn't initialized. Make sure UseiOS was called.");

            public void Invalidate(Rect rect)
            {
                // No-op
            }

            public void SetInputRoot(IInputRoot inputRoot)
            {
                _view._inputRoot = inputRoot;
            }

            public Point PointToClient(PixelPoint point) => new Point(point.X, point.Y);

            public PixelPoint PointToScreen(Point point) => new PixelPoint((int)point.X, (int)point.Y);

            public void SetCursor(ICursorImpl? cursor)
            {
                // no-op
            }

            public IPopupImpl? CreatePopup()
            {
                // In-window popups
                return null;
            }

            public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevel)
            {
                // No-op
            }

            public double DesktopScaling => RenderScaling;
            public IPlatformHandle? Handle { get; }
            public Size ClientSize => new Size(_view.Bounds.Width, _view.Bounds.Height);
            public Size? FrameSize => null;
            public double RenderScaling => _view.ContentScaleFactor;
            public IEnumerable<object> Surfaces { get; set; } = Array.Empty<object>();
            public Action<RawInputEventArgs>? Input { get; set; }
            public Action<Rect>? Paint { get; set; }
            public Action<Size, WindowResizeReason>? Resized { get; set; }
            public Action<double>? ScalingChanged { get; set; }
            public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }
            public Action? Closed { get; set; }

            public Action? LostFocus { get; set; }

            public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;

            public void SetFrameThemeVariant(PlatformThemeVariant? themeVariant)
            {
#if !TVOS
                // TODO adjust status bar depending on full screen mode.
                if ((OperatingSystem.IsIOSVersionAtLeast(13)
                    || OperatingSystem.IsMacCatalyst())
                    && _view._controller is not null)
                {
                    _view._controller.PreferredStatusBarStyle = themeVariant switch
                    {
                        PlatformThemeVariant.Light => UIStatusBarStyle.DarkContent,
                        PlatformThemeVariant.Dark => UIStatusBarStyle.LightContent,
                        _ => UIStatusBarStyle.Default
                    };
                }
#endif
            }
            
            public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } =
                new AcrylicPlatformCompensationLevels();

            public object? TryGetFeature(Type featureType)
            {
                if (featureType == typeof(ITextInputMethodImpl))
                {
                    return _view;
                }

                if (featureType == typeof(INativeControlHostImpl))
                {
                    return _nativeControlHost;
                }

                if (featureType == typeof(IInsetsManager))
                {
                    return _insetsManager;
                }

                if (featureType == typeof(IClipboard))
                {
                    return _clipboard;
                }

                if (featureType == typeof(IStorageProvider))
                {
                    return _storageProvider;
                }

                if (featureType == typeof(IInputPane))
                {
                    return _inputPane;
                }

                if (featureType == typeof(ILauncher))
                {
                    return new IOSLauncher();
                }

                if (featureType == typeof(IScreenImpl))
                {
                    return (iOSScreens)AvaloniaLocator.Current.GetRequiredService<IScreenImpl>();
                }

                return null;
            }
        }

        [Export("layerClass")]
        public static Class LayerClass()
        {
#if !MACCATALYST
            if (Platform.Graphics is Eagl.EaglPlatformGraphics)
            {
                return new Class(typeof(CAEAGLLayer));
            }
            else
#endif
            {
                return new Class(typeof(CAMetalLayer));
            }
        }

        /// <inheritdoc/>
        public override void TouchesBegan(NSSet touches, UIEvent? evt) => _input.Handle(touches, evt);

        /// <inheritdoc/>
        public override void TouchesMoved(NSSet touches, UIEvent? evt) => _input.Handle(touches, evt);

        /// <inheritdoc/>
        public override void TouchesEnded(NSSet touches, UIEvent? evt) => _input.Handle(touches, evt);

        /// <inheritdoc/>
        public override void TouchesCancelled(NSSet touches, UIEvent? evt) => _input.Handle(touches, evt);

        /// <inheritdoc/>
        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            if (!_input.Handle(presses, evt))
            {
                base.PressesBegan(presses, evt);
            }
        }

        /// <inheritdoc/>
        public override void PressesChanged(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            if (!_input.Handle(presses, evt))
            {
                base.PressesBegan(presses, evt);
            }
        }

        /// <inheritdoc/>
        public override void PressesEnded(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            if (!_input.Handle(presses, evt))
            {
                base.PressesEnded(presses, evt);
            }
        }

        /// <inheritdoc/>
        public override void PressesCancelled(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            if (!_input.Handle(presses, evt))
            {
                base.PressesCancelled(presses, evt);
            }
        }

        /// <inheritdoc/>
        public override void LayoutSubviews()
        {
            _topLevelImpl.Resized?.Invoke(_topLevelImpl.ClientSize, WindowResizeReason.Layout);
            var scaling = (double)ContentScaleFactor;
            _latestLayoutProps = (new PixelSize((int)(Bounds.Width * scaling), (int)(Bounds.Height * scaling)), scaling);
            if (_currentRenderTarget is not null)
            {
                _currentRenderTarget.PendingLayout = _latestLayoutProps;
            }

            base.LayoutSubviews();
        }

        public Control? Content
        {
            get => (Control?)_topLevel.Content;
            set => _topLevel.Content = value;
        }

        internal void SetRenderTarget(Metal.MetalRenderTarget target)
        {
            _currentRenderTarget = target;
            _currentRenderTarget.PendingLayout = _latestLayoutProps;
        }
    }
}
