using Vortice.Direct2D1;

namespace Avalonia.Direct2D1
{
    public interface IExternalDirect2DRenderTargetSurface
    {
        ID2D1RenderTarget GetOrCreateRenderTarget();
        void DestroyRenderTarget();
        void BeforeDrawing();
        void AfterDrawing();
    }
}
