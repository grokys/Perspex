using System.Linq;
using Avalonia.Media;

namespace Avalonia.Direct2D1.Media
{
    internal class RadialGradientBrushImpl : BrushImpl
    {
        public RadialGradientBrushImpl(
            IRadialGradientBrush brush,
            SharpDX.Direct2D1.RenderTarget target,
            Size destinationSize)
        {
            if (brush.GradientStops.Count == 0)
            {
                return;
            }

            var gradientStops = brush.GradientStops.Select(s => new SharpDX.Direct2D1.GradientStop
            {
                Color = s.Color.ToDirect2D(),
                Position = (float)s.Offset
            }).ToArray();

            var centerPoint = brush.Center.ToPixels(destinationSize);
            var gradientOrigin = brush.GradientOrigin.ToPixels(destinationSize) - centerPoint;
            
            var radiusX = brush.RadiusX.ToValue(destinationSize.Width);
            var radiusY = brush.RadiusY.ToValue(destinationSize.Height);

            using (var stops = new SharpDX.Direct2D1.GradientStopCollection(
                target,
                gradientStops,
                brush.SpreadMethod.ToDirect2D()))
            {
                PlatformBrush = new SharpDX.Direct2D1.RadialGradientBrush(
                    target,
                    new SharpDX.Direct2D1.RadialGradientBrushProperties
                    {
                        Center = centerPoint.ToSharpDX(),
                        GradientOriginOffset = gradientOrigin.ToSharpDX(),
                        RadiusX = (float)radiusX,
                        RadiusY = (float)radiusY
                    },
                    new SharpDX.Direct2D1.BrushProperties
                    {
                        Opacity = (float)brush.Opacity,
                        Transform = PrimitiveExtensions.Matrix3x2Identity,
                    },
                    stops);
            }
        }
    }
}
