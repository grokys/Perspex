using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Reactive;
using System.Threading.Tasks;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia.DesignerSupport.Remote
{
    class WindowStub : IWindowImpl, IPopupImpl
    {
        public Action Deactivated { get; set; }
        public Action Activated { get; set; }
        public IPlatformHandle Handle { get; }
        public Size MaxAutoSizeHint { get; }
        public Size ClientSize { get; }
        public Size? FrameSize => null;
        public double RenderScaling { get; } = 1.0;
        public double DesktopScaling => 1.0;
        public IEnumerable<object> Surfaces { get; }
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size, WindowResizeReason> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }
        public Func<WindowCloseReason, bool> Closing { get; set; }
        public Action Closed { get; set; }
        public Action LostFocus { get; set; }
        public IMouseDevice MouseDevice { get; } = new MouseDevice();
        public IPopupImpl CreatePopup() => new WindowStub(this);

        public PixelPoint Position { get; set; }
        public Action<PixelPoint> PositionChanged { get; set; }
        public WindowState WindowState { get; set; }
        public Action<WindowState> WindowStateChanged { get; set; }

        public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }        

        public Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }

        public Thickness ExtendedMargins { get; } = new Thickness();

        public Thickness OffScreenMargin { get; } = new Thickness();

        public WindowStub(IWindowImpl parent = null)
        {
            if (parent != null)
                PopupPositioner = new ManagedPopupPositioner(new ManagedPopupPositionerPopupImplHelper(parent,
                    (_, size, __) =>
                    {
                        Resize(size, WindowResizeReason.Unspecified);
                    }));
        }

        private sealed class DummyRenderTimer : IRenderTimer
        {
            public event Action<TimeSpan> Tick
            {
                add { }
                remove { }
            }

            public bool RunsInBackground => false;
        }

        public Compositor Compositor { get; } = new(new RenderLoop(new DummyRenderTimer()), null);

        public void Dispose()
        {
        }
        public void Invalidate(Rect rect)
        {
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
        }

        public Point PointToClient(PixelPoint p) => p.ToPoint(1);

        public PixelPoint PointToScreen(Point p) => PixelPoint.FromPoint(p, 1);

        public void SetCursor(ICursorImpl cursor)
        {
        }

        public void Show(bool activate, bool isDialog)
        {
        }

        public void Hide()
        {
        }

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {
        }

        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {
        }

        public void Activate()
        {
        }

        public void Resize(Size clientSize, WindowResizeReason reason)
        {
        }

        public void Move(PixelPoint point)
        {

        }
        
        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
        }

        public void SetTitle(string title)
        {
        }

        public void ShowDialog(IWindowImpl parent)
        {
        }

        public void SetSystemDecorations(SystemDecorations enabled)
        {
        }

        public void SetIcon(IWindowIconImpl icon)
        {
        }

        public void ShowTaskbarIcon(bool value)
        {
        }

        public void CanResize(bool value)
        {
        }

        public void SetTopmost(bool value)
        {
        }

        public void SetParent(IWindowImpl parent)
        {
        }

        public void SetEnabled(bool enable)
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

        public void GetWindowsZOrder(Span<Window> windows, Span<long> zOrder) => throw new NotSupportedException();

        public IPopupPositioner PopupPositioner { get; }

        public Action GotInputWhenDisabled { get; set; }

        public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevel) { }

        public void SetWindowManagerAddShadowHint(bool enabled)
        {
        }

        public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;

        public bool IsClientAreaExtendedToDecorations { get; }

        public bool NeedsManagedDecorations => false;

        public void SetFrameThemeVariant(PlatformThemeVariant themeVariant) { }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels(1, 1, 1);
        public object TryGetFeature(Type featureType)
        {
            if (featureType == typeof(IStorageProvider))
            {
                return new NoopStorageProvider();
            }

            if (featureType == typeof(IScreenImpl))
            {
                return new ScreenStub();
            }

            return null;
        }
        public void TakeFocus() { }
    }

    class ClipboardStub : IClipboard
    {
        public Task<string> GetTextAsync() => Task.FromResult("");

        public Task SetTextAsync(string text) => Task.CompletedTask;

        public Task ClearAsync() => Task.CompletedTask;
        public Task SetDataObjectAsync(IDataObject data) => Task.CompletedTask;
        public Task<string[]> GetFormatsAsync() => Task.FromResult(Array.Empty<string>());

        public Task<object> GetDataAsync(string format) => Task.FromResult((object)null);

        public Task FlushAsync() =>
            Task.CompletedTask;
    }

    class CursorFactoryStub : ICursorFactory
    {
        public ICursorImpl GetCursor(StandardCursorType cursorType) => new CursorStub();
        public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot) => new CursorStub();

        private class CursorStub : ICursorImpl
        {
            public void Dispose() { }
        }
    }

    class IconLoaderStub : IPlatformIconLoader
    {
        class IconStub : IWindowIconImpl
        {
            public void Save(Stream outputStream)
            {

            }
        }

        public IWindowIconImpl LoadIcon(string fileName) => new IconStub();

        public IWindowIconImpl LoadIcon(Stream stream) => new IconStub();

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap) => new IconStub();
    }

    class ScreenStub : ScreensBase<int, PlatformScreen>
    {
        protected override IReadOnlyList<int> GetAllScreenKeys() => new[] { 1 };

        protected override PlatformScreen CreateScreenFromKey(int key) => new PlatformScreenStub(key);

        private class PlatformScreenStub : PlatformScreen
        {
            public PlatformScreenStub(int key) : base(new PlatformHandle((nint) key, nameof(ScreenStub))) 
            {
                Scaling = 1;
                Bounds = WorkingArea = new PixelRect(0, 0, 4000, 4000);
                IsPrimary = true;
            }
        }
    }

    internal class NoopStorageProvider : BclStorageProvider
    {
        public override bool CanOpen => false;
        public override Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
        {
            return Task.FromResult<IReadOnlyList<IStorageFile>>(Array.Empty<IStorageFile>());
        }

        public override bool CanSave => false;
        public override Task<IStorageFile> SaveFilePickerAsync(FilePickerSaveOptions options)
        {
            return Task.FromResult<IStorageFile>(null);
        }

        public override bool CanPickFolder => false;
        public override Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
        {
            return Task.FromResult<IReadOnlyList<IStorageFolder>>(Array.Empty<IStorageFolder>());
        }
    }
}
