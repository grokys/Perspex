using System;
using System.IO;
using Vortice.Direct2D1;
using Vortice.WIC;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A Direct2D Bitmap implementation that uses a GPU memory bitmap as its image.
    /// </summary>
    /// <remarks>
    /// Initialize a new instance of the <see cref="BitmapImpl"/> class
    /// with a bitmap backed by GPU memory.
    /// </remarks>
    /// <param name="d2DBitmap">The GPU bitmap.</param>
    /// <remarks>
    /// This bitmap must be either from the same render target,
    /// or if the render target is a <see cref="ID2D1DeviceContext"/>,
    /// the device associated with this context, to be renderable.
    /// </remarks>
    internal class D2DBitmapImpl(ID2D1Bitmap1 d2DBitmap) : BitmapImpl
    {
        private readonly ID2D1Bitmap1 _direct2DBitmap = d2DBitmap ?? throw new ArgumentNullException(nameof(d2DBitmap));

        public override Vector Dpi => new Vector(96, 96);
        public override PixelSize PixelSize => _direct2DBitmap.PixelSize.ToAvalonia();

        public override void Dispose()
        {
            base.Dispose();
            _direct2DBitmap.Dispose();
        }

        public override OptionalDispose<ID2D1Bitmap1> GetDirect2DBitmap(ID2D1RenderTarget target)
        {
            return new OptionalDispose<ID2D1Bitmap1>(_direct2DBitmap, false);
        }

        public override void Save(Stream stream, int? quality = null)
        {
            using (var encoder = Direct2D1Platform.ImagingFactory.CreateEncoder(ContainerFormat.Png, stream))
            using (var frame = encoder.CreateNewFrame(out var props))
            using (var bitmapSource = _direct2DBitmap.QueryInterface<IWICBitmapSource>())
            {
                frame.Initialize(props);
                frame.WriteSource(bitmapSource);
                frame.Commit();
                encoder.Commit();
            }
        }
    }
}
;
