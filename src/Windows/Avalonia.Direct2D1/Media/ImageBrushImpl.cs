using Avalonia.Media;
using Avalonia.Rendering.Utilities;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    internal sealed class ImageBrushImpl : BrushImpl
    {
        private readonly OptionalDispose<ID2D1Bitmap1> _bitmap;

        public ImageBrushImpl(
            ITileBrush brush,
            ID2D1RenderTarget target,
            BitmapImpl bitmap,
            Rect destinationRect)
        {
            var dpi = new Vector(target.Dpi.Width, target.Dpi.Height);
            var calc = new TileBrushCalculator(brush, bitmap.PixelSize.ToSizeWithDpi(dpi), destinationRect.Size);

            Vector brushOffset = default;
            if (brush.DestinationRect.Unit == RelativeUnit.Relative)
                brushOffset = new Vector(destinationRect.X, destinationRect.Y);
            
            if (!calc.NeedsIntermediate)
            {
                _bitmap = bitmap.GetDirect2DBitmap(target);
                PlatformBrush = target.CreateBitmapBrush(
                    _bitmap.Value,
                    GetBitmapBrushProperties(brush),
                    GetBrushProperties(brush, calc.DestinationRect, brushOffset));
            }
            else
            {
                using (var intermediate = RenderIntermediate(target, bitmap, calc))
                {
                    PlatformBrush = target.CreateBitmapBrush(
                        intermediate.Bitmap,
                        GetBitmapBrushProperties(brush),
                        GetBrushProperties(brush, calc.DestinationRect, brushOffset));
                }
            }
        }

        public override void Dispose()
        {
            _bitmap.Dispose();
            base.Dispose();
        }

        private static BitmapBrushProperties GetBitmapBrushProperties(ITileBrush brush)
        {
            var tileMode = brush.TileMode;

            return new BitmapBrushProperties
            {
                ExtendModeX = GetExtendModeX(tileMode),
                ExtendModeY = GetExtendModeY(tileMode),
            };
        }

        private static BrushProperties GetBrushProperties(ITileBrush brush, Rect destinationRect, Vector offset)
        {
            var tileTransform =
                brush.TileMode != TileMode.None ?
                Matrix.CreateTranslation(destinationRect.X, destinationRect.Y) :
                Matrix.Identity;

            if (offset != default)
                tileTransform = Matrix.CreateTranslation(offset);

            if (brush.Transform != null && brush.TileMode != TileMode.None)
            {
                var transformOrigin = brush.TransformOrigin.ToPixels(destinationRect);
                var originOffset = Matrix.CreateTranslation(transformOrigin);

                tileTransform = -originOffset * brush.Transform.Value * originOffset * tileTransform;
            }

            return new BrushProperties
            {
                Opacity = (float)brush.Opacity,
                Transform = tileTransform.ToDirect2D(),
            };
        }

        private static ExtendMode GetExtendModeX(TileMode tileMode)
        {
            return (tileMode & TileMode.FlipX) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;
        }

        private static ExtendMode GetExtendModeY(TileMode tileMode)
        {
            return (tileMode & TileMode.FlipY) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;
        }

        private ID2D1BitmapRenderTarget RenderIntermediate(
            ID2D1RenderTarget target,
            BitmapImpl bitmap,
            TileBrushCalculator calc)
        {
            var result = target.CreateCompatibleRenderTarget(
                calc.IntermediateSize.ToSharpDX(),
                null,
                null,
                CompatibleRenderTargetOptions.None);

            using (var context = new RenderTarget(result).CreateDrawingContext(true))
            {
                var dpi = new Vector(target.Dpi.Width, target.Dpi.Height);
                var rect = new Rect(bitmap.PixelSize.ToSizeWithDpi(dpi));

                context.Clear(Colors.Transparent);
                context.PushClip(calc.IntermediateClip);
                context.Transform = calc.IntermediateTransform;
                context.DrawBitmap(bitmap, 1, rect, rect);
                context.PopClip();
            }

            return result;
        }
    }
}
