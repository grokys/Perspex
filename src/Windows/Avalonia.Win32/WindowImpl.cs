using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Collections.Pooled;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Rendering.Composition;
using Avalonia.Win32.DirectX;
using Avalonia.Win32.Input;
using Avalonia.Win32.Interop;
using Avalonia.Win32.OpenGl;
using Avalonia.Win32.OpenGl.Angle;
using Avalonia.Win32.WinRT.Composition;
using static Avalonia.Win32.Interop.UnmanagedMethods;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Threading;
using static Avalonia.Controls.Win32Properties;
using Avalonia.Logging;

namespace Avalonia.Win32
{
    /// <summary>
    /// Window implementation for Win32 platform.
    /// </summary>
    internal partial class WindowImpl : IWindowImpl, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo, IWin32OptionsTopLevelImpl
    {
        private static readonly List<WindowImpl> s_instances = new();

        private static readonly IntPtr s_defaultCursor = LoadCursor(
            IntPtr.Zero, new IntPtr((int)UnmanagedMethods.Cursor.IDC_ARROW));

        private static readonly Dictionary<WindowEdge, HitTestValues> s_edgeLookup =
            new()
            {
                { WindowEdge.East, HitTestValues.HTRIGHT },
                { WindowEdge.North, HitTestValues.HTTOP },
                { WindowEdge.NorthEast, HitTestValues.HTTOPRIGHT },
                { WindowEdge.NorthWest, HitTestValues.HTTOPLEFT },
                { WindowEdge.South, HitTestValues.HTBOTTOM },
                { WindowEdge.SouthEast, HitTestValues.HTBOTTOMRIGHT },
                { WindowEdge.SouthWest, HitTestValues.HTBOTTOMLEFT },
                { WindowEdge.West, HitTestValues.HTLEFT }
            };

        /// <summary>
        /// The Windows DPI which equates to a <see cref="RenderScaling"/> of 1.0.
        /// </summary>
        public const double StandardDpi = 96;

        private SavedWindowInfo _savedWindowInfo;
        private bool _isFullScreenActive;
        private bool _isClientAreaExtended;
        private Thickness _extendedMargins;
        private Thickness _offScreenMargin;
        private double _extendTitleBarHint = -1;
        private WindowResizeReason _resizeReason;
        private MOUSEMOVEPOINT _lastWmMousePoint;

#if USE_MANAGED_DRAG
        private readonly ManagedWindowResizeDragHelper _managedDrag;
#endif

        private const WindowStyles WindowStateMask = (WindowStyles.WS_MAXIMIZE | WindowStyles.WS_MINIMIZE);
        private readonly TouchDevice _touchDevice;
        private readonly WindowsMouseDevice _mouseDevice;
        private readonly PenDevice _penDevice;
        private readonly FramebufferManager _framebuffer;
        private readonly object? _glSurface;
        private readonly bool _wmPointerEnabled;

        private readonly Win32NativeControlHost _nativeControlHost;
        private readonly IStorageProvider _storageProvider;
        private WindowsInputPane? _inputPane;
        private WndProc _wndProcDelegate;
        private string? _className;
        private IntPtr _hwnd;
        private IInputRoot? _owner;
        protected WindowProperties _windowProperties;
        private IconImpl? _iconImpl;
        private readonly Dictionary<(Icons type, uint dpi), Win32Icon> _iconCache = new();
        private bool _trackingMouse;//ToDo - there is something missed. Needs investigation @Steven Kirk
        private bool _topmost;
        private double _scaling = 1;
        private uint _dpi = 96;
        private WindowState _showWindowState;
        private WindowState _lastWindowState;
        private OleDropTarget? _dropTarget;
        private Size _minSize;
        private Size _maxSize;
        private POINT _maxTrackSize;
        private WindowImpl? _parent;
        private ExtendClientAreaChromeHints _extendChromeHints = ExtendClientAreaChromeHints.Default;
        private bool _isCloseRequested;
        private bool _shown;
        private bool _hiddenWindowIsParent;
        private uint _langid;
        private bool _ignoreDpiChanges;
        internal bool _ignoreWmChar;
        private WindowTransparencyLevel _transparencyLevel;
        private readonly WindowTransparencyLevel _defaultTransparencyLevel;

        private const int MaxPointerHistorySize = 512;
        private static readonly PooledList<RawPointerPoint> s_intermediatePointsPooledList = new();
        private static POINTER_TOUCH_INFO[]? s_historyTouchInfos;
        private static POINTER_PEN_INFO[]? s_historyPenInfos;
        private static POINTER_INFO[]? s_historyInfos;
        private static MOUSEMOVEPOINT[]? s_mouseHistoryInfos;
        private PlatformThemeVariant _currentThemeVariant;

        public WindowImpl()
        {
            _touchDevice = new TouchDevice();
            _mouseDevice = new WindowsMouseDevice();
            _penDevice = new PenDevice();

#if USE_MANAGED_DRAG
            _managedDrag = new ManagedWindowResizeDragHelper(this, capture =>
            {
                if (capture)
                    UnmanagedMethods.SetCapture(Handle.Handle);
                else
                    UnmanagedMethods.ReleaseCapture();
            });
#endif

            _windowProperties = new WindowProperties
            {
                ShowInTaskbar = false,
                IsResizable = true,
                Decorations = SystemDecorations.Full
            };

            var surfaceFactory = AvaloniaLocator.Current.GetService<IWindowsSurfaceFactory>();
            var glPlatform = AvaloniaLocator.Current.GetService<IPlatformGraphics>();
            UseRedirectionBitmap = surfaceFactory is null || glPlatform is null ||
                                   !surfaceFactory.RequiresNoRedirectionBitmap;

            _wmPointerEnabled = Win32Platform.WindowsVersion >= PlatformConstants.Windows8;

            CreateWindow();
            _framebuffer = new FramebufferManager(_hwnd);

            if (this is not PopupImpl)
            {
                UpdateInputMethod(GetKeyboardLayout(0));
            }

            if (glPlatform != null)
            {
                if (surfaceFactory is not null)
                {
                    _glSurface = surfaceFactory.CreateSurface(this);
                }
                else
                {
                    if (glPlatform is D3D11AngleWin32PlatformGraphics or D3D9AngleWin32PlatformGraphics)
                        _glSurface = new EglGlPlatformSurface(this);
                    else if (glPlatform is WglPlatformOpenGlInterface)
                        _glSurface = new WglGlPlatformSurface(this);
                }
            }

            Screen = Win32Platform.Instance.Screen;
            _storageProvider = new Win32StorageProvider(this);
            _inputPane = WindowsInputPane.TryCreate(this);
            _nativeControlHost = new Win32NativeControlHost(this, !UseRedirectionBitmap);
            _defaultTransparencyLevel = UseRedirectionBitmap ? WindowTransparencyLevel.None : WindowTransparencyLevel.Transparent;
            _transparencyLevel = _defaultTransparencyLevel;
            s_instances.Add(this);
        }

