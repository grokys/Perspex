using Avalonia.Platform;
using Vortice.Direct2D1;
using AM = Avalonia.Media;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A Direct2D implementation of a <see cref="Avalonia.Media.CombinedGeometry"/>.
    /// </summary>
    internal class CombinedGeometryImpl : GeometryImpl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        public CombinedGeometryImpl(
            AM.GeometryCombineMode combineMode,
            IGeometryImpl geometry1,
            IGeometryImpl geometry2)
            : base(CreateGeometry(combineMode, geometry1, geometry2))
        {
        }

        private static ID2D1Geometry CreateGeometry(
            AM.GeometryCombineMode combineMode,
            IGeometryImpl geometry1,
            IGeometryImpl geometry2)
        {
            var g1 = ((GeometryImpl)geometry1).Geometry;
            var g2 = ((GeometryImpl)geometry2).Geometry;
            var dest = Direct2D1Platform.Direct2D1Factory.CreatePathGeometry();
            using var sink = dest.Open();
            g1.CombineWithGeometry(g2, (CombineMode)combineMode, sink);
            sink.Close();
            return dest;
        }
    }
}
