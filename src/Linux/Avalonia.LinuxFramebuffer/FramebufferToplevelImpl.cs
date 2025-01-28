 using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.LinuxFramebuffer.Input;
using Avalonia.LinuxFramebuffer.Output;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
 using Avalonia.Threading;

 namespace Avalonia.LinuxFramebuffer
{
    class FramebufferToplevelImpl : ITopLevelImpl, IScreenInfoProvider
    {
        private readonly IOutputBackend _outputBackend;
        private readonly IInputBackend _inputBackend;
        private readonly RawEventGrouper _inputQueue;

        public IInputRoot? InputRoot { get; private set; }

        public FramebufferToplevelImpl(IOutputBackend outputBackend, IInputBackend inputBackend)
        {
            _outputBackend = outputBackend;
            _inputBackend = inputBackend;
            _inputQueue = new RawEventGrouper(groupedInput => Input?.Invoke(groupedInput),
                LinuxFramebufferPlatform.EventGrouperDispatchQueue);

            Surfaces = new object[] { _outputBackend };
            _inputBackend.Initialize(this, e =>
                Dispatcher.UIThread.Post(() => _inputQueue.HandleEvent(e), DispatcherPriority.Send ));
        }

        public Compositor Compositor => LinuxFramebufferPlatform.Compositor;

        public void Dispose()
        {
            throw new NotSupportedException();
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
            _inputBackend.SetInputRoot(inputRoot);
        }

        public Point PointToClient(PixelPoint p) => p.ToPoint(1);

        public PixelPoint PointToScreen(Point p) => PixelPoint.FromPoint(p, 1);

        public void SetCursor(ICursorImpl? cursor)
        {
        }

        public double DesktopScaling => 1;
        public IPlatformHandle? Handle => null;
        public Size ClientSize => ScaledSize;
        public Size? FrameSize => null;
        public IMouseDevice MouseDevice { get; } = new MouseDevice();
        public IPopupImpl? CreatePopup() => null;

        public double RenderScaling => _outputBackend.Scaling;
        public IEnumerable<object> Surfaces { get; }
        public Action<RawInputEventArgs>? Input { get; set; }
        public Action<Rect>? Paint { get; set; }
        public Action<Size, WindowResizeReason>? Resized { get; set; }
        public Action<double>? ScalingChanged { get; set; }

        public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

        public Action? Closed { get; set; }
        public Action? LostFocus { get; set; }

        public Size ScaledSize => _outputBackend.PixelSize.ToSize(RenderScaling);

        public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevel) { }

        public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;

        public void SetFrameThemeVariant(PlatformThemeVariant? themeVariant) { }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels(1, 1, 1);
        public object? TryGetFeature(Type featureType) => null;
    }
}
