using System;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    internal abstract class BrushImpl : IDisposable
    {
        public ID2D1Brush PlatformBrush { get; set; }

        public virtual void Dispose()
        {
            PlatformBrush?.Dispose();
        }
    }
}
