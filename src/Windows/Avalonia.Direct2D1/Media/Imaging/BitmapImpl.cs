using System;
using System.IO;
using Avalonia.Platform;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    internal abstract class BitmapImpl : IBitmapImpl, IDisposable
    {
        public abstract Vector Dpi { get; }
        public abstract PixelSize PixelSize { get; }
        public int Version { get; protected set; } = 1;

        public abstract OptionalDispose<ID2D1Bitmap1> GetDirect2DBitmap(ID2D1RenderTarget target);

        public void Save(string fileName, int? quality = null)
        {
            if (Path.GetExtension(fileName) != ".png")
            {
                // Yeah, we need to support other formats.
                throw new NotSupportedException("Use PNG, stoopid.");
            }

            using (FileStream s = new FileStream(fileName, FileMode.Create))
            {
                Save(s, quality);
            }
        }

        public abstract void Save(Stream stream, int? quality = null);

        public virtual void Dispose()
        {
        }
    }
}
