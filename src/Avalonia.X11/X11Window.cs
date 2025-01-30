using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.FreeDesktop;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.X11.Glx;
using Avalonia.X11.NativeDialogs;
using static Avalonia.X11.XLib;
using Avalonia.Input.Platform;
using System.Runtime.InteropServices;
using Avalonia.Dialogs;
using Avalonia.Platform.Storage.FileIO;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace Avalonia.X11
{
    internal unsafe partial class X11Window : IWindowImpl, IPopupImpl, IXI2Client,
        IX11OptionsToplevelImplFeature
    {
        private readonly AvaloniaX11Platform _platform;
        private readonly bool _popup;
        private readonly bool _overrideRedirect;
        private readonly X11Info _x11;
        private XConfigureEvent? _configure;
        private PixelPoint? _configurePoint;
        private bool _triggeredExpose;
        private IInputRoot? _inputRoot;
        private readonly MouseDevice _mouse;
        private readonly TouchDevice _touch;
        private readonly IKeyboardDevice _keyboard;
        private readonly ITopLevelNativeMenuExporter? _nativeMenuExporter;
        private readonly IStorageProvider _storageProvider;
        private readonly X11NativeControlHost _nativeControlHost;
        private PixelPoint? _position;
        private PixelSize _realSize;
        private bool _cleaningUp;
        private IntPtr _handle;
        private IntPtr _xic;
        private IntPtr _renderHandle;
        private IntPtr _xSyncCounter;
        private XSyncValue _xSyncValue;
        private XSyncState _xSyncState = 0;
        private bool _mapped;
        private bool _wasMappedAtLeastOnce = false;
        private double? _scalingOverride;
        private bool _disabled;
        private TransparencyHelper? _transparencyHelper;
        private RawEventGrouper? _rawEventGrouper;
        private bool _useRenderWindow = false;
        private bool _useCompositorDrivenRenderWindowResize = false;
        private bool _usePositioningFlags = false;
        private X11WindowMode _mode;

        private enum XSyncState
        {
            None,
            WaitConfigure,
            WaitPaint
        }

        public X11Window(AvaloniaX11Platform platform, IWindowImpl? popupParent, bool overrideRedirect = false)
            : this(platform, popupParent,
                platform.Options.EnableInputFocusProxy
                    ? new InputProxyWindowMode()
                    : new DefaultTopLevelWindowMode(),
                overrideRedirect)
        {

        }

        public X11Window(AvaloniaX11Platform platform, IWindowImpl? popupParent, X11WindowMode mode,
            bool overrideRedirect = false)
        {
            _platform = platform;
            _mode = mode;
            _mode.Init(this);
            _popup = popupParent != null;
            _overrideRedirect = _popup || overrideRedirect;
            _x11 = platform.Info;
            _mouse = new MouseDevice();
            _touch = new TouchDevice();
            _keyboard = platform.KeyboardDevice;

            var glfeature = AvaloniaLocator.Current.GetService<IPlatformGraphics>();
            XSetWindowAttributes attr = new XSetWindowAttributes();
            var valueMask = default(SetWindowValuemask);

            attr.backing_store = 1;
            attr.bit_gravity = Gravity.NorthWestGravity;
            attr.win_gravity = Gravity.NorthWestGravity;
            valueMask |= SetWindowValuemask.BackPixel | SetWindowValuemask.BorderPixel
                         | SetWindowValuemask.BackPixmap | SetWindowValuemask.BackingStore
                         | SetWindowValuemask.BitGravity | SetWindowValuemask.WinGravity;

            if (_overrideRedirect)
            {
                attr.override_redirect = 1;
                valueMask |= SetWindowValuemask.OverrideRedirect;
            }

            XVisualInfo? visualInfo = null;

            // OpenGL seems to be do weird things to it's current window which breaks resize sometimes
            _useRenderWindow = glfeature != null;
            
            var glx = glfeature as GlxPlatformGraphics;
            if (glx != null)
            {
                visualInfo = *glx.Display.VisualInfo;
                // TODO: We should query this from the active render surface, however we don't actually track what
                // the target sufrace currently is
                _useCompositorDrivenRenderWindowResize = true;
            }
            else if (glfeature == null)
                visualInfo = _x11.TransparentVisualInfo;

            var egl = glfeature as EglPlatformGraphics;
            
            var visual = IntPtr.Zero;
            var depth = 24;
            if (visualInfo != null)
            {
                visual = visualInfo.Value.visual;
                depth = (int)visualInfo.Value.depth;
                attr.colormap = XCreateColormap(_x11.Display, _x11.RootWindow, visualInfo.Value.visual, 0);
                valueMask |= SetWindowValuemask.ColorMap;   
            }

            int defaultWidth = 0, defaultHeight = 0;

            if (!_popup && _platform.Screens != null)
            {
                var monitor = _platform.Screens.AllScreens.OrderBy(x => x.Scaling)
                   .FirstOrDefault(m => m.Bounds.Contains(_position ?? default));

                if (monitor != null)
                {
                    // Emulate Window 7+'s default window size behavior.
                    defaultWidth = (int)(monitor.WorkingArea.Width * 0.75d);
                    defaultHeight = (int)(monitor.WorkingArea.Height * 0.7d);
                }
            }

            // check if the calculated size is zero then compensate to hardcoded resolution
            defaultWidth = Math.Max(defaultWidth, 300);
            defaultHeight = Math.Max(defaultHeight, 200);

            _handle = XCreateWindow(_x11.Display, _x11.RootWindow, 10, 10, defaultWidth, defaultHeight, 0,
                depth,
                (int)CreateWindowArgs.InputOutput, 
                visual,
                new UIntPtr((uint)valueMask), ref attr);
            AppendPid(_handle);

            if (_useRenderWindow)
            {
                _renderHandle = XCreateWindow(_x11.Display, _handle, 0, 0, defaultWidth, defaultHeight, 0, depth,
                    (int)CreateWindowArgs.InputOutput,
                    visual,
                    new UIntPtr((uint)(SetWindowValuemask.BorderPixel | SetWindowValuemask.BitGravity |
                                       SetWindowValuemask.WinGravity | SetWindowValuemask.BackingStore)), ref attr);
            }
            else
            {
                _renderHandle = _handle;
            }

            Handle = new PlatformHandle(_handle, "XID");

            _mode.OnHandleCreated(_handle);
            
            _realSize = new PixelSize(defaultWidth, defaultHeight);
            platform.Windows[_handle] = OnEvent;
            XEventMask ignoredMask = XEventMask.SubstructureRedirectMask
                                     | XEventMask.ResizeRedirectMask
                                     | XEventMask.PointerMotionHintMask;
            if (platform.XI2 != null)
                ignoredMask |= platform.XI2.AddWindow(_handle, this);
            var mask = new IntPtr(0xffffff ^ (int)ignoredMask);
            XSelectInput(_x11.Display, _handle, mask);
            if (!_overrideRedirect)
            {
                var protocols = new[]
                {
                    _x11.Atoms.WM_DELETE_WINDOW
                };
                XSetWMProtocols(_x11.Display, _handle, protocols, protocols.Length);
                SetNetWmWindowType(X11NetWmWindowType.Normal);
                
                SetWmClass(_handle, _platform.Options.WmClass);
            }

            var surfaces = new List<object>
            {
                new X11FramebufferSurface(_x11.DeferredDisplay, _renderHandle, 
                   depth, _platform.Options.UseRetainedFramebuffer ?? false)
            };
            
            if (egl != null)
                surfaces.Insert(0,
                    new EglGlPlatformSurface(new SurfaceInfo(this, _x11.DeferredDisplay, _handle, _renderHandle)));
            if (glx != null)
                surfaces.Insert(0, new GlxGlPlatformSurface(new SurfaceInfo(this, _x11.DeferredDisplay, _handle, _renderHandle)));

            surfaces.Add(new SurfacePlatformHandle(this));

            Surfaces = surfaces.ToArray();
            UpdateMotifHints();
            UpdateSizeHints(null);

            _rawEventGrouper = new RawEventGrouper(DispatchInput, platform.EventGrouperDispatchQueue);
            
            _transparencyHelper = new TransparencyHelper(_x11, _handle, platform.Globals);
            _transparencyHelper.SetTransparencyRequest(Array.Empty<WindowTransparencyLevel>());

            CreateIC();

            XFlush(_x11.Display);
            if(_popup)
                PopupPositioner = new ManagedPopupPositioner(new ManagedPopupPositionerPopupImplHelper(popupParent!, MoveResize));
            if (platform.Options.UseDBusMenu)
                _nativeMenuExporter = DBusMenuExporter.TryCreateTopLevelNativeMenu(_handle);
            _nativeControlHost = new X11NativeControlHost(_platform, this);
            InitializeIme();

            var data = new List<IntPtr> { _x11.Atoms.WM_DELETE_WINDOW, _x11.Atoms._NET_WM_SYNC_REQUEST };

            _mode.AppendWmProtocols(data);

            XChangeProperty(_x11.Display, _handle, _x11.Atoms.WM_PROTOCOLS, _x11.Atoms.XA_ATOM, 32,
                    PropertyMode.Replace, data.ToArray(), data.Count);

            if (_x11.HasXSync)
            {
                _xSyncCounter = XSyncCreateCounter(_x11.Display, _xSyncValue);
                XChangeProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_SYNC_REQUEST_COUNTER,
                    _x11.Atoms.XA_CARDINAL, 32, PropertyMode.Replace, ref _xSyncCounter, 1);
            }

            _storageProvider = new FallbackStorageProvider(new[]
            {
                () => _platform.Options.UseDBusFilePicker
                    ? DBusSystemDialog.TryCreateAsync(Handle)
                    : Task.FromResult<IStorageProvider?>(null),
                () => GtkSystemDialog.TryCreate(this),
                () => Task.FromResult(InputRoot is TopLevel tl
                    ? (IStorageProvider?)new ManagedStorageProvider(tl)
                    : null)
            });

            platform.X11Screens.Changed += OnScreensChanged;
        }

        private class SurfaceInfo  : EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo
        {
            private readonly X11Window _window;
            private readonly IntPtr _display;
            private readonly IntPtr _parent;

            public SurfaceInfo(X11Window window, IntPtr display, IntPtr parent, IntPtr xid)
            {
                _window = window;
                _display = display;
                _parent = parent;
                Handle = xid;
            }
            public IntPtr Handle { get; }

            public PixelSize Size
            {
                get
                {
                    XLockDisplay(_display);
                    XGetGeometry(_display, _parent, out var geo);
                    XResizeWindow(_display, Handle, geo.width, geo.height);
                    XUnlockDisplay(_display);
                    return new PixelSize(geo.width, geo.height);
                }
            }

            public double Scaling => _window.RenderScaling;
        }

        private void UpdateMotifHints()
        {
            if(_overrideRedirect)
                return;
            var functions = MotifFunctions.Move | MotifFunctions.Close | MotifFunctions.Resize |
                            MotifFunctions.Minimize | MotifFunctions.Maximize;
            var decorations = MotifDecorations.Menu | MotifDecorations.Title | MotifDecorations.Border |
                              MotifDecorations.Maximize | MotifDecorations.Minimize | MotifDecorations.ResizeH;

            if (_popup 
                || _systemDecorations == SystemDecorations.None) 
                decorations = 0;

            if (!_canResize || !IsEnabled)
            {
                functions &= ~(MotifFunctions.Resize | MotifFunctions.Maximize);
                decorations &= ~(MotifDecorations.Maximize | MotifDecorations.ResizeH);
            }
            if (!IsEnabled)
            {
                functions &= ~(MotifFunctions.Resize | MotifFunctions.Minimize);

                UpdateSizeHints(null, true);
            }
            else
            {
                UpdateSizeHints(null);
            }

            var hints = new MotifWmHints
            {
                flags = new IntPtr((int)(MotifFlags.Decorations | MotifFlags.Functions)),
                decorations = new IntPtr((int)decorations),
                functions = new IntPtr((int)functions)
            };

            XChangeProperty(_x11.Display, _handle,
                _x11.Atoms._MOTIF_WM_HINTS, _x11.Atoms._MOTIF_WM_HINTS, 32,
                PropertyMode.Replace, ref hints, 5);
        }

        /// <summary>
        /// Append `_NET_WM_PID` atom to X11 window
        /// </summary>
        /// <param name="windowXId"></param>
        private void AppendPid(IntPtr windowXId)
        {
            // See https://github.com/AvaloniaUI/Avalonia/issues/17444
            var pid = (uint)s_pid;
            // The type of `_NET_WM_PID` is `CARDINAL` which is 32-bit unsigned integer, see https://specifications.freedesktop.org/wm-spec/1.3/ar01s05.html
            XChangeProperty(_x11.Display, windowXId,
                _x11.Atoms._NET_WM_PID, _x11.Atoms.XA_CARDINAL, 32,
                PropertyMode.Replace, ref pid, 1);

            const int maxLength = 1024;
            var name = stackalloc byte[maxLength];
            var result = gethostname(name, maxLength);
            if (result != 0)
            {
                // Fail
                return;
            }

            var length = 0;
            while (length < maxLength && name[length] != 0)
            {
                length++;
            }

            XChangeProperty(_x11.Display, windowXId,
                _x11.Atoms.XA_WM_CLIENT_MACHINE, _x11.Atoms.XA_STRING, 8,
                PropertyMode.Replace, name, length);
        }

        [DllImport("libc")]
        private static extern int gethostname(byte* name, int len);

        private static readonly int s_pid = GetProcessId();

        private static int GetProcessId()
        {
#if NET6_0_OR_GREATER
            var pid = Environment.ProcessId;
#else
            using var currentProcess = Process.GetCurrentProcess();
            var pid = currentProcess.Id;
#endif
            return pid;
        }

        private void UpdateSizeHints(PixelSize? preResize, bool forceDisableResize = false)
        {
            if (_overrideRedirect)
                return;
            var min = _minMaxSize.minSize;
            var max = _minMaxSize.maxSize;

            if (!_canResize || forceDisableResize)
            {
                if (preResize.HasValue)
                {
                    max = min = preResize.Value;
                }
                else
                {
                    max = min = _realSize;
                }
            }
            else
            {
                if (preResize.HasValue)
                {
                    var desired = preResize.Value;
                    max = new PixelSize(Math.Max(desired.Width, max.Width), Math.Max(desired.Height, max.Height));
                    min = new PixelSize(Math.Min(desired.Width, min.Width), Math.Min(desired.Height, min.Height));
                }
            }

            var hints = new XSizeHints
            {
                min_width = min.Width,
                min_height = min.Height
            };
            hints.height_inc = hints.width_inc = 1;
            var flags = XSizeHintsFlags.PMinSize | XSizeHintsFlags.PResizeInc;
            if (_usePositioningFlags)
                flags |= XSizeHintsFlags.PPosition | XSizeHintsFlags.PSize;
            
            // People might be passing double.MaxValue
            if (max.Width < 100000 && max.Height < 100000)
            {
                hints.max_width = max.Width;
                hints.max_height = max.Height;
                flags |= XSizeHintsFlags.PMaxSize;
            }

            hints.flags = (IntPtr)flags;

            XSetWMNormalHints(_x11.Display, _handle, ref hints);
        }

        public Size ClientSize => new Size(_realSize.Width / RenderScaling, _realSize.Height / RenderScaling);

        public Size? FrameSize
        {
            get
            {
                var extents = GetFrameExtents();

                if(extents == null)
                {
                    return null;
                }

                return new Size(
                    (_realSize.Width + extents.Value.Left + extents.Value.Right) / RenderScaling,
                    (_realSize.Height + extents.Value.Top + extents.Value.Bottom) / RenderScaling);
            }
        }

        public double RenderScaling
        {
            get => Interlocked.CompareExchange(ref _scaling, 0.0, 0.0); 
            private set => Interlocked.Exchange(ref _scaling, value); 
        }
        
        public double DesktopScaling => RenderScaling;

        public IEnumerable<object> Surfaces { get; }
        public Action<RawInputEventArgs>? Input { get; set; }
        public Action<Rect>? Paint { get; set; }
        public Action<Size, WindowResizeReason>? Resized { get; set; }
        //TODO
        public Action<double>? ScalingChanged { get; set; }
        public Action? Deactivated { get; set; }
        public Action? Activated { get; set; }
        public Func<WindowCloseReason, bool>? Closing { get; set; }
        public Action<WindowState>? WindowStateChanged { get; set; }

        public Action<WindowTransparencyLevel>? TransparencyLevelChanged
        {
            get => _transparencyHelper?.TransparencyLevelChanged;
            set
            {
                if (_transparencyHelper != null)
                    _transparencyHelper.TransparencyLevelChanged = value;
            }
        }

        public Action<bool>? ExtendClientAreaToDecorationsChanged { get; set; }

        public Thickness ExtendedMargins { get; } = new Thickness();

        public Thickness OffScreenMargin { get; } = new Thickness();

        public bool IsClientAreaExtendedToDecorations { get; }

        public Action? Closed { get; set; }
        public Action<PixelPoint>? PositionChanged { get; set; }
        public Action? LostFocus { get; set; }

        public Compositor Compositor => _platform.Compositor;
        
        private void OnEvent(ref XEvent ev)
        {
            if (_inputRoot is null)
                return;
            
            if(_mode.OnEvent(ref ev))
                return;

            if (ev.type == XEventName.MapNotify)
            {
                _mapped = true;
                if (_useRenderWindow)
                    XMapWindow(_x11.Display, _renderHandle);
            }
            else if (ev.type == XEventName.UnmapNotify)
                _mapped = false;
            else if (ev.type == XEventName.Expose ||
                     (ev.type == XEventName.VisibilityNotify &&
                      ev.VisibilityEvent.state < 2))
            {
                EnqueuePaint();
            }
            else if (ev.type == XEventName.FocusIn)
            {
                if (ActivateTransientChildIfNeeded())
                    return;
                // See: https://github.com/fltk/fltk/issues/295
                if ((NotifyMode)ev.FocusChangeEvent.mode is not NotifyMode.NotifyNormal)
                    return;
                Activated?.Invoke();
                _imeControl?.SetWindowActive(true);
            }
            else if (ev.type == XEventName.FocusOut)
            {
                // See: https://github.com/fltk/fltk/issues/295
                if ((NotifyMode)ev.FocusChangeEvent.mode is not NotifyMode.NotifyNormal)
                    return;
                _imeControl?.SetWindowActive(false);
                Deactivated?.Invoke();
            }
            else if (ev.type == XEventName.MotionNotify)
                MouseEvent(RawPointerEventType.Move, ref ev, ev.MotionEvent.state);
            else if (ev.type == XEventName.LeaveNotify)
                MouseEvent(RawPointerEventType.LeaveWindow, ref ev, ev.CrossingEvent.state);
            else if (ev.type == XEventName.PropertyNotify)
            {
                OnPropertyChange(ev.PropertyEvent.atom, ev.PropertyEvent.state == 0);
            }
            else if (ev.type == XEventName.ButtonPress)
            {
                if (ActivateTransientChildIfNeeded())
                    return;
                if (ev.ButtonEvent.button < 4 || ev.ButtonEvent.button == 8 || ev.ButtonEvent.button == 9)
                    MouseEvent(
                        ev.ButtonEvent.button switch
                        {
                            1 => RawPointerEventType.LeftButtonDown,
                            2 => RawPointerEventType.MiddleButtonDown,
                            3 => RawPointerEventType.RightButtonDown,
                            8 => RawPointerEventType.XButton1Down,
                            9 => RawPointerEventType.XButton2Down,
                            _ => throw new NotSupportedException("Unexepected RawPointerEventType.")
                        },
                        ref ev, ev.ButtonEvent.state);
                else
                {
                    var delta = ev.ButtonEvent.button == 4
                        ? new Vector(0, 1)
                        : ev.ButtonEvent.button == 5
                            ? new Vector(0, -1)
                            : ev.ButtonEvent.button == 6
                                ? new Vector(1, 0)
                                : new Vector(-1, 0);
                    ScheduleInput(new RawMouseWheelEventArgs(_mouse, (ulong)ev.ButtonEvent.time.ToInt64(),
                        _inputRoot, new Point(ev.ButtonEvent.x, ev.ButtonEvent.y), delta,
                        TranslateModifiers(ev.ButtonEvent.state)), ref ev);
                }
                
            }
            else if (ev.type == XEventName.ButtonRelease)
            {
                if (ev.ButtonEvent.button < 4 || ev.ButtonEvent.button == 8 || ev.ButtonEvent.button == 9)
                    MouseEvent(
                        ev.ButtonEvent.button switch
                        {
                            1 => RawPointerEventType.LeftButtonUp,
                            2 => RawPointerEventType.MiddleButtonUp,
                            3 => RawPointerEventType.RightButtonUp,
                            8 => RawPointerEventType.XButton1Up,
                            9 => RawPointerEventType.XButton2Up,
                            _ => throw new NotSupportedException("Unexepected RawPointerEventType.")
                        },
                        ref ev, ev.ButtonEvent.state);
            }
            else if (ev.type == XEventName.ConfigureNotify)
            {
                if (ev.ConfigureEvent.window != _handle)
                    return;
                var needEnqueue = (_configure == null);
                _configure = ev.ConfigureEvent;
                if (ev.ConfigureEvent.override_redirect != 0  || ev.ConfigureEvent.send_event != 0)
                    _configurePoint = new PixelPoint(ev.ConfigureEvent.x, ev.ConfigureEvent.y);
                else
                {
                    XTranslateCoordinates(_x11.Display, _handle, _x11.RootWindow,
                        0, 0,
                        out var tx, out var ty, out _);
                    _configurePoint = new PixelPoint(tx, ty);
                }
                if (needEnqueue)
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (_configure == null)
                            return;
                        var cev = _configure.Value;
                        var npos = _configurePoint.Value;
                        _configure = null;
                        _configurePoint = null;
                        
                        var nsize = new PixelSize(cev.width, cev.height);
                        var changedSize = _realSize != nsize;
                        var changedPos = _position == null || npos != _position;
                        _realSize = nsize;
                        _position = npos;
                        bool updatedSizeViaScaling = false;
                        if (changedPos)
                        {
                            PositionChanged?.Invoke(npos);
                            updatedSizeViaScaling = UpdateScaling();
                        }
                        UpdateImePosition();

                        if (changedSize && !updatedSizeViaScaling && !_overrideRedirect)
                            Resized?.Invoke(ClientSize, WindowResizeReason.Unspecified);

                    }, DispatcherPriority.AsyncRenderTargetResize);
                
                if (_useRenderWindow && !_useCompositorDrivenRenderWindowResize)
                    XConfigureResizeWindow(_x11.Display, _renderHandle, ev.ConfigureEvent.width,
                        ev.ConfigureEvent.height);
                if (_xSyncState == XSyncState.WaitConfigure)
                {
                    _xSyncState = XSyncState.WaitPaint;
                    EnqueuePaint();
                }
            }
            else if (ev.type == XEventName.DestroyNotify 
                     && ev.DestroyWindowEvent.window == _handle)
            {
                _mode.OnDestroyNotify();
                Cleanup(true);
            }
            else if (ev.type == XEventName.ClientMessage)
            {
                if (ev.ClientMessageEvent.message_type == _x11.Atoms.WM_PROTOCOLS)
                {
                    if (ev.ClientMessageEvent.ptr1 == _x11.Atoms.WM_DELETE_WINDOW)
                    {
                        if (IsEnabled && Closing?.Invoke(WindowCloseReason.WindowClosing) != true)
                            Dispose();
                    }
                    else if (ev.ClientMessageEvent.ptr1 == _x11.Atoms._NET_WM_SYNC_REQUEST)
                    {
                        _xSyncValue.Lo = new UIntPtr(ev.ClientMessageEvent.ptr3.ToPointer()).ToUInt32();
                        _xSyncValue.Hi = ev.ClientMessageEvent.ptr4.ToInt32();
                        _xSyncState = XSyncState.WaitConfigure;
                    }
                }
            }
            else if (ev.type == XEventName.KeyPress || ev.type == XEventName.KeyRelease)
            {
                if (ActivateTransientChildIfNeeded())
                    return;
                HandleKeyEvent(ref ev);
            }
        }

        private Thickness? GetFrameExtents()
        {
            if (_systemDecorations != SystemDecorations.Full)
                return new Thickness(0);

            XGetWindowProperty(_x11.Display, _handle, _x11.Atoms._NET_FRAME_EXTENTS, IntPtr.Zero,
                new IntPtr(4), false, (IntPtr)Atom.AnyPropertyType, out var _,
                out var _, out var nitems, out var _, out var prop);

            if (nitems.ToInt64() != 4)
            {
                // Window hasn't been mapped by the WM yet, so can't get the extents.
                return null;
            }

            var data = (IntPtr*)prop.ToPointer();
            var extents = new Thickness(data[0].ToInt32(), data[2].ToInt32(), data[1].ToInt32(), data[3].ToInt32());
            XFree(prop);

            return extents;
        }

        private void OnScreensChanged()
        {
            UpdateScaling();
        }

        private bool UpdateScaling(bool skipResize = false)
        {
            double newScaling;
            if (_scalingOverride.HasValue)
                newScaling = _scalingOverride.Value;
            else
            {
                var monitor = _platform.X11Screens.AllScreens.OrderBy(x => x.Scaling)
                    .FirstOrDefault(m => m.Bounds.Contains(_position ?? default));
                newScaling = monitor?.Scaling ?? RenderScaling;
            }

            if (RenderScaling != newScaling)
            {
                var oldScaledSize = ClientSize;
                RenderScaling = newScaling;
                ScalingChanged?.Invoke(RenderScaling);
                UpdateImePosition();
                SetMinMaxSize(_scaledMinMaxSize.minSize, _scaledMinMaxSize.maxSize);
                if(!skipResize)
                    Resize(oldScaledSize, true, WindowResizeReason.DpiChange);
                return true;
            }
            
            return false;
        }

        private WindowState _lastWindowState;
        public WindowState WindowState
        {
            get => _lastWindowState;
            set
            {
                if(_lastWindowState == value)
                    return;
                _lastWindowState = value;
                if (value == WindowState.Minimized)
                {
                    XIconifyWindow(_x11.Display, _handle, _x11.DefaultScreen);
                }
                else if (value == WindowState.Maximized)
                {
                    ChangeWMAtoms(false, _x11.Atoms._NET_WM_STATE_HIDDEN);
                    ChangeWMAtoms(false, _x11.Atoms._NET_WM_STATE_FULLSCREEN);
                    ChangeWMAtoms(true, _x11.Atoms._NET_WM_STATE_MAXIMIZED_VERT,
                        _x11.Atoms._NET_WM_STATE_MAXIMIZED_HORZ);
                }
                else if (value == WindowState.FullScreen)
                {
                    ChangeWMAtoms(false, _x11.Atoms._NET_WM_STATE_HIDDEN);
                    ChangeWMAtoms(true, _x11.Atoms._NET_WM_STATE_FULLSCREEN);
                    ChangeWMAtoms(false, _x11.Atoms._NET_WM_STATE_MAXIMIZED_VERT,
                        _x11.Atoms._NET_WM_STATE_MAXIMIZED_HORZ);
                }
                else
                {
                    ChangeWMAtoms(false, _x11.Atoms._NET_WM_STATE_HIDDEN);
                    ChangeWMAtoms(false, _x11.Atoms._NET_WM_STATE_FULLSCREEN);
                    ChangeWMAtoms(false, _x11.Atoms._NET_WM_STATE_MAXIMIZED_VERT,
                        _x11.Atoms._NET_WM_STATE_MAXIMIZED_HORZ);
                    SendNetWMMessage(_x11.Atoms._NET_ACTIVE_WINDOW, (IntPtr)1, _x11.LastActivityTimestamp,
                        IntPtr.Zero);
                }
                WindowStateChanged?.Invoke(value);
            }
        }

        private void OnPropertyChange(IntPtr atom, bool hasValue)
        {
            if (atom == _x11.Atoms._NET_FRAME_EXTENTS)
            {
                // Occurs once the window has been mapped, which is the earliest the extents
                // can be retrieved, so invoke event to force update of TopLevel.FrameSize.
                Resized?.Invoke(ClientSize, WindowResizeReason.Unspecified);
            }

            if (atom == _x11.Atoms._NET_WM_STATE)
            {
                WindowState state = WindowState.Normal;
                if(hasValue)
                {

                    XGetWindowProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_STATE, IntPtr.Zero, new IntPtr(256),
                        false, (IntPtr)Atom.XA_ATOM, out _, out _, out var nitems, out _,
                        out var prop);
                    int maximized = 0;
                    var pitems = (IntPtr*)prop.ToPointer();
                    for (var c = 0; c < nitems.ToInt32(); c++)
                    {
                        if (pitems[c] == _x11.Atoms._NET_WM_STATE_HIDDEN)
                        {
                            state = WindowState.Minimized;
                            break;
                        }

                        if(pitems[c] == _x11.Atoms._NET_WM_STATE_FULLSCREEN)
                        {
                            state = WindowState.FullScreen;
                            break;
                        }

                        if (pitems[c] == _x11.Atoms._NET_WM_STATE_MAXIMIZED_HORZ ||
                            pitems[c] == _x11.Atoms._NET_WM_STATE_MAXIMIZED_VERT)
                        {
                            maximized++;
                            if (maximized == 2)
                            {
                                state = WindowState.Maximized;
                                break;
                            }
                        }
                    }
                    XFree(prop);
                }
                if (_lastWindowState != state)
                {
                    _lastWindowState = state;
                    WindowStateChanged?.Invoke(state);
                }
            }

        }

        private static RawInputModifiers TranslateModifiers(XModifierMask state)
        {
            var rv = default(RawInputModifiers);
            if (state.HasAllFlags(XModifierMask.Button1Mask))
                rv |= RawInputModifiers.LeftMouseButton;
            if (state.HasAllFlags(XModifierMask.Button2Mask))
                rv |= RawInputModifiers.RightMouseButton;
            if (state.HasAllFlags(XModifierMask.Button3Mask))
                rv |= RawInputModifiers.MiddleMouseButton;
            if (state.HasAllFlags(XModifierMask.Button4Mask))
                rv |= RawInputModifiers.XButton1MouseButton;
            if (state.HasAllFlags(XModifierMask.Button5Mask))
                rv |= RawInputModifiers.XButton2MouseButton;
            if (state.HasAllFlags(XModifierMask.ShiftMask))
                rv |= RawInputModifiers.Shift;
            if (state.HasAllFlags(XModifierMask.ControlMask))
                rv |= RawInputModifiers.Control;
            if (state.HasAllFlags(XModifierMask.Mod1Mask))
                rv |= RawInputModifiers.Alt;
            if (state.HasAllFlags(XModifierMask.Mod4Mask))
                rv |= RawInputModifiers.Meta;
            return rv;
        }
        
        private SystemDecorations _systemDecorations = SystemDecorations.Full;
        private bool _canResize = true;
        private const int MaxWindowDimension = 100000;

        private (Size minSize, Size maxSize) _scaledMinMaxSize =
            (new Size(1, 1), new Size(double.PositiveInfinity, double.PositiveInfinity));

        private (PixelSize minSize, PixelSize maxSize) _minMaxSize = (new PixelSize(1, 1),
            new PixelSize(MaxWindowDimension, MaxWindowDimension));
        
        private double _scaling = 1;

        private void ScheduleInput(RawInputEventArgs args, ref XEvent xev)
        {
            _x11.LastActivityTimestamp = xev.ButtonEvent.time;
            ScheduleInput(args);
        }

        private void DispatchInput(RawInputEventArgs args)
        {
            if (_inputRoot is null)
                return;

            if (_disabled && args is RawPointerEventArgs pargs && pargs.Type == RawPointerEventType.Move)
                return;

            Input?.Invoke(args);
            if (!args.Handled && args is RawKeyEventArgsWithText { Text: { Length: > 0 } text })
                Input?.Invoke(new RawTextInputEventArgs(_keyboard, args.Timestamp, _inputRoot, text));
        }

        public void ScheduleXI2Input(RawInputEventArgs args)
        {
            if (args is RawPointerEventArgs pargs)
            {
                if ((pargs.Type == RawPointerEventType.TouchBegin
                     || pargs.Type == RawPointerEventType.TouchUpdate
                     || pargs.Type == RawPointerEventType.LeftButtonDown
                     || pargs.Type == RawPointerEventType.RightButtonDown
                     || pargs.Type == RawPointerEventType.MiddleButtonDown
                     || pargs.Type == RawPointerEventType.NonClientLeftButtonDown)
                    && ActivateTransientChildIfNeeded())
                    return;
                if (pargs.Type == RawPointerEventType.TouchEnd
                    && ActivateTransientChildIfNeeded())
                    pargs.Type = RawPointerEventType.TouchCancel;
            }

            ScheduleInput(args);
        }
        
        private void ScheduleInput(RawInputEventArgs args)
        {
            if (args is RawPointerEventArgs mouse)
                mouse.Position = mouse.Position / RenderScaling;
            if (args is RawDragEvent drag)
                drag.Location = drag.Location / RenderScaling;
            
            _rawEventGrouper?.HandleEvent(args);
        }

        private void MouseEvent(RawPointerEventType type, ref XEvent ev, XModifierMask mods)
        {
            if (_inputRoot is null)
                return;
            var mev = new RawPointerEventArgs(
                _mouse, (ulong)ev.ButtonEvent.time.ToInt64(), _inputRoot,
                type, new Point(ev.ButtonEvent.x, ev.ButtonEvent.y), TranslateModifiers(mods));
            ScheduleInput(mev, ref ev);
        }

        private void EnqueuePaint()
        {
            if (!_triggeredExpose)
            {
                _triggeredExpose = true;
                Dispatcher.UIThread.Post(() =>
                {
                    _triggeredExpose = false;
                    DoPaint();
                }, DispatcherPriority.UiThreadRender);
            }
        }

        private void DoPaint()
        {
            Paint?.Invoke(new Rect());
            if (_xSyncCounter != IntPtr.Zero && _xSyncState == XSyncState.WaitPaint)
            {
                _xSyncState = XSyncState.None;
                XSyncSetCounter(_x11.Display, _xSyncCounter, _xSyncValue);
            }
        }
        
        public void Invalidate(Rect rect)
        {

        }

        public IInputRoot InputRoot
            => _inputRoot ?? throw new InvalidOperationException($"{nameof(SetInputRoot)} must have been called");
        
        public void SetInputRoot(IInputRoot inputRoot)
        {
            _inputRoot = inputRoot;
        }

        public void Dispose()
        {
            Cleanup(false);            
        }

        public virtual object? TryGetFeature(Type featureType)
        {
            if (featureType == typeof(ITopLevelNativeMenuExporter))
            {
                return _nativeMenuExporter;
            }
            
            if (featureType == typeof(IStorageProvider))
            {
                return _storageProvider;
            }

            if (featureType == typeof(ITextInputMethodImpl))
            {
                return _ime;
            }

            if (featureType == typeof(INativeControlHostImpl))
            {
                return _nativeControlHost;
            }

            if (featureType == typeof(IClipboard))
            {
                return AvaloniaLocator.Current.GetRequiredService<IClipboard>();
            }

            if (featureType == typeof(ILauncher))
            {
                return new BclLauncher();
            }

            if (featureType == typeof(IX11OptionsToplevelImplFeature))
            {
                return this;
            }

            if (featureType == typeof(IScreenImpl))
            {
                return _platform.Screens;
            }

            return null;
        }

        private void Cleanup(bool fromDestroyNotification)
        {
            // Prevent reentrancy
            if(_cleaningUp)
                return;
            _cleaningUp = true;
            
            // Before doing anything else notify the TopLevel that ITopLevelImpl is no longer valid
            if (_handle != IntPtr.Zero)
                Closed?.Invoke();
            
            if (_nativeMenuExporter is IDisposable disposable)
                disposable.Dispose();
            
            if (_rawEventGrouper != null)
            {
                _rawEventGrouper.Dispose();
                _rawEventGrouper = null;
            }
            
            if (_transparencyHelper != null)
            {
                _transparencyHelper.Dispose();
                _transparencyHelper = null;
            }
            
            if (_imeControl != null)
            {
                _imeControl.Dispose();
                _imeControl = null;
                _ime = null;
            }
            
            if (_xic != IntPtr.Zero)
            {
                XDestroyIC(_xic);
                _xic = IntPtr.Zero;
            }

            if (_xSyncCounter != IntPtr.Zero)
            {
                XSyncDestroyCounter(_x11.Display, _xSyncCounter);
                _xSyncCounter = IntPtr.Zero;
            }
            
            if (_handle != IntPtr.Zero)
            {
                _platform.Windows.Remove(_handle);
                _platform.XI2?.OnWindowDestroyed(_handle);
                var handle = _handle;
                _handle = IntPtr.Zero;
                _mouse.Dispose();
                _touch.Dispose();
                if (!fromDestroyNotification) 
                    XDestroyWindow(_x11.Display, handle);
            }

            _platform.X11Screens.Changed -= OnScreensChanged;
            
            if (_useRenderWindow && _renderHandle != IntPtr.Zero)
            {                
                _renderHandle = IntPtr.Zero;
            }
        }

        private bool ActivateTransientChildIfNeeded()
        {
            if (_disabled)
            {
                GotInputWhenDisabled?.Invoke();
                return true;
            }

            return false;
        }

        public void SetParent(IWindowImpl? parent)
        {
            if (parent == null || parent.Handle == null || parent.Handle.Handle == IntPtr.Zero)
                XDeleteProperty(_x11.Display, _handle, _x11.Atoms.XA_WM_TRANSIENT_FOR);
            else
                XSetTransientForHint(_x11.Display, _handle, parent.Handle.Handle);
        }

        public void Show(bool activate, bool isDialog)
        {
            _mode.Show(activate, isDialog);
        }

        public void Hide() => _mode.Hide();

        public Point PointToClient(PixelPoint point) => _mode.PointToClient(point);

        public PixelPoint PointToScreen(Point point) => _mode.PointToScreen(point);
        
        public void SetSystemDecorations(SystemDecorations enabled)
        {
            _systemDecorations = enabled == SystemDecorations.Full ? SystemDecorations.Full : SystemDecorations.None;
            UpdateMotifHints();
            UpdateSizeHints(null);
        }


        public void Resize(Size clientSize, WindowResizeReason reason) => Resize(clientSize, false, reason);
        public void Move(PixelPoint point)
        {
            Position = point;
            UpdateScaling();
        }
        private void MoveResize(PixelPoint position, Size size, double scaling)
        {
            Move(position);
            _scalingOverride = scaling;
            UpdateScaling(true);
            Resize(size, true, WindowResizeReason.Layout);
        }

        private PixelSize ToPixelSize(Size size) => new PixelSize((int)(size.Width * RenderScaling), (int)(size.Height * RenderScaling));

        private void Resize(Size clientSize, bool force, WindowResizeReason reason)
        {
            if (!force && (clientSize == ClientSize))
            {
                return;
            }
            
            var needImmediatePopupResize = clientSize != ClientSize;

            var pixelSize = ToPixelSize(clientSize);
            UpdateSizeHints(pixelSize);
            XConfigureResizeWindow(_x11.Display, _handle, pixelSize);
            if (_useRenderWindow && !_useCompositorDrivenRenderWindowResize)
                XConfigureResizeWindow(_x11.Display, _renderHandle, pixelSize);
            XFlush(_x11.Display);

            if (force || !_wasMappedAtLeastOnce || (_overrideRedirect && needImmediatePopupResize))
            {
                _realSize = pixelSize;
                Resized?.Invoke(ClientSize, reason);
            }
        }
        
        public void CanResize(bool value)
        {
            _canResize = value;
            UpdateMotifHints();
            UpdateSizeHints(null);
        }

        public void SetCursor(ICursorImpl? cursor)
        {
            if (cursor == null)
                XDefineCursor(_x11.Display, _handle, _x11.DefaultCursor);
            else if (cursor is CursorImpl impl)
            {
                XDefineCursor(_x11.Display, _handle, impl.Handle);
            }
        }

        public IPlatformHandle Handle { get; }
        
        public PixelPoint Position
        {
            get
            {
                if(_position == null)
                {
                    return default;
                }

                var extents = GetFrameExtents();

                if(extents == null)
                {
                    extents = default(Thickness);
                }

                return new PixelPoint(_position.Value.X - (int)extents.Value.Left, _position.Value.Y - (int)extents.Value.Top);
            }
            set
            {
                if (!_usePositioningFlags)
                {
                    _usePositioningFlags = true;
                    UpdateSizeHints(null);
                }

                var changes = new XWindowChanges
                {
                    x = value.X,
                    y = (int)value.Y
                };

                XConfigureWindow(_x11.Display, _handle, ChangeWindowFlags.CWX | ChangeWindowFlags.CWY,
                    ref changes);
                XFlush(_x11.Display);
                if (!_wasMappedAtLeastOnce)
                {
                    _position = value;
                    PositionChanged?.Invoke(value);
                    UpdateScaling();
                }
            }
        }

        public IMouseDevice MouseDevice => _mouse;
        public TouchDevice TouchDevice => _touch;

        public IPopupImpl? CreatePopup() 
            => _platform.Options.OverlayPopups ? null : new X11Window(_platform, this);

        public void Activate() => _mode.Activate();

        public Size MaxAutoSizeHint => _platform.X11Screens.AllScreens.Select(s => s.Bounds.Size.ToSize(s.Scaling))
            .OrderByDescending(x => x.Width + x.Height).FirstOrDefault();


        private void SendNetWMMessage(IntPtr message_type, IntPtr l0,
            IntPtr? l1 = null, IntPtr? l2 = null, IntPtr? l3 = null, IntPtr? l4 = null)
        {
            var xev = new XEvent
            {
                ClientMessageEvent =
                {
                    type = XEventName.ClientMessage,
                    send_event = 1,
                    window = _handle,
                    message_type = message_type,
                    format = 32,
                    ptr1 = l0,
                    ptr2 = l1 ?? IntPtr.Zero,
                    ptr3 = l2 ?? IntPtr.Zero,
                    ptr4 = l3 ?? IntPtr.Zero
                }
            };
            xev.ClientMessageEvent.ptr4 = l4 ?? IntPtr.Zero;
            XSendEvent(_x11.Display, _x11.RootWindow, false,
                new IntPtr((int)(EventMask.SubstructureRedirectMask | EventMask.SubstructureNotifyMask)), ref xev);

        }

        private void BeginMoveResize(NetWmMoveResize side, PointerPressedEventArgs e)
        {
            var pos = GetCursorPos(_x11);
            XUngrabPointer(_x11.Display, new IntPtr(0));
            SendNetWMMessage (_x11.Atoms._NET_WM_MOVERESIZE, (IntPtr) pos.x, (IntPtr) pos.y,
                (IntPtr) side,
                (IntPtr) 1, (IntPtr)1); // left button
                
            e.Pointer.Capture(null);
        }

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {
            BeginMoveResize(NetWmMoveResize._NET_WM_MOVERESIZE_MOVE, e);
        }

        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {
            var side = NetWmMoveResize._NET_WM_MOVERESIZE_CANCEL;
            if (edge == WindowEdge.East)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_RIGHT;
            if (edge == WindowEdge.North)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_TOP;
            if (edge == WindowEdge.South)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_BOTTOM;
            if (edge == WindowEdge.West)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_LEFT;
            if (edge == WindowEdge.NorthEast)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_TOPRIGHT;
            if (edge == WindowEdge.NorthWest)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_TOPLEFT;
            if (edge == WindowEdge.SouthEast)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_BOTTOMRIGHT;
            if (edge == WindowEdge.SouthWest)
                side = NetWmMoveResize._NET_WM_MOVERESIZE_SIZE_BOTTOMLEFT;
            BeginMoveResize(side, e);
        }

        public void SetTitle(string? title)
        {
            if (string.IsNullOrEmpty(title))
            {
                XDeleteProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_NAME);
                XDeleteProperty(_x11.Display, _handle, _x11.Atoms.XA_WM_NAME);
            }
            else
            {
                var data = Encoding.UTF8.GetBytes(title);
                fixed (void* pdata = data)
                {
                    XChangeProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_NAME, _x11.Atoms.UTF8_STRING, 8,
                        PropertyMode.Replace, pdata, data.Length);
                    XStoreName(_x11.Display, _handle, title);
                }
            }
        }

        public void SetWmClass(IntPtr handle, string? wmClass)
        {
            // See https://tronche.com/gui/x/icccm/sec-4.html#WM_CLASS
            // We don't actually parse the application's command line, so we only use RESOURCE_NAME and argv[0]
            var appId = Environment.GetEnvironmentVariable("RESOURCE_NAME") 
                        ?? Process.GetCurrentProcess().ProcessName;
            
            var encodedAppId = Encoding.ASCII.GetBytes(appId);
            var encodedWmClass = Encoding.ASCII.GetBytes(wmClass ?? appId);

            var hint = XAllocClassHint();
            fixed(byte* pAppId = encodedAppId)
            fixed (byte* pWmClass = encodedWmClass)
            {
                hint->res_name = pAppId;
                hint->res_class = pWmClass;
                XSetClassHint(_x11.Display, handle, hint);
            }

            XFree(hint);
        }
        
        public void SetWmClass(string? className)
        {
            if (_handle == IntPtr.Zero)
                return;
            SetWmClass(_handle, className ?? _platform.Options.WmClass);
        }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
            _scaledMinMaxSize = (minSize, maxSize);
            var min = new PixelSize(
                (int)(minSize.Width < 1 ? 1 : minSize.Width * RenderScaling),
                (int)(minSize.Height < 1 ? 1 : minSize.Height * RenderScaling));

            const int maxDim = MaxWindowDimension;
            var max = new PixelSize(
                (int)(maxSize.Width > maxDim ? maxDim : Math.Max(min.Width, maxSize.Width * RenderScaling)),
                (int)(maxSize.Height > maxDim ? maxDim : Math.Max(min.Height, maxSize.Height * RenderScaling)));
            
            _minMaxSize = (min, max);
            UpdateSizeHints(null);
        }

        public void SetTopmost(bool value)
        {
            ChangeWMAtoms(value, _x11.Atoms._NET_WM_STATE_ABOVE);
        }
        
        public void SetEnabled(bool enable)
        {
            _disabled = !enable;

            UpdateWMHints();
            UpdateMotifHints();

            if (enable)
            {
                // Some window managers ignore Motif hints when switching from disabled to enabled on the first update
                // so setting it again forces the update
                UpdateMotifHints();
            }
            else
            {
                // Showing a dialog should result in pointer capture being lost. We don't currently use XGrabPointer on
                // X11 to implement pointer capture, so no we have no OS-level event to hook into. Instead, release the 
                // pointer capture when the owner window is disabled. This behavior matches win32, which sends a
                // WM_CANCELMODE message when EnableWindow(hWnd, false) is called from SetEnabled.
                _mouse.PlatformCaptureLost();
                _touch.PlatformCaptureLost();
            }
        }

        private void UpdateWMHints()
        {
            var wmHintsPtr = XGetWMHints(_x11.Display, _handle);

            XWMHints hints = default;

            if (wmHintsPtr != IntPtr.Zero)
            {
                hints = Marshal.PtrToStructure<XWMHints>(wmHintsPtr);
            }

            var flags = hints.flags.ToInt64();
            flags |= (long)XWMHintsFlags.InputHint;
            hints.flags = (IntPtr)flags;
            hints.input = !_disabled ? 1 : 0;

            XSetWMHints(_x11.Display, _handle, ref hints);

            if (wmHintsPtr != IntPtr.Zero)
            {
                XFree(wmHintsPtr);
            }
        }

        public void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint)
        {
        }

        public void SetExtendClientAreaChromeHints(ExtendClientAreaChromeHints hints)
        {
        }

        public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight)
        {
        }

        public Action? GotInputWhenDisabled { get; set; }

        public void SetIcon(IWindowIconImpl? icon)
        {
            if (icon != null)
            {
                var data = ((X11IconData)icon).Data;
                fixed (void* pdata = data)
                    XChangeProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_ICON,
                        new IntPtr((int)Atom.XA_CARDINAL), 32, PropertyMode.Replace,
                        pdata, data.Length);
            }
            else
            {
                XDeleteProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_ICON);
            }
        }

        public void ShowTaskbarIcon(bool value)
        {
            ChangeWMAtoms(!value, _x11.Atoms._NET_WM_STATE_SKIP_TASKBAR);
        }

        private void ChangeWMAtoms(bool enable, params IntPtr[] atoms)
        {
            if (atoms.Length != 1 && atoms.Length != 2)
                throw new ArgumentException();

            if (!_mapped)
            {
                XGetWindowProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_STATE, IntPtr.Zero, new IntPtr(256),
                    false, (IntPtr)Atom.XA_ATOM, out _, out _, out var nitems, out _,
                    out var prop);
                var ptr = (IntPtr*)prop.ToPointer();
                var newAtoms = new HashSet<IntPtr>();
                for (var c = 0; c < nitems.ToInt64(); c++) 
                    newAtoms.Add(*(ptr+c));
                XFree(prop);
                foreach(var atom in atoms)
                    if (enable)
                        newAtoms.Add(atom);
                    else
                        newAtoms.Remove(atom);

                XChangeProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_STATE, (IntPtr)Atom.XA_ATOM, 32,
                    PropertyMode.Replace, newAtoms.ToArray(), newAtoms.Count);
            }
            
            SendNetWMMessage(_x11.Atoms._NET_WM_STATE,
                (IntPtr)(enable ? 1 : 0),
                atoms[0],
                atoms.Length > 1 ? atoms[1] : IntPtr.Zero,
                atoms.Length > 2 ? atoms[2] : IntPtr.Zero,
                atoms.Length > 3 ? atoms[3] : IntPtr.Zero
            );
        }

        public IPopupPositioner? PopupPositioner { get; }

        public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels)
        {
            _transparencyHelper?.SetTransparencyRequest(transparencyLevels);
        }

        public void SetWindowManagerAddShadowHint(bool enabled)
        {
        }

        public WindowTransparencyLevel TransparencyLevel =>
            _transparencyHelper?.CurrentLevel ?? WindowTransparencyLevel.None;

        public void SetFrameThemeVariant(PlatformThemeVariant? themeVariant) { }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels(1, 0.8, 0.8);

        public bool NeedsManagedDecorations => false;

        public bool IsEnabled => !_disabled && !_mode.BlockInput;

        public class SurfacePlatformHandle : INativePlatformHandleSurface
        {
            private readonly X11Window _owner;

            public PixelSize Size => _owner.ToPixelSize(_owner.ClientSize);

            public double Scaling => _owner.RenderScaling;

            public SurfacePlatformHandle(X11Window owner)
            {
                _owner = owner;
            }

            public IntPtr Handle => _owner._renderHandle;
            public string HandleDescriptor => "XID";
        }

        public void SetNetWmWindowType(X11NetWmWindowType type)
        {
            if(_handle == IntPtr.Zero)
                return;

            var atom = type switch
            {
                X11NetWmWindowType.Dialog => _x11.Atoms._NET_WM_WINDOW_TYPE_DIALOG,
                X11NetWmWindowType.Utility => _x11.Atoms._NET_WM_WINDOW_TYPE_UTILITY,
                X11NetWmWindowType.Toolbar => _x11.Atoms._NET_WM_WINDOW_TYPE_TOOLBAR,
                X11NetWmWindowType.Splash => _x11.Atoms._NET_WM_WINDOW_TYPE_SPLASH,
                X11NetWmWindowType.Dock => _x11.Atoms._NET_WM_WINDOW_TYPE_DOCK,
                X11NetWmWindowType.Desktop => _x11.Atoms._NET_WM_WINDOW_TYPE_DESKTOP,
                _ => _x11.Atoms._NET_WM_WINDOW_TYPE_NORMAL
            };

            XChangeProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_WINDOW_TYPE, _x11.Atoms.XA_ATOM,
                32, PropertyMode.Replace, new[] { atom }, 1);

        }

        /// <inheritdoc/>
        public void GetWindowsZOrder(Span<Window> windows, Span<long> outputZOrder)
        {
            // a mapping of parent windows to their children, sorted by z-order (bottom to top)
            var windowsChildren = new Dictionary<IntPtr, List<IntPtr>>();

            var indexInWindowsSpan = new Dictionary<IntPtr, int>();
            for (var i = 0; i < windows.Length; i++)
                if (windows[i].PlatformImpl is { Handle: { } handle })
                    indexInWindowsSpan[handle.Handle] = i;

            foreach (var window in windows)
            {
                if (window.PlatformImpl is not X11Window x11Window)
                    continue;

                var node = x11Window.Handle.Handle;
                while (node != IntPtr.Zero)
                {
                    if (windowsChildren.ContainsKey(node))
                    {
                        break;
                    }

                    if (XQueryTree(_x11.Display, node, out _, out var parent,
                            out var childrenPtr, out var childrenCount) == 0)
                    {
                        break;
                    }

                    if (childrenPtr != IntPtr.Zero)
                    {
                        var children = (IntPtr*)childrenPtr;
                        windowsChildren[node] = new List<IntPtr>(childrenCount);
                        for (var i = 0; i < childrenCount; i++)
                        {
                            windowsChildren[node].Add(children[i]);
                        }
                        XFree(childrenPtr);
                    }

                    node = parent;
                }
            }

            var stack = new Stack<IntPtr>();
            var zOrder = 0;
            stack.Push(_x11.RootWindow);

            while (stack.Count > 0)
            {
                var currentWindow = stack.Pop();

                if (!windowsChildren.TryGetValue(currentWindow, out var children))
                {
                    continue;
                }

                if (indexInWindowsSpan.TryGetValue(currentWindow, out var index))
                {
                    outputZOrder[index] = zOrder;
                }

                zOrder++;

                // Children are returned bottom to top, so we need to push them in reverse order
                // In order to traverse bottom children first
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    stack.Push(children[i]);
                }
            }
        }

        public void TakeFocus()
        {
            // TODO: Not yet implemented: need to check if this is required on X11 or not.
        }
    }
}
