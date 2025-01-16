using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using AndroidX.AppCompat.App;
using Avalonia.Android.Platform.Input;
using Avalonia.Android.Platform.Specific;
using Avalonia.Android.Platform.Specific.Helpers;
using Avalonia.Android.Platform.Storage;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Rendering.Composition;
using Java.Lang;
using ClipboardManager = Android.Content.ClipboardManager;

namespace Avalonia.Android.Platform.SkiaPlatform
{
    class TopLevelImpl : IAndroidView, ITopLevelImpl, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo
    {
        private readonly IGlPlatformSurface _gl;
        private readonly IFramebufferPlatformSurface _framebuffer;

        private readonly AndroidKeyboardEventsHelper<TopLevelImpl> _keyboardHelper;
        private readonly AndroidMotionEventsHelper _pointerHelper;
        private readonly AndroidInputMethod<ViewImpl> _textInputMethod;
        private readonly INativeControlHostImpl _nativeControlHost;
        private readonly IStorageProvider? _storageProvider;
        private readonly AndroidSystemNavigationManagerImpl _systemNavigationManager;
        private readonly AndroidInsetsManager? _insetsManager;
        private readonly ClipboardImpl _clipboard;
        private readonly AndroidLauncher? _launcher;
        private readonly AndroidScreens? _screens;
        private ViewImpl _view;
        private WindowTransparencyLevel _transparencyLevel;

        public TopLevelImpl(AvaloniaView avaloniaView, bool placeOnTop = false)
        {
            if (avaloniaView.Context is null)
            {
                throw new ArgumentException("AvaloniaView.Context must not be null");
            }

            _view = new ViewImpl(avaloniaView.Context, this, placeOnTop);
            _textInputMethod = new AndroidInputMethod<ViewImpl>(_view);
            _keyboardHelper = new AndroidKeyboardEventsHelper<TopLevelImpl>(this);
            _pointerHelper = new AndroidMotionEventsHelper(this);
            _gl = new EglGlPlatformSurface(this);
            _framebuffer = new FramebufferManager(this);
            _clipboard = new ClipboardImpl(avaloniaView.Context.GetSystemService(Context.ClipboardService).JavaCast<ClipboardManager>());
            _screens = new AndroidScreens(avaloniaView.Context);

            RenderScaling = _view.Scaling;

            if (avaloniaView.Context is Activity mainActivity)
            {
                _insetsManager = new AndroidInsetsManager(mainActivity, this);
                _storageProvider = new AndroidStorageProvider(mainActivity);
                _launcher = new AndroidLauncher(mainActivity);
            }

            _nativeControlHost = new AndroidNativeControlHostImpl(avaloniaView);
            _transparencyLevel = WindowTransparencyLevel.None;

            _systemNavigationManager = new AndroidSystemNavigationManagerImpl(avaloniaView.Context as IActivityNavigationService);

            Surfaces = new object[] { _gl, _framebuffer, Handle };
        }

        public IInputRoot? InputRoot { get; private set; }

        public virtual Size ClientSize => _view.Size.ToSize(RenderScaling);

        public Size? FrameSize => null;

        public Action? Closed { get; set; }

        public Action<RawInputEventArgs>? Input { get; set; }

        public Action<Rect>? Paint { get; set; }

        public Action<Size, WindowResizeReason>? Resized { get; set; }

        public Action<double>? ScalingChanged { get; set; }

        public View View => _view;

        internal InvalidationAwareSurfaceView InternalView => _view;

        public double DesktopScaling => RenderScaling;
        public IPlatformHandle Handle => _view;

        public IEnumerable<object> Surfaces { get; }

        public Compositor Compositor => AndroidPlatform.Compositor ??
            throw new InvalidOperationException("Android backend wasn't initialized. Make sure .UseAndroid() was executed.");

        public virtual void Hide()
        {
            _view.Visibility = ViewStates.Invisible;
        }

        public void Invalidate(Rect rect)
        {
            if (_view.Holder?.Surface?.IsValid == true) _view.Invalidate();
        }

        public Point PointToClient(PixelPoint point)
        {
            return point.ToPoint(RenderScaling);
        }

        public PixelPoint PointToScreen(Point point)
        {
            return PixelPoint.FromPoint(point, RenderScaling);
        }

        public void SetCursor(ICursorImpl? cursor)
        {
            //still not implemented
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }

