using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A Direct2D implementation of a <see cref="Avalonia.Media.RectangleGeometry"/>.
    /// </summary>
    internal class RectangleGeometryImpl : GeometryImpl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        public RectangleGeometryImpl(Rect rect)
            : base(CreateGeometry(rect))
        {
        }

        private static ID2D1Geometry CreateGeometry(Rect rect)
        {
            return Direct2D1Platform.Direct2D1Factory.CreateRectangleGeometry(rect.ToDirect2D());
        }
    }
}
