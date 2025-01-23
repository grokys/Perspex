using System;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A <see cref="Panel"/> with uniform column and row sizes.
    /// </summary>
    public class UniformGrid : Panel
    {
        /// <summary>
        /// Defines the <see cref="Rows"/> property.
        /// </summary>
        public static readonly StyledProperty<int> RowsProperty =
            AvaloniaProperty.Register<UniformGrid, int>(nameof(Rows));

        /// <summary>
        /// Defines the <see cref="Columns"/> property.
        /// </summary>
        public static readonly StyledProperty<int> ColumnsProperty =
            AvaloniaProperty.Register<UniformGrid, int>(nameof(Columns));

        /// <summary>
        /// Defines the <see cref="FirstColumn"/> property.
        /// </summary>
        public static readonly StyledProperty<int> FirstColumnProperty =
            AvaloniaProperty.Register<UniformGrid, int>(nameof(FirstColumn));

        /// <summary>
        /// Defines the <see cref="RowSpacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> RowSpacingProperty =
            AvaloniaProperty.Register<UniformGrid, double>(nameof(RowSpacing), 0);

        /// <summary>
        /// Defines the <see cref="ColumnSpacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ColumnSpacingProperty =
            AvaloniaProperty.Register<UniformGrid, double>(nameof(ColumnSpacing), 0);

        private int _rows;
        private int _columns;

        static UniformGrid()
        {
            AffectsMeasure<UniformGrid>(RowsProperty, ColumnsProperty, FirstColumnProperty);
        }

        /// <summary>
        /// Specifies the row count. If set to 0, row count will be calculated automatically.
        /// </summary>
        public int Rows
        {
            get => GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        /// <summary>
        /// Specifies the column count. If set to 0, column count will be calculated automatically.
        /// </summary>
        public int Columns
        {
            get => GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        /// <summary>
        /// Specifies, for the first row, the column where the items should start.
        /// </summary>
        public int FirstColumn
        {
            get => GetValue(FirstColumnProperty);
            set => SetValue(FirstColumnProperty, value);
        }

        /// <summary>
        /// Specifies the spacing between rows.
        /// </summary>
        public double RowSpacing
        {
            get => GetValue(RowSpacingProperty);
            set => SetValue(RowSpacingProperty, value);
        }

        /// <summary>
        /// Specifies the spacing between columns.
        /// </summary>
        public double ColumnSpacing
        {
            get => GetValue(ColumnSpacingProperty);
            set => SetValue(ColumnSpacingProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            UpdateRowsAndColumns();

            var maxWidth = 0d;
            var maxHeight = 0d;

            var childAvailableSize = new Size(
                (availableSize.Width - (_columns - 1) * ColumnSpacing) / _columns,
                (availableSize.Height - (_rows - 1) * RowSpacing) / _rows);

            foreach (var child in Children)
            {
                child.Measure(childAvailableSize);

                if (child.DesiredSize.Width > maxWidth)
                {
                    maxWidth = child.DesiredSize.Width;
                }

                if (child.DesiredSize.Height > maxHeight)
                {
                    maxHeight = child.DesiredSize.Height;
                }
            }

            var totalWidth = maxWidth * _columns + ColumnSpacing * (_columns - 1);
            var totalHeight = maxHeight * _rows + RowSpacing * (_rows - 1);

            totalWidth = Math.Max(totalWidth, 0);
            totalHeight = Math.Max(totalHeight, 0);

            return new Size(totalWidth, totalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var x = FirstColumn;
            var y = 0;

            var columnSpacing = ColumnSpacing;
            var rowSpacing = RowSpacing;

            var width = (finalSize.Width - (_columns - 1) * columnSpacing) / _columns;
            var height = (finalSize.Height - (_rows - 1) * rowSpacing) / _rows;

            foreach (var child in Children)
            {
                if (!child.IsVisible)
                {
                    continue;
                }

                var rect = new Rect(
                    x * (width + columnSpacing),
                    y * (height + rowSpacing),
                    width,
                    height);

                child.Arrange(rect);

                x++;

                if (x >= _columns)
                {
                    x = 0;
                    y++;
                }
            }

            return finalSize;
        }

        private void UpdateRowsAndColumns()
        {
            _rows = Rows;
            _columns = Columns;

            if (FirstColumn >= _columns)
            {
                SetCurrentValue(FirstColumnProperty, 0);
            }

            var itemCount = FirstColumn;

            foreach (var child in Children)
            {
                if (child.IsVisible)
                {
                    itemCount++;
                }
            }

            if (_rows == 0)
            {
                if (_columns == 0)
                {
                    _rows = _columns = (int)Math.Ceiling(Math.Sqrt(itemCount));
                }
                else
                {
                    _rows = Math.DivRem(itemCount, _columns, out int rem);

                    if (rem != 0)
                    {
                        _rows++;
                    }
                }
            }
            else if (_columns == 0)
            {
                _columns = Math.DivRem(itemCount, _rows, out int rem);

                if (rem != 0)
                {
                    _columns++;
                }
            }
        }
    }
}
