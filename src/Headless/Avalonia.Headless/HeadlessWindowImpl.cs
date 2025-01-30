using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia.Headless
{
    internal class HeadlessWindowImpl : IWindowImpl, IPopupImpl, IFramebufferPlatformSurface, IHeadlessWindow
    {
        private static int _nextGlobalZOrder = 1;

        private readonly IKeyboardDevice _keyboard;
        private readonly IScreenImpl _screen;
        private readonly Pointer _mousePointer;
        private WriteableBitmap? _lastRenderedFrame;
        private readonly object _sync = new object();
        private readonly PixelFormat _frameBufferFormat;
        private int _zOrder;
        public bool IsPopup { get; }

        public HeadlessWindowImpl(bool isPopup, PixelFormat frameBufferFormat)
        {
            IsPopup = isPopup;
            Surfaces = new object[] { this };
            _keyboard = AvaloniaLocator.Current.GetRequiredService<IKeyboardDevice>();
            _screen = new HeadlessScreensStub();
            _mousePointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            MouseDevice = new MouseDevice(_mousePointer);
            ClientSize = new Size(1024, 768);
            _frameBufferFormat = frameBufferFormat;
        }

        public void Dispose()
        {
            Closed?.Invoke();
            _lastRenderedFrame?.Dispose();
            _lastRenderedFrame = null;
        }

        public Size ClientSize { get; set; }
        public Size? FrameSize => null;
        public double RenderScaling { get; } = 1;
        public double DesktopScaling => RenderScaling;
        public IEnumerable<object> Surfaces { get; }
        public Action<RawInputEventArgs>? Input { get; set; }
        public Action<Rect>? Paint { get; set; }
        public Action<Size, WindowResizeReason>? Resized { get; set; }
        public Action<double>? ScalingChanged { get; set; }

        public Compositor Compositor => AvaloniaHeadlessPlatform.Compositor!;

        public void Invalidate(Rect rect)
        {
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }

        public IInputRoot? InputRoot { get; set; }

        public Point PointToClient(PixelPoint point) => point.ToPoint(RenderScaling);

        public PixelPoint PointToScreen(Point point) => PixelPoint.FromPoint(point, RenderScaling);

        public void SetCursor(ICursorImpl? cursor)
        {

        }

        public Action? Closed { get; set; }
        public IMouseDevice MouseDevice { get; }

        public void Show(bool activate, bool isDialog)
        {
            if (activate)
            {
                _zOrder = _nextGlobalZOrder++;
                Dispatcher.UIThread.Post(() => Activated?.Invoke(), DispatcherPriority.Input);
            }
        }

        public void Hide()
        {
            Dispatcher.UIThread.Post(() => Deactivated?.Invoke(), DispatcherPriority.Input);
        }

        public void BeginMoveDrag()
        {

        }

        public void BeginResizeDrag(WindowEdge edge)
        {

        }

        public PixelPoint Position { get; set; }
        public Action<PixelPoint>? PositionChanged { get; set; }
        public void Activate()
        {
            _zOrder = _nextGlobalZOrder++;
            Dispatcher.UIThread.Post(() => Activated?.Invoke(), DispatcherPriority.Input);
        }

        public Action? Deactivated { get; set; }
        public Action? Activated { get; set; }
        public IPlatformHandle Handle { get; } = new PlatformHandle(IntPtr.Zero, "STUB");
        public void Resize(Size clientSize, WindowResizeReason reason)
        {
            if (ClientSize == clientSize)
                return;

            // Emulate X11 behavior here
            if (IsPopup)
                DoResize(clientSize, reason);
            else
                Dispatcher.UIThread.Post(() =>
                {
                    DoResize(clientSize, reason);
                }, DispatcherPriority.Send);
        }

        private void DoResize(Size clientSize, WindowResizeReason reason)
        {
            // Uncomment this check and experience a weird bug in layout engine
            if (ClientSize != clientSize)
            {
                ClientSize = clientSize;
                Resized?.Invoke(clientSize, reason);
            }
        }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {

        }

        public void SetTopmost(bool value)
        {

        }

        public WindowState WindowState { get; set; }
        public Action<WindowState>? WindowStateChanged { get; set; }
        public void SetTitle(string? title)
        {

        }

        public void SetSystemDecorations(bool enabled)
        {

        }

        public void SetIcon(IWindowIconImpl? icon)
        {

        }

        public void ShowTaskbarIcon(bool value)
        {

        }

        public void CanResize(bool value)
        {

        }

        public Func<WindowCloseReason, bool>? Closing { get; set; }

        private class FramebufferProxy : ILockedFramebuffer
        {
            private readonly ILockedFramebuffer _fb;
            private readonly Action _onDispose;
            private bool _disposed;

            public FramebufferProxy(ILockedFramebuffer fb, Action onDispose)
            {
                _fb = fb;
                _onDispose = onDispose;
            }
            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                _fb.Dispose();
                _onDispose();
            }

            public IntPtr Address => _fb.Address;
            public PixelSize Size => _fb.Size;
            public int RowBytes => _fb.RowBytes;
            public Vector Dpi => _fb.Dpi;
            public PixelFormat Format => _fb.Format;
        }

        public ILockedFramebuffer Lock()
        {
            var bmp = new WriteableBitmap(PixelSize.FromSize(ClientSize, RenderScaling), new Vector(96, 96) * RenderScaling, _frameBufferFormat, AlphaFormat.Premul);
            var fb = bmp.Lock();
            return new FramebufferProxy(fb, () =>
            {
                lock (_sync)
                {
                    _lastRenderedFrame?.Dispose();
                    _lastRenderedFrame = bmp;
                }
            });
        }

        public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new FuncFramebufferRenderTarget(Lock);

        public WriteableBitmap? GetLastRenderedFrame()
        {
            lock (_sync)
            {
                if (_lastRenderedFrame is null)
                {
                    return null;
                }

                using var lockedFramebuffer = _lastRenderedFrame.Lock();
                return new WriteableBitmap(lockedFramebuffer.Format, AlphaFormat.Opaque, lockedFramebuffer.Address,
                    lockedFramebuffer.Size, lockedFramebuffer.Dpi, lockedFramebuffer.RowBytes);
            }
        }

        private ulong Timestamp => (ulong)HeadlessTimeProvider.GetCurrent().GetTimestamp();

        // TODO: Hook recent Popup changes. 
        IPopupPositioner IPopupImpl.PopupPositioner => null!;

        public Size MaxAutoSizeHint => new Size(1920, 1080);

        public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

        public WindowTransparencyLevel TransparencyLevel { get; set; } = WindowTransparencyLevel.Transparent;

        public Action? GotInputWhenDisabled { get; set; }

        public bool IsClientAreaExtendedToDecorations => false;

        public Action<bool>? ExtendClientAreaToDecorationsChanged { get; set; }

        public bool NeedsManagedDecorations => false;

        public Thickness ExtendedMargins => new Thickness();

        public Thickness OffScreenMargin => new Thickness();

        public Action? LostFocus { get; set; }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels => new AcrylicPlatformCompensationLevels(1, 1, 1);
        public object? TryGetFeature(Type featureType)
        {
        	if (featureType == typeof(IClipboard))
            {
                return AvaloniaLocator.Current.GetRequiredService<IClipboard>();
            }

            if (featureType == typeof(IScreenImpl))
            {
                return _screen;
            }

            return null;
        }

        void IHeadlessWindow.KeyPress(Key key, RawInputModifiers modifiers, PhysicalKey physicalKey, string? keySymbol)
        {
            Input?.Invoke(new RawKeyEventArgs(
                _keyboard,
                Timestamp,
                InputRoot!,
                RawKeyEventType.KeyDown,
                key,
                modifiers,
                physicalKey,
                keySymbol));
        }

        void IHeadlessWindow.KeyRelease(Key key, RawInputModifiers modifiers, PhysicalKey physicalKey, string? keySymbol)
        {
            Input?.Invoke(new RawKeyEventArgs(
                _keyboard,
                Timestamp,
                InputRoot!,
                RawKeyEventType.KeyUp,
                key,
                modifiers,
                physicalKey,
                keySymbol));
        }

        void IHeadlessWindow.TextInput(string text)
        {
            if (InputRoot == null)
                return;

            Input?.Invoke(new RawTextInputEventArgs(_keyboard, 0, InputRoot, text));
        }

        void IHeadlessWindow.MouseDown(Point point, MouseButton button, RawInputModifiers modifiers)
        {
            Input?.Invoke(new RawPointerEventArgs(MouseDevice, Timestamp, InputRoot!,
                button switch
                {
                    MouseButton.Left => RawPointerEventType.LeftButtonDown,
                    MouseButton.Right => RawPointerEventType.RightButtonDown,
                    MouseButton.Middle => RawPointerEventType.MiddleButtonDown,
                    MouseButton.XButton1 => RawPointerEventType.XButton1Down,
                    MouseButton.XButton2 => RawPointerEventType.XButton2Down,
                    _ => RawPointerEventType.Move,
                }, point, modifiers));
        }

        void IHeadlessWindow.MouseMove(Point point, RawInputModifiers modifiers)
        {
            Input?.Invoke(new RawPointerEventArgs(MouseDevice, Timestamp, InputRoot!,
                RawPointerEventType.Move, point, modifiers));
        }

        void IHeadlessWindow.MouseUp(Point point, MouseButton button, RawInputModifiers modifiers)
        {
            Input?.Invoke(new RawPointerEventArgs(MouseDevice, Timestamp, InputRoot!,
                button switch
                {
                    MouseButton.Left => RawPointerEventType.LeftButtonUp,
                    MouseButton.Right => RawPointerEventType.RightButtonUp,
                    MouseButton.Middle => RawPointerEventType.MiddleButtonUp,
                    MouseButton.XButton1 => RawPointerEventType.XButton1Up,
                    MouseButton.XButton2 => RawPointerEventType.XButton2Up,
                    _ => RawPointerEventType.Move,
                }, point, modifiers));
        }
        
        void IHeadlessWindow.MouseWheel(Point point, Vector delta, RawInputModifiers modifiers)
        {
            Input?.Invoke(new RawMouseWheelEventArgs(MouseDevice, Timestamp, InputRoot!,
                point, delta, modifiers));
        }
        
        void IHeadlessWindow.DragDrop(Point point, RawDragEventType type, IDataObject data, DragDropEffects effects, RawInputModifiers modifiers)
        {
            var device = AvaloniaLocator.Current.GetRequiredService<IDragDropDevice>();
            Input?.Invoke(new RawDragEvent(device, type, InputRoot!, point, data, effects, modifiers));
        }

        void IWindowImpl.Move(PixelPoint point)
        {
            Position = point;
            PositionChanged?.Invoke(point);
        }

        public IPopupImpl? CreatePopup()
        {
            // TODO: Hook recent Popup changes. 
            return null;
        }

        public void SetWindowManagerAddShadowHint(bool enabled)
        {
            
        }

        public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevel)
        {
            foreach (var item in transparencyLevel)
            {
                if (item == WindowTransparencyLevel.Transparent) {
                    TransparencyLevel = item;
                    return;
                }
            }

            TransparencyLevel = WindowTransparencyLevel.None;
        }

        public void SetParent(IWindowImpl? parent)
        {
            
        }

        public void SetEnabled(bool enable)
        {
            
        }

        public void SetSystemDecorations(SystemDecorations enabled)
        {
            
        }

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {
            
        }

        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {
            
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
        
        public void SetFrameThemeVariant(PlatformThemeVariant themeVariant)
        {
            
        }

        public void GetWindowsZOrder(Span<Window> windows, Span<long> zOrder)
        {
            for (int i = 0; i < windows.Length; ++i)
            {
                if (windows[i].PlatformImpl is HeadlessWindowImpl headlessWindowImpl)
                    zOrder[i] = headlessWindowImpl._zOrder;
            }
        }

        public void TakeFocus() 
        {
        }
    }
}