        public virtual void Show()
        {
            _view.Visibility = ViewStates.Visible;
        }

        public double RenderScaling { get; }

        void Draw()
        {
            Paint?.Invoke(new Rect(new Point(0, 0), ClientSize));
        }

        public virtual void Dispose()
        {
            _systemNavigationManager.Dispose();
            _view.Dispose();
            _view = null!;
        }

        protected virtual void OnResized(Size size)
        {
            Resized?.Invoke(size, WindowResizeReason.Unspecified);
        }

        internal void Resize(Size size)
        {
            Resized?.Invoke(size, WindowResizeReason.Layout);
        }

        class ViewImpl : InvalidationAwareSurfaceView, ISurfaceHolderCallback, IInitEditorInfo
        {
            private readonly TopLevelImpl _tl;
            private Size _oldSize;

            public ViewImpl(Context context, TopLevelImpl tl, bool placeOnTop) : base(context)
            {
                _tl = tl;
                if (placeOnTop)
                    SetZOrderOnTop(true);
            }

            public TopLevelImpl TopLevelImpl => _tl;

            protected override void Draw()
            {
                _tl.Draw();
            }

            protected override void DispatchDraw(global::Android.Graphics.Canvas canvas)
            {
                // Workaround issue #9230 on where screen remains gray after splash screen.
                // base.DispatchDraw should punch a hole into the canvas so the surface
                // can be seen below, but it does not.
                if (OperatingSystem.IsAndroidVersionAtLeast(29))
                {
                    // Android 10+ does this (BlendMode was new)
                    var paint = new Paint();
                    paint.SetColor(0);
                    paint.BlendMode = BlendMode.Clear;
                    canvas.DrawRect(0, 0, Width, Height, paint);
                }
                else
                {
                    // Android 9 did this
                    canvas.DrawColor(Color.Transparent, PorterDuff.Mode.Clear!);
                }

                base.DispatchDraw(canvas);
            }

            protected override bool DispatchGenericPointerEvent(MotionEvent? e)
            {
                var result = _tl._pointerHelper.DispatchMotionEvent(e, out var callBase);
                var baseResult = callBase && base.DispatchGenericPointerEvent(e);

                return result ?? baseResult;
            }

            public override bool DispatchTouchEvent(MotionEvent? e)
            {
                var result = _tl._pointerHelper.DispatchMotionEvent(e, out var callBase);
                var baseResult = callBase && base.DispatchTouchEvent(e);

                return result ?? baseResult;
            }

            public override bool DispatchKeyEvent(KeyEvent? e)
            {
                var res = _tl._keyboardHelper.DispatchKeyEvent(e, out var callBase);
                var baseResult = callBase && base.DispatchKeyEvent(e);

                return res ?? baseResult;
            }

            void ISurfaceHolderCallback.SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
            {
                var newSize = new PixelSize(width, height).ToSize(_tl.RenderScaling);

                if (newSize != _oldSize)
                {
                    _oldSize = newSize;
                    _tl.OnResized(newSize);
                }

                base.SurfaceChanged(holder, format, width, height);
            }

            public sealed override bool OnCheckIsTextEditor()
            {
                return true;
            }

            private Func<TopLevelImpl, EditorInfo, IInputConnection>? _initEditorInfo;

            public void InitEditorInfo(Func<TopLevelImpl, EditorInfo, IInputConnection> init)
            {
                _initEditorInfo = init;
            }

            public sealed override IInputConnection OnCreateInputConnection(EditorInfo? outAttrs)
            {
                return _initEditorInfo?.Invoke(_tl, outAttrs!)!;
            }

        }

        public IPopupImpl? CreatePopup() => null;

        public Action? LostFocus { get; set; }
        public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

        public WindowTransparencyLevel TransparencyLevel
        {
            get => _transparencyLevel;
            private set
            {
                if (_transparencyLevel != value)
                {
                    _transparencyLevel = value;
                    TransparencyLevelChanged?.Invoke(value);
                }
            }
        }

