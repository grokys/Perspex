using Avalonia.Direct2D1.Media;
using Avalonia.Direct2D1.Media.Imaging;
using Avalonia.Platform;
using SharpGen.Runtime;

namespace Avalonia.Direct2D1
{
    class ExternalRenderTarget : IRenderTarget, ILayerFactory
    {
        private readonly IExternalDirect2DRenderTargetSurface _externalRenderTargetProvider;

        public ExternalRenderTarget(
            IExternalDirect2DRenderTargetSurface externalRenderTargetProvider)
        {
            _externalRenderTargetProvider = externalRenderTargetProvider;
        }

        public void Dispose()
        {
            _externalRenderTargetProvider.DestroyRenderTarget();
        }

        public IDrawingContextImpl CreateDrawingContext(bool useScaledDrawing)
        {
            var target =  _externalRenderTargetProvider.GetOrCreateRenderTarget();
            _externalRenderTargetProvider.BeforeDrawing();
            return new DrawingContextImpl( null, target, useScaledDrawing, null, () =>
            {
                try
                {
                    _externalRenderTargetProvider.AfterDrawing();
                }
                catch (SharpGenException ex) when ((uint) ex.HResult == 0x8899000C) // D2DERR_RECREATE_TARGET
                {
                    _externalRenderTargetProvider.DestroyRenderTarget();
                }
            });
        }

        public bool IsCorrupted => false;

        public IDrawingContextLayerImpl CreateLayer(Size size)
        {
            var renderTarget = _externalRenderTargetProvider.GetOrCreateRenderTarget();
            return D2DRenderTargetBitmapImpl.CreateCompatible(renderTarget, size);
        }
    }
}
