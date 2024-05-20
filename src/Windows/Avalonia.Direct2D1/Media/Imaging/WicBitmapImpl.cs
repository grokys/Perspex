using System;
using System.IO;
using Avalonia.Win32.Interop;
using Vortice.WIC;
using APixelFormat = Avalonia.Platform.PixelFormat;
using AlphaFormat = Avalonia.Platform.AlphaFormat;
using Avalonia.Platform;
using Vortice.Direct2D1;
using BitmapInterpolationMode = Vortice.WIC.BitmapInterpolationMode;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A WIC implementation of a <see cref="Avalonia.Media.Imaging.Bitmap"/>.
    /// </summary>
    internal class WicBitmapImpl : BitmapImpl, IReadableBitmapWithAlphaImpl
    {
        private readonly IWICBitmapDecoder _decoder;

        private static BitmapInterpolationMode ConvertInterpolationMode(Avalonia.Media.Imaging.BitmapInterpolationMode interpolationMode)
        {
            return interpolationMode switch
            {
                Avalonia.Media.Imaging.BitmapInterpolationMode.Unspecified => BitmapInterpolationMode.Fant,
                Avalonia.Media.Imaging.BitmapInterpolationMode.LowQuality => BitmapInterpolationMode.NearestNeighbor,
                Avalonia.Media.Imaging.BitmapInterpolationMode.MediumQuality => BitmapInterpolationMode.Fant,
                _ => BitmapInterpolationMode.HighQualityCubic,
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WicBitmapImpl"/> class.
        /// </summary>
        /// <param name="fileName">The filename of the bitmap to load.</param>
        public WicBitmapImpl(string fileName)
        {
            using (var decoder = Direct2D1Platform.ImagingFactory.CreateDecoderFromFileName(fileName, metadataOptions: DecodeOptions.CacheOnDemand))
            using (var frame = decoder.GetFrame(0))
            {
                WicImpl = Direct2D1Platform.ImagingFactory.CreateBitmapFromSource(frame, BitmapCreateCacheOption.CacheOnDemand);
                Dpi = new Vector(96, 96);
                SetFormatFromWic(WicImpl.PixelFormat);
            }
        }

        private WicBitmapImpl(IWICBitmap bmp)
        {
            WicImpl = bmp;
            Dpi = new Vector(96, 96);
            SetFormatFromWic(WicImpl.PixelFormat);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WicBitmapImpl"/> class.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param>
        public WicBitmapImpl(Stream stream)
        {
            // https://stackoverflow.com/questions/48982749/decoding-image-from-stream-using-wic/48982889#48982889
            _decoder = Direct2D1Platform.ImagingFactory.CreateDecoderFromStream(stream, DecodeOptions.CacheOnLoad);

            using var frame = _decoder.GetFrame(0);
            WicImpl = Direct2D1Platform.ImagingFactory.CreateBitmapFromSource(frame, BitmapCreateCacheOption.CacheOnLoad);
            Dpi = new Vector(96, 96);
            SetFormatFromWic(WicImpl.PixelFormat);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WicBitmapImpl"/> class.
        /// </summary>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="pixelFormat">Pixel format</param>
        /// <param name="alphaFormat">Alpha format.</param>
        public WicBitmapImpl(PixelSize size, Vector dpi, APixelFormat? pixelFormat = null, AlphaFormat? alphaFormat = null)
        {
            if (!pixelFormat.HasValue)
            {
                pixelFormat = APixelFormat.Bgra8888;
            }

            if (!alphaFormat.HasValue)
            {
                alphaFormat = Platform.AlphaFormat.Premul;
            }

            PixelFormat = pixelFormat;
            AlphaFormat = alphaFormat;
            WicImpl = Direct2D1Platform.ImagingFactory.CreateBitmap(
                size.Width,
                size.Height,
                pixelFormat.Value.ToWic(alphaFormat.Value),
                BitmapCreateCacheOption.CacheOnLoad);

            Dpi = dpi;
        }

        public WicBitmapImpl(APixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            WicImpl = Direct2D1Platform.ImagingFactory.CreateBitmap(size.Width, size.Height, format.ToWic(alphaFormat), BitmapCreateCacheOption.CacheOnDemand);
            WicImpl.SetResolution(dpi.X, dpi.Y);
            PixelFormat = format;
            AlphaFormat = alphaFormat;
            Dpi = dpi;

            using (var l = WicImpl.Lock(BitmapLockFlags.Write))
            {
                for (var row = 0; row < size.Height; row++)
                {
                    UnmanagedMethods.CopyMemory(
                        (l.Data.DataPointer + row * l.Stride),
                        (data + row * stride),
                        (UIntPtr)l.Data.Pitch);
                }
            }
        }

        public WicBitmapImpl(Stream stream, int decodeSize, bool horizontal, Avalonia.Media.Imaging.BitmapInterpolationMode interpolationMode)
        {
            _decoder = Direct2D1Platform.ImagingFactory.CreateDecoderFromStream(stream, DecodeOptions.CacheOnLoad);

            using var frame = _decoder.GetFrame(0);

            // now scale that to the size that we want
            var realScale = horizontal ? ((double)frame.Size.Height / frame.Size.Width) : ((double)frame.Size.Width / frame.Size.Height);

            PixelSize desired;

            if (horizontal)
            {
                desired = new PixelSize(decodeSize, (int)(realScale * decodeSize));
            }
            else
            {
                desired = new PixelSize((int)(realScale * decodeSize), decodeSize);
            }

            if (frame.Size.Width != desired.Width || frame.Size.Height != desired.Height)
            {
                using (var scaler = Direct2D1Platform.ImagingFactory.CreateBitmapScaler())
                {
                    scaler.Initialize(frame, desired.Width, desired.Height, ConvertInterpolationMode(interpolationMode));

                    WicImpl = Direct2D1Platform.ImagingFactory.CreateBitmapFromSource(scaler, BitmapCreateCacheOption.CacheOnLoad);
                }
            }
            else
            {
                WicImpl = Direct2D1Platform.ImagingFactory.CreateBitmapFromSource(frame, BitmapCreateCacheOption.CacheOnLoad);
            }

            Dpi = new Vector(96, 96);
        }

        private void SetFormatFromWic(Guid pixelFormat)
        {
            if (pixelFormat == Vortice.WIC.PixelFormat.Format16bppBGR565)
            {
                PixelFormat = APixelFormat.Rgb565;
                AlphaFormat = Platform.AlphaFormat.Premul;
            }
            else if (pixelFormat == Vortice.WIC.PixelFormat.Format32bppRGB)
            {
                PixelFormat = APixelFormat.Rgb32;
                AlphaFormat = Platform.AlphaFormat.Premul;
            }
            else if (pixelFormat == PixelFormats.Rgba8888.ToWic(Platform.AlphaFormat.Premul))
            {
                PixelFormat = APixelFormat.Rgba8888;
                AlphaFormat = Platform.AlphaFormat.Premul;
            }
            else if (pixelFormat == PixelFormats.Rgba8888.ToWic(Platform.AlphaFormat.Opaque))
            {
                PixelFormat = APixelFormat.Rgba8888;
                AlphaFormat = Platform.AlphaFormat.Opaque;
            }
            else if (pixelFormat == PixelFormats.Bgra8888.ToWic(Platform.AlphaFormat.Premul))
            {
                PixelFormat = APixelFormat.Bgra8888;
                AlphaFormat = Platform.AlphaFormat.Premul;
            }
            else if (pixelFormat == PixelFormats.Bgra8888.ToWic(Platform.AlphaFormat.Opaque))
            {
                PixelFormat = APixelFormat.Bgra8888;
                AlphaFormat = Platform.AlphaFormat.Opaque;
            }
        }

        public override Vector Dpi { get; }

        public override PixelSize PixelSize => WicImpl.Size.ToAvalonia();

        public APixelFormat? PixelFormat { get; private set; }

        public AlphaFormat? AlphaFormat { get; private set; }

        public override void Dispose()
        {
            WicImpl.Dispose();
            _decoder?.Dispose();
        }

        /// <summary>
        /// Gets the WIC implementation of the bitmap.
        /// </summary>
        public IWICBitmap WicImpl { get; }

        /// <summary>
        /// Gets a Direct2D bitmap to use on the specified render target.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <returns>The Direct2D bitmap.</returns>
        public override OptionalDispose<ID2D1Bitmap1> GetDirect2DBitmap(ID2D1RenderTarget renderTarget)
        {
            using var converter = Direct2D1Platform.ImagingFactory.CreateFormatConverter();
            converter.Initialize(WicImpl, Vortice.WIC.PixelFormat.Format32bppPBGRA);

            var d2dBitmap = renderTarget.CreateBitmapFromWicBitmap(converter).QueryInterface<ID2D1Bitmap1>();

            return new OptionalDispose<ID2D1Bitmap1>(d2dBitmap, true);
        }

        public override void Save(Stream stream, int? quality = null)
        {
            using (var encoder = Direct2D1Platform.ImagingFactory.CreateEncoder(ContainerFormat.Png, stream))
            using (var frame = encoder.CreateNewFrame(out var props))
            {
                frame.Initialize(props);
                frame.WriteSource(WicImpl);
                frame.Commit();
                encoder.Commit();
            }
        }

        class LockedBitmap(WicBitmapImpl parent, IWICBitmapLock l, APixelFormat format) : ILockedFramebuffer
        {
            private readonly WicBitmapImpl _parent = parent;
            private readonly IWICBitmapLock _lock = l;
            private readonly APixelFormat _format = format;

            public void Dispose()
            {
                _lock.Dispose();
                _parent.Version++;
            }

            public IntPtr Address => _lock.Data.DataPointer;
            public PixelSize Size => _lock.Size.ToAvalonia();
            public int RowBytes => _lock.Stride;
            public Vector Dpi => _parent.Dpi;
            public APixelFormat Format => _format;

        }

        APixelFormat? IReadableBitmapImpl.Format => PixelFormat;

        public ILockedFramebuffer Lock() =>
            new LockedBitmap(this, WicImpl.Lock(BitmapLockFlags.Write), PixelFormat.Value);
    }
}
