using Avalonia.Platform;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    internal class TransformedGeometryImpl : GeometryImpl, ITransformedGeometryImpl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        /// <param name="source">The source geometry.</param>
        /// <param name="geometry">An existing Direct2D <see cref="ID2D1TransformedGeometry"/>.</param>
        public TransformedGeometryImpl(ID2D1TransformedGeometry geometry, GeometryImpl source)
            : base(geometry)
        {
            SourceGeometry = source;
        }

        public IGeometryImpl SourceGeometry { get; }

        /// <inheritdoc/>
        public Matrix Transform => Geometry.QueryInterface<ID2D1TransformedGeometry>().Transform.ToAvalonia();

        protected override ID2D1Geometry GetSourceGeometry() => Geometry.QueryInterface<ID2D1TransformedGeometry>().SourceGeometry;
    }
}
