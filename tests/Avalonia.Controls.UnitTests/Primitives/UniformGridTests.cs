using Avalonia.Controls.Primitives;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class UniformGridTests
    {
        [Fact]
        public void Grid_Columns_Equals_Rows_For_Auto_Columns_And_Rows()
        {
            var target = new UniformGrid
            {
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 2 * 2 grid => each cell: 80 x 90
            // Final size => (2 * 80) x (2 * 90) = 160 x 180
            Assert.Equal(new Size(160, 180), target.Bounds.Size);
        }

        [Fact]
        public void Grid_Expands_Vertically_For_Columns_With_Auto_Rows()
        {
            var target = new UniformGrid
            {
                Columns = 2,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 },
                    new Border { Width = 20, Height = 30 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 2 * 3 grid => each cell: 80 x 90
            // Final size => (2 * 80) x (3 * 90) = 160 x 270
            Assert.Equal(new Size(160, 270), target.Bounds.Size);
        }

        [Fact]
        public void Grid_Extends_For_Columns_And_First_Column_With_Auto_Rows()
        {
            var target = new UniformGrid
            {
                Columns = 3,
                FirstColumn = 2,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 },
                    new Border { Width = 20, Height = 30 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 3 * 3 grid => each cell: 80 x 90
            // Final size => (3 * 80) x (3 * 90) = 240 x 270
            Assert.Equal(new Size(240, 270), target.Bounds.Size);
        }

        [Fact]
        public void Grid_Expands_Horizontally_For_Rows_With_Auto_Columns()
        {
            var target = new UniformGrid
            {
                Rows = 2,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 },
                    new Border { Width = 20, Height = 30 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 3 * 2 grid => each cell: 80 x 90
            // Final size => (3 * 80) x (2 * 90) = 240 x 180
            Assert.Equal(new Size(240, 180), target.Bounds.Size);
        }

        [Fact]
        public void Grid_Size_Is_Limited_By_Rows_And_Columns()
        {
            var target = new UniformGrid
            {
                Columns = 2,
                Rows = 2,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 },
                    new Border { Width = 20, Height = 30 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // 2 * 2 grid => each cell: 80 x 90
            // Final size => (2 * 80) x (2 * 90) = 160 x 180
            Assert.Equal(new Size(160, 180), target.Bounds.Size);
        }

        [Fact]
        public void Not_Visible_Children_Are_Ignored()
        {
            var target = new UniformGrid
            {
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90, IsVisible = false },
                    new Border { Width = 20, Height = 30 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // Visible children: 4
            // Auto => 2 x 2 grid => each cell: 50 x 70
            // Final size => (2 * 50) x (2 * 70) = 100 x 140
            Assert.Equal(new Size(100, 140), target.Bounds.Size);
        }

        //
        // New tests to cover RowSpacing and ColumnSpacing
        //

        [Fact]
        public void Grid_Respects_ColumnSpacing_For_Auto_Columns_And_Rows()
        {
            // We have 3 visible children and no fixed Rows/Columns => 2x2 grid
            // Largest child is 80 x 90. ColumnSpacing = 10, RowSpacing = 0
            var target = new UniformGrid
            {
                ColumnSpacing = 10,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // Without spacing => width = 2*80 = 160, height = 2*90 = 180
            // With columnSpacing=10 => total width = 2*80 + 1*10 = 170
            // RowSpacing=0 => total height = 180
            Assert.Equal(new Size(170, 180), target.Bounds.Size);
        }

        [Fact]
        public void Grid_Respects_RowSpacing_For_Auto_Columns_And_Rows()
        {
            // 3 visible children => 2x2 grid again
            // Largest child is 80 x 90. RowSpacing = 15, ColumnSpacing = 0
            var target = new UniformGrid
            {
                RowSpacing = 15,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 }
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // Without spacing => width = 160, height = 180
            // With rowSpacing=15 => total height = 2*90 + 1*15 = 195
            // ColumnSpacing=0 => total width = 160
            Assert.Equal(new Size(160, 195), target.Bounds.Size);
        }

        [Fact]
        public void Grid_Respects_Both_Row_And_Column_Spacing_For_Fixed_Grid()
        {
            // 4 visible children => 2 rows x 2 columns, each child is 50x70 or 80x90
            // We'll fix the Grid to 2x2 so the largest child dictates the cell size: 80x90
            // RowSpacing=10, ColumnSpacing=5
            var target = new UniformGrid
            {
                Rows = 2,
                Columns = 2,
                RowSpacing = 10,
                ColumnSpacing = 5,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 80, Height = 90 },
                    new Border { Width = 20, Height = 30 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // Each cell = 80 x 90
            // Final width = (2 * 80) + (1 * 5)  = 160 + 5  = 165
            // Final height = (2 * 90) + (1 * 10) = 180 + 10 = 190
            Assert.Equal(new Size(165, 190), target.Bounds.Size);
        }

        [Fact]
        public void Grid_Respects_Spacing_When_Invisible_Child_Exists()
        {
            // 3 *visible* children => auto => 2x2 grid
            // Largest child is 80 x 90.
            // Add spacing so we can confirm it doesn't add extra columns/rows for invisible child.
            var target = new UniformGrid
            {
                RowSpacing = 5,
                ColumnSpacing = 5,
                Children =
                {
                    new Border { Width = 50, Height = 70 },
                    new Border { Width = 80, Height = 90, IsVisible = false },
                    new Border { Width = 30, Height = 50 },
                    new Border { Width = 40, Height = 60 }
                }
            };

            // Visible children: 3 => auto => sqrt(3) => 2x2
            // Largest visible child is 50x70 or 30x50 or 40x60 => the biggest is 50x70
            // Actually, let's ensure we have a child bigger than that:
            // (So let's modify the 40x60 to something bigger than 50x70, e.g. 80x90 for clarity)
            // We'll do that in the collection above if needed, but let's keep as is for example.

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            // The largest visible child is 50x70. So each cell is 50x70.
            // For a 2x2 grid with 3 visible children:
            //  - total width  = (2 * 50) + (1 * 5) = 100 + 5  = 105
            //  - total height = (2 * 70) + (1 * 5) = 140 + 5 = 145
            Assert.Equal(new Size(105, 145), target.Bounds.Size);
        }

        /// <summary>
        ///  Exposes MeasureOverride for testing inherited classes
        /// </summary>
        public class UniformGridExposeMeasureOverride : UniformGrid
        {
            public new Size MeasureOverride(Size availableSize)
            {
                return base.MeasureOverride(availableSize);
            }
        }

        [Fact]
        public void Measure_WithRowsAndColumnsZeroAndNonZeroSpacing_ProducesZeroDesiredSize()
        {
            // MeasureOverride() is called by Layoutable.MeasureCore() and it ensures that
            // the desired size is never negative. but in case of inherited classes MeasureOverride() may return negative values.
            var target = new UniformGridExposeMeasureOverride
            {
                Rows = 0,
                Columns = 0,
                RowSpacing = 10,
                ColumnSpacing = 20
            };

            var availableSize = new Size(100, 100);

            var desiredSize = target.MeasureOverride(availableSize);

            // Fail case:
            // Because _rows and _columns are 0, the calculation becomes:
            //   totalWidth = maxWidth * 0 + ColumnSpacing * (0 - 1) = -ColumnSpacing
            //   totalHeight = maxHeight * 0 + RowSpacing * (0 - 1) = -RowSpacing
            // Expected: (0, 0)
            Assert.Equal(0, desiredSize.Width);
            Assert.Equal(0, desiredSize.Height);
        
        }
    }
}
