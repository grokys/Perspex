using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Rendering;
using SkiaSharp;

namespace Avalonia.Skia.Helpers
{
    public static class DrawingContextHelper
    {
        /// <inheritdoc cref="RenderAsync(SKCanvas, Visual, Rect, Vector)"/>.
        public static Task RenderAsync(SKCanvas canvas, Visual visual)
        {
            return RenderAsync(canvas, visual, visual.Bounds, SkiaPlatform.DefaultDpi);
        }

        /// <summary>
        /// Renders Avalonia visual into a SKCanvas instance.
        /// This is useful in scenarios where canvas is not controlled by application, but received from another non avalonia api
        /// like: SKCanvas canvas = SKDocument.BeginPage(...);
        /// </summary>
        /// <param name="canvas">Skia canvas to render into.</param>
        /// <param name="visual">Avalonia visual.</param>
        /// <param name="clipRect">Clipping rectangle.</param>
        /// <param name="dpi">Dpi of drawings.</param>
        public static Task RenderAsync(SKCanvas canvas, Visual visual, Rect clipRect, Vector dpi)
        {
            using var drawingContextImpl = WrapSkiaCanvas(canvas, dpi);
            using var drawingContext = new PlatformDrawingContext(drawingContextImpl, false);
            ImmediateRenderer.Render(drawingContext, visual, clipRect);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Wrap Skia canvas in drawing context so we can use Avalonia api to render to external skia canvas
        /// this is useful in scenarios where canvas is not controlled by application, but received from another non avalonia api
        /// like: SKCanvas canvas = SKDocument.BeginPage(...);
        /// </summary>
        /// <param name="canvas">Skia canvas to render into.</param>
        /// <param name="dpi"></param>
        /// <returns>DrawingContext</returns>
        [Unstable("IDrawingContextImpl usage is not supported in Avalonia 11.0.")]
        public static IDrawingContextImpl WrapSkiaCanvas(SKCanvas canvas, Vector dpi)
        {
            var createInfo = new DrawingContextImpl.CreateInfo
            {
                Canvas = canvas,
                Dpi = dpi,
                DisableSubpixelTextRendering = true,
            };

            return new DrawingContextImpl(createInfo);
        }

        public static bool TryCreateDashEffect(IPen? pen, [NotNullWhen(true)] out SKPathEffect? effect)
        {
            if (pen?.DashStyle?.Dashes != null && pen.DashStyle.Dashes.Count > 0)
            {
                var srcDashes = pen.DashStyle.Dashes;

                var count = srcDashes.Count - 1 % 2 == 0 && srcDashes.Count > 1 ? srcDashes.Count : srcDashes.Count * 2;

                var dashesArray = new float[count];

                for (var i = 0; i < count; i++)
                {
                    dashesArray[i] = (float)(srcDashes[i % srcDashes.Count] * pen.Thickness);
                }

                var offset = (float)(pen.DashStyle.Offset * pen.Thickness);

                effect = SKPathEffect.CreateDash(dashesArray, offset);

                return true;
            }

            effect = null;

            return false;
        }
    }
}
