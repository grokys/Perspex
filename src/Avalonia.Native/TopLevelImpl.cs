#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia.Native;

internal class MacOSTopLevelHandle : IPlatformHandle, IMacOSTopLevelPlatformHandle
{
    internal MacOSTopLevelHandle(IAvnTopLevel native)
    {
        Native = native;

        HandleDescriptor = "NSView";

        Handle = NSView;
    }

    internal MacOSTopLevelHandle(IAvnWindowBase native)
    {
        Native = native;

        HandleDescriptor = "NSWindow";

        Handle = NSWindow;
    }

    internal IAvnTopLevel Native { get; }

    public IntPtr Handle { get; }

    public string HandleDescriptor { get; }

    public IntPtr NSView => Native.ObtainNSViewHandle();

    public IntPtr GetNSViewRetained()
    {
        return Native.ObtainNSViewHandleRetained();
    }
    
    public IntPtr NSWindow => (Native as IAvnWindowBase)?.ObtainNSWindowHandle() ?? IntPtr.Zero;

    public IntPtr GetNSWindowRetained()
    {
        return (Native as IAvnWindowBase)?.ObtainNSWindowHandleRetained() ?? IntPtr.Zero;
    }
}

internal class TopLevelImpl : ITopLevelImpl, IFramebufferPlatformSurface
{
    protected IInputRoot? _inputRoot;
    private NativeControlHostImpl? _nativeControlHost;
    private PlatformBehaviorInhibition? _platformBehaviorInhibition;

    private readonly MouseDevice? _mouse;
    private readonly PenDevice? _pen;

    private readonly IKeyboardDevice? _keyboard;
    private readonly ICursorFactory? _cursorFactory;

    protected readonly IAvaloniaNativeFactory Factory;

    private Size _savedLogicalSize;
    private double _savedScaling;
    private WindowTransparencyLevel _transparencyLevel = WindowTransparencyLevel.None;

    protected MacOSTopLevelHandle? _handle;

    private object _syncRoot = new object();
    private IEnumerable<object>? _surfaces;

    public TopLevelImpl(IAvaloniaNativeFactory factory)
    {
        Factory = factory;

        _keyboard = AvaloniaLocator.Current.GetService<IKeyboardDevice>();
        _mouse = new MouseDevice();
        _pen = new PenDevice();
        _cursorFactory = AvaloniaLocator.Current.GetService<ICursorFactory>();
    }

    internal virtual void Init(MacOSTopLevelHandle handle)
    {
        _handle = handle;
        _savedLogicalSize = ClientSize;
        _savedScaling = Native?.Scaling ?? 1;;
        _nativeControlHost = new NativeControlHostImpl(Native!.CreateNativeControlHost());
        _platformBehaviorInhibition = new PlatformBehaviorInhibition(Factory.CreatePlatformBehaviorInhibition());
        _surfaces = new object[] { new GlPlatformSurface(Native), new MetalPlatformSurface(Native), this };
        InputMethod = new AvaloniaNativeTextInputMethod(Native);
    }

    public double DesktopScaling => 1;

    public IAvnTopLevel? Native => _handle?.Native;
    public IPlatformHandle? Handle => _handle;
    public AvaloniaNativeTextInputMethod? InputMethod { get; protected set; }
    public Size ClientSize
    {
        get
        {
            if (Native == null)
            {
                return default;
            }
            
            var s = Native.ClientSize;
            return new Size(s.Width, s.Height);

        }
    }

