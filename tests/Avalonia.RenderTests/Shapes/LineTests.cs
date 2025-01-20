using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Shapes
#endif
{
    public class LineTests : TestBase
    {
        public LineTests()
            : base(@"Shapes\Line")
        {
        }
        
        [Fact]
        public async Task Line_1px_Stroke()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(200, 200)
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task Line_1px_Stroke_Reversed()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    StartPoint = new Point(200, 0),
                    EndPoint = new Point(0, 200)
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Line_1px_Stroke_Vertical()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    StartPoint = new Point(100, 200),
                    EndPoint = new Point(100, 0)
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Lines_With_DashArray()
        {
            var stackPanel = new StackPanel();

            stackPanel.Children.Add(new Line() { Margin = new Thickness(8), StrokeThickness = 8, StartPoint = new Point(0, 0), EndPoint = new Point(200, 0), Stroke = Brushes.Black, StrokeDashArray = [1] });
            stackPanel.Children.Add(new Line() { Margin = new Thickness(8), StrokeThickness = 8, StartPoint = new Point(0, 0), EndPoint = new Point(200, 0), Stroke = Brushes.Black, StrokeDashArray = [1, 1] });
            stackPanel.Children.Add(new Line() { Margin = new Thickness(8), StrokeThickness = 8, StartPoint = new Point(0, 0), EndPoint = new Point(200, 0), Stroke = Brushes.Black, StrokeDashArray = [1, 6] });
            stackPanel.Children.Add(new Line() { Margin = new Thickness(8), StrokeThickness = 8, StartPoint = new Point(0, 0), EndPoint = new Point(200, 0), Stroke = Brushes.Black, StrokeDashArray = [6, 1] });
            stackPanel.Children.Add(new Line() { Margin = new Thickness(8), StrokeThickness = 8, StartPoint = new Point(0, 0), EndPoint = new Point(200, 0), Stroke = Brushes.Black, StrokeDashArray = [0.25, 1] });
            stackPanel.Children.Add(new Line() { Margin = new Thickness(8), StrokeThickness = 8, StartPoint = new Point(0, 0), EndPoint = new Point(200, 0), Stroke = Brushes.Black, StrokeDashArray = [4, 1, 1, 1, 1, 1] });
            stackPanel.Children.Add(new Line() { Margin = new Thickness(8), StrokeThickness = 8, StartPoint = new Point(0, 0), EndPoint = new Point(200, 0), Stroke = Brushes.Black, StrokeDashArray = [5, 5, 1, 5] });
            stackPanel.Children.Add(new Line() { Margin = new Thickness(8), StrokeThickness = 8, StartPoint = new Point(0, 0), EndPoint = new Point(200, 0), Stroke = Brushes.Black, StrokeDashArray = [1, 2, 4] });
            stackPanel.Children.Add(new Line() { Margin = new Thickness(8), StrokeThickness = 8, StartPoint = new Point(0, 0), EndPoint = new Point(200, 0), Stroke = Brushes.Black, StrokeDashArray = [4, 2, 4] });
            stackPanel.Children.Add(new Line() { Margin = new Thickness(8), StrokeThickness = 8, StartPoint = new Point(0, 0), EndPoint = new Point(200, 0), Stroke = Brushes.Black, StrokeDashArray = [4, 2, 4, 1, 1] });


            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = stackPanel
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
