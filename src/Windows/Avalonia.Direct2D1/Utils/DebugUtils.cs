using Avalonia.Direct2D1.Media.Imaging;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Utils
{
    internal static class DebugUtils
    {
        public static void Save(ID2D1BitmapRenderTarget bitmap, string filename)
        {
            var rtb = new D2DRenderTargetBitmapImpl(bitmap);
            rtb.Save(filename);
        }
    }
}
