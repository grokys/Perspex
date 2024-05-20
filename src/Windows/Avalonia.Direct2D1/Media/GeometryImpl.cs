using System;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Platform;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// The platform-specific interface for <see cref="Avalonia.Media.Geometry"/>.
    /// </summary>
    internal abstract class GeometryImpl(ID2D1Geometry geometry) : IGeometryImpl
    {
        private const float ContourApproximation = 0.0001f;

        /// <inheritdoc/>
        public Rect Bounds => Geometry.GetWidenedBounds(0).ToAvalonia();

        /// <inheritdoc />
        public double ContourLength => Geometry.ComputeLength(null, ContourApproximation);

        public ID2D1Geometry Geometry { get; } = geometry;

        /// <inheritdoc/>
        public Rect GetRenderBounds(IPen pen)
        {
            if (pen == null || Math.Abs(pen.Thickness) < float.Epsilon)
                return Geometry.GetBounds().ToAvalonia();
            var originalBounds = Geometry.GetWidenedBounds((float)pen.Thickness).ToAvalonia();
            switch (pen.LineCap)
            {
                case PenLineCap.Flat:
                    return originalBounds;
                case PenLineCap.Round:
                    return originalBounds.Inflate(pen.Thickness / 2);
                case PenLineCap.Square:
                    return originalBounds.Inflate(pen.Thickness);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IGeometryImpl GetWidenedGeometry(IPen pen)
        {
            var result = Direct2D1Platform.Direct2D1Factory.CreatePathGeometry();

            using (var sink = result.Open())
            {
                Geometry.Widen(
                    (float)pen.Thickness,
                    pen.ToDirect2DStrokeStyle(Direct2D1Platform.Direct2D1Factory),
                    0.25f,
                    sink);
                sink.Close();
            }

            return new StreamGeometryImpl(result);
        }

        /// <inheritdoc/>
        public bool FillContains(Point point)
        {
            return Geometry.FillContainsPoint(point.ToVortice());
        }

        /// <inheritdoc/>
        public IGeometryImpl Intersect(IGeometryImpl geometry)
        {
            var result = Direct2D1Platform.Direct2D1Factory.CreatePathGeometry();
            using (var sink = result.Open())
            {
                Geometry.CombineWithGeometry(((GeometryImpl)geometry).Geometry, CombineMode.Intersect, sink);
                sink.Close();
            }
            return new StreamGeometryImpl(result);
        }

        /// <inheritdoc/>
        public bool StrokeContains(Avalonia.Media.IPen pen, Point point)
        {
            return Geometry.StrokeContainsPoint(point.ToVortice(), (float)(pen?.Thickness ?? 0));
        }

        public ITransformedGeometryImpl WithTransform(Matrix transform)
        {
            return new TransformedGeometryImpl(
                Direct2D1Platform.Direct2D1Factory.CreateTransformedGeometry(
                    GetSourceGeometry(),
                    transform.ToDirect2D()),
                this);
        }

        /// <inheritdoc />
        public bool TryGetPointAtDistance(double distance, out Point point)
        {
            Geometry.ComputePointAtLength((float)distance, ContourApproximation, out var tangentVector);
            point = new Point(tangentVector.X, tangentVector.Y);
            return true;
        }

        /// <inheritdoc />
        public bool TryGetPointAndTangentAtDistance(double distance, out Point point, out Point tangent)
        {
            // Direct2D doesnt have this sadly.
            Logger.TryGet(LogEventLevel.Warning, LogArea.Visual)?.Log(this, "TryGetPointAndTangentAtDistance is not available in Direct2D.");
            point = new Point();
            tangent = new Point();
            return false;
        }

        public bool TryGetSegment(double startDistance, double stopDistance, bool startOnBeginFigure, out IGeometryImpl segmentGeometry)
        {
            // Direct2D doesnt have this too sadly.
            Logger.TryGet(LogEventLevel.Warning, LogArea.Visual)?.Log(this, "TryGetSegment is not available in Direct2D.");

            segmentGeometry = null;
            return false;
        }

        protected virtual ID2D1Geometry GetSourceGeometry() => Geometry;
    }
}