    public double RenderScaling => _savedScaling;
    public IEnumerable<object> Surfaces => _surfaces ?? Array.Empty<object>();
    public Action<RawInputEventArgs>? Input { get; set; }
    public Action<Rect>? Paint { get; set; }
    public Action<Size, WindowResizeReason>? Resized { get; set; }
    public Action<double>? ScalingChanged { get; set; }
    public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }
    public Compositor Compositor => AvaloniaNativePlatform.Compositor;
    public Action? Closed { get; set; }
    public Action? LostFocus { get; set; }
    
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

    public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels(1, 0, 0);
    public virtual void SetFrameThemeVariant(PlatformThemeVariant themeVariant)
    {
        //noop
    }

    public IMouseDevice? MouseDevice => _mouse;

    public INativeControlHostImpl? NativeControlHost => _nativeControlHost;

    public AutomationPeer? GetAutomationPeer()
    {
        return _inputRoot is Control c ? ControlAutomationPeer.CreatePeerForElement(c) : null;
    }

    public bool RawTextInputEvent(ulong timeStamp, string text)
    {
        if (_inputRoot is null)
            return false;

        if (_keyboard is null)
        {
            return false;
        }

        Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

        var args = new RawTextInputEventArgs(_keyboard, timeStamp, _inputRoot, text);

        Input?.Invoke(args);

        return args.Handled;
    }

    public bool RawKeyEvent(
        AvnRawKeyEventType type,
        ulong timeStamp,
        AvnInputModifiers modifiers,
        AvnKey key,
        AvnPhysicalKey physicalKey,
        string keySymbol)
    {
        if (_inputRoot is null)
            return false;

        if (_keyboard is null)
        {
            return false;
        }

        Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

        var args = new RawKeyEventArgs(
            _keyboard,
            timeStamp,
            _inputRoot,
            (RawKeyEventType)type,
            (Key)key,
            (RawInputModifiers)modifiers,
            (PhysicalKey)physicalKey,
            keySymbol);

        Input?.Invoke(args);

        return args.Handled;
    }

    public void RawMouseEvent(AvnRawMouseEventType type, AvnPointerDeviceType deviceType, ulong timeStamp, AvnInputModifiers modifiers, AvnPoint point, AvnVector delta, float pressure, float xTilt, float yTilt)
    {
        if (_inputRoot is null)
            return;

        if (_mouse is null)
        {
            return;
        }

        Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

        switch (type)
        {
            case AvnRawMouseEventType.Wheel:
                Input?.Invoke(new RawMouseWheelEventArgs(_mouse, timeStamp, _inputRoot,
                    point.ToAvaloniaPoint(), new Vector(delta.X, delta.Y), (RawInputModifiers)modifiers));
                break;

            case AvnRawMouseEventType.Magnify:
                Input?.Invoke(new RawPointerGestureEventArgs(_mouse, timeStamp, _inputRoot, RawPointerEventType.Magnify,
                    point.ToAvaloniaPoint(), new Vector(delta.X, delta.Y), (RawInputModifiers)modifiers));
                break;

            case AvnRawMouseEventType.Rotate:
                Input?.Invoke(new RawPointerGestureEventArgs(_mouse, timeStamp, _inputRoot, RawPointerEventType.Rotate,
                    point.ToAvaloniaPoint(), new Vector(delta.X, delta.Y), (RawInputModifiers)modifiers));
                break;

            case AvnRawMouseEventType.Swipe:
                Input?.Invoke(new RawPointerGestureEventArgs(_mouse, timeStamp, _inputRoot, RawPointerEventType.Swipe,
                    point.ToAvaloniaPoint(), new Vector(delta.X, delta.Y), (RawInputModifiers)modifiers));
                break;

            default:
                IInputDevice device = deviceType == AvnPointerDeviceType.Pen && _pen != null ? _pen : _mouse;
                var e = new RawPointerEventArgs(device, timeStamp, _inputRoot, (RawPointerEventType)type,
                    new RawPointerPoint
                    {
                        Position = point.ToAvaloniaPoint(),
                        Pressure = pressure,
                        XTilt = xTilt,
                        YTilt = yTilt
                    }, (RawInputModifiers)modifiers);

                if (!ChromeHitTest(e))
                {
                    Input?.Invoke(e);
                }
                break;
        }
    }

    public void Invalidate()
    {
        Native?.Invalidate();
    }

    public void SetInputRoot(IInputRoot inputRoot)
    {
        _inputRoot = inputRoot;
    }

    public Point PointToClient(PixelPoint point)
    {
        return Native?.PointToClient(point.ToAvnPoint()).ToAvaloniaPoint() ?? default;
    }

    public PixelPoint PointToScreen(Point point)
    {
        return Native?.PointToScreen(point.ToAvnPoint()).ToAvaloniaPixelPoint() ?? default;
    }

    public void SetCursor(ICursorImpl? cursor)
    {
        if (Native == null)
        {
            return;
        }

        var newCursor = cursor as AvaloniaNativeCursor;
        newCursor ??= (_cursorFactory?.GetCursor(StandardCursorType.Arrow) as AvaloniaNativeCursor);
        Native.SetCursor(newCursor?.Cursor);
    }

    public virtual IPopupImpl CreatePopup()
    {
        return new PopupImpl(Factory, this);
    }

    public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels)
    {
        foreach (var level in transparencyLevels)
        {
            AvnWindowTransparencyMode? mode = null;

            if (level == WindowTransparencyLevel.None)
                mode = AvnWindowTransparencyMode.Opaque;
            if (level == WindowTransparencyLevel.Transparent)
                mode = AvnWindowTransparencyMode.Transparent;
            else if (level == WindowTransparencyLevel.AcrylicBlur)
                mode = AvnWindowTransparencyMode.Blur;

            if (mode.HasValue && level != TransparencyLevel)
            {
                Native?.SetTransparencyMode(mode.Value);
                TransparencyLevel = level;
                return;
            }
        }

        // If we get here, we didn't find a supported level. Use the default of None.
        if (TransparencyLevel != WindowTransparencyLevel.None)
        {
            Native?.SetTransparencyMode(AvnWindowTransparencyMode.Opaque);
            TransparencyLevel = WindowTransparencyLevel.None;
        }
    }

    public virtual object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(ITextInputMethodImpl))
        {
            return InputMethod;
        }

        if (featureType == typeof(INativeControlHostImpl))
        {
            return _nativeControlHost;
        }

        if (featureType == typeof(IPlatformBehaviorInhibition))
        {
            return _platformBehaviorInhibition;
        }

        if (featureType == typeof(IClipboard))
        {
            return AvaloniaLocator.Current.GetRequiredService<IClipboard>();
        }

        if (featureType == typeof(IScreenImpl))
        {
            return AvaloniaLocator.Current.GetRequiredService<IScreenImpl>();
        }

        if (featureType == typeof(ILauncher))
        {
            return new BclLauncher();
        }

        return null;
    }

    public virtual void Dispose()
    {
        Native?.Dispose();
        _handle = null;

        _nativeControlHost?.Dispose();
        _nativeControlHost = null;

        _mouse?.Dispose();
    }

    protected virtual bool ChromeHitTest(RawPointerEventArgs e)
    {
        return false;
    }

    IFramebufferRenderTarget IFramebufferPlatformSurface.CreateFramebufferRenderTarget()
    {
        if (!Dispatcher.UIThread.CheckAccess())
            throw new RenderTargetNotReadyException();

        var nativeRenderTarget = Native?.CreateSoftwareRenderTarget();

        if (nativeRenderTarget is null)
        {
            throw new RenderTargetNotReadyException();
        }
        
        return new FramebufferRenderTarget(this, nativeRenderTarget);
    }

    protected internal unsafe class TopLevelEvents : NativeCallbackBase, IAvnTopLevelEvents
    {
        private readonly TopLevelImpl _parent;

        public TopLevelEvents(TopLevelImpl parent)
        {
            _parent = parent;
        }

        void IAvnTopLevelEvents.Closed()
        {
            var n = _parent.Native;

            try
            {
                _parent?.Closed?.Invoke();
            }
            finally
            {

                _parent?.Dispose();
                n?.Dispose();
            }
        }

        void IAvnTopLevelEvents.Paint()
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.UiThreadRender);
            var s = _parent.ClientSize;
            _parent.Paint?.Invoke(new Rect(0, 0, s.Width, s.Height));
        }

        void IAvnTopLevelEvents.Resized(AvnSize* size, AvnPlatformResizeReason reason)
        {
            if (_parent?.Native == null)
            {
                return;
            }
            
            var s = new Size(size->Width, size->Height);
            _parent._savedLogicalSize = s;
            _parent.Resized?.Invoke(s, (WindowResizeReason)reason);
        }

        void IAvnTopLevelEvents.RawMouseEvent(AvnRawMouseEventType type, AvnPointerDeviceType pointerDeviceType, ulong timeStamp, AvnInputModifiers modifiers, AvnPoint point, AvnVector delta, float pressure, float xTilt, float yTilt)
        {
            _parent.RawMouseEvent(type, pointerDeviceType, timeStamp, modifiers, point, delta, pressure, xTilt, yTilt);
        }

        int IAvnTopLevelEvents.RawKeyEvent(AvnRawKeyEventType type, ulong timeStamp, AvnInputModifiers modifiers, AvnKey key, AvnPhysicalKey physicalKey, string keySymbol)
        {
            return _parent.RawKeyEvent(type, timeStamp, modifiers, key, physicalKey, keySymbol).AsComBool();
        }

        int IAvnTopLevelEvents.RawTextInputEvent(ulong timeStamp, string text)
        {
            return _parent.RawTextInputEvent(timeStamp, text).AsComBool();
        }

        void IAvnTopLevelEvents.ScalingChanged(double scaling)
        {
            _parent._savedScaling = scaling;
            _parent.ScalingChanged?.Invoke(scaling);
        }

        void IAvnTopLevelEvents.RunRenderPriorityJobs()
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.UiThreadRender);
        }

        void IAvnTopLevelEvents.LostFocus()
        {
            _parent.LostFocus?.Invoke();
        }

        AvnDragDropEffects IAvnTopLevelEvents.DragEvent(AvnDragEventType type, AvnPoint position,
            AvnInputModifiers modifiers,
            AvnDragDropEffects effects,
            IAvnClipboard clipboard, IntPtr dataObjectHandle)
        {
            var device = AvaloniaLocator.Current.GetService<IDragDropDevice>();

            if (device is null)
            {
                return AvnDragDropEffects.None;
            }

            if (_parent._inputRoot is null)
            {
                return AvnDragDropEffects.None;
            }
            
            IDataObject? dataObject = null;
            if (dataObjectHandle != IntPtr.Zero)
                dataObject = GCHandle.FromIntPtr(dataObjectHandle).Target as IDataObject;

            using (var clipboardDataObject = new ClipboardDataObject(clipboard))
            {
                if (dataObject == null)
                    dataObject = clipboardDataObject;

                var args = new RawDragEvent(device, (RawDragEventType)type,
                    _parent._inputRoot, position.ToAvaloniaPoint(), dataObject, (DragDropEffects)effects,
                    (RawInputModifiers)modifiers);
                _parent.Input?.Invoke(args);
                return (AvnDragDropEffects)args.Effects;
            }
        }

        IAvnAutomationPeer? IAvnTopLevelEvents.AutomationPeer
        {
            get
            {
                var native = _parent.GetAutomationPeer();

                return native is null ? null : AvnAutomationPeer.Wrap(native);
            }
        }
    }

    private class FramebufferRenderTarget : IFramebufferRenderTarget
    {
        private readonly TopLevelImpl _parent;
        private IAvnSoftwareRenderTarget? _target;

        public FramebufferRenderTarget(TopLevelImpl parent, IAvnSoftwareRenderTarget target)
        {
            _parent = parent;
            _target = target;
        }

        public void Dispose()
        {
            lock (_parent._syncRoot)
            {
                _target?.Dispose();
                _target = null;
            }
        }

        public ILockedFramebuffer Lock()
        {
            var w = _parent._savedLogicalSize.Width * _parent._savedScaling;
            var h = _parent._savedLogicalSize.Height * _parent._savedScaling;
            var dpi = _parent._savedScaling * 96;
            return new DeferredFramebuffer(_target, cb =>
            {
                lock (_parent._syncRoot)
                {
                    if (_parent.Native != null && _target != null)
                    {
                        cb(_parent.Native);
                    }
                }
            }, (int)w, (int)h, new Vector(dpi, dpi));
        }
    }
}
