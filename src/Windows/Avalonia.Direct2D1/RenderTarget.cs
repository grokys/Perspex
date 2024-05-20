using Avalonia.Direct2D1.Media;
using Avalonia.Direct2D1.Media.Imaging;
using Avalonia.Platform;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RenderTarget"/> class.
    /// </summary>
    /// <param name="renderTarget">The render target.</param>
    internal class RenderTarget(ID2D1RenderTarget renderTarget) : IRenderTarget, ILayerFactory
    {
        /// <summary>
        /// The render target.
        /// </summary>
        private readonly ID2D1RenderTarget _renderTarget = renderTarget;

        /// <summary>
        /// Creates a drawing context for a rendering session.
        /// </summary>
        /// <returns>An <see cref="Avalonia.Platform.IDrawingContextImpl"/>.</returns>
        public IDrawingContextImpl CreateDrawingContext(bool useScaledDrawing)
        {
            return new DrawingContextImpl(this, _renderTarget, useScaledDrawing);
        }

        public bool IsCorrupted => false;

        public IDrawingContextLayerImpl CreateLayer(Size size)
        {
            return D2DRenderTargetBitmapImpl.CreateCompatible(_renderTarget, size);
        }

        public void Dispose()
        {
            _renderTarget.Dispose();
        }
    }
}
