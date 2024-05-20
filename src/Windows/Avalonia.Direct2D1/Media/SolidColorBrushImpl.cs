using Avalonia.Media;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    internal class SolidColorBrushImpl : BrushImpl
    {
        public SolidColorBrushImpl(ISolidColorBrush brush, ID2D1RenderTarget target)
        {
            PlatformBrush = target.CreateSolidColorBrush(
                brush?.Color.ToDirect2D() ?? new Vortice.Mathematics.Color(),
                new BrushProperties
                {
                    Opacity = brush != null ? (float)brush.Opacity : 1.0f,
                    Transform = target.Transform
                }
            );
        }

        /// <summary>
        /// Direct2D has no ConicGradient implementation so fall back to a solid colour brush based on 
        /// the first gradient stop.
        /// </summary>
        public SolidColorBrushImpl(IConicGradientBrush brush, ID2D1DeviceContext target)
        {
            PlatformBrush = target.CreateSolidColorBrush(
                brush?.GradientStops[0].Color.ToDirect2D() ?? new Vortice.Mathematics.Color(),
                new BrushProperties
                {
                    Opacity = brush != null ? (float)brush.Opacity : 1.0f,
                    Transform = target.Transform
                }
            );
        }
    }
}