        internal IInputRoot Owner
            => _owner ?? throw new InvalidOperationException($"{nameof(SetInputRoot)} must have been called");

        internal WindowImpl? ParentImpl => _parent;

        public Action? Activated { get; set; }

        public Func<WindowCloseReason, bool>? Closing { get; set; }

        public Action? Closed { get; set; }

        public Action? Deactivated { get; set; }

        public Action<RawInputEventArgs>? Input { get; set; }

        public Action<Rect>? Paint { get; set; }

        public Action<Size, WindowResizeReason>? Resized { get; set; }

        public Action<double>? ScalingChanged { get; set; }

        public Action<PixelPoint>? PositionChanged { get; set; }

        public Action<WindowState>? WindowStateChanged { get; set; }

        public Action? LostFocus { get; set; }

        public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

        public Thickness BorderThickness
        {
            get
            {
                if (HasFullDecorations)
                {
                    var style = GetStyle();
                    var exStyle = GetExtendedStyle();

                    var padding = new RECT();

                    if (AdjustWindowRectEx(ref padding, (uint)style, false, (uint)exStyle))
                    {
                        return new Thickness(-padding.left, -padding.top, padding.right, padding.bottom);
                    }
                    else
                    {
                        throw new Win32Exception();
                    }
                }
                else
                {
                    return new Thickness();
                }
            }
        }

        private double PrimaryScreenRenderScaling => Screen.AllScreens.FirstOrDefault(screen => screen.IsPrimary)?.Scaling ?? 1;

        private ICompositionEffectsSurface? CompositionEffectsSurface => _glSurface as ICompositionEffectsSurface;
        private bool UseRedirectionBitmap { get; }

        public double RenderScaling => _scaling;

        public double DesktopScaling => RenderScaling;

        public Size ClientSize
        {
            get
            {
                GetClientRect(_hwnd, out var rect);

                return new Size(rect.right, rect.bottom) / RenderScaling;
            }
        }

        Size? IWindowBaseImpl.FrameSize => FrameSize;

        public Size FrameSize
        {
            get
            {
                GetWindowRect(_hwnd, out var rcWindow);
                return new Size(rcWindow.Width, rcWindow.Height) / RenderScaling;
            }
        }

        public ScreenImpl Screen { get; }

        public IPlatformHandle Handle { get; private set; }

        public virtual Size MaxAutoSizeHint => new Size(_maxTrackSize.X / RenderScaling, _maxTrackSize.Y / RenderScaling);

        public IMouseDevice MouseDevice => _mouseDevice;

        public WindowState WindowState
        {
            get
            {
                if (!IsWindowVisible(_hwnd))
                {
                    return _showWindowState;
                }

                if (_isFullScreenActive)
                {
                    return WindowState.FullScreen;
                }

                GetWindowPlacement(_hwnd, out var placement);

                return placement.ShowCmd switch
                {
                    ShowWindowCommand.Maximize => WindowState.Maximized,
                    ShowWindowCommand.Minimize => WindowState.Minimized,
                    _ => WindowState.Normal
                };
            }

            set
            {
                if (IsWindowVisible(_hwnd) && _lastWindowState != value)
                {
                    ShowWindow(value, value != WindowState.Minimized); // If the window is minimized, it shouldn't be activated
                }

                _lastWindowState = value;
                _showWindowState = value;
            }
        }

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

        protected IntPtr Hwnd => _hwnd;

        private bool IsMouseInPointerEnabled => _wmPointerEnabled && IsMouseInPointerEnabled();

        public object? TryGetFeature(Type featureType)
        {
            if (featureType == typeof(IScreenImpl))
            {
                return Screen;
            }

            if (featureType == typeof(ITextInputMethodImpl))
            {
                return Imm32InputMethod.Current;
            }

            if (featureType == typeof(INativeControlHostImpl))
            {
                return _nativeControlHost;
            }

            if (featureType == typeof(IStorageProvider))
            {
                return _storageProvider;
            }

            if (featureType == typeof(IClipboard))
            {
                return AvaloniaLocator.Current.GetRequiredService<IClipboard>();
            }

            if (featureType == typeof(IInputPane))
            {
                return _inputPane;
            }

            if (featureType == typeof(ILauncher))
            {
                return new BclLauncher();
            }

            return null;
        }

        public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels)
        {
            foreach (var level in transparencyLevels)
            {
                if (!IsSupported(level))
                    continue;

                if (level == TransparencyLevel)
                {
                    return;
                }
                if (level == WindowTransparencyLevel.Transparent)
                {
                    if (!SetTransparencyTransparent())
                        continue;
                }
                else if (level == WindowTransparencyLevel.AcrylicBlur)
                {
                    if (!SetTransparencyAcrylicBlur())
                        continue;
                }
                else if (level == WindowTransparencyLevel.Mica)
                {
                    if (!SetTransparencyMica())
                        continue;
                }

                TransparencyLevel = level;
                return;
            }

            // If we get here, we didn't find a supported level. Report the default.
            TransparencyLevel = _defaultTransparencyLevel;
        }

        private bool IsSupported(WindowTransparencyLevel level)
        {
            // None is only supported with redirection bitmap.
            // Note, it's still possible to have non-transparent window with a fallback background brush.
            if (level == WindowTransparencyLevel.None)
                return UseRedirectionBitmap;

            // Transparent is supported either with DwmEnableBlurBehindWindow (win8+) or with NoRedirectionBitmap.
            if (level == WindowTransparencyLevel.Transparent)
                return !UseRedirectionBitmap || Win32Platform.WindowsVersion >= PlatformConstants.Windows8;

            if (level == WindowTransparencyLevel.Blur)
                return CompositionEffectsSurface?.IsBlurSupported(BlurEffect.GaussianBlur) ?? false;

            if (level == WindowTransparencyLevel.AcrylicBlur)
                return CompositionEffectsSurface?.IsBlurSupported(BlurEffect.Acrylic) ?? false;

            if (level == WindowTransparencyLevel.Mica)
                return CompositionEffectsSurface?.IsBlurSupported(BlurEffect.MicaDark) ?? false;

            return false;
        }

        private bool SetTransparencyTransparent()
        {
            if (CompositionEffectsSurface is {} surface)
            {
                surface.SetBlur(BlurEffect.None);
                return true;
            }
            else
            {
                return SetLegacyTransparency(true);
            }
        }

        private bool SetTransparencyAcrylicBlur()
        {
            SetUseHostBackdropBrush(true);
            SetLegacyTransparency(false);

            CompositionEffectsSurface!.SetBlur(BlurEffect.Acrylic);
            return true;
        }

        /// <summary>
        /// Sets the transparency mica
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private bool SetTransparencyMica()
        {
            SetUseHostBackdropBrush(false);
            SetLegacyTransparency(false);

            CompositionEffectsSurface!.SetBlur(_currentThemeVariant switch
            {
                PlatformThemeVariant.Light => BlurEffect.MicaLight,
                PlatformThemeVariant.Dark => BlurEffect.MicaDark,
                _ => throw new ArgumentOutOfRangeException()
            });
            return true;
        }

