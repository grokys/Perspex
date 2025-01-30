using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.UnitTests;
public class CompositorTestServices : IDisposable
{
    private readonly IDisposable _app;
    public Compositor Compositor { get; }
    internal CompositingRenderer Renderer { get; }
    public ManualRenderTimer Timer { get; } = new();
    public EmbeddableControlRoot TopLevel { get; }
    public DebugEvents Events { get; } = new();

    public void Dispose()
    {
        TopLevel.StopRendering();
        TopLevel.Dispose();
        _app.Dispose();
    }

    public CompositorTestServices(Size? size = null, IPlatformRenderInterface renderInterface = null)
    {
        var services = TestServices.MockPlatformRenderInterface;
        if (renderInterface != null)
            services = services.With(renderInterface: renderInterface);
        
        _app = UnitTestApplication.Start(services);
        try
        {
            AvaloniaLocator.CurrentMutable.Bind<IRenderTimer>().ToConstant(Timer);

            Compositor = new Compositor(new RenderLoop(Timer), null,
                true, new DispatcherCompositorScheduler(), true, Dispatcher.UIThread);
            var impl = new TopLevelImpl(Compositor, size ?? new Size(1000, 1000));
            TopLevel = new EmbeddableControlRoot(impl)
            {
                Template = new FuncControlTemplate((parent, scope) =>
                {
                    var presenter = new ContentPresenter
                    {
                        [~ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty)
                    };
                    scope.Register("PART_ContentPresenter", presenter);
                    return presenter;
                })
            };
            TopLevel.Prepare();
            TopLevel.StartRendering();
            RunJobs();
            Renderer = ((CompositingRenderer)TopLevel.Renderer);
            Renderer.CompositionTarget.Server.DebugEvents = Events;
        }
        catch
        {
            _app.Dispose();
            throw;
        }
    }

    public void RunJobs()
    {
        Dispatcher.UIThread.RunJobs();
        Timer.TriggerTick();
        Dispatcher.UIThread.RunJobs();
    }

    public void AssertRects(params Rect[] rects)
    {
        RunJobs();
        var toAssert = rects.Select(x => x.ToString()).Distinct().OrderBy(x => x);
        var invalidated = Events.Rects.Select(x => x.ToString()).Distinct().OrderBy(x => x);
        Assert.Equal(toAssert, invalidated);
        Events.Rects.Clear();
    }

    public void AssertRenderedVisuals(int renderVisuals)
    {
        RunJobs();
        Assert.Equal(Events.RenderedVisuals, renderVisuals);
        Events.Rects.Clear();
    }

    public void AssertHitTest(double x, double y, Func<Visual, bool> filter, params object[] expected)
        => AssertHitTest(new Point(x, y), filter, expected);

    public void AssertHitTest(Point pt, Func<Visual, bool> filter, params object[] expected)
    {
        RunJobs();
        var tested = Renderer.HitTest(pt, TopLevel, filter);
        Assert.Equal(expected, tested);
    }

    public void AssertHitTestFirst(Point pt, Func<Visual, bool> filter, object expected)
    {
        RunJobs();
        var tested = Renderer.HitTest(pt, TopLevel, filter).First();
        Assert.Equal(expected, tested);
    }

    public class DebugEvents : ICompositionTargetDebugEvents
    {
        public List<Rect> Rects = new();

        public int RenderedVisuals { get; private set; }

        public void IncrementRenderedVisuals()
        {
            RenderedVisuals++;
        }

        public void RectInvalidated(Rect rc)
        {
            Rects.Add(rc);
        }

        public void Reset()
        {
            Rects.Clear();
            RenderedVisuals = 0;
        }
    }

    public class ManualRenderTimer : IRenderTimer
    {
        public event Action<TimeSpan> Tick;
        public bool RunsInBackground => false;
        public void TriggerTick() => Tick?.Invoke(TimeSpan.Zero);
    }

    class TopLevelImpl : ITopLevelImpl
    {
        private readonly Compositor _compositor;

        public TopLevelImpl(Compositor compositor, Size clientSize)
        {
            ClientSize = clientSize;
            _compositor = compositor;
        }

        public void Dispose()
        {

        }

        public double DesktopScaling => 1;
        public IPlatformHandle Handle { get; }
        public Size ClientSize { get; }
        public Size? FrameSize { get; }
        public double RenderScaling => 1;
        public IEnumerable<object> Surfaces { get; } = new[] { new DummyFramebufferSurface() };
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size, WindowResizeReason> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }
        public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }

        class DummyFramebufferSurface : IFramebufferPlatformSurface
        {
            public ILockedFramebuffer Lock()
            {
                var ptr = Marshal.AllocHGlobal(128);
                return new LockedFramebuffer(ptr, new PixelSize(1, 1), 4, new Vector(96, 96),
                    PixelFormat.Rgba8888, () => Marshal.FreeHGlobal(ptr));
            }
            
            public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new FuncFramebufferRenderTarget(Lock);
        }

        public Compositor Compositor => _compositor;

        public void Invalidate(Rect rect)
        {
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
        }

        public Point PointToClient(PixelPoint point) => default;

        public PixelPoint PointToScreen(Point point) => new();

        public void SetCursor(ICursorImpl cursor)
        {
        }

        public Action Closed { get; set; }
        public Action LostFocus { get; set; }
        public IMouseDevice MouseDevice { get; } = new MouseDevice();
        public IPopupImpl CreatePopup() => throw new NotImplementedException();

        public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevel)
        {
        }

        public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;

        public void SetFrameThemeVariant(PlatformThemeVariant? themeVariant)
        {
        }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; }
        
        public object TryGetFeature(Type featureType) => null;
    }
}

public class NullCompositorScheduler : ICompositorScheduler
{
    public void CommitRequested(Compositor compositor)
    {
        
    }
}

public class DispatcherCompositorScheduler : ICompositorScheduler
{
    public void CommitRequested(Compositor compositor)
    {
        Dispatcher.UIThread.Post(() => compositor.Commit(), DispatcherPriority.UiThreadRender);
    }
}
