using System;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Platform;
using Vortice.Direct2D1;
using D2D = Vortice.Direct2D1;
using SweepDirection = Vortice.Direct2D1.SweepDirection;

namespace Avalonia.Direct2D1.Media
{
    internal class StreamGeometryContextImpl : IStreamGeometryContextImpl
    {
        private readonly ID2D1GeometrySink _sink;

        public StreamGeometryContextImpl(ID2D1GeometrySink sink)
        {
            _sink = sink;
        }

        public void ArcTo(
            Point point,
            Size size,
            double rotationAngle,
            bool isLargeArc,
            Avalonia.Media.SweepDirection sweepDirection)
        {
            _sink.AddArc(new D2D.ArcSegment
            {
                Point = point.ToVortice(),
                Size = size.ToSharpDX(),
                RotationAngle = (float)rotationAngle,
                ArcSize = isLargeArc ? ArcSize.Large : ArcSize.Small,
                SweepDirection = (SweepDirection)sweepDirection,
            });
        }

        public void BeginFigure(Point startPoint, bool isFilled)
        {
            _sink.BeginFigure(startPoint.ToVortice(), isFilled ? FigureBegin.Filled : FigureBegin.Hollow);
        }

        public void CubicBezierTo(Point point1, Point point2, Point point3)
        {
            _sink.AddBezier(new D2D.BezierSegment
            {
                Point1 = point1.ToVortice(),
                Point2 = point2.ToVortice(),
                Point3 = point3.ToVortice(),
            });
        }

        public void QuadraticBezierTo(Point control, Point dest)
        {
            _sink.AddQuadraticBezier(new D2D.QuadraticBezierSegment
            {
                Point1 = control.ToVortice(),
                Point2 = dest.ToVortice()
            });
        }

        public void LineTo(Point point)
        {
            _sink.AddLine(point.ToVortice());
        }

        public void EndFigure(bool isClosed)
        {
            _sink.EndFigure(isClosed ? FigureEnd.Closed : FigureEnd.Open);
        }

        public void SetFillRule(FillRule fillRule)
        {
            _sink.SetFillMode(fillRule == FillRule.EvenOdd ? FillMode.Alternate : FillMode.Winding);
        }

        public void Dispose()
        {
            // Put a catch around sink.Close as it may throw if there were an error e.g. parsing a path.
            try
            {
                _sink.Close();
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(
                    this,
                    "GeometrySink.Close exception: {Exception}",
                    ex);
            }

            _sink.Dispose();
        }
    }
}
