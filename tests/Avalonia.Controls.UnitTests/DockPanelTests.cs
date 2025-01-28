using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class DockPanelTests
    {
        [Fact]
        public void Should_Dock_Controls_Horizontal_First()
        {
            var target = new DockPanel
            {
                Children =
                {
                    new Border { Width = 500, Height = 50, [DockPanel.DockProperty] = Dock.Top },
                    new Border { Width = 500, Height = 50, [DockPanel.DockProperty] = Dock.Bottom },
                    new Border { Width = 50, Height = 400, [DockPanel.DockProperty] = Dock.Left },
                    new Border { Width = 50, Height = 400, [DockPanel.DockProperty] = Dock.Right },
                    new Border { },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Rect(0, 0, 500, 500), target.Bounds);
            Assert.Equal(new Rect(0, 0, 500, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 450, 500, 50), target.Children[1].Bounds);
            Assert.Equal(new Rect(0, 50, 50, 400), target.Children[2].Bounds);
            Assert.Equal(new Rect(450, 50, 50, 400), target.Children[3].Bounds);
            Assert.Equal(new Rect(50, 50, 400, 400), target.Children[4].Bounds);
        }

        [Fact]
        public void Should_Dock_Controls_Vertical_First()
        {
            var target = new DockPanel
            {
                Children =
                {
                    new Border { Width = 50, Height = 400, [DockPanel.DockProperty] = Dock.Left },
                    new Border { Width = 50, Height = 400, [DockPanel.DockProperty] = Dock.Right },
                    new Border { Width = 500, Height = 50, [DockPanel.DockProperty] = Dock.Top },
                    new Border { Width = 500, Height = 50, [DockPanel.DockProperty] = Dock.Bottom },
                    new Border { },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Rect(0, 0, 600, 400), target.Bounds);
            Assert.Equal(new Rect(0, 0, 50, 400), target.Children[0].Bounds);
            Assert.Equal(new Rect(550, 0, 50, 400), target.Children[1].Bounds);
            Assert.Equal(new Rect(50, 0, 500, 50), target.Children[2].Bounds);
            Assert.Equal(new Rect(50, 350, 500, 50), target.Children[3].Bounds);
            Assert.Equal(new Rect(50, 50, 500, 300), target.Children[4].Bounds);
        }

        [Fact]
        public void Should_Dock_Controls_With_Spacing()
        {
            var target = new DockPanel
            {
                HorizontalSpacing = 10,
                VerticalSpacing = 10,
                Children =
                {
                    new Border { Width = 500, Height = 50, [DockPanel.DockProperty] = Dock.Top },
                    new Border { Width = 500, Height = 50, [DockPanel.DockProperty] = Dock.Bottom },
                    new Border { Width = 50, Height = 400, [DockPanel.DockProperty] = Dock.Left },
                    new Border { Width = 50, Height = 400, [DockPanel.DockProperty] = Dock.Right },
                    new Border { },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Rect(0, 0, 500, 520), target.Bounds);
            Assert.Equal(new Rect(0, 0, 500, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 470, 500, 50), target.Children[1].Bounds);
            Assert.Equal(new Rect(0, 60, 50, 400), target.Children[2].Bounds);
            Assert.Equal(new Rect(450, 60, 50, 400), target.Children[3].Bounds);
            Assert.Equal(new Rect(60, 60, 380, 400), target.Children[4].Bounds);
        }

        [Fact]
        public void Changing_Child_Dock_Invalidates_Measure()
        {
            Border child;
            var target = new DockPanel
            {
                Children =
                {
                    (child = new Border
                    {
                        [DockPanel.DockProperty] = Dock.Left,
                    }),
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));
            Assert.True(target.IsMeasureValid);

            DockPanel.SetDock(child, Dock.Right);

            Assert.False(target.IsMeasureValid);
        }
    }
}
