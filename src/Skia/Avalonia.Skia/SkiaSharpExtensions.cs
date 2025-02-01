using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace Avalonia.Skia
{
    public static class SkiaSharpExtensions
    {
        public static SKFilterQuality ToSKFilterQuality(this BitmapInterpolationMode interpolationMode)
        {
            switch (interpolationMode)
            {
                case BitmapInterpolationMode.Unspecified:
                case BitmapInterpolationMode.LowQuality:
                    return SKFilterQuality.Low;
                case BitmapInterpolationMode.MediumQuality:
                    return SKFilterQuality.Medium;
                case BitmapInterpolationMode.HighQuality:
                    return SKFilterQuality.High;
                case BitmapInterpolationMode.None:
                    return SKFilterQuality.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(interpolationMode), interpolationMode, null);
            }
        }

        public static SKBlendMode ToSKBlendMode(this BitmapBlendingMode blendingMode)
        {
            return blendingMode switch
            {
                BitmapBlendingMode.Unspecified => SKBlendMode.SrcOver,
                BitmapBlendingMode.SourceOver => SKBlendMode.SrcOver,
                BitmapBlendingMode.Source => SKBlendMode.Src,
                BitmapBlendingMode.SourceIn => SKBlendMode.SrcIn,
                BitmapBlendingMode.SourceOut => SKBlendMode.SrcOut,
                BitmapBlendingMode.SourceAtop => SKBlendMode.SrcATop,
                BitmapBlendingMode.Destination => SKBlendMode.Dst,
                BitmapBlendingMode.DestinationIn => SKBlendMode.DstIn,
                BitmapBlendingMode.DestinationOut => SKBlendMode.DstOut,
                BitmapBlendingMode.DestinationOver => SKBlendMode.DstOver,
                BitmapBlendingMode.DestinationAtop => SKBlendMode.DstATop,
                BitmapBlendingMode.Xor => SKBlendMode.Xor,
                BitmapBlendingMode.Plus => SKBlendMode.Plus,
                BitmapBlendingMode.Screen => SKBlendMode.Screen,
                BitmapBlendingMode.Overlay => SKBlendMode.Overlay,
                BitmapBlendingMode.Darken => SKBlendMode.Darken,
                BitmapBlendingMode.Lighten => SKBlendMode.Lighten,
                BitmapBlendingMode.ColorDodge => SKBlendMode.ColorDodge,
                BitmapBlendingMode.ColorBurn => SKBlendMode.ColorBurn,
                BitmapBlendingMode.HardLight => SKBlendMode.HardLight,
                BitmapBlendingMode.SoftLight => SKBlendMode.SoftLight,
                BitmapBlendingMode.Difference => SKBlendMode.Difference,
                BitmapBlendingMode.Exclusion => SKBlendMode.Exclusion,
                BitmapBlendingMode.Multiply => SKBlendMode.Multiply,
                BitmapBlendingMode.Hue => SKBlendMode.Hue,
                BitmapBlendingMode.Saturation => SKBlendMode.Saturation,
                BitmapBlendingMode.Color => SKBlendMode.Color,
                BitmapBlendingMode.Luminosity => SKBlendMode.Luminosity,
                _ => throw new ArgumentOutOfRangeException(nameof(blendingMode), blendingMode, null)
            };
        }

        public static SKPoint ToSKPoint(this Point p)
        {
            return new SKPoint((float)p.X, (float)p.Y);
        }
        
        public static SKPoint ToSKPoint(this Vector p)
        {
            return new SKPoint((float)p.X, (float)p.Y);
        }

        public static SKRect ToSKRect(this Rect r)
        {
            return new SKRect((float)r.X, (float)r.Y, (float)r.Right, (float)r.Bottom);
        }
        
        internal static SKRect ToSKRect(this LtrbRect r)
        {
            return new SKRect((float)r.Left, (float)r.Right, (float)r.Right, (float)r.Bottom);
        }
        
        public static SKRectI ToSKRectI(this PixelRect r)
        {
            return new SKRectI(r.X, r.Y, r.Right, r.Bottom);
        }
        
        internal static SKRectI ToSKRectI(this LtrbPixelRect r)
        {
            return new SKRectI(r.Left, r.Top, r.Right, r.Bottom);
        }

        public static SKRoundRect ToSKRoundRect(this RoundedRect r)
        {
            var rc = r.Rect.ToSKRect();
            var result = new SKRoundRect();

            result.SetRectRadii(rc,
                   new[]
                   {
                        r.RadiiTopLeft.ToSKPoint(), r.RadiiTopRight.ToSKPoint(),
                        r.RadiiBottomRight.ToSKPoint(), r.RadiiBottomLeft.ToSKPoint(),
                   });            

            return result;
        }

        public static Rect ToAvaloniaRect(this SKRect r)
        {
            return new Rect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
        }
        
        internal static LtrbRect ToAvaloniaLtrbRect(this SKRect r)
        {
            return new LtrbRect(r.Left, r.Top, r.Right, r.Bottom);
        }
        
        public static PixelRect ToAvaloniaPixelRect(this SKRectI r)
        {
            return new PixelRect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
        }
        
        internal static LtrbPixelRect ToAvaloniaLtrbPixelRect(this SKRectI r)
        {
            return new LtrbPixelRect(r.Left, r.Top, r.Right, r.Bottom);
        }

        public static SKMatrix ToSKMatrix(this Matrix m)
        {
            var sm = new SKMatrix
            {
                ScaleX = (float)m.M11,
                SkewX = (float)m.M21,
                TransX = (float)m.M31,
                SkewY = (float)m.M12,
                ScaleY = (float)m.M22,
                TransY = (float)m.M32,
                Persp0 = (float)m.M13,
                Persp1 = (float)m.M23,
                Persp2 = (float)m.M33
            };

            return sm;
        }

        internal static Matrix ToAvaloniaMatrix(this SKMatrix m) => new(
            m.ScaleX, m.SkewY, m.Persp0,
            m.SkewX, m.ScaleY, m.Persp1,
            m.TransX, m.TransY, m.Persp2);

        public static SKColor ToSKColor(this Color c)
        {
            return new SKColor(c.R, c.G, c.B, c.A);
        }

        public static SKColorType ToSkColorType(this PixelFormat fmt)
        {
            if (fmt == PixelFormat.Rgb565)
                return SKColorType.Rgb565;
            if (fmt == PixelFormat.Bgra8888)
                return SKColorType.Bgra8888;
            if (fmt == PixelFormat.Rgba8888)
                return SKColorType.Rgba8888;
            if (fmt == PixelFormat.Rgb32)
                return SKColorType.Rgb888x;
            throw new ArgumentException("Unknown pixel format: " + fmt);
        }

        public static PixelFormat? ToAvalonia(this SKColorType colorType)
        {
            if (colorType == SKColorType.Rgb565)
                return PixelFormats.Rgb565;
            if (colorType == SKColorType.Bgra8888)
                return PixelFormats.Bgra8888;
            if (colorType == SKColorType.Rgba8888)
                return PixelFormats.Rgba8888;
            if (colorType == SKColorType.Rgb888x)
                return PixelFormats.Rgb32;
            return null;
        }

        public static PixelFormat ToPixelFormat(this SKColorType fmt)
        {
            if (fmt == SKColorType.Rgb565)
                return PixelFormat.Rgb565;
            if (fmt == SKColorType.Bgra8888)
                return PixelFormat.Bgra8888;
            if (fmt == SKColorType.Rgba8888)
                return PixelFormat.Rgba8888;
            throw new ArgumentException("Unknown pixel format: " + fmt);
        }

        public static SKAlphaType ToSkAlphaType(this AlphaFormat fmt)
        {
            return fmt switch
            {
                AlphaFormat.Premul => SKAlphaType.Premul,
                AlphaFormat.Unpremul => SKAlphaType.Unpremul,
                AlphaFormat.Opaque => SKAlphaType.Opaque,
                _ => throw new ArgumentException($"Unknown alpha format: {fmt}")
            };
        }

        public static AlphaFormat ToAlphaFormat(this SKAlphaType fmt)
        {
            return fmt switch
            {
                SKAlphaType.Premul => AlphaFormat.Premul,
                SKAlphaType.Unpremul => AlphaFormat.Unpremul,
                SKAlphaType.Opaque => AlphaFormat.Opaque,
                _ => throw new ArgumentException($"Unknown alpha format: {fmt}")
            };
        }

        public static SKShaderTileMode ToSKShaderTileMode(this GradientSpreadMethod m)
        {
            switch (m)
            {
                default:
                case GradientSpreadMethod.Pad: return SKShaderTileMode.Clamp;
                case GradientSpreadMethod.Reflect: return SKShaderTileMode.Mirror;
                case GradientSpreadMethod.Repeat: return SKShaderTileMode.Repeat;
            }
        }

        public static SKTextAlign ToSKTextAlign(this TextAlignment a)
        {
            switch (a)
            {
                default:
                case TextAlignment.Left: return SKTextAlign.Left;
                case TextAlignment.Center: return SKTextAlign.Center;
                case TextAlignment.Right: return SKTextAlign.Right;
            }
        }

        public static SKStrokeCap ToSKStrokeCap(this PenLineCap cap)
        {
            return cap switch
            {
                PenLineCap.Round => SKStrokeCap.Round,
                PenLineCap.Square => SKStrokeCap.Square,
                _ => SKStrokeCap.Butt
            };
        }

        public static SKStrokeJoin ToSKStrokeJoin(this PenLineJoin join)
        {
            return join switch
            {
                PenLineJoin.Bevel => SKStrokeJoin.Bevel,
                PenLineJoin.Round => SKStrokeJoin.Round,
                _ => SKStrokeJoin.Miter
            };
        }

        public static TextAlignment ToAvalonia(this SKTextAlign a)
        {
            switch (a)
            {
                default:
                case SKTextAlign.Left: return TextAlignment.Left;
                case SKTextAlign.Center: return TextAlignment.Center;
                case SKTextAlign.Right: return TextAlignment.Right;
            }
        }

        public static FontStyle ToAvalonia(this SKFontStyleSlant slant)
        {
            return slant switch
            {
                SKFontStyleSlant.Upright => FontStyle.Normal,
                SKFontStyleSlant.Italic => FontStyle.Italic,
                SKFontStyleSlant.Oblique => FontStyle.Oblique,
                _ => throw new ArgumentOutOfRangeException(nameof (slant), slant, null)
            };
        }

        [return: NotNullIfNotNull(nameof(src))]
        public static SKPath? Clone(this SKPath? src)
        {
            return src != null ? new SKPath(src) : null;
        }
    }
}