        private bool SetLegacyTransparency(bool enabled)
        {
            if (Win32Platform.WindowsVersion < PlatformConstants.Windows8 || !UseRedirectionBitmap)
                return false;

            // On pre-Win8 this method was blurring a window, which is a different from desired behavior.
            // On win8+ we use this method as a fallback, when WinUI/DComp composition with true transparency isn't available.
            // Note: there is no guarantee that this behavior won't be changed back to true blur in Win12.
            // See https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmenableblurbehindwindow#remarks
            // Also https://github.com/qt/qtbase/blob/fd300f143fd30947bba60a03d614acd2711b635f/src/plugins/platforms/windows/qwindowswindow.cpp#L519
            var blurInfo = new DWM_BLURBEHIND();
            blurInfo.fEnable = enabled;
            blurInfo.dwFlags = DWM_BB.Enable | DWM_BB.BlurRegion;
            blurInfo.hRgnBlur = CreateRectRgn(0, 0, -1, -1);

            var result = DwmEnableBlurBehindWindow(_hwnd, ref blurInfo);

            if (blurInfo.hRgnBlur != default)
            {
                DeleteObject(blurInfo.hRgnBlur);
            }

            return result == 0;
        }

        private unsafe bool SetUseHostBackdropBrush(bool useHostBackdropBrush)
        {
            if (Win32Platform.WindowsVersion < WinUiCompositionShared.MinHostBackdropVersion)
                return false;

            // AcrylicBlur requires window to set DWMWA_USE_HOSTBACKDROPBRUSH flag on Win11+.
            // It's not necessary on older versions and it's not necessary with Mica brush.

            var pvUseBackdropBrush = useHostBackdropBrush ? 1 : 0;
            var result = DwmSetWindowAttribute(_hwnd, (int)DwmWindowAttribute.DWMWA_USE_HOSTBACKDROPBRUSH, &pvUseBackdropBrush, sizeof(int));
            return result == 0;
        }

        public IEnumerable<object> Surfaces
            => _glSurface is null ?
                new object[] { Handle, _framebuffer } :
                new object[] { Handle, _glSurface, _framebuffer };

        public PixelPoint Position
        {
            get
            {
                GetWindowRect(_hwnd, out var rc);
                return new PixelPoint(rc.left, rc.top);
            }
            set
            {
                SetWindowPos(
                    Handle.Handle,
                    IntPtr.Zero,
                    value.X,
                    value.Y,
                    0,
                    0,
                    SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_NOZORDER);
                
                if (ShCoreAvailable && Win32Platform.WindowsVersion >= PlatformConstants.Windows8_1)
                {
                    var monitor = MonitorFromPoint(new POINT() { X = value.X, Y = value.Y },
                        MONITOR.MONITOR_DEFAULTTONEAREST);

                    if (GetDpiForMonitor(
                            monitor,
                            MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                            out _dpi,
                            out _) == 0)
                    {
                        _scaling = _dpi / StandardDpi;
                    }
                }
            }
        }

        private bool HasFullDecorations => _windowProperties.Decorations == SystemDecorations.Full;


