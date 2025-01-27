using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media;

public class PathGeometryTests
{
    [Fact]
    public void PathGeometry_Triggers_Invalidation_On_Figures_Add()
    {
        var segment = new PolyLineSegment()
        {
            Points = [new Point(1, 1), new Point(2, 2)]
        };

        var figure = new PathFigure()
        {
            Segments = [segment],
            IsClosed = false,
            IsFilled = false,
        };
        
        var target = new PathGeometry();

        var changed = false;

        target.Changed += (_, _) => { changed = true; };
        
        target.Figures?.Add(figure);
        Assert.True(changed);
    }
}
