using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;

namespace Avalonia.Controls.Embedding.Offscreen
{
    [Unstable]
    public abstract class OffscreenTopLevelImplBase : ITopLevelImpl
    {
        private double _scaling = 1;
        private Size _clientSize;

        public IInputRoot? InputRoot { get; private set; }
        public bool IsDisposed { get; private set; }

        public virtual void Dispose()
        {
            IsDisposed = true;
        }

        public Compositor Compositor { get; }

        public OffscreenTopLevelImplBase()
            => Compositor = new Compositor(null);

        public abstract IEnumerable<object> Surfaces { get; }

        public virtual double DesktopScaling => _scaling;

        public IPlatformHandle? Handle { get; }

        public Size ClientSize
        {
            get => _clientSize;
            set
            {
                _clientSize = value;
                Resized?.Invoke(value, WindowResizeReason.Unspecified);
            }
        }

        public Size? FrameSize => null;

        public double RenderScaling
        {
            get => _scaling;
            set
            {
                _scaling = value;
                ScalingChanged?.Invoke(value);
            }
        }

        public Action<RawInputEventArgs>? Input { get; set; }
        public Action<Rect>? Paint { get; set; }
        public Action<Size, WindowResizeReason>? Resized { get; set; }
        public Action<double>? ScalingChanged { get; set; }

        public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

        public void SetFrameThemeVariant(PlatformThemeVariant? themeVariant) { }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels(1, 1, 1);

        public void SetInputRoot(IInputRoot inputRoot) => InputRoot = inputRoot;

        public virtual Point PointToClient(PixelPoint point) => point.ToPoint(1);

        public virtual PixelPoint PointToScreen(Point point) => PixelPoint.FromPoint(point, 1);

        public virtual void SetCursor(ICursorImpl? cursor)
        {
        }

        public Action? Closed { get; set; }
        public Action? LostFocus { get; set; }
        public abstract IMouseDevice MouseDevice { get; }

        public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevel) { }

        public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;

        public IPopupImpl? CreatePopup() => null;
        
        public virtual object? TryGetFeature(Type featureType) => null;
    }
}
