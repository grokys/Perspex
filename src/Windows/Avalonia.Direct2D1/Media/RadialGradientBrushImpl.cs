using System.Linq;
using System.Numerics;
using Avalonia.Media;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    internal class RadialGradientBrushImpl : BrushImpl
    {
        public RadialGradientBrushImpl(
            IRadialGradientBrush brush,
            ID2D1RenderTarget target,
            Rect destinationRect)
        {
            if (brush.GradientStops.Count == 0)
            {
                return;
            }

            var gradientStops = brush.GradientStops.Select(s => new Vortice.Direct2D1.GradientStop
            {
                Color = s.Color.ToDirect2D(),
                Position = (float)s.Offset
            }).ToArray();

            var centerPoint = brush.Center.ToPixels(destinationRect);
            var gradientOrigin = brush.GradientOrigin.ToPixels(destinationRect) - centerPoint;
            
            var radiusX = brush.RadiusX.ToValue(destinationRect.Width);
            var radiusY = brush.RadiusY.ToValue(destinationRect.Height);

            using (var stops = target.CreateGradientStopCollection(
                gradientStops,
                brush.SpreadMethod.ToDirect2D()))
            {
                PlatformBrush = target.CreateRadialGradientBrush(
                    new RadialGradientBrushProperties
                    {
                        Center = centerPoint.ToVortice(),
                        GradientOriginOffset = gradientOrigin.ToVortice(),
                        RadiusX = (float)radiusX,
                        RadiusY = (float)radiusY
                    },
                    new BrushProperties
                    {
                        Opacity = (float)brush.Opacity,
                        Transform = Matrix3x2.Identity,
                    },
                    stops);
            }
        }
    }
}
