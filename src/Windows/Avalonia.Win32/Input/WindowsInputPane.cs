using System;
using Avalonia.Controls.Platform;
using Avalonia.Win32.Interop;
using Avalonia.Win32.Win32Com;
using Avalonia.Win32.WinRT;
using MicroCom.Runtime;

namespace Avalonia.Win32.Input;

internal unsafe class WindowsInputPane : InputPaneBase, IDisposable
{
    private static readonly Lazy<bool> s_inputPaneSupported = new(() =>
        WinRTApiInformation.IsTypePresent("Windows.UI.ViewManagement.InputPane"));

    // GUID: D5120AA3-46BA-44C5-822D-CA8092C1FC72
    private static readonly Guid CLSID_FrameworkInputPane = new(0xD5120AA3, 0x46BA, 0x44C5, 0x82, 0x2D, 0xCA, 0x80, 0x92, 0xC1, 0xFC, 0x72);
    // GUID: 5752238B-24F0-495A-82F1-2FD593056796
    private static readonly Guid SID_IFrameworkInputPane = new(0x5752238B, 0x24F0, 0x495A, 0x82, 0xF1, 0x2F, 0xD5, 0x93, 0x05, 0x67, 0x96);

    private WindowImpl _windowImpl;
    private IFrameworkInputPane? _inputPane;
    private readonly uint _cookie;
    private Handler _eventHandler;

    private WindowsInputPane(WindowImpl windowImpl)
    {
        _windowImpl = windowImpl;
        using (var inputPane =
               UnmanagedMethods.CreateInstance<IFrameworkInputPane>(in CLSID_FrameworkInputPane, in SID_IFrameworkInputPane))
        {
            _inputPane = inputPane.CloneReference();
        }

        _eventHandler = new Handler(this);
        uint cookie = 0;
        _inputPane.AdviseWithHWND(windowImpl.Handle.Handle, _eventHandler, &cookie);
        _cookie = cookie;

    }

    public static WindowsInputPane? TryCreate(WindowImpl windowImpl)
    {
        if (s_inputPaneSupported.Value)
        {
            return new WindowsInputPane(windowImpl);
        }

        return null;
    }

    private void OnStateChanged(bool showing, UnmanagedMethods.RECT? prcInputPaneScreenLocation)
    {
        var oldState = (OccludedRect, State);
        OccludedRect = prcInputPaneScreenLocation.HasValue
            ? ScreenRectToClient(prcInputPaneScreenLocation.Value)
            : default;
        State = showing ? InputPaneState.Open : InputPaneState.Closed;

        if (oldState != (OccludedRect, State))
        {
            OnStateChanged(new InputPaneStateEventArgs(State, null, OccludedRect));
        }
    }

    private Rect ScreenRectToClient(UnmanagedMethods.RECT screenRect)
    {
        var position = new PixelPoint(screenRect.left, screenRect.top);
        var size = new PixelSize(screenRect.Width, screenRect.Height);
        return new Rect(_windowImpl.PointToClient(position), size.ToSize(_windowImpl.DesktopScaling));
    }

    public void Dispose()
    {
        _windowImpl = null!;
        _eventHandler?.Dispose();
        _eventHandler = null!;
        if (_inputPane is not null)
        {
            if (_cookie != 0)
            {
                _inputPane.Unadvise(_cookie);
            }

            _inputPane.Dispose();
            _inputPane = null;
        }
    }

    private class Handler : IUnknown, IMicroComShadowContainer, IFrameworkInputPaneHandler
    {
        private WindowsInputPane _pane;

        private readonly object _lock = new();
        private bool _destroyed;
        public bool IsDestroyed => _destroyed;

        public void Dispose()
        {
            lock (_lock)
            {
                DestroyIfNeeded();
            }
        }

        void DestroyIfNeeded()
        {
            if (_destroyed)
                return;
            _destroyed = true;
            Shadow?.Dispose();
            Shadow = null;
            _pane = null!;
        }

        public MicroComShadow? Shadow { get; set; }
        public void OnReferencedFromNative()
        {

        }

        public void OnUnreferencedFromNative()
        {
            lock (_lock)
            {
                DestroyIfNeeded();
            }
        }

        public Handler(WindowsInputPane pane) => _pane = pane;
        public void Showing(UnmanagedMethods.RECT* rect, int _) => _pane.OnStateChanged(true, *rect);
        public void Hiding(int fEnsureFocusedElementInView) => _pane.OnStateChanged(false, null);
    }
}
