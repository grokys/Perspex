using System;
using Avalonia.Layout;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class WrapPanelTests
    {
        [Fact]
        public void Lays_Out_Horizontally_On_Separate_Lines()
        {
            var target = new WrapPanel()
            {
                Width = 100,
                Children =
                {
                    new Border { Height = 50, Width = 100 },
                    new Border { Height = 50, Width = 100 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(100, 100), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 50, 100, 50), target.Children[1].Bounds);
        }

        [Fact]
        public void Lays_Out_Horizontally_On_A_Single_Line()
        {
            var target = new WrapPanel()
            {
                Width = 200,
                Children =
                {
                    new Border { Height = 50, Width = 100 },
                    new Border { Height = 50, Width = 100 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(200, 50), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(100, 0, 100, 50), target.Children[1].Bounds);
        }

        public static TheoryData<Orientation, WrapPanelItemsAlignment> GetItemsAlignmentValues()
        {
            var data = new TheoryData<Orientation, WrapPanelItemsAlignment>();
            foreach (var orientation in Enum.GetValues<Orientation>())
            {
                foreach (var alignment in Enum.GetValues<WrapPanelItemsAlignment>())
                {
                    data.Add(orientation, alignment);
                }
            }
            return data;
        }

        [Theory, MemberData(nameof(GetItemsAlignmentValues))]
        public void Lays_Out_With_Items_Alignment(Orientation orientation, WrapPanelItemsAlignment itemsAlignment)
        {
            var target = new WrapPanel()
            {
                Width = 200,
                Height = 200,
                Orientation = orientation,
                ItemsAlignment = itemsAlignment,
                Children =
                {
                    new Border { Height = 50, Width = 50 },
                    new Border { Height = 50, Width = 50 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(200, 200), target.Bounds.Size);

            var rowBounds = target.Children[0].Bounds.Union(target.Children[1].Bounds);

            Assert.Equal(orientation switch
            {
                Orientation.Horizontal => new(100, 50),
                Orientation.Vertical => new(50, 100),
                _ => throw new NotImplementedException()
            }, rowBounds.Size);

            Assert.Equal((orientation, itemsAlignment) switch
            {
                (_, WrapPanelItemsAlignment.Start) => new(0, 0),
                (Orientation.Horizontal, WrapPanelItemsAlignment.Center) => new(50, 0),
                (Orientation.Vertical, WrapPanelItemsAlignment.Center) => new(0, 50),
                (Orientation.Horizontal, WrapPanelItemsAlignment.End) => new(100, 0),
                (Orientation.Vertical, WrapPanelItemsAlignment.End) => new(0, 100),
                _ => throw new NotImplementedException(),
            }, rowBounds.Position);
        }

        [Fact]
        public void Lays_Out_Vertically_Children_On_A_Single_Line()
        {
            var target = new WrapPanel()
            {
                Orientation = Orientation.Vertical,
                Height = 120,
                Children =
                {
                    new Border { Height = 50, Width = 100 },
                    new Border { Height = 50, Width = 100 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(100, 120), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 50, 100, 50), target.Children[1].Bounds);
        }

        [Fact]
        public void Lays_Out_Vertically_On_Separate_Lines()
        {
            var target = new WrapPanel()
            {
                Orientation = Orientation.Vertical,
                Height = 60,
                Children =
                {
                    new Border { Height = 50, Width = 100 },
                    new Border { Height = 50, Width = 100 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(200, 60), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(100, 0, 100, 50), target.Children[1].Bounds);
        }

        [Fact]
        public void Lays_Out_Horizontally_On_Separate_Lines_With_Spacing()
        {
            var target = new WrapPanel
            {
                Width = 100,
                ItemSpacing = 10,
                LineSpacing = 20,
                Children =
                {
                    new Border { Height = 50, Width = 60 }, // line 0
                    new Border { Height = 50, Width = 30 }, // line 0
                    new Border { Height = 50, Width = 70 }, // line 1
                    new Border { Height = 50, Width = 30 }, // line 2
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(100, 190), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 60, 50), target.Children[0].Bounds);
            Assert.Equal(new Rect(70, 0, 30, 50), target.Children[1].Bounds);
            Assert.Equal(new Rect(0, 70, 70, 50), target.Children[2].Bounds);
            Assert.Equal(new Rect(0, 140, 30, 50), target.Children[3].Bounds);
        }

        [Fact]
        public void Lays_Out_Horizontally_On_Separate_Lines_With_Spacing_Vertical()
        {
            var target = new WrapPanel
            {
                Height = 100,
                Orientation = Orientation.Vertical,
                ItemSpacing = 10,
                LineSpacing = 20,
                Children =
                {
                    new Border { Width = 50, Height = 60 }, // line 0
                    new Border { Width = 50, Height = 30 }, // line 0
                    new Border { Width = 50, Height = 70 }, // line 1
                    new Border { Width = 50, Height = 30 }, // line 2
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(190, 100), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 50, 60), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 70, 50, 30), target.Children[1].Bounds);
            Assert.Equal(new Rect(70, 0, 50, 70), target.Children[2].Bounds);
            Assert.Equal(new Rect(140, 0, 50, 30), target.Children[3].Bounds);
        }

        [Fact]
        public void Applies_ItemWidth_And_ItemHeight_Properties()
        {
            var target = new WrapPanel()
            {
                Orientation = Orientation.Horizontal,
                Width = 50,
                ItemWidth = 20,
                ItemHeight = 15,
                Children =
                {
                    new Border(),
                    new Border(),
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(50, 15), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 20, 15), target.Children[0].Bounds);
            Assert.Equal(new Rect(20, 0, 20, 15), target.Children[1].Bounds);
        }

        [Fact]
        void ItemWidth_Trigger_InvalidateMeasure()
        {
            var target = new WrapPanel();

            target.Measure(new Size(10, 10));

            Assert.True(target.IsMeasureValid);

            target.ItemWidth = 1;

            Assert.False(target.IsMeasureValid);
        }

        [Fact]
        void ItemHeight_Trigger_InvalidateMeasure()
        {
            var target = new WrapPanel();

            target.Measure(new Size(10, 10));

            Assert.True(target.IsMeasureValid);

            target.ItemHeight = 1;

            Assert.False(target.IsMeasureValid);
        }
    }
}
