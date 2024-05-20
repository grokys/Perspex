using System.Numerics;
using System.Runtime.InteropServices;
using SharpGen.Runtime;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DirectWrite;

namespace Avalonia.Direct2D1.Media
{
    internal class AvaloniaTextRenderer(
        DrawingContextImpl context,
        ID2D1RenderTarget target,
        ID2D1Brush foreground) : TextRendererBase
    {
        private readonly DrawingContextImpl _context = context;

        private readonly ID2D1RenderTarget _renderTarget = target;

        private readonly ID2D1Brush _foreground = foreground;

        public override void DrawGlyphRun(
            nint clientDrawingContext,
            float baselineOriginX,
            float baselineOriginY,
            MeasuringMode measuringMode,
            GlyphRun glyphRun,
            GlyphRunDescription glyphRunDescription,
            IUnknown clientDrawingEffect)
        {
            var comObject = (ComObject)clientDrawingEffect;
            var wrapper = (BrushWrapper)Marshal.GetObjectForIUnknown(comObject.NativePointer);

            // TODO: Work out how to get the size below rather than passing new Size().
            var brush = (wrapper == null) ?
                _foreground :
                _context.CreateBrush(wrapper.Brush, default).PlatformBrush;

            _renderTarget.DrawGlyphRun(
                new Vector2 { X = baselineOriginX, Y = baselineOriginY },
                glyphRun,
                brush,
                measuringMode);

            if (wrapper != null)
            {
                brush.Dispose();
            }
        }

        public override Matrix3x2 GetCurrentTransform(nint clientDrawingContext)
        {
            return _renderTarget.Transform;
        }

        public override float GetPixelsPerDip(nint clientDrawingContext)
        {
            return _renderTarget.Dpi.Width / 96;
        }
    }
}
