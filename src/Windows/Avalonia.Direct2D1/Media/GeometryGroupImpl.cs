using System.Collections.Generic;
using Avalonia.Platform;
using Vortice.Direct2D1;
using AM = Avalonia.Media;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A Direct2D implementation of a <see cref="Avalonia.Media.GeometryGroup"/>.
    /// </summary>
    internal class GeometryGroupImpl : GeometryImpl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        public GeometryGroupImpl(AM.FillRule fillRule, IReadOnlyList<IGeometryImpl> geometry)
            : base(CreateGeometry(fillRule, geometry))
        {
        }

        private static ID2D1Geometry CreateGeometry(AM.FillRule fillRule, IReadOnlyList<IGeometryImpl> children)
        {
            var count = children.Count;
            var c = new ID2D1Geometry[count];

            for (var i = 0; i < count; ++i)
            {
                c[i] = ((GeometryImpl)children[i]).Geometry;
            }

            return Direct2D1Platform.Direct2D1Factory.CreateGeometryGroup((FillMode)fillRule, c);
        }
    }
}