        public void Move(PixelPoint point) => Position = point;

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
            _minSize = minSize;
            _maxSize = maxSize;
        }

        public Compositor Compositor => Win32Platform.Compositor;

        public void Resize(Size value, WindowResizeReason reason)
        {
            int requestedClientWidth = (int)(value.Width * RenderScaling);
            int requestedClientHeight = (int)(value.Height * RenderScaling);

            GetClientRect(_hwnd, out var currentClientRect);
            if (currentClientRect.Width == requestedClientWidth && currentClientRect.Height == requestedClientHeight)
            {
                // Don't update our window position if the client size is already correct. This leads to Windows updating our
                // "normal position" (i.e. restored bounds) to match our maximised or areo snap size, which is incorrect behaviour.
                // We only want to proceed with this method if the new size is coming from Avalonia.
                return;
            }

            if (_lastWindowState == WindowState.FullScreen)
            {
                // Fullscreen mode is really a restored window without a frame filling the whole monitor.
                // It doesn't make sense to resize the window in this state, so ignore this request.
                Logger.TryGet(LogEventLevel.Warning, LogArea.Win32Platform)?.Log(this, "Ignoring resize event on fullscreen window.");
                return;
            }

            GetWindowPlacement(_hwnd, out var windowPlacement);

            var requestedClientRect = new RECT
            {
                left = 0,
                top = 0,
                right = requestedClientWidth,
                bottom = requestedClientHeight
            };

            var requestedWindowRect = _isClientAreaExtended ? requestedClientRect : ClientRectToWindowRect(requestedClientRect);
            var windowWidth = requestedWindowRect.Width;
            var windowHeight = requestedWindowRect.Height;

            if (windowWidth == windowPlacement.NormalPosition.Width && windowHeight == windowPlacement.NormalPosition.Height)
            {
                return;
            }

            // If the window is minimized, don't change the restore position, because this.Position is currently
            // out of screen with values similar to -32000,-32000. Windows considers such a position invalid on restore
            // and instead moves the window back to 0,0.
            if (windowPlacement.ShowCmd == ShowWindowCommand.ShowMinimized)
            {
                // The window is minimized but will be restored to maximized: don't change our normal size,
                // or it will incorrectly be set to the maximized size.
                if ((windowPlacement.Flags & WindowPlacementFlags.RestoreToMaximized) != 0)
                {
                    return;
                }
            }

            windowPlacement.NormalPosition.right = windowPlacement.NormalPosition.left + windowWidth;
            windowPlacement.NormalPosition.bottom = windowPlacement.NormalPosition.top + windowHeight;

            windowPlacement.ShowCmd = !_shown ? ShowWindowCommand.Hide : _lastWindowState switch
            {
                WindowState.Minimized => ShowWindowCommand.ShowMinNoActive,
                WindowState.Maximized => ShowWindowCommand.ShowMaximized,
                WindowState.Normal => ShowWindowCommand.ShowNoActivate,
                _ => throw new NotImplementedException(),
            };

            using var scope = SetResizeReason(reason);
            SetWindowPlacement(_hwnd, in windowPlacement);
        }

        public void Activate()
        {
            SetForegroundWindow(_hwnd);
        }

        public IPopupImpl? CreatePopup() => Win32Platform.UseOverlayPopups ? null : new PopupImpl(this);

        public void Dispose()
        {
            _inputPane?.Dispose();
            _inputPane = null;
            if (_hwnd != IntPtr.Zero)
            {
                // Detect if we are being closed programmatically - this would mean that WM_CLOSE was not called
                // and we didn't prepare this window for destruction.
                if (!_isCloseRequested)
                {
                    BeforeCloseCleanup(true);
                }

                DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }

            ClearIconCache();
        }

        public void Invalidate(Rect rect)
        {
            var scaling = RenderScaling;
            var r = new RECT
            {
                left = (int)Math.Floor(rect.X * scaling),
                top = (int)Math.Floor(rect.Y * scaling),
                right = (int)Math.Ceiling(rect.Right * scaling),
                bottom = (int)Math.Ceiling(rect.Bottom * scaling),
            };

            InvalidateRect(_hwnd, ref r, false);
        }

        /// <summary>
        /// Transform a screen pixel point to the point in the client area.<br/>
        /// To transform a point with precise value, use the <see cref="PointToClient(Point)"/> overload instead.
        /// </summary>
        /// <param name="point">The screen pixel point to be transformed.</param>
        /// <returns>The point in the client area.</returns>
        public Point PointToClient(PixelPoint point)
        {
            var p = new POINT { X = point.X, Y = point.Y };
            ScreenToClient(_hwnd, ref p);
            return new Point(p.X, p.Y) / RenderScaling;
        }

        /// <summary>
        /// Transform a screen point to the point in the client area.<br/>
        /// Comparing to the <see cref="PixelPoint"/> overload, this method receives double values and can be more precise.
        /// </summary>
        /// <param name="point">The screen point to be transformed.</param>
        /// <returns>The point in the client area.</returns>
        public Point PointToClient(Point point)
        {
            var p = new POINT { X = 0, Y = 0 };
            ClientToScreen(_hwnd, ref p);
            return new Point(point.X - p.X, point.Y - p.Y) / RenderScaling;
        }

        public PixelPoint PointToScreen(Point point)
        {
            point *= RenderScaling;
            var p = new POINT { X = (int)point.X, Y = (int)point.Y };
            ClientToScreen(_hwnd, ref p);
            return new PixelPoint(p.X, p.Y);
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            _owner = inputRoot;
            CreateDropTarget(inputRoot);
        }

        public void Hide()
        {
            UnmanagedMethods.ShowWindow(_hwnd, ShowWindowCommand.Hide);
        }

        public virtual void Show(bool activate, bool isDialog)
        {
            SetParent(_parent);
            ShowWindow(_showWindowState, activate);
        }

        public Action? GotInputWhenDisabled { get; set; }

        public void SetParent(IWindowImpl? parent)
        {
            _parent = parent as WindowImpl;

            var parentHwnd = _parent?._hwnd ?? IntPtr.Zero;

            if (parentHwnd == IntPtr.Zero && !_windowProperties.ShowInTaskbar)
            {
                parentHwnd = OffscreenParentWindow.Handle;
            }

            _hiddenWindowIsParent = parentHwnd == OffscreenParentWindow.Handle;

            // I can't find mention of this *anywhere* online, but it seems that setting
            // GWL_HWNDPARENT to a window which is on the non-primary monitor can cause two
            // WM_DPICHANGED messages to be sent: the first changing the DPI to the parent's DPI,
            // then another changing the DPI back. This then causes Windows to provide an incorrect
            // suggested new rectangle to the WM_DPICHANGED message if the window is immediately
            // moved to the parent window's monitor (e.g. when using
            // WindowStartupLocation.CenterOwner) causing the window to be shown with an incorrect
            // size.
            //
            // Just ignore any WM_DPICHANGED while we're setting the parent as this shouldn't
            // change the DPI anyway.
            _ignoreDpiChanges = true;
            SetWindowLongPtr(_hwnd, (int)WindowLongParam.GWL_HWNDPARENT, parentHwnd);
            _ignoreDpiChanges = false;

            // Windows doesn't seem to respect the HWND_TOPMOST flag of a window when showing an owned window for the first time.
            // So we set the HWND_TOPMOST again before the owned window is shown. This only needs to be done once.
            (parent as WindowImpl)?.EnsureTopmost();
        }

        public void SetEnabled(bool enable) => EnableWindow(_hwnd, enable);

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {
            e.Pointer.Capture(null);

            Dispatcher.UIThread.Post(() =>
            {
                if (e.Pointer.IsPrimary)
                {
                    // SendMessage's return value is dependent on the message send.  WM_SYSCOMMAND
                    // and WM_LBUTTONUP return value just signify whether the WndProc handled the
                    // message or not, so they are not interesting

                    SendMessage(_hwnd, (int)WindowsMessage.WM_SYSCOMMAND, (IntPtr)SC_MOUSEMOVE, IntPtr.Zero);
                    SendMessage(_hwnd, (int)WindowsMessage.WM_LBUTTONUP, IntPtr.Zero, IntPtr.Zero);
                }
                else
                {
                    throw new InvalidOperationException("BeginMoveDrag Failed");
                }
            }, DispatcherPriority.Send);
        }

        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {
            if (_windowProperties.IsResizable)
            {
#if USE_MANAGED_DRAG
                _managedDrag.BeginResizeDrag(edge, ScreenToClient(MouseDevice.Position.ToPoint(_scaling)));
#else
                e.Pointer.Capture(null);
                DefWindowProc(_hwnd, (int)WindowsMessage.WM_NCLBUTTONDOWN,
                    new IntPtr((int)s_edgeLookup[edge]), IntPtr.Zero);
#endif
            }
        }

        public void SetTitle(string? title)
        {
            SetWindowText(_hwnd, title);
        }

        public void SetCursor(ICursorImpl? cursor)
        {
            var impl = cursor as CursorImpl;

            var hCursor = impl?.Handle ?? s_defaultCursor;
            SetClassLong(_hwnd, ClassLongIndex.GCLP_HCURSOR, hCursor);

            UnmanagedMethods.SetCursor(hCursor);    
        }

        public void SetIcon(IWindowIconImpl? icon)
        {
            _iconImpl = (IconImpl?)icon;
            ClearIconCache();
            RefreshIcon();
        }

        private void ClearIconCache()
        {
            foreach (var icon in _iconCache.Values)
            {
                icon.Dispose();
            }
            _iconCache.Clear();
        }

        private Win32Icon? LoadIcon(Icons type, uint dpi)
        {
            if (_iconImpl == null)
            {
                return null;
            }

            if (type == Icons.ICON_SMALL2)
            {
                type = Icons.ICON_SMALL;
            }

            var iconKey = (type, dpi);
            if (!_iconCache.TryGetValue(iconKey, out var icon))
            {
                var scale = dpi / 96.0;
                _iconCache[iconKey] = icon = type switch
                {
                    Icons.ICON_SMALL => _iconImpl.LoadSmallIcon(scale),
                    Icons.ICON_BIG => _iconImpl.LoadBigIcon(scale),
                    _ => throw new NotImplementedException(),
                };
            }

            return icon;
        }

        private void RefreshIcon()
        {
            SendMessage(_hwnd, (int)WindowsMessage.WM_SETICON, (nint)Icons.ICON_SMALL, LoadIcon(Icons.ICON_SMALL, _dpi)?.Handle ?? default);
            SendMessage(_hwnd, (int)WindowsMessage.WM_SETICON, (nint)Icons.ICON_BIG, LoadIcon(Icons.ICON_BIG, _dpi)?.Handle ?? default);

            TaskBarList.SetOverlayIcon(_hwnd, default, null); // This will prompt the taskbar to redraw the icon
        }

        public void ShowTaskbarIcon(bool value)
        {
            var newWindowProperties = _windowProperties;

            newWindowProperties.ShowInTaskbar = value;

            UpdateWindowProperties(newWindowProperties);
        }

        public void CanResize(bool value)
        {
            var newWindowProperties = _windowProperties;

            newWindowProperties.IsResizable = value;

            UpdateWindowProperties(newWindowProperties);
        }

        public void SetSystemDecorations(SystemDecorations value)
        {
            var newWindowProperties = _windowProperties;

            newWindowProperties.Decorations = value;

            UpdateWindowProperties(newWindowProperties);
        }

        public void SetTopmost(bool value)
        {
            if (value == _topmost)
            {
                return;
            }

            IntPtr hWndInsertAfter = value ? WindowPosZOrder.HWND_TOPMOST : WindowPosZOrder.HWND_NOTOPMOST;
            SetWindowPos(_hwnd,
                hWndInsertAfter,
                0, 0, 0, 0,
                SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE);

            _topmost = value;
        }

        private void EnsureTopmost()
        {
            if(_topmost)
            {
                SetWindowPos(_hwnd,
                    WindowPosZOrder.HWND_TOPMOST,
                    0, 0, 0, 0,
                    SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE);
            }
        }

        public unsafe void SetFrameThemeVariant(PlatformThemeVariant themeVariant)
        {
            _currentThemeVariant = themeVariant;
            if (Win32Platform.WindowsVersion.Build >= 22000)
            {
                var pvUseBackdropBrush = themeVariant == PlatformThemeVariant.Dark ? 1 : 0;
                DwmSetWindowAttribute(
                    _hwnd,
                    (int)DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE,
                        &pvUseBackdropBrush,
                        sizeof(int));
                if (TransparencyLevel == WindowTransparencyLevel.Mica)
                {
                    SetTransparencyMica();
                }
            }
        }

        protected virtual IntPtr CreateWindowOverride(ushort atom)
        {
            return CreateWindowEx(
                UseRedirectionBitmap ? 0 : (int)WindowStyles.WS_EX_NOREDIRECTIONBITMAP,
                atom,
                null,
                (int)WindowStyles.WS_OVERLAPPEDWINDOW | (int)WindowStyles.WS_CLIPCHILDREN,
                CW_USEDEFAULT,
                CW_USEDEFAULT,
                CW_USEDEFAULT,
                CW_USEDEFAULT,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
        }

        [MemberNotNull(nameof(_wndProcDelegate))]
        [MemberNotNull(nameof(_className))]
        [MemberNotNull(nameof(Handle))]
        private void CreateWindow()
        {
            // Ensure that the delegate doesn't get garbage collected by storing it as a field.
            _wndProcDelegate = WndProcMessageHandler;

            _className = $"Avalonia-{Guid.NewGuid().ToString()}";

            // Unique DC helps with performance when using Gpu based rendering
            const ClassStyles windowClassStyle = ClassStyles.CS_OWNDC | ClassStyles.CS_HREDRAW | ClassStyles.CS_VREDRAW;

            var wndClassEx = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                style = (int)windowClassStyle,
                lpfnWndProc = _wndProcDelegate,
                hInstance = GetModuleHandle(null),
                hCursor = s_defaultCursor,
                hbrBackground = IntPtr.Zero,
                lpszClassName = _className
            };

            ushort atom = RegisterClassEx(ref wndClassEx);

            if (atom == 0)
            {
                throw new Win32Exception();
            }

            _hwnd = CreateWindowOverride(atom);

            if (_hwnd == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            Handle = new WindowImplPlatformHandle(this);

                RegisterTouchWindow(_hwnd, 0);

            if (ShCoreAvailable && Win32Platform.WindowsVersion >= PlatformConstants.Windows8_1)
            {
                var monitor = MonitorFromWindow(
                    _hwnd,
                    MONITOR.MONITOR_DEFAULTTONEAREST);

                if (GetDpiForMonitor(
                    monitor,
                    MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                    out _dpi,
                    out _) == 0)
                {
                    _scaling = _dpi / StandardDpi;
                }
            }
        }

        private IntPtr WndProcMessageHandler(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            bool handled = false;
            IntPtr ret = IntPtr.Zero;

            if (WndProcHookCallback is { } callback)
                ret = callback(hWnd, msg, wParam, lParam, ref handled);

            if (handled)
                return ret;

            return WndProc(hWnd, msg, wParam, lParam);
        }

        private void CreateDropTarget(IInputRoot inputRoot)
        {
            if (AvaloniaLocator.Current.GetService<IDragDropDevice>() is { } dragDropDevice)
            {
                var odt = new OleDropTarget(this, inputRoot, dragDropDevice);

                if (OleContext.Current?.RegisterDragDrop(Handle, odt) ?? false)
                {
                    _dropTarget = odt;
                }
            }
        }

        /// <summary>
        /// Ported from https://github.com/chromium/chromium/blob/master/ui/views/win/fullscreen_handler.cc
        /// Method must only be called from inside UpdateWindowProperties.
        /// </summary>
        /// <param name="fullscreen"></param>
        private void SetFullScreen(bool fullscreen)
        {
            if (fullscreen)
            {
                GetWindowRect(_hwnd, out var windowRect);

                _savedWindowInfo.WindowRect = windowRect;

                var current = GetStyle();
                var currentEx = GetExtendedStyle();

                _savedWindowInfo.Style = current;
                _savedWindowInfo.ExStyle = currentEx;

                // Set new window style and size.
                SetStyle(current & ~(WindowStyles.WS_CAPTION | WindowStyles.WS_THICKFRAME), false);
                SetExtendedStyle(currentEx & ~(WindowStyles.WS_EX_DLGMODALFRAME | WindowStyles.WS_EX_WINDOWEDGE | WindowStyles.WS_EX_CLIENTEDGE | WindowStyles.WS_EX_STATICEDGE), false);

                // On expand, if we're given a window_rect, grow to it, otherwise do
                // not resize.
                var screen = Screen.ScreenFromHwnd(_hwnd, MONITOR.MONITOR_DEFAULTTONEAREST);
                if (screen?.Bounds is { } window_rect)
                {
                    _isFullScreenActive = true;
                    SetWindowPos(_hwnd, IntPtr.Zero, window_rect.X, window_rect.Y,
                                 window_rect.Width, window_rect.Height,
                                 SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_FRAMECHANGED);
                }
            }
            else
            {
                // Reset original window style and size.  The multiple window size/moves
                // here are ugly, but if SetWindowPos() doesn't redraw, the taskbar won't be
                // repainted.  Better-looking methods welcome.
                _isFullScreenActive = false;

                var windowStates = GetWindowStateStyles();
                SetStyle((_savedWindowInfo.Style & ~WindowStateMask) | windowStates, false);
                SetExtendedStyle(_savedWindowInfo.ExStyle, false);

                // On restore, resize to the previous saved rect size.
                var newClientRect = _savedWindowInfo.WindowRect.ToPixelRect();

                SetWindowPos(_hwnd, IntPtr.Zero, newClientRect.X, newClientRect.Y, newClientRect.Width,
                             newClientRect.Height,
                            SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_FRAMECHANGED);

                UpdateWindowProperties(_windowProperties, true);
            }

            TaskBarList.MarkFullscreen(_hwnd, fullscreen);

            ExtendClientArea();
        }

        private MARGINS UpdateExtendMargins()
        {
            RECT borderThickness = new RECT();
            RECT borderCaptionThickness = new RECT();

            AdjustWindowRectEx(ref borderCaptionThickness, (uint)(GetStyle()), false, 0);
            AdjustWindowRectEx(ref borderThickness, (uint)(GetStyle() & ~WindowStyles.WS_CAPTION), false, 0);
            borderThickness.left *= -1;
            borderThickness.top *= -1;
            borderCaptionThickness.left *= -1;
            borderCaptionThickness.top *= -1;

            bool wantsTitleBar = _extendChromeHints.HasAllFlags(ExtendClientAreaChromeHints.SystemChrome) || _extendTitleBarHint == -1;

            if (!wantsTitleBar)
            {
                borderCaptionThickness.top = 1;
            }

            //using a default margin of 0 when using WinUiComp removes artefacts when resizing. See issue #8316
            var defaultMargin = UseRedirectionBitmap ? 1 : 0;

            MARGINS margins = new MARGINS();
            margins.cxLeftWidth = defaultMargin;
            margins.cxRightWidth = defaultMargin;
            margins.cyBottomHeight = defaultMargin;

            if (_extendTitleBarHint != -1)
            {
                borderCaptionThickness.top = (int)(_extendTitleBarHint * RenderScaling);
            }

            margins.cyTopHeight = _extendChromeHints.HasAllFlags(ExtendClientAreaChromeHints.SystemChrome) && !_extendChromeHints.HasAllFlags(ExtendClientAreaChromeHints.PreferSystemChrome) ? borderCaptionThickness.top : defaultMargin;

            if (WindowState == WindowState.Maximized)
            {
                _extendedMargins = new Thickness(0, (borderCaptionThickness.top - borderThickness.top) / RenderScaling, 0, 0);
                _offScreenMargin = new Thickness(borderThickness.left / PrimaryScreenRenderScaling, borderThickness.top / PrimaryScreenRenderScaling, borderThickness.right / PrimaryScreenRenderScaling, borderThickness.bottom / PrimaryScreenRenderScaling);
            }
            else
            {
                _extendedMargins = new Thickness(0, (borderCaptionThickness.top) / RenderScaling, 0, 0);
                _offScreenMargin = new Thickness();
            }

            return margins;
        }

        private void ExtendClientArea()
        {
            if (!_shown)
            {
                return;
            }

            if (DwmIsCompositionEnabled(out bool compositionEnabled) < 0 || !compositionEnabled)
            {
                _isClientAreaExtended = false;
                return;
            }
            GetWindowRect(_hwnd, out var rcWindow);

            if (_isClientAreaExtended && WindowState != WindowState.FullScreen)
            {
                var margins = UpdateExtendMargins();
                DwmExtendFrameIntoClientArea(_hwnd, ref margins);

                unsafe
                {
                    int cornerPreference = (int)DwmWindowCornerPreference.DWMWCP_ROUND;
                    DwmSetWindowAttribute(_hwnd, (int)DwmWindowAttribute.DWMWA_WINDOW_CORNER_PREFERENCE, &cornerPreference, sizeof(int));
                }
            }
            else
            {
                var margins = new MARGINS();
                DwmExtendFrameIntoClientArea(_hwnd, ref margins);

                _offScreenMargin = new Thickness();
                _extendedMargins = new Thickness();

                unsafe
                {
                    int cornerPreference = (int)DwmWindowCornerPreference.DWMWCP_DEFAULT;
                    DwmSetWindowAttribute(_hwnd, (int)DwmWindowAttribute.DWMWA_WINDOW_CORNER_PREFERENCE, &cornerPreference, sizeof(int));
                }
            }

            if (!_isClientAreaExtended || (_extendChromeHints.HasAllFlags(ExtendClientAreaChromeHints.SystemChrome) &&
                !_extendChromeHints.HasAllFlags(ExtendClientAreaChromeHints.PreferSystemChrome)))
            {
                EnableCloseButton(_hwnd);
            }
            else
            {
                DisableCloseButton(_hwnd);
            }

            // Inform the application of the frame change.
            SetWindowPos(_hwnd,
                IntPtr.Zero,
                rcWindow.left, rcWindow.top,
                0, 0,
                SetWindowPosFlags.SWP_FRAMECHANGED | SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_NOSIZE);

            ExtendClientAreaToDecorationsChanged?.Invoke(_isClientAreaExtended);
        }

        private void ShowWindow(WindowState state, bool activate)
        {
            if (_isClientAreaExtended)
            {
                ExtendClientArea();
            }

            ShowWindowCommand? command;

            var newWindowProperties = _windowProperties;

            switch (state)
            {
                case WindowState.Minimized:
                    newWindowProperties.IsFullScreen = false;
                    command = ShowWindowCommand.Minimize;
                    break;
                case WindowState.Maximized:
                    newWindowProperties.IsFullScreen = false;
                    command = ShowWindowCommand.Maximize;
                    break;

                case WindowState.Normal:
                    newWindowProperties.IsFullScreen = false;
                    command = IsWindowVisible(_hwnd) ? ShowWindowCommand.Restore :
                        activate ? ShowWindowCommand.Normal : ShowWindowCommand.ShowNoActivate;
                    break;

                case WindowState.FullScreen:
                    newWindowProperties.IsFullScreen = true;
                    command = IsWindowVisible(_hwnd) ? null : ShowWindowCommand.Restore;
                    break;

                default:
                    throw new ArgumentException("Invalid WindowState.");
            }

            newWindowProperties.WindowState = state;

            UpdateWindowProperties(newWindowProperties, newWindowProperties.Decorations != SystemDecorations.Full);

            if (command.HasValue)
            {
                UnmanagedMethods.ShowWindow(_hwnd, command.Value);
            }

            if (state == WindowState.Maximized)
            {
                MaximizeWithoutCoveringTaskbar();
            }

            if (!Design.IsDesignMode && activate)
            {
                SetFocus(_hwnd);
                SetForegroundWindow(_hwnd);
            }
        }

        private void BeforeCloseCleanup(bool isDisposing)
        {
            // Based on https://github.com/dotnet/wpf/blob/master/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Window.cs#L4270-L4337
            // We need to enable parent window before destroying child window to prevent OS from activating a random window behind us (or last active window).
            // This is described here: https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enablewindow#remarks
            // We need to verify if parent is still alive (perhaps it got destroyed somehow).
            if (_parent != null && IsWindow(_parent._hwnd))
            {
                var wasActive = GetActiveWindow() == _hwnd;

                // We can only set enabled state if we are not disposing - generally Dispose happens after enabled state has been set.
                // Ignoring this would cause us to enable a window that might be disabled.
                if (!isDisposing)
                {
                    // Our window closed callback will set enabled state to a correct value after child window gets destroyed.
                    _parent.SetEnabled(true);
                }

                // We also need to activate our parent window since again OS might try to activate a window behind if it is not set.
                if (wasActive)
                {
                    SetActiveWindow(_parent._hwnd);
                }
            }
        }

        private void AfterCloseCleanup()
        {
            if (_className != null)
            {
                UnregisterClass(_className, GetModuleHandle(null));
                _className = null;
            }
        }

        private void MaximizeWithoutCoveringTaskbar()
        {
            var screen = Screen.ScreenFromHwnd(Hwnd, MONITOR.MONITOR_DEFAULTTONEAREST);
            if (screen?.WorkingArea is { } workingArea)
            {
                var x = workingArea.X;
                var y = workingArea.Y;
                var cx = workingArea.Width;
                var cy = workingArea.Height;
                var style = (WindowStyles)GetWindowLong(_hwnd, (int)WindowLongParam.GWL_STYLE);

                if (!style.HasFlag(WindowStyles.WS_THICKFRAME))
                {
                    // When calling SetWindowPos on a maximized window it automatically adjusts
                    // for "hidden" borders which are placed offscreen, EVEN IF THE WINDOW HAS
                    // NO BORDERS, meaning that the window is placed wrong when we have CanResize
                    // == false. Account for this here.
                    var borderThickness = BorderThickness;
                    x -= (int)borderThickness.Left;
                    cx += (int)borderThickness.Left + (int)borderThickness.Right;
                    cy += (int)borderThickness.Bottom;
                }

                SetWindowPos(_hwnd, WindowPosZOrder.HWND_NOTOPMOST, x, y, cx, cy, SetWindowPosFlags.SWP_SHOWWINDOW | SetWindowPosFlags.SWP_FRAMECHANGED);
            }
        }

        private WindowStyles GetWindowStateStyles()
        {
            return GetStyle() & WindowStateMask;
        }

        private WindowStyles GetStyle()
        {
            if (_isFullScreenActive)
            {
                return _savedWindowInfo.Style;
            }
            else
            {
                return (WindowStyles)GetWindowLong(_hwnd, (int)WindowLongParam.GWL_STYLE);
            }
        }

        private WindowStyles GetExtendedStyle()
        {
            if (_isFullScreenActive)
            {
                return _savedWindowInfo.ExStyle;
            }
            else
            {
                return (WindowStyles)GetWindowLong(_hwnd, (int)WindowLongParam.GWL_EXSTYLE);
            }
        }

        private void SetStyle(WindowStyles style, bool save = true)
        {
            if (save)
            {
                _savedWindowInfo.Style = style;
            }

            if (!_isFullScreenActive)
            {
                SetWindowLong(_hwnd, (int)WindowLongParam.GWL_STYLE, (uint)style);
            }
        }

        private void SetExtendedStyle(WindowStyles style, bool save = true)
        {
            if (save)
            {
                _savedWindowInfo.ExStyle = style;
            }

            if (!_isFullScreenActive)
            {
                SetWindowLong(_hwnd, (int)WindowLongParam.GWL_EXSTYLE, (uint)style);
            }
        }

        private void UpdateWindowProperties(WindowProperties newProperties, bool forceChanges = false)
        {
            var oldProperties = _windowProperties;

            // Calling SetWindowPos will cause events to be sent and we need to respond
            // according to the new values already.
            _windowProperties = newProperties;

            if (oldProperties.IsFullScreen == newProperties.IsFullScreen)
            {
                var exStyle = WindowStyles.WS_EX_WINDOWEDGE | (UseRedirectionBitmap ? 0 : WindowStyles.WS_EX_NOREDIRECTIONBITMAP);

                if ((oldProperties.ShowInTaskbar != newProperties.ShowInTaskbar) || forceChanges)
                {
                    if (newProperties.ShowInTaskbar)
                    {
                        exStyle |= WindowStyles.WS_EX_APPWINDOW;

                        if (_hiddenWindowIsParent)
                        {
                            // Can't enable the taskbar icon by clearing the parent window unless the window
                            // is hidden. Hide the window and show it again with the same activation state
                            // when we've finished. Interestingly it seems to work fine the other way.
                            var shown = IsWindowVisible(_hwnd);
                            var activated = GetActiveWindow() == _hwnd;

                            if (shown)
                                Hide();

                            _hiddenWindowIsParent = false;
                            SetParent(null);

                            if (shown)
                                Show(activated, false);
                        }
                    }
                    else
                    {
                        // To hide a non-owned window's taskbar icon we need to parent it to a hidden window.
                        if (_parent is null)
                        {
                            SetWindowLongPtr(_hwnd, (int)WindowLongParam.GWL_HWNDPARENT, OffscreenParentWindow.Handle);
                            _hiddenWindowIsParent = true;
                        }

                        exStyle &= ~WindowStyles.WS_EX_APPWINDOW;
                    }
                }

                if (newProperties.ShowInTaskbar)
                {
                    exStyle |= WindowStyles.WS_EX_APPWINDOW;
                }
                else
                {
                    exStyle &= ~WindowStyles.WS_EX_APPWINDOW;
                }

                WindowStyles style = WindowStyles.WS_CLIPCHILDREN | WindowStyles.WS_OVERLAPPEDWINDOW | WindowStyles.WS_CLIPSIBLINGS;

                if (this is EmbeddedWindowImpl)
                    style |= WindowStyles.WS_CHILD;

                if (IsWindowVisible(_hwnd))
                    style |= WindowStyles.WS_VISIBLE;

                if (newProperties.IsResizable || newProperties.WindowState == WindowState.Maximized)
                {
                    style |= WindowStyles.WS_THICKFRAME;
                    style |= WindowStyles.WS_MAXIMIZEBOX;
                }
                else
                {
                    style &= ~WindowStyles.WS_THICKFRAME;
                    style &= ~WindowStyles.WS_MAXIMIZEBOX;
                }

                const WindowStyles fullDecorationFlags = WindowStyles.WS_CAPTION | WindowStyles.WS_SYSMENU | WindowStyles.WS_BORDER;

                if (newProperties.Decorations == SystemDecorations.Full)
                {
                    style |= fullDecorationFlags;
                }
                else
                {
                    style &= ~(fullDecorationFlags | WindowStyles.WS_THICKFRAME);

                    if (newProperties.Decorations == SystemDecorations.BorderOnly && newProperties.WindowState != WindowState.Maximized && newProperties.IsResizable)
                    {
                        style |= WindowStyles.WS_THICKFRAME | WindowStyles.WS_BORDER;
                    }
                }

                var windowStates = GetWindowStateStyles();
                style &= ~WindowStateMask;
                style |= windowStates;

                _savedWindowInfo.Style = style;
                _savedWindowInfo.ExStyle = exStyle;

                if (WindowStylesCallback is { } callback)
                {
                    var (s, e) = callback((uint)style, (uint)exStyle);

                    style = (WindowStyles)s;
                    exStyle = (WindowStyles)e;
                }

                SetStyle(style);
                SetExtendedStyle(exStyle);
            }
            else
                SetFullScreen(newProperties.IsFullScreen);

            if (!_isFullScreenActive && ((oldProperties.Decorations != newProperties.Decorations) || forceChanges))
            {
                var margin = newProperties.Decorations == SystemDecorations.BorderOnly ? 1 : 0;

                var margins = new MARGINS
                {
                    cyBottomHeight = margin,
                    cxRightWidth = margin,
                    cxLeftWidth = margin,
                    cyTopHeight = margin
                };

                DwmExtendFrameIntoClientArea(_hwnd, ref margins);

                if (_shown || forceChanges)
                {
                    SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0 ,0,
                        SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE |
                        SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOMOVE |
                        SetWindowPosFlags.SWP_FRAMECHANGED);
                }
            }

            // Ensure window state if decorations change
            if (_shown && oldProperties.Decorations != newProperties.Decorations)
                ShowWindow(WindowState, false);
        }

        private const int MF_BYCOMMAND = 0x0;
        private const int MF_ENABLED = 0x0;
        private const int MF_GRAYED = 0x1;
        private const int MF_DISABLED = 0x2;
        private const int SC_CLOSE = 0xF060;

        private static void DisableCloseButton(IntPtr hwnd)
        {
            EnableMenuItem(GetSystemMenu(hwnd, false), SC_CLOSE,
                           MF_BYCOMMAND | MF_DISABLED | MF_GRAYED);
        }

        private static void EnableCloseButton(IntPtr hwnd)
        {
            EnableMenuItem(GetSystemMenu(hwnd, false), SC_CLOSE,
                           MF_BYCOMMAND | MF_ENABLED);
        }

        private RECT ClientRectToWindowRect(RECT clientRect, WindowStyles? styleOverride = null, WindowStyles? extendedStyleOverride = null)
        {
            var style = styleOverride ?? GetStyle();
            var extendedStyle = extendedStyleOverride ?? GetExtendedStyle();

            var result = Win32Platform.WindowsVersion < PlatformConstants.Windows10_1607
                ? AdjustWindowRectEx(ref clientRect, (uint)style, false, (uint)extendedStyle)
                : AdjustWindowRectExForDpi(ref clientRect, style, false, extendedStyle, (uint)(RenderScaling * StandardDpi));

            if (!result)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            return clientRect;
        }

