#if AVALONIA_SKIA
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.UnitTests;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.RenderTests;

public class DirectFbCompositionTests : TestBase
{
    public DirectFbCompositionTests()
        : base(@"Composition\DirectFb")
    {
    }
    
    class FuncFramebufferSurface : IFramebufferPlatformSurface
    {
        private readonly Func<IFramebufferRenderTarget> _cb;

        public FuncFramebufferSurface(Func<IFramebufferRenderTarget> cb)
        {
            _cb = cb;
        }
            
        public IFramebufferRenderTarget CreateFramebufferRenderTarget()
        {
            return _cb();
        }
    }

    [Theory,
     InlineData(false),
     InlineData(true)]
    void Should_Only_Update_Clipped_Rects_When_Retained_Fb_Is_Advertised(bool advertised)
    {
        var timer = new ManualRenderTimer();
        var compositor = new Compositor(new RenderLoop(timer), null, true,
            new DispatcherCompositorScheduler(), true, Dispatcher.UIThread, null, new CompositionOptions
            {
                UseRegionDirtyRectClipping = true
            });

        Rectangle r1, r2;
        var control = new Canvas
        {
            Width = 200, Height = 200, Background = Brushes.Yellow,
            Children =
            {
                (r1 = new Rectangle
                {
                    Fill = Brushes.Black,
                    Width = 40,
                    Height = 40,
                    Opacity = 0.6,
                    [Canvas.LeftProperty] = 40,
                    [Canvas.TopProperty] = 40,
                }),
                (r2 = new Rectangle
                {
                    Fill = Brushes.Black,
                    Width = 40,
                    Height = 40,
                    Opacity = 0.6,
                    [Canvas.LeftProperty] = 120,
                    [Canvas.TopProperty] = 40,
                }),
            }
        };
        var root = new TestRenderRoot(1, null!);
        SKBitmap fb = new SKBitmap(200, 200, SKColorType.Rgba8888, SKAlphaType.Premul);

        ILockedFramebuffer LockFb() => new LockedFramebuffer(fb.GetAddress(0, 0), new(fb.Width, fb.Height),
            fb.RowBytes, new Vector(96, 96), PixelFormat.Rgba8888, null);

        bool previousFrameIsRetained = false;
        IFramebufferRenderTarget rt = advertised
            ? new FuncRetainedFramebufferRenderTarget((out FramebufferLockProperties props) =>
            {
                props = new() { PreviousFrameIsRetained = previousFrameIsRetained };
                return LockFb();
            })
            : new FuncFramebufferRenderTarget(LockFb);
        
        using var renderer =
            new CompositingRenderer(root, compositor, () => new[] { new FuncFramebufferSurface(() => rt) });
        root.Initialize(renderer, control);
        control.Measure(new Size(control.Width, control.Height));
        control.Arrange(new Rect(control.DesiredSize));
        renderer.Start();
        Dispatcher.UIThread.RunJobs();
        timer.TriggerTick();
        var image1 =
            $"{nameof(Should_Only_Update_Clipped_Rects_When_Retained_Fb_Is_Advertised)}_advertized-{advertised}_initial";
        SaveFile(fb, image1);

        fb.Erase(SKColor.Empty);
        
        previousFrameIsRetained = advertised;
        
        r1.Fill = Brushes.Red;
        r2.Fill = Brushes.Green;
        Dispatcher.UIThread.RunJobs();
        timer.TriggerTick();
        var image2 =
            $"{nameof(Should_Only_Update_Clipped_Rects_When_Retained_Fb_Is_Advertised)}_advertized-{advertised}_updated";
        SaveFile(fb, image2);
        CompareImages(image1, skipImmediate: true);
        CompareImages(image2, skipImmediate: true);

    }

    void SaveFile(SKBitmap bmp, string name)
    {
        Directory.CreateDirectory(OutputPath);
        var path = System.IO.Path.Combine(OutputPath, name + ".composited.out.png");
        using var d = bmp.Encode(SKEncodedImageFormat.Png, 100);
        using var f = File.Create(path);
        d.SaveTo(f);
    }
    
}
#endif
