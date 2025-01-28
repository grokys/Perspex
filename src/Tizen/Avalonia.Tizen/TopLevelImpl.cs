using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Rendering.Composition;
using Avalonia.Tizen.Platform.Input;

namespace Avalonia.Tizen;

internal class TopLevelImpl : ITopLevelImpl
{
    private readonly ITizenView _view;
    private readonly NuiClipboardImpl _clipboard;
    private readonly IStorageProvider _storageProvider;
    private readonly NuiScreens _screen;

    public TopLevelImpl(ITizenView view, IEnumerable<object> surfaces)
    {
        _view = view;
        Surfaces = surfaces;

        _storageProvider = new TizenStorageProvider();
        _clipboard = new NuiClipboardImpl();
        _screen = new NuiScreens();
    }

    public double DesktopScaling => RenderScaling;
    public IPlatformHandle? Handle { get; }

    public Size ClientSize => _view.ClientSize;

    public Size? FrameSize => null;

    public double RenderScaling => 1;

    public IEnumerable<object> Surfaces { get; set; }

    public Action<RawInputEventArgs>? Input { get; set; }
    public Action<Rect>? Paint { get; set; }
    public Action<Size, WindowResizeReason>? Resized { get; set; }
    public Action<double>? ScalingChanged { get; set; }
    public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

    public Compositor Compositor => TizenPlatform.Compositor;

    public Action? Closed { get; set; }
    public Action? LostFocus { get; set; }

    public WindowTransparencyLevel TransparencyLevel { get; set; }

    public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels();
    public IPopupImpl? CreatePopup()
    {
        return null;
    }

    public void Dispose()
    {
        //
    }

    public Point PointToClient(PixelPoint point) => new Point(point.X, point.Y);

    public PixelPoint PointToScreen(Point point) => new PixelPoint((int)point.X, (int)point.Y);

    public void SetCursor(ICursorImpl? cursor)
    {
        //
    }

    public void SetFrameThemeVariant(PlatformThemeVariant? themeVariant)
    {
        //
    }

    public void SetInputRoot(IInputRoot inputRoot)
    {
        _view.InputRoot = inputRoot;
    }

    public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels)
    {
        //
    }

    public object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(IStorageProvider))
        {
            return _storageProvider;
        }

        if (featureType == typeof(ITextInputMethodImpl))
        {
            return _view;
        }

        if (featureType == typeof(INativeControlHostImpl))
        {
            return _view.NativeControlHost;
        }

        if (featureType == typeof(IClipboard))
        {
            return _clipboard;
        }

        if (featureType == typeof(ILauncher))
        {
            return new TizenLauncher();
        }

        if (featureType == typeof(IScreenImpl))
        {
            return _screen;
        }

        return null;
    }

    internal void TextInput(string text)
    {
        if (Input == null) return;
        var args = new RawTextInputEventArgs(TizenKeyboardDevice.Instance!, (ulong)DateTime.Now.Ticks, _view.InputRoot, text);

        Input(args);
    }
}