#if USE_MANAGED_DRAG
        private Point ScreenToClient(Point point)
        {
            var p = new UnmanagedMethods.POINT { X = (int)point.X, Y = (int)point.Y };
            UnmanagedMethods.ScreenToClient(_hwnd, ref p);
            return new Point(p.X, p.Y);
        }
#endif

        PixelSize EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Size
        {
            get
            {
                GetClientRect(_hwnd, out var rect);

                return new PixelSize(
                    Math.Max(1, rect.right - rect.left),
                    Math.Max(1, rect.bottom - rect.top));
            }
        }

        double EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Scaling => RenderScaling;

        IntPtr EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Handle => Handle.Handle;

        public void SetExtendClientAreaToDecorationsHint(bool hint)
        {
            _isClientAreaExtended = hint;

            ExtendClientArea();
        }

        public void SetExtendClientAreaChromeHints(ExtendClientAreaChromeHints hints)
        {
            _extendChromeHints = hints;

            ExtendClientArea();
        }

        /// <inheritdoc/>
        public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight)
        {
            _extendTitleBarHint = titleBarHeight;

            ExtendClientArea();
        }

        /// <inheritdoc/>
        public void GetWindowsZOrder(Span<Window> windows, Span<long> zOrder)
        {
            var handlesToIndex = new Dictionary<IntPtr, int>(windows.Length);
            var outputArray = new long[windows.Length];

            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i].PlatformImpl is WindowImpl platformImpl)
                    handlesToIndex.Add(platformImpl.Handle.Handle, i);
            }

            long nextZOrder = 0;
            bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam)
            {
                if (handlesToIndex.TryGetValue(hWnd, out var index))
                {
                    // We negate the z-order so that the topmost window has the highest number.
                    outputArray[index] = -nextZOrder;
                    nextZOrder++;
                }
                return nextZOrder < outputArray.Length;
            }

            EnumChildWindows(IntPtr.Zero, EnumWindowsProc, IntPtr.Zero);

            for (int i = 0; i < windows.Length; i++)
            {
                zOrder[i] = outputArray[i];
            }
        }

        /// <inheritdoc/>
        public bool IsClientAreaExtendedToDecorations => _isClientAreaExtended;

        /// <inheritdoc/>
        public Action<bool>? ExtendClientAreaToDecorationsChanged { get; set; }

        /// <inheritdoc/>
        public bool NeedsManagedDecorations => _isClientAreaExtended && _extendChromeHints.HasAllFlags(ExtendClientAreaChromeHints.PreferSystemChrome);

        /// <inheritdoc/>
        public Thickness ExtendedMargins => _extendedMargins;

        /// <inheritdoc/>
        public Thickness OffScreenMargin => _offScreenMargin;

        /// <inheritdoc/>
        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels(1, 0.8, 0);

        /// <inheritdoc/>
        public CustomWindowStylesCallback? WindowStylesCallback { get; set; }

        /// <inheritdoc/>
        public CustomWndProcHookCallback? WndProcHookCallback { get; set; }

        private ResizeReasonScope SetResizeReason(WindowResizeReason reason)
        {
            var old = _resizeReason;
            _resizeReason = reason;
            return new ResizeReasonScope(this, old);
        }

        private struct SavedWindowInfo
        {
            public WindowStyles Style { get; set; }
            public WindowStyles ExStyle { get; set; }
            public RECT WindowRect { get; set; }
        };

        protected struct WindowProperties
        {
            public bool ShowInTaskbar;
            public bool IsResizable;
            public SystemDecorations Decorations;
            public bool IsFullScreen;
            public WindowState WindowState;
        }

        private struct ResizeReasonScope : IDisposable
        {
            private readonly WindowImpl _owner;
            private readonly WindowResizeReason _restore;

            public ResizeReasonScope(WindowImpl owner, WindowResizeReason restore)
            {
                _owner = owner;
                _restore = restore;
            }

            public void Dispose() => _owner._resizeReason = _restore;
        }

        private class WindowImplPlatformHandle : INativePlatformHandleSurface
        {
            private readonly WindowImpl _owner;
            public WindowImplPlatformHandle(WindowImpl owner) => _owner = owner;
            public IntPtr Handle => _owner.Hwnd;
            public string HandleDescriptor => PlatformConstants.WindowHandleType;

            public PixelSize Size => PixelSize.FromSize(_owner.ClientSize, Scaling);

            public double Scaling => _owner.RenderScaling;
        }
    }
}