        public void SetFrameThemeVariant(PlatformThemeVariant? themeVariant)
        {
            if (_insetsManager != null)
            {
                _insetsManager.SystemBarTheme = themeVariant switch
                {
                    PlatformThemeVariant.Light => SystemBarTheme.Light,
                    PlatformThemeVariant.Dark => SystemBarTheme.Dark,
                    _ => null,
                };
            }

            AppCompatDelegate.DefaultNightMode = themeVariant == null ? AppCompatDelegate.ModeNightFollowSystem :
                themeVariant == PlatformThemeVariant.Light ? AppCompatDelegate.ModeNightNo : AppCompatDelegate.ModeNightYes;

            if (AppCompatDelegate.DefaultNightMode == AppCompatDelegate.ModeNightFollowSystem && _view.Context is { } context
                && context.Resources?.Configuration is { } config)
            {
                var settings =
                    AvaloniaLocator.Current.GetRequiredService<IPlatformSettings>() as AndroidPlatformSettings;
                settings?.OnViewConfigurationChanged(context, config);
            }
        }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels => new AcrylicPlatformCompensationLevels(1, 1, 1);

        IntPtr EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Handle => ((IPlatformHandle)_view).Handle;

        public PixelSize Size => _view.Size;

        public double Scaling => RenderScaling;

        internal AndroidInsetsManager? InsetsManager => _insetsManager;

        public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels)
        {
            if (_view.Context is not AvaloniaMainActivity activity)
                return;

            foreach (var level in transparencyLevels)
            {
                if (!IsSupported(level))
                {
                    continue;
                }

                if (level == TransparencyLevel)
                {
                    return;
                }

                if (level == WindowTransparencyLevel.None)
                {
                    if (OperatingSystem.IsAndroidVersionAtLeast(30))
                    {
                        activity.SetTranslucent(false);
                    }

                    activity.Window?.SetBackgroundDrawable(new ColorDrawable(Color.White));
                }
                else if (level == WindowTransparencyLevel.Transparent)
                {
                    if (OperatingSystem.IsAndroidVersionAtLeast(30))
                    {
                        activity.SetTranslucent(true);
                        SetBlurBehind(activity, 0);
                        activity.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
                    }
                }
                else if (level == WindowTransparencyLevel.Blur)
                {
                    if (OperatingSystem.IsAndroidVersionAtLeast(31))
                    {
                        activity.SetTranslucent(true);
                        SetBlurBehind(activity, 120);
                        activity.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
                    }
                }

                TransparencyLevel = level;
                return;
            }

            // If we get here, we didn't find a supported level. Use the default of None.
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                activity.SetTranslucent(false);
            }

            activity.Window?.SetBackgroundDrawable(new ColorDrawable(Color.White));
        }

        public virtual object? TryGetFeature(Type featureType)
        {
            if (featureType == typeof(IStorageProvider))
            {
                return _storageProvider;
            }

            if (featureType == typeof(ITextInputMethodImpl))
            {
                return _textInputMethod;
            }

            if (featureType == typeof(ISystemNavigationManagerImpl))
            {
                return _systemNavigationManager;
            }

            if (featureType == typeof(INativeControlHostImpl))
            {
                return _nativeControlHost;
            }

            if (featureType == typeof(IInsetsManager) || featureType == typeof(IInputPane))
            {
                return _insetsManager;
            }

            if(featureType == typeof(IClipboard))
            {
                return _clipboard;
            }

            if (featureType == typeof(ILauncher))
            {
                return _launcher;
            }

            if (featureType == typeof(IScreenImpl))
            {
                return _screens;
            }
            
            return null;
        }

        private static bool IsSupported(WindowTransparencyLevel level)
        {
            if (level == WindowTransparencyLevel.None)
                return true;
            if (level == WindowTransparencyLevel.Transparent)
                return OperatingSystem.IsAndroidVersionAtLeast(30);
            if (level == WindowTransparencyLevel.Blur)
                return OperatingSystem.IsAndroidVersionAtLeast(31);
            return false;
        }

        private static void SetBlurBehind(AvaloniaMainActivity activity, int radius)
        {
            if (radius == 0)
                activity.Window?.ClearFlags(WindowManagerFlags.BlurBehind);
            else
                activity.Window?.AddFlags(WindowManagerFlags.BlurBehind);

            if (OperatingSystem.IsAndroidVersionAtLeast(31) && activity.Window?.Attributes is { } attr)
            {
                attr.BlurBehindRadius = radius;
                activity.Window.Attributes = attr;
            }
        }

        internal void TextInput(string text)
        {
            if(Input != null)
            {
                var args = new RawTextInputEventArgs(AndroidKeyboardDevice.Instance!, (ulong)DateTime.Now.Ticks, InputRoot!, text);

                Input(args);
            }
        }
    }
}
