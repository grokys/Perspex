// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/) 
// 
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines a flexible grid area that consists of columns and rows.
    /// </summary>
    public class Grid : Panel
    {
        static Grid()
        {
            ShowGridLinesProperty.Changed.AddClassHandler<Grid>(OnShowGridLinesPropertyChanged);
            ColumnSpacingProperty.Changed.AddClassHandler<Control>(OnSpacingPropertyChanged);
            RowSpacingProperty.Changed.AddClassHandler<Control>(OnSpacingPropertyChanged);

            IsSharedSizeScopeProperty.Changed.AddClassHandler<Control>(DefinitionBase.OnIsSharedSizeScopePropertyChanged);
            ColumnProperty.Changed.AddClassHandler<Control>(OnCellAttachedPropertyChanged);
            ColumnSpanProperty.Changed.AddClassHandler<Control>(OnCellAttachedPropertyChanged);
            RowProperty.Changed.AddClassHandler<Control>(OnCellAttachedPropertyChanged);
            RowSpanProperty.Changed.AddClassHandler<Control>(OnCellAttachedPropertyChanged);

            AffectsMeasure<Grid>(ColumnSpacingProperty, RowSpacingProperty);
            AffectsParentMeasure<Grid>(ColumnProperty, ColumnSpanProperty, RowProperty, RowSpanProperty);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Grid()
        {
        }

        /// <summary>
        /// Helper for setting Column property on a Control.
        /// </summary>
        /// <param name="element">Control to set Column property on.</param>
        /// <param name="value">Column property value.</param>
        public static void SetColumn(Control element, int value)
        {
            _ = element ?? throw new ArgumentNullException(nameof(element));
            element.SetValue(ColumnProperty, value);
        }

        /// <summary>
        /// Helper for reading Column property from a Control.
        /// </summary>
        /// <param name="element">Control to read Column property from.</param>
        /// <returns>Column property value.</returns>
        public static int GetColumn(Control element)
        {
            _ = element ?? throw new ArgumentNullException(nameof(element));
            return element.GetValue(ColumnProperty);
        }

        /// <summary>
        /// Helper for setting Row property on a Control.
        /// </summary>
        /// <param name="element">Control to set Row property on.</param>
        /// <param name="value">Row property value.</param>
        public static void SetRow(Control element, int value)
        {
            _ = element ?? throw new ArgumentNullException(nameof(element));
            element.SetValue(RowProperty, value);
        }

        /// <summary>
        /// Helper for reading Row property from a Control.
        /// </summary>
        /// <param name="element">Control to read Row property from.</param>
        /// <returns>Row property value.</returns>
        public static int GetRow(Control element)
        {
            _ = element ?? throw new ArgumentNullException(nameof(element));
            return element.GetValue(RowProperty);
        }

        /// <summary>
        /// Helper for setting ColumnSpan property on a Control.
        /// </summary>
        /// <param name="element">Control to set ColumnSpan property on.</param>
        /// <param name="value">ColumnSpan property value.</param>
        public static void SetColumnSpan(Control element, int value)
        {
            _ = element ?? throw new ArgumentNullException(nameof(element));
            element.SetValue(ColumnSpanProperty, value);
        }

        /// <summary>
        /// Helper for reading ColumnSpan property from a Control.
        /// </summary>
        /// <param name="element">Control to read ColumnSpan property from.</param>
        /// <returns>ColumnSpan property value.</returns>
        public static int GetColumnSpan(Control element)
        {
            _ = element ?? throw new ArgumentNullException(nameof(element));
            return element.GetValue(ColumnSpanProperty);
        }

        /// <summary>
        /// Helper for setting RowSpan property on a Control.
        /// </summary>
        /// <param name="element">Control to set RowSpan property on.</param>
        /// <param name="value">RowSpan property value.</param>
        public static void SetRowSpan(Control element, int value)
        {
            _ = element ?? throw new ArgumentNullException(nameof(element));
            element.SetValue(RowSpanProperty, value);
        }

        /// <summary>
        /// Helper for reading RowSpan property from a Control.
        /// </summary>
        /// <param name="element">Control to read RowSpan property from.</param>
        /// <returns>RowSpan property value.</returns>
        public static int GetRowSpan(Control element)
        {
            _ = element ?? throw new ArgumentNullException(nameof(element));
            return element.GetValue(RowSpanProperty);
        }

        /// <summary>
        /// Helper for setting IsSharedSizeScope property on a Control.
        /// </summary>
        /// <param name="element">Control to set IsSharedSizeScope property on.</param>
        /// <param name="value">IsSharedSizeScope property value.</param>
        public static void SetIsSharedSizeScope(Control element, bool value)
        {
            _ = element ?? throw new ArgumentNullException(nameof(element));
            element.SetValue(IsSharedSizeScopeProperty, value);
        }

        /// <summary>
        /// Helper for reading IsSharedSizeScope property from a Control.
        /// </summary>
        /// <param name="element">Control to read IsSharedSizeScope property from.</param>
        /// <returns>IsSharedSizeScope property value.</returns>
        public static bool GetIsSharedSizeScope(Control element)
        {
            _ = element ?? throw new ArgumentNullException(nameof(element));
            return element.GetValue(IsSharedSizeScopeProperty);
        }

        /// <summary>
        /// ShowGridLines property.
        /// </summary>
        public bool ShowGridLines
        {
            get => GetValue(ShowGridLinesProperty);
            set => SetValue(ShowGridLinesProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the spacing to place between grid rows.
        /// </summary>
        public double RowSpacing
        {
            get => GetValue(RowSpacingProperty);
            set => SetValue(RowSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the spacing to place between grid columns.
        /// </summary>
        public double ColumnSpacing
        {
            get => GetValue(ColumnSpacingProperty);
            set => SetValue(ColumnSpacingProperty, value);
        }

        /// <summary>
        /// Returns a ColumnDefinitions of column definitions.
        /// </summary>
        [MemberNotNull(nameof(_extData))]
        public ColumnDefinitions ColumnDefinitions
        {
            get
            {
                if (_extData == null) { _extData = new ExtendedData(); }
                if (_extData.ColumnDefinitions == null) { _extData.ColumnDefinitions = new ColumnDefinitions() { Parent = this }; }

                return (_extData.ColumnDefinitions);
            }
            set
            {
                if (_extData == null) { _extData = new ExtendedData(); }
                _extData.ColumnDefinitions = value;
                _extData.ColumnDefinitions.Parent = this;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Returns a RowDefinitions of row definitions.
        /// </summary>
        [MemberNotNull(nameof(_extData))]
        public RowDefinitions RowDefinitions
        {
            get
            {
                if (_extData == null) { _extData = new ExtendedData(); }
                if (_extData.RowDefinitions == null) { _extData.RowDefinitions = new RowDefinitions() { Parent = this }; }

                return (_extData.RowDefinitions);
            }
            set
            {
                if (_extData == null) { _extData = new ExtendedData(); }
                _extData.RowDefinitions = value;
                _extData.RowDefinitions.Parent = this;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Content measurement.
        /// </summary>
        /// <param name="constraint">Constraint</param>
        /// <returns>Desired size</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            Size gridDesiredSize;
            var extData = _extData;

            try
            {
                ListenToNotifications = true;
                MeasureOverrideInProgress = true;

                if (extData == null)
                {
                    gridDesiredSize = new Size();
                    var children = Children;

                    for (int i = 0, count = children.Count; i < count; ++i)
                    {
                        var child = children[i];
                        child.Measure(constraint);
                        gridDesiredSize = new Size(Math.Max(gridDesiredSize.Width, child.DesiredSize.Width),
                                                   Math.Max(gridDesiredSize.Height, child.DesiredSize.Height));
                    }
                }
                else
                {
                    {
                        bool sizeToContentU = double.IsPositiveInfinity(constraint.Width);
                        bool sizeToContentV = double.IsPositiveInfinity(constraint.Height);

                        // Clear index information and rounding errors
                        if (RowDefinitionsDirty || ColumnDefinitionsDirty)
                        {
                            if (_definitionIndices != null)
                            {
                                Array.Clear(_definitionIndices, 0, _definitionIndices.Length);
                                _definitionIndices = null;
                            }

                            if (UseLayoutRounding)
                            {
                                if (_roundingErrors != null)
                                {
                                    Array.Clear(_roundingErrors, 0, _roundingErrors.Length);
                                    _roundingErrors = null;
                                }
                            }
                        }

                        ValidateDefinitionsUStructure();
                        ValidateDefinitionsLayout(DefinitionsU, sizeToContentU);

                        ValidateDefinitionsVStructure();
                        ValidateDefinitionsLayout(DefinitionsV, sizeToContentV);

                        CellsStructureDirty |= (SizeToContentU != sizeToContentU) || (SizeToContentV != sizeToContentV);

                        SizeToContentU = sizeToContentU;
                        SizeToContentV = sizeToContentV;
                    }

                    ValidateCells();

                    Debug.Assert(DefinitionsU.Count > 0 && DefinitionsV.Count > 0);

                    //  Grid classifies cells into four groups depending on
                    //  the column / row type a cell belongs to (number corresponds to
                    //  group number):
                    //
                    //                   Px      Auto     Star
                    //               +--------+--------+--------+
                    //               |        |        |        |
                    //            Px |    1   |    1   |    3   |
                    //               |        |        |        |
                    //               +--------+--------+--------+
                    //               |        |        |        |
                    //          Auto |    1   |    1   |    3   |
                    //               |        |        |        |
                    //               +--------+--------+--------+
                    //               |        |        |        |
                    //          Star |    4   |    2   |    4   |
                    //               |        |        |        |
                    //               +--------+--------+--------+
                    //
                    //  The group number indicates the order in which cells are measured.
                    //  Certain order is necessary to be able to dynamically resolve star
                    //  columns / rows sizes which are used as input for measuring of
                    //  the cells belonging to them.
                    //
                    //  However, there are cases when topology of a grid causes cyclical
                    //  size dependencies. For example:
                    //
                    //
                    //                         column width="Auto"      column width="*"
                    //                      +----------------------+----------------------+
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //  row height="Auto"   |                      |      cell 1 2        |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      +----------------------+----------------------+
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //  row height="*"      |       cell 2 1       |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      |                      |                      |
                    //                      +----------------------+----------------------+
                    //
                    //  In order to accurately calculate constraint width for "cell 1 2"
                    //  (which is the remaining of grid's available width and calculated
                    //  value of Auto column), "cell 2 1" needs to be calculated first,
                    //  as it contributes to the Auto column's calculated value.
                    //  At the same time in order to accurately calculate constraint
                    //  height for "cell 2 1", "cell 1 2" needs to be calculated first,
                    //  as it contributes to Auto row height, which is used in the
                    //  computation of Star row resolved height.
                    //
                    //  to "break" this cyclical dependency we are making (arbitrary)
                    //  decision to treat cells like "cell 2 1" as if they appear in Auto
                    //  rows. And then recalculate them one more time when star row
                    //  heights are resolved.
                    //
                    //  (Or more strictly) the code below implement the following logic:
                    //
                    //                       +---------+
                    //                       |  enter  |
                    //                       +---------+
                    //                            |
                    //                            V
                    //                    +----------------+
                    //                    | Measure Group1 |
                    //                    +----------------+
                    //                            |
                    //                            V
                    //                          / - \
                    //                        /       \
                    //                  Y   /    Can    \    N
                    //            +--------|   Resolve   |-----------+
                    //            |         \  StarsV?  /            |
                    //            |           \       /              |
                    //            |             \ - /                |
                    //            V                                  V
                    //    +----------------+                       / - \
                    //    | Resolve StarsV |                     /       \
                    //    +----------------+               Y   /    Can    \    N
                    //            |                      +----|   Resolve   |------+
                    //            V                      |     \  StarsU?  /       |
                    //    +----------------+             |       \       /         |
                    //    | Measure Group2 |             |         \ - /           |
                    //    +----------------+             |                         V
                    //            |                      |                 +-----------------+
                    //            V                      |                 | Measure Group2' |
                    //    +----------------+             |                 +-----------------+
                    //    | Resolve StarsU |             |                         |
                    //    +----------------+             V                         V
                    //            |              +----------------+        +----------------+
                    //            V              | Resolve StarsU |        | Resolve StarsU |
                    //    +----------------+     +----------------+        +----------------+
                    //    | Measure Group3 |             |                         |
                    //    +----------------+             V                         V
                    //            |              +----------------+        +----------------+
                    //            |              | Measure Group3 |        | Measure Group3 |
                    //            |              +----------------+        +----------------+
                    //            |                      |                         |
                    //            |                      V                         V
                    //            |              +----------------+        +----------------+
                    //            |              | Resolve StarsV |        | Resolve StarsV |
                    //            |              +----------------+        +----------------+
                    //            |                      |                         |
                    //            |                      |                         V
                    //            |                      |                +------------------+
                    //            |                      |                | Measure Group2'' |
                    //            |                      |                +------------------+
                    //            |                      |                         |
                    //            +----------------------+-------------------------+
                    //                                   |
                    //                                   V
                    //                           +----------------+
                    //                           | Measure Group4 |
                    //                           +----------------+
                    //                                   |
                    //                                   V
                    //                               +--------+
                    //                               |  exit  |
                    //                               +--------+
                    //
                    //  where:
                    //  *   all [Measure GroupN] - regular children measure process -
                    //      each cell is measured given constraint size as an input
                    //      and each cell's desired size is accumulated on the
                    //      corresponding column / row;
                    //  *   [Measure Group2'] - is when each cell is measured with
                    //      infinite height as a constraint and a cell's desired
                    //      height is ignored;
                    //  *   [Measure Groups''] - is when each cell is measured (second
                    //      time during single Grid.MeasureOverride) regularly but its
                    //      returned width is ignored;
                    //
                    //  This algorithm is believed to be as close to ideal as possible.
                    //  It has the following drawbacks:
                    //  *   cells belonging to Group2 can be called to measure twice;
                    //  *   iff during second measure a cell belonging to Group2 returns
                    //      desired width greater than desired width returned the first
                    //      time, such a cell is going to be clipped, even though it
                    //      appears in Auto column.
                    //

                    MeasureCellsGroup(extData.CellGroup1, constraint, false, false);
                    double combinedRowSpacing = RowSpacing * (RowDefinitions.Count - 1);
                    double combinedColumnSpacing = ColumnSpacing * (ColumnDefinitions.Count - 1);
                    Size innerAvailableSize = new Size(constraint.Width - combinedRowSpacing, constraint.Height - combinedColumnSpacing);
                    {
                        //  after Group1 is measured,  only Group3 may have cells belonging to Auto rows.
                        bool canResolveStarsV = !HasGroup3CellsInAutoRows;

                        if (canResolveStarsV)
                        {
                            if (HasStarCellsV) { ResolveStar(DefinitionsV, innerAvailableSize.Height); }
                            MeasureCellsGroup(extData.CellGroup2, innerAvailableSize, false, false);
                            if (HasStarCellsU) { ResolveStar(DefinitionsU, innerAvailableSize.Width); }
                            MeasureCellsGroup(extData.CellGroup3, innerAvailableSize, false, false);
                        }
                        else
                        {
                            //  if at least one cell exists in Group2, it must be measured before
                            //  StarsU can be resolved.
                            bool canResolveStarsU = extData.CellGroup2 > PrivateCells.Length;
                            if (canResolveStarsU)
                            {
                                if (HasStarCellsU) { ResolveStar(DefinitionsU, innerAvailableSize.Width); }
                                MeasureCellsGroup(extData.CellGroup3, innerAvailableSize, false, false);
                                if (HasStarCellsV) { ResolveStar(DefinitionsV, innerAvailableSize.Height); }
                            }
                            else
                            {
                                // This is a revision to the algorithm employed for the cyclic
                                // dependency case described above. We now repeatedly
                                // measure Group3 and Group2 until their sizes settle. We
                                // also use a count heuristic to break a loop in case of one.

                                bool hasDesiredSizeUChanged = false;
                                int cnt = 0;

                                // Cache Group2MinWidths & Group3MinHeights
                                double[] group2MinSizes = CacheMinSizes(extData.CellGroup2, false);
                                double[] group3MinSizes = CacheMinSizes(extData.CellGroup3, true);

                                MeasureCellsGroup(extData.CellGroup2, innerAvailableSize, false, true);

                                do
                                {
                                    if (hasDesiredSizeUChanged)
                                    {
                                        // Reset cached Group3Heights
                                        ApplyCachedMinSizes(group3MinSizes, true);
                                    }

                                    if (HasStarCellsU) { ResolveStar(DefinitionsU, innerAvailableSize.Width); }
                                    MeasureCellsGroup(extData.CellGroup3, innerAvailableSize, false, false);

                                    // Reset cached Group2Widths
                                    ApplyCachedMinSizes(group2MinSizes, false);

                                    if (HasStarCellsV) { ResolveStar(DefinitionsV, innerAvailableSize.Height); }
                                    MeasureCellsGroup(extData.CellGroup2, innerAvailableSize, cnt == c_layoutLoopMaxCount, false, out hasDesiredSizeUChanged);
                                }
                                while (hasDesiredSizeUChanged && ++cnt <= c_layoutLoopMaxCount);
                            }
                        }
                    }

                    MeasureCellsGroup(extData.CellGroup4, constraint, false, false);

                    gridDesiredSize = new Size(
                            CalculateDesiredSize(DefinitionsU) + ColumnSpacing * (DefinitionsU.Count - 1),
                            CalculateDesiredSize(DefinitionsV) + RowSpacing * (DefinitionsU.Count - 1));
                }
            }
            finally
            {
                MeasureOverrideInProgress = false;
            }

            return (gridDesiredSize);
        }

        /// <summary>
        /// Content arrangement.
        /// </summary>
        /// <param name="arrangeSize">Arrange size</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            try
            {
                ArrangeOverrideInProgress = true;

                if (_extData is null)
                {
                    var children = Children;

                    for (int i = 0, count = children.Count; i < count; ++i)
                    {
                        var child = children[i];
                        child.Arrange(new Rect(arrangeSize));
                    }
                }
                else
                {
                    Debug.Assert(DefinitionsU.Count > 0 && DefinitionsV.Count > 0);
                    double columnSpacing = ColumnSpacing;
                    double rowSpacing = RowSpacing;
                    double combinedRowSpacing = rowSpacing * (RowDefinitions.Count - 1);
                    double combinedColumnSpacing = columnSpacing * (ColumnDefinitions.Count - 1);
                    SetFinalSize(DefinitionsU, arrangeSize.Width - combinedColumnSpacing, true);
                    SetFinalSize(DefinitionsV, arrangeSize.Height - combinedRowSpacing, false);

                    var children = Children;

                    for (int currentCell = 0; currentCell < PrivateCells.Length; ++currentCell)
                    {
                        var cell = children[currentCell];

                        int columnIndex = PrivateCells[currentCell].ColumnIndex;
                        int rowIndex = PrivateCells[currentCell].RowIndex;
                        int columnSpan = PrivateCells[currentCell].ColumnSpan;
                        int rowSpan = PrivateCells[currentCell].RowSpan;

                        Rect cellRect = new Rect(
                            columnIndex == 0 ? 0.0 : DefinitionsU[columnIndex].FinalOffset + (columnSpacing * columnIndex),
                            rowIndex == 0 ? 0.0 : DefinitionsV[rowIndex].FinalOffset + (rowSpacing * rowIndex),
                            GetFinalSizeForRange(DefinitionsU, columnIndex, columnSpan),
                            GetFinalSizeForRange(DefinitionsV, rowIndex, rowSpan));


                        cell.Arrange(cellRect);
                    }

                    //  update render bound on grid lines renderer visual
                    var gridLinesRenderer = EnsureGridLinesRenderer();
                    gridLinesRenderer?.UpdateRenderBounds(arrangeSize);
                }
            }
            finally
            {
                SetValid();
                ArrangeOverrideInProgress = false;
            }
            return (arrangeSize);
        }

        /// <summary>
        /// <see cref="Panel.ChildrenChanged"/>
        /// </summary>
        protected override void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CellsStructureDirty = true;
            base.ChildrenChanged(sender, e);
        }

        /// <summary>
        /// Invalidates grid caches and makes the grid dirty for measure.
        /// </summary>
        internal void Invalidate()
        {
            CellsStructureDirty = true;
            InvalidateMeasure();
        }

        /// <summary>
        /// Returns final width for a column.
        /// </summary>
        /// <remarks>
        /// Used from public ColumnDefinition ActualWidth. Calculates final width using offset data.
        /// </remarks>
        internal double GetFinalColumnDefinitionWidth(int columnIndex)
        {
            double value = 0.0;

            Debug.Assert(_extData != null);

            //  actual value calculations require structure to be up-to-date
            if (!ColumnDefinitionsDirty)
            {
                IReadOnlyList<DefinitionBase> definitions = DefinitionsU;
                value = definitions[(columnIndex + 1) % definitions.Count].FinalOffset;
                if (columnIndex != 0) { value -= definitions[columnIndex].FinalOffset; }
            }
            return (value);
        }

        /// <summary>
        /// Returns final height for a row.
        /// </summary>
        /// <remarks>
        /// Used from public RowDefinition ActualHeight. Calculates final height using offset data.
        /// </remarks>
        internal double GetFinalRowDefinitionHeight(int rowIndex)
        {
            double value = 0.0;

            Debug.Assert(_extData != null);

            //  actual value calculations require structure to be up-to-date
            if (!RowDefinitionsDirty)
            {
                IReadOnlyList<DefinitionBase> definitions = DefinitionsV;
                value = definitions[(rowIndex + 1) % definitions.Count].FinalOffset;
                if (rowIndex != 0) { value -= definitions[rowIndex].FinalOffset; }
            }
            return (value);
        }

        /// <summary>
        /// Convenience accessor to MeasureOverrideInProgress bit flag.
        /// </summary>
        internal bool MeasureOverrideInProgress
        {
            get => CheckFlags(Flags.MeasureOverrideInProgress);
            set => SetFlags(value, Flags.MeasureOverrideInProgress);
        }

        /// <summary>
        /// Convenience accessor to ArrangeOverrideInProgress bit flag.
        /// </summary>
        internal bool ArrangeOverrideInProgress
        {
            get => CheckFlags(Flags.ArrangeOverrideInProgress);
            set => SetFlags(value, Flags.ArrangeOverrideInProgress);
        }

        /// <summary>
        /// Convenience accessor to ValidDefinitionsUStructure bit flag.
        /// </summary> 
        [MemberNotNull(nameof(_extData))]
        internal bool ColumnDefinitionsDirty
        {
            get => ColumnDefinitions.IsDirty;
            set => ColumnDefinitions.IsDirty = value;
        }

        /// <summary>
        /// Convenience accessor to ValidDefinitionsVStructure bit flag.
        /// </summary>
        [MemberNotNull(nameof(_extData))]
        internal bool RowDefinitionsDirty
        {
            get => RowDefinitions.IsDirty;
            set => RowDefinitions.IsDirty = value;
        }

        /// <summary>
        /// Lays out cells according to rows and columns, and creates lookup grids.
        /// </summary>
        private void ValidateCells()
        {
            if (CellsStructureDirty)
            {
                ValidateCellsCore();
                CellsStructureDirty = false;
            }
        }

        /// <summary>
        /// ValidateCellsCore
        /// </summary>
        private void ValidateCellsCore()
        {
            Debug.Assert(_extData is not null);

            var children = Children;
            var extData = _extData!;

            extData.CellCachesCollection = new CellCache[children.Count];
            extData.CellGroup1 = int.MaxValue;
            extData.CellGroup2 = int.MaxValue;
            extData.CellGroup3 = int.MaxValue;
            extData.CellGroup4 = int.MaxValue;

            bool hasStarCellsU = false;
            bool hasStarCellsV = false;
            bool hasGroup3CellsInAutoRows = false;

            for (int i = PrivateCells.Length - 1; i >= 0; --i)
            {
                var child = children[i];

                CellCache cell = new CellCache();


                //  Read indices from the corresponding properties:
                //      clamp to value < number_of_columns
                //      column >= 0 is guaranteed by property value validation callback
                cell.ColumnIndex = Math.Min(GetColumn(child), DefinitionsU.Count - 1);
                //      clamp to value < number_of_rows
                //      row >= 0 is guaranteed by property value validation callback
                cell.RowIndex = Math.Min(GetRow(child), DefinitionsV.Count - 1);

                //  Read span properties:
                //      clamp to not exceed beyond right side of the grid
                //      column_span > 0 is guaranteed by property value validation callback
                cell.ColumnSpan = Math.Min(GetColumnSpan(child), DefinitionsU.Count - cell.ColumnIndex);

                //      clamp to not exceed beyond bottom side of the grid
                //      row_span > 0 is guaranteed by property value validation callback
                cell.RowSpan = Math.Min(GetRowSpan(child), DefinitionsV.Count - cell.RowIndex);

                Debug.Assert(0 <= cell.ColumnIndex && cell.ColumnIndex < DefinitionsU.Count);
                Debug.Assert(0 <= cell.RowIndex && cell.RowIndex < DefinitionsV.Count);

                //  Calculate and cache length types for the child.

                cell.SizeTypeU = GetLengthTypeForRange(DefinitionsU, cell.ColumnIndex, cell.ColumnSpan);
                cell.SizeTypeV = GetLengthTypeForRange(DefinitionsV, cell.RowIndex, cell.RowSpan);

                hasStarCellsU |= cell.IsStarU;
                hasStarCellsV |= cell.IsStarV;

                //  Distribute cells into four groups.

                if (!cell.IsStarV)
                {
                    if (!cell.IsStarU)
                    {
                        cell.Next = extData.CellGroup1;
                        extData.CellGroup1 = i;
                    }
                    else
                    {
                        cell.Next = extData.CellGroup3;
                        extData.CellGroup3 = i;

                        //  Remember if this cell belongs to auto row
                        hasGroup3CellsInAutoRows |= cell.IsAutoV;
                    }
                }
                else
                {
                    if (cell.IsAutoU
                        //  Note below: if spans through Star column it is NOT Auto
                        && !cell.IsStarU)
                    {
                        cell.Next = extData.CellGroup2;
                        extData.CellGroup2 = i;
                    }
                    else
                    {
                        cell.Next = extData.CellGroup4;
                        extData.CellGroup4 = i;
                    }
                }

                PrivateCells[i] = cell;
            }

            HasStarCellsU = hasStarCellsU;
            HasStarCellsV = hasStarCellsV;
            HasGroup3CellsInAutoRows = hasGroup3CellsInAutoRows;
        }

        /// <summary>
        /// Initializes DefinitionsU member either to user supplied ColumnDefinitions collection
        /// or to a default single element collection. DefinitionsU gets trimmed to size.
        /// </summary>
        /// <remarks>
        /// This is one of two methods, where ColumnDefinitions and DefinitionsU are directly accessed.
        /// All the rest measure / arrange / render code must use DefinitionsU.
        /// </remarks>
        private void ValidateDefinitionsUStructure()
        {
            if (ColumnDefinitionsDirty)
            {
                var extData = _extData;

                if (extData.ColumnDefinitions == null)
                {
                    if (extData.DefinitionsU == null)
                    {
                        extData.DefinitionsU = new DefinitionBase[] { new ColumnDefinition() { Parent = this } };
                    }
                }
                else
                {
                    if (extData.ColumnDefinitions.Count == 0)
                    {
                        //  if column definitions collection is empty
                        //  mockup array with one column
                        extData.DefinitionsU = new DefinitionBase[] { new ColumnDefinition() { Parent = this } };
                    }
                    else
                    {
                        extData.DefinitionsU = extData.ColumnDefinitions;
                    }
                }

                ColumnDefinitionsDirty = false;
            }

            Debug.Assert(_extData is { DefinitionsU.Count: > 0 });
        }

        /// <summary>
        /// Initializes DefinitionsV member either to user supplied RowDefinitions collection
        /// or to a default single element collection. DefinitionsV gets trimmed to size.
        /// </summary>
        /// <remarks>
        /// This is one of two methods, where RowDefinitions and DefinitionsV are directly accessed.
        /// All the rest measure / arrange / render code must use DefinitionsV.
        /// </remarks>
        private void ValidateDefinitionsVStructure()
        {
            if (RowDefinitionsDirty)
            {
                var extData = _extData;

                if (extData.RowDefinitions == null)
                {
                    if (extData.DefinitionsV == null)
                    {
                        extData.DefinitionsV = new DefinitionBase[] { new RowDefinition() { Parent = this } };
                    }
                }
                else
                {
                    if (extData.RowDefinitions.Count == 0)
                    {
                        //  if row definitions collection is empty
                        //  mockup array with one row
                        extData.DefinitionsV = new DefinitionBase[] { new RowDefinition() { Parent = this } };
                    }
                    else
                    {
                        extData.DefinitionsV = extData.RowDefinitions;
                    }
                }

                RowDefinitionsDirty = false;
            }

            Debug.Assert(_extData is { DefinitionsV.Count: > 0 });
        }

        /// <summary>
        /// Validates layout time size type information on given array of definitions.
        /// Sets MinSize and MeasureSizes.
        /// </summary>
        /// <param name="definitions">Array of definitions to update.</param>
        /// <param name="treatStarAsAuto">if "true" then star definitions are treated as Auto.</param>
        private void ValidateDefinitionsLayout(
            IReadOnlyList<DefinitionBase> definitions,
            bool treatStarAsAuto)
        {
            for (int i = 0; i < definitions.Count; ++i)
            {
                definitions[i].OnBeforeLayout(this);

                double userMinSize = definitions[i].UserMinSize;
                double userMaxSize = definitions[i].UserMaxSize;
                double userSize = 0;

                switch (definitions[i].UserSize.GridUnitType)
                {
                    case (GridUnitType.Pixel):
                        definitions[i].SizeType = LayoutTimeSizeType.Pixel;
                        userSize = definitions[i].UserSize.Value;
                        // this was brought with NewLayout and defeats squishy behavior
                        userMinSize = Math.Max(userMinSize, Math.Min(userSize, userMaxSize));
                        break;
                    case (GridUnitType.Auto):
                        definitions[i].SizeType = LayoutTimeSizeType.Auto;
                        userSize = double.PositiveInfinity;
                        break;
                    case (GridUnitType.Star):
                        if (treatStarAsAuto)
                        {
                            definitions[i].SizeType = LayoutTimeSizeType.Auto;
                            userSize = double.PositiveInfinity;
                        }
                        else
                        {
                            definitions[i].SizeType = LayoutTimeSizeType.Star;
                            userSize = double.PositiveInfinity;
                        }
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }

                definitions[i].UpdateMinSize(userMinSize);
                definitions[i].MeasureSize = Math.Max(userMinSize, Math.Min(userSize, userMaxSize));
            }
        }

        private double[] CacheMinSizes(int cellsHead, bool isRows)
        {
            double[] minSizes = isRows ? new double[DefinitionsV.Count] : new double[DefinitionsU.Count];

            for (int j = 0; j < minSizes.Length; j++)
            {
                minSizes[j] = -1;
            }

            int i = cellsHead;
            do
            {
                if (isRows)
                {
                    minSizes[PrivateCells[i].RowIndex] = DefinitionsV[PrivateCells[i].RowIndex].MinSize;
                }
                else
                {
                    minSizes[PrivateCells[i].ColumnIndex] = DefinitionsU[PrivateCells[i].ColumnIndex].MinSize;
                }

                i = PrivateCells[i].Next;
            } while (i < PrivateCells.Length);

            return minSizes;
        }

        private void ApplyCachedMinSizes(double[] minSizes, bool isRows)
        {
            for (int i = 0; i < minSizes.Length; i++)
            {
                if (MathUtilities.GreaterThanOrClose(minSizes[i], 0))
                {
                    if (isRows)
                    {
                        DefinitionsV[i].SetMinSize(minSizes[i]);
                    }
                    else
                    {
                        DefinitionsU[i].SetMinSize(minSizes[i]);
                    }
                }
            }
        }

        private void MeasureCellsGroup(
            int cellsHead,
            Size referenceSize,
            bool ignoreDesiredSizeU,
            bool forceInfinityV)
        {
            MeasureCellsGroup(cellsHead, referenceSize, ignoreDesiredSizeU, forceInfinityV, out _);
        }

        /// <summary>
        /// Measures one group of cells.
        /// </summary>
        /// <param name="cellsHead">Head index of the cells chain.</param>
        /// <param name="referenceSize">Reference size for spanned cells
        /// calculations.</param>
        /// <param name="ignoreDesiredSizeU">When "true" cells' desired
        /// width is not registered in columns.</param>
        /// <param name="forceInfinityV">Passed through to MeasureCell.
        /// When "true" cells' desired height is not registered in rows.</param>
        /// <param name="hasDesiredSizeUChanged">When the method exits, indicates whether the desired size has changed.</param>
        private void MeasureCellsGroup(
            int cellsHead,
            Size referenceSize,
            bool ignoreDesiredSizeU,
            bool forceInfinityV,
            out bool hasDesiredSizeUChanged)
        {
            hasDesiredSizeUChanged = false;

            if (cellsHead >= PrivateCells.Length)
            {
                return;
            }

            var children = Children;
            Hashtable? spanStore = null;
            bool ignoreDesiredSizeV = forceInfinityV;

            int i = cellsHead;
            do
            {
                double oldWidth = children[i].DesiredSize.Width;

                MeasureCell(i, forceInfinityV);

                hasDesiredSizeUChanged |= !MathUtilities.AreClose(oldWidth, children[i].DesiredSize.Width);

                if (!ignoreDesiredSizeU)
                {
                    if (PrivateCells[i].ColumnSpan == 1)
                    {
                        DefinitionsU[PrivateCells[i].ColumnIndex].UpdateMinSize(Math.Min(children[i].DesiredSize.Width, DefinitionsU[PrivateCells[i].ColumnIndex].UserMaxSize));
                    }
                    else
                    {
                        RegisterSpan(
                            ref spanStore,
                            PrivateCells[i].ColumnIndex,
                            PrivateCells[i].ColumnSpan,
                            true,
                            children[i].DesiredSize.Width);
                    }
                }

                if (!ignoreDesiredSizeV)
                {
                    if (PrivateCells[i].RowSpan == 1)
                    {
                        DefinitionsV[PrivateCells[i].RowIndex].UpdateMinSize(Math.Min(children[i].DesiredSize.Height, DefinitionsV[PrivateCells[i].RowIndex].UserMaxSize));
                    }
                    else
                    {
                        RegisterSpan(
                            ref spanStore,
                            PrivateCells[i].RowIndex,
                            PrivateCells[i].RowSpan,
                            false,
                            children[i].DesiredSize.Height);
                    }
                }

                i = PrivateCells[i].Next;
            } while (i < PrivateCells.Length);

            if (spanStore != null)
            {
                foreach (DictionaryEntry e in spanStore)
                {
                    SpanKey key = (SpanKey)e.Key;
                    double requestedSize = (double)e.Value!;

                    EnsureMinSizeInDefinitionRange(
                        key.U ? DefinitionsU : DefinitionsV,
                        key.Start,
                        key.Count,
                        requestedSize,
                        key.U ? referenceSize.Width : referenceSize.Height);
                }
            }
        }

        /// <summary>
        /// Helper method to register a span information for delayed processing.
        /// </summary>
        /// <param name="store">Reference to a hashtable object used as storage.</param>
        /// <param name="start">Span starting index.</param>
        /// <param name="count">Span count.</param>
        /// <param name="u"><c>true</c> if this is a column span. <c>false</c> if this is a row span.</param>
        /// <param name="value">Value to store. If an entry already exists the biggest value is stored.</param>
        private static void RegisterSpan(
            ref Hashtable? store,
            int start,
            int count,
            bool u,
            double value)
        {
            if (store == null)
            {
                store = new Hashtable();
            }

            SpanKey key = new SpanKey(start, count, u);
            object? o = store[key];

            if (o == null
                || value > (double)o)
            {
                store[key] = value;
            }
        }

        /// <summary>
        /// Takes care of measuring a single cell.
        /// </summary>
        /// <param name="cell">Index of the cell to measure.</param>
        /// <param name="forceInfinityV">If "true" then cell is always
        /// calculated to infinite height.</param>
        private void MeasureCell(
            int cell,
            bool forceInfinityV)
        {
            double cellMeasureWidth;
            double cellMeasureHeight;

            if (PrivateCells[cell].IsAutoU
                && !PrivateCells[cell].IsStarU)
            {
                //  if cell belongs to at least one Auto column and not a single Star column
                //  then it should be calculated "to content", thus it is possible to "shortcut"
                //  calculations and simply assign PositiveInfinity here.
                cellMeasureWidth = double.PositiveInfinity;
            }
            else
            {
                //  otherwise...
                cellMeasureWidth = GetMeasureSizeForRange(
                                        DefinitionsU,
                                        PrivateCells[cell].ColumnIndex,
                                        PrivateCells[cell].ColumnSpan);
            }

            if (forceInfinityV)
            {
                cellMeasureHeight = double.PositiveInfinity;
            }
            else if (PrivateCells[cell].IsAutoV
                    && !PrivateCells[cell].IsStarV)
            {
                //  if cell belongs to at least one Auto row and not a single Star row
                //  then it should be calculated "to content", thus it is possible to "shortcut"
                //  calculations and simply assign PositiveInfinity here.
                cellMeasureHeight = double.PositiveInfinity;
            }
            else
            {
                cellMeasureHeight = GetMeasureSizeForRange(
                                        DefinitionsV,
                                        PrivateCells[cell].RowIndex,
                                        PrivateCells[cell].RowSpan);
            }


            var child = Children[cell];
            Size childConstraint = new Size(cellMeasureWidth, cellMeasureHeight);
            child.Measure(childConstraint);
        }

        /// <summary>
        /// Calculates one dimensional measure size for given definitions' range.
        /// </summary>
        /// <param name="definitions">Source array of definitions to read values from.</param>
        /// <param name="start">Starting index of the range.</param>
        /// <param name="count">Number of definitions included in the range.</param>
        /// <returns>Calculated measure size.</returns>
        /// <remarks>
        /// For "Auto" definitions MinWidth is used in place of PreferredSize.
        /// </remarks>
        private static double GetMeasureSizeForRange(
            IReadOnlyList<DefinitionBase> definitions,
            int start,
            int count)
        {
            Debug.Assert(0 < count && 0 <= start && (start + count) <= definitions.Count);

            double measureSize = 0;
            int i = start + count - 1;

            do
            {
                measureSize += (definitions[i].SizeType == LayoutTimeSizeType.Auto)
                    ? definitions[i].MinSize
                    : definitions[i].MeasureSize;
            } while (--i >= start);

            return (measureSize);
        }

        /// <summary>
        /// Accumulates length type information for given definition's range.
        /// </summary>
        /// <param name="definitions">Source array of definitions to read values from.</param>
        /// <param name="start">Starting index of the range.</param>
        /// <param name="count">Number of definitions included in the range.</param>
        /// <returns>Length type for given range.</returns>
        private static LayoutTimeSizeType GetLengthTypeForRange(
            IReadOnlyList<DefinitionBase> definitions,
            int start,
            int count)
        {
            Debug.Assert(0 < count && 0 <= start && (start + count) <= definitions.Count);

            LayoutTimeSizeType lengthType = LayoutTimeSizeType.None;
            int i = start + count - 1;

            do
            {
                lengthType |= definitions[i].SizeType;
            } while (--i >= start);

            return (lengthType);
        }

        /// <summary>
        /// Distributes min size back to definition array's range.
        /// </summary>
        /// <param name="start">Start of the range.</param>
        /// <param name="count">Number of items in the range.</param>
        /// <param name="requestedSize">Minimum size that should "fit" into the definitions range.</param>
        /// <param name="definitions">Definition array receiving distribution.</param>
        /// <param name="percentReferenceSize">Size used to resolve percentages.</param>
        private void EnsureMinSizeInDefinitionRange(
            IReadOnlyList<DefinitionBase> definitions,
            int start,
            int count,
            double requestedSize,
            double percentReferenceSize)
        {
            Debug.Assert(1 < count && 0 <= start && (start + count) <= definitions.Count);

            //  avoid processing when asked to distribute "0"
            if (!MathUtilities.IsZero(requestedSize))
            {
                DefinitionBase?[] tempDefinitions = TempDefinitions; //  temp array used to remember definitions for sorting
                int end = start + count;
                int autoDefinitionsCount = 0;
                double rangeMinSize = 0;
                double rangePreferredSize = 0;
                double rangeMaxSize = 0;
                double maxMaxSize = 0;                              //  maximum of maximum sizes

                //  first accumulate the necessary information:
                //  a) sum up the sizes in the range;
                //  b) count the number of auto definitions in the range;
                //  c) initialize temp array
                //  d) cache the maximum size into SizeCache
                //  e) accumulate max of max sizes
                for (int i = start; i < end; ++i)
                {
                    double minSize = definitions[i].MinSize;
                    double preferredSize = definitions[i].PreferredSize;
                    double maxSize = Math.Max(definitions[i].UserMaxSize, minSize);

                    rangeMinSize += minSize;
                    rangePreferredSize += preferredSize;
                    rangeMaxSize += maxSize;

                    definitions[i].SizeCache = maxSize;

                    //  sanity check: no matter what, but min size must always be the smaller;
                    //  max size must be the biggest; and preferred should be in between
                    Debug.Assert(minSize <= preferredSize
                                && preferredSize <= maxSize
                                && rangeMinSize <= rangePreferredSize
                                && rangePreferredSize <= rangeMaxSize);

                    if (maxMaxSize < maxSize) maxMaxSize = maxSize;
                    if (definitions[i].UserSize.IsAuto) autoDefinitionsCount++;
                    tempDefinitions[i - start] = definitions[i];
                }

                //  avoid processing if the range already big enough
                if (requestedSize > rangeMinSize)
                {
                    if (requestedSize <= rangePreferredSize)
                    {
                        //
                        //  requestedSize fits into preferred size of the range.
                        //  distribute according to the following logic:
                        //  * do not distribute into auto definitions - they should continue to stay "tight";
                        //  * for all non-auto definitions distribute to equi-size min sizes, without exceeding preferred size.
                        //
                        //  in order to achieve that, definitions are sorted in a way that all auto definitions
                        //  are first, then definitions follow ascending order with PreferredSize as the key of sorting.
                        //
                        double sizeToDistribute;
                        int i;

                        Array.Sort(tempDefinitions, 0, count, s_spanPreferredDistributionOrderComparer);
                        for (i = 0, sizeToDistribute = requestedSize; i < autoDefinitionsCount; ++i)
                        {
                            var tempDefinition = tempDefinitions[i]!;

                            //  sanity check: only auto definitions allowed in this loop
                            Debug.Assert(tempDefinition.UserSize.IsAuto);

                            //  adjust sizeToDistribute value by subtracting auto definition min size
                            sizeToDistribute -= (tempDefinition.MinSize);
                        }

                        for (; i < count; ++i)
                        {
                            var tempDefinition = tempDefinitions[i]!;

                            //  sanity check: no auto definitions allowed in this loop
                            Debug.Assert(!tempDefinition.UserSize.IsAuto);

                            double newMinSize = Math.Min(sizeToDistribute / (count - i), tempDefinition.PreferredSize);
                            if (newMinSize > tempDefinition.MinSize) { tempDefinition.UpdateMinSize(newMinSize); }
                            sizeToDistribute -= newMinSize;
                        }

                        //  sanity check: requested size must all be distributed
                        Debug.Assert(MathUtilities.IsZero(sizeToDistribute));
                    }
                    else if (requestedSize <= rangeMaxSize)
                    {
                        //
                        //  requestedSize bigger than preferred size, but fit into max size of the range.
                        //  distribute according to the following logic:
                        //  * do not distribute into auto definitions, if possible - they should continue to stay "tight";
                        //  * for all non-auto definitions distribute to euqi-size min sizes, without exceeding max size.
                        //
                        //  in order to achieve that, definitions are sorted in a way that all non-auto definitions
                        //  are last, then definitions follow ascending order with MaxSize as the key of sorting.
                        //
                        double sizeToDistribute;
                        int i;

                        Array.Sort(tempDefinitions, 0, count, s_spanMaxDistributionOrderComparer);
                        for (i = 0, sizeToDistribute = requestedSize - rangePreferredSize; i < count - autoDefinitionsCount; ++i)
                        {
                            var tempDefinition = tempDefinitions[i]!;

                            //  sanity check: no auto definitions allowed in this loop
                            Debug.Assert(!tempDefinition.UserSize.IsAuto);

                            double preferredSize = tempDefinition.PreferredSize;
                            double newMinSize = preferredSize + sizeToDistribute / (count - autoDefinitionsCount - i);
                            tempDefinition.UpdateMinSize(Math.Min(newMinSize, tempDefinition.SizeCache));
                            sizeToDistribute -= (tempDefinition.MinSize - preferredSize);
                        }

                        for (; i < count; ++i)
                        {
                            var tempDefinition = tempDefinitions[i]!;

                            //  sanity check: only auto definitions allowed in this loop
                            Debug.Assert(tempDefinition.UserSize.IsAuto);

                            double preferredSize = tempDefinition.MinSize;
                            double newMinSize = preferredSize + sizeToDistribute / (count - i);
                            tempDefinition.UpdateMinSize(Math.Min(newMinSize, tempDefinition.SizeCache));
                            sizeToDistribute -= (tempDefinition.MinSize - preferredSize);
                        }

                        //  sanity check: requested size must all be distributed
                        Debug.Assert(MathUtilities.IsZero(sizeToDistribute));
                    }
                    else
                    {
                        //
                        //  requestedSize bigger than max size of the range.
                        //  distribute according to the following logic:
                        //  * for all definitions distribute to equi-size min sizes.
                        //
                        double equalSize = requestedSize / count;

                        if (equalSize < maxMaxSize
                            && !MathUtilities.AreClose(equalSize, maxMaxSize))
                        {
                            //  equi-size is less than maximum of maxSizes.
                            //  in this case distribute so that smaller definitions grow faster than
                            //  bigger ones.
                            double totalRemainingSize = maxMaxSize * count - rangeMaxSize;
                            double sizeToDistribute = requestedSize - rangeMaxSize;

                            //  sanity check: totalRemainingSize and sizeToDistribute must be real positive numbers
                            Debug.Assert(!double.IsInfinity(totalRemainingSize)
                                        && !double.IsNaN(totalRemainingSize)
                                        && totalRemainingSize > 0
                                        && !double.IsInfinity(sizeToDistribute)
                                        && !double.IsNaN(sizeToDistribute)
                                        && sizeToDistribute > 0);

                            for (int i = 0; i < count; ++i)
                            {
                                var tempDefinition = tempDefinitions[i]!;

                                double deltaSize = (maxMaxSize - tempDefinition.SizeCache) * sizeToDistribute / totalRemainingSize;
                                tempDefinition.UpdateMinSize(tempDefinition.SizeCache + deltaSize);
                            }
                        }
                        else
                        {
                            //
                            //  equi-size is greater or equal to maximum of max sizes.
                            //  all definitions receive equalSize as their mim sizes.
                            //
                            for (int i = 0; i < count; ++i)
                            {
                                tempDefinitions[i]!.UpdateMinSize(equalSize);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resolves Star's for given array of definitions.
        /// </summary>
        /// <param name="definitions">Array of definitions to resolve stars.</param>
        /// <param name="availableSize">All available size.</param>
        /// <remarks>
        /// Must initialize LayoutSize for all Star entries in given array of definitions.
        /// </remarks>
        private void ResolveStar(
            IReadOnlyList<DefinitionBase> definitions,
            double availableSize)
        {
            ResolveStarMaxDiscrepancy(definitions, availableSize);
        }

        // New implementation as of 4.7.  Several improvements:
        // 1. Allocate to *-defs hitting their min or max constraints, before allocating
        //      to other *-defs.  A def that hits its min uses more space than its
        //      proportional share, reducing the space available to everyone else.
        //      The legacy algorithm deducted this space only from defs processed
        //      after the min;  the new algorithm deducts it proportionally from all
        //      defs.   This avoids the "*-defs exceed available space" problem,
        //      and other related problems where *-defs don't receive proportional
        //      allocations even though no constraints are preventing it.
        // 2. When multiple defs hit min or max, resolve the one with maximum
        //      discrepancy (defined below).   This avoids discontinuities - small
        //      change in available space resulting in large change to one def's allocation.
        // 3. Correct handling of large *-values, including Infinity.
        private void ResolveStarMaxDiscrepancy(
            IReadOnlyList<DefinitionBase> definitions,
            double availableSize)
        {
            int defCount = definitions.Count;
            DefinitionBase?[] tempDefinitions = TempDefinitions;
            int minCount = 0, maxCount = 0;
            double takenSize = 0;
            double totalStarWeight = 0.0;
            int starCount = 0;      // number of unresolved *-definitions
            double scale = 1.0;     // scale factor applied to each *-weight;  negative means "Infinity is present"

            // Phase 1.  Determine the maximum *-weight and prepare to adjust *-weights
            double maxStar = 0.0;
            for (int i = 0; i < defCount; ++i)
            {
                DefinitionBase def = definitions[i];

                if (def.SizeType == LayoutTimeSizeType.Star)
                {
                    ++starCount;
                    def.MeasureSize = 1.0;  // meaning "not yet resolved in phase 3"
                    if (def.UserSize.Value > maxStar)
                    {
                        maxStar = def.UserSize.Value;
                    }
                }
            }

            if (Double.IsPositiveInfinity(maxStar))
            {
                // negative scale means one or more of the weights was Infinity
                scale = -1.0;
            }
            else if (starCount > 0)
            {
                // if maxStar * starCount > Double.Max, summing all the weights could cause
                // floating-point overflow.  To avoid that, scale the weights by a factor to keep
                // the sum within limits.  Choose a power of 2, to preserve precision.
                double power = Math.Floor(Math.Log(Double.MaxValue / maxStar / starCount, 2.0));
                if (power < 0.0)
                {
                    scale = Math.Pow(2.0, power - 4.0); // -4 is just for paranoia
                }
            }

            // normally Phases 2 and 3 execute only once.  But certain unusual combinations of weights
            // and constraints can defeat the algorithm, in which case we repeat Phases 2 and 3.
            // More explanation below...
            for (bool runPhase2and3 = true; runPhase2and3;)
            {
                // Phase 2.   Compute total *-weight W and available space S.
                // For *-items that have Min or Max constraints, compute the ratios used to decide
                // whether proportional space is too big or too small and add the item to the
                // corresponding list.  (The "min" list is in the first half of tempDefinitions,
                // the "max" list in the second half.  TempDefinitions has capacity at least
                // 2*defCount, so there's room for both lists.)
                totalStarWeight = 0.0;
                takenSize = 0.0;
                minCount = maxCount = 0;

                for (int i = 0; i < defCount; ++i)
                {
                    DefinitionBase def = definitions[i];

                    switch (def.SizeType)
                    {
                        case (LayoutTimeSizeType.Auto):
                            takenSize += definitions[i].MinSize;
                            break;
                        case (LayoutTimeSizeType.Pixel):
                            takenSize += def.MeasureSize;
                            break;
                        case (LayoutTimeSizeType.Star):
                            if (def.MeasureSize < 0.0)
                            {
                                takenSize += -def.MeasureSize;  // already resolved
                            }
                            else
                            {
                                double starWeight = StarWeight(def, scale);
                                totalStarWeight += starWeight;

                                if (def.MinSize > 0.0)
                                {
                                    // store ratio w/min in MeasureSize (for now)
                                    tempDefinitions[minCount++] = def;
                                    def.MeasureSize = starWeight / def.MinSize;
                                }

                                double effectiveMaxSize = Math.Max(def.MinSize, def.UserMaxSize);
                                if (!Double.IsPositiveInfinity(effectiveMaxSize))
                                {
                                    // store ratio w/max in SizeCache (for now)
                                    tempDefinitions[defCount + maxCount++] = def;
                                    def.SizeCache = starWeight / effectiveMaxSize;
                                }
                            }
                            break;
                    }
                }

                // Phase 3.  Resolve *-items whose proportional sizes are too big or too small.
                int minCountPhase2 = minCount, maxCountPhase2 = maxCount;
                double takenStarWeight = 0.0;
                double remainingAvailableSize = availableSize - takenSize;
                double remainingStarWeight = totalStarWeight - takenStarWeight;
                Array.Sort(tempDefinitions, 0, minCount, s_minRatioComparer);
                Array.Sort(tempDefinitions, defCount, maxCount, s_maxRatioComparer);

                while (minCount + maxCount > 0 && remainingAvailableSize > 0.0)
                {
                    // the calculation
                    //            remainingStarWeight = totalStarWeight - takenStarWeight
                    // is subject to catastrophic cancellation if the two terms are nearly equal,
                    // which leads to meaningless results.   Check for that, and recompute from
                    // the remaining definitions.   [This leads to quadratic behavior in really
                    // pathological cases - but they'd never arise in practice.]
                    const double starFactor = 1.0 / 256.0;      // lose more than 8 bits of precision -> recalculate
                    if (remainingStarWeight < totalStarWeight * starFactor)
                    {
                        takenStarWeight = 0.0;
                        totalStarWeight = 0.0;

                        for (int i = 0; i < defCount; ++i)
                        {
                            DefinitionBase def = definitions[i];
                            if (def.SizeType == LayoutTimeSizeType.Star && def.MeasureSize > 0.0)
                            {
                                totalStarWeight += StarWeight(def, scale);
                            }
                        }

                        remainingStarWeight = totalStarWeight - takenStarWeight;
                    }

                    double minRatio = (minCount > 0) ? tempDefinitions[minCount - 1]!.MeasureSize : Double.PositiveInfinity;
                    double maxRatio = (maxCount > 0) ? tempDefinitions[defCount + maxCount - 1]!.SizeCache : -1.0;

                    // choose the def with larger ratio to the current proportion ("max discrepancy")
                    double proportion = remainingStarWeight / remainingAvailableSize;
                    bool? chooseMin = Choose(minRatio, maxRatio, proportion);

                    // if no def was chosen, advance to phase 4;  the current proportion doesn't
                    // conflict with any min or max values.
                    if (!(chooseMin.HasValue))
                    {
                        break;
                    }

                    // get the chosen definition and its resolved size
                    DefinitionBase resolvedDef;
                    double resolvedSize;
                    if (chooseMin == true)
                    {
                        resolvedDef = tempDefinitions[minCount - 1]!;
                        resolvedSize = resolvedDef.MinSize;
                        --minCount;
                    }
                    else
                    {
                        resolvedDef = tempDefinitions[defCount + maxCount - 1]!;
                        resolvedSize = Math.Max(resolvedDef.MinSize, resolvedDef.UserMaxSize);
                        --maxCount;
                    }

                    // resolve the chosen def, deduct its contributions from W and S.
                    // Defs resolved in phase 3 are marked by storing the negative of their resolved
                    // size in MeasureSize, to distinguish them from a pending def.
                    takenSize += resolvedSize;
                    resolvedDef.MeasureSize = -resolvedSize;
                    takenStarWeight += StarWeight(resolvedDef, scale);
                    --starCount;

                    remainingAvailableSize = availableSize - takenSize;
                    remainingStarWeight = totalStarWeight - takenStarWeight;

                    // advance to the next candidate defs, removing ones that have been resolved.
                    // Both counts are advanced, as a def might appear in both lists.
                    while (minCount > 0 && tempDefinitions[minCount - 1]!.MeasureSize < 0.0)
                    {
                        --minCount;
                        tempDefinitions[minCount] = null!;
                    }
                    while (maxCount > 0 && tempDefinitions[defCount + maxCount - 1]!.MeasureSize < 0.0)
                    {
                        --maxCount;
                        tempDefinitions[defCount + maxCount] = null!;
                    }
                }

                // decide whether to run Phase2 and Phase3 again.  There are 3 cases:
                // 1. There is space available, and *-defs remaining.  This is the
                //      normal case - move on to Phase 4 to allocate the remaining
                //      space proportionally to the remaining *-defs.
                // 2. There is space available, but no *-defs.  This implies at least one
                //      def was resolved as 'max', taking less space than its proportion.
                //      If there are also 'min' defs, reconsider them - we can give
                //      them more space.   If not, all the *-defs are 'max', so there's
                //      no way to use all the available space.
                // 3. We allocated too much space.   This implies at least one def was
                //      resolved as 'min'.  If there are also 'max' defs, reconsider
                //      them, otherwise the over-allocation is an inevitable consequence
                //      of the given min constraints.
                // Note that if we return to Phase2, at least one *-def will have been
                // resolved.  This guarantees we don't run Phase2+3 infinitely often.
                runPhase2and3 = false;
                if (starCount == 0 && takenSize < availableSize)
                {
                    // if no *-defs remain and we haven't allocated all the space, reconsider the defs
                    // resolved as 'min'.   Their allocation can be increased to make up the gap.
                    for (int i = minCount; i < minCountPhase2; ++i)
                    {
                        if (tempDefinitions[i] is { } def)
                        {
                            def.MeasureSize = 1.0;      // mark as 'not yet resolved'
                            ++starCount;
                            runPhase2and3 = true;       // found a candidate, so re-run Phases 2 and 3
                        }
                    }
                }

                if (takenSize > availableSize)
                {
                    // if we've allocated too much space, reconsider the defs
                    // resolved as 'max'.   Their allocation can be decreased to make up the gap.
                    for (int i = maxCount; i < maxCountPhase2; ++i)
                    {
                        if (tempDefinitions[defCount + i] is { } def)
                        {
                            def.MeasureSize = 1.0;      // mark as 'not yet resolved'
                            ++starCount;
                            runPhase2and3 = true;    // found a candidate, so re-run Phases 2 and 3
                        }
                    }
                }
            }

            // Phase 4.  Resolve the remaining defs proportionally.
            starCount = 0;
            for (int i = 0; i < defCount; ++i)
            {
                DefinitionBase def = definitions[i];

                if (def.SizeType == LayoutTimeSizeType.Star)
                {
                    if (def.MeasureSize < 0.0)
                    {
                        // this def was resolved in phase 3 - fix up its measure size
                        def.MeasureSize = -def.MeasureSize;
                    }
                    else
                    {
                        // this def needs resolution, add it to the list, sorted by *-weight
                        tempDefinitions[starCount++] = def;
                        def.MeasureSize = StarWeight(def, scale);
                    }
                }
            }

            if (starCount > 0)
            {
                Array.Sort(tempDefinitions, 0, starCount, s_starWeightComparer);

                // compute the partial sums of *-weight, in increasing order of weight
                // for minimal loss of precision.
                totalStarWeight = 0.0;
                for (int i = 0; i < starCount; ++i)
                {
                    DefinitionBase def = tempDefinitions[i]!;
                    totalStarWeight += def.MeasureSize;
                    def.SizeCache = totalStarWeight;
                }

                // resolve the defs, in decreasing order of weight
                for (int i = starCount - 1; i >= 0; --i)
                {
                    DefinitionBase def = tempDefinitions[i]!;
                    double resolvedSize = (def.MeasureSize > 0.0) ? Math.Max(availableSize - takenSize, 0.0) * (def.MeasureSize / def.SizeCache) : 0.0;

                    // min and max should have no effect by now, but just in case...
                    resolvedSize = Math.Min(resolvedSize, def.UserMaxSize);
                    resolvedSize = Math.Max(def.MinSize, resolvedSize);

                    def.MeasureSize = resolvedSize;
                    takenSize += resolvedSize;
                }
            }
        }

        /// <summary>
        /// Calculates desired size for given array of definitions.
        /// </summary>
        /// <param name="definitions">Array of definitions to use for calculations.</param>
        /// <returns>Desired size.</returns>
        private static double CalculateDesiredSize(
            IReadOnlyList<DefinitionBase> definitions)
        {
            double desiredSize = 0;

            for (int i = 0; i < definitions.Count; ++i)
            {
                desiredSize += definitions[i].MinSize;
            }

            return (desiredSize);
        }

        /// <summary>
        /// Calculates and sets final size for all definitions in the given array.
        /// </summary>
        /// <param name="definitions">Array of definitions to process.</param>
        /// <param name="finalSize">Final size to lay out to.</param>
        /// <param name="columns">True if sizing column definitions, false for rows</param>
        private void SetFinalSize(
            IReadOnlyList<DefinitionBase> definitions,
            double finalSize,
            bool columns)
        {
            SetFinalSizeMaxDiscrepancy(definitions, finalSize, columns);
        }

        // new implementation, as of 4.7.  This incorporates the same algorithm
        // as in ResolveStarMaxDiscrepancy.  It differs in the same way that SetFinalSizeLegacy
        // differs from ResolveStarLegacy, namely (a) leaves results in def.SizeCache
        // instead of def.MeasureSize, (b) implements LayoutRounding if requested,
        // (c) stores intermediate results differently.
        // The LayoutRounding logic is improved:
        // 1. Use pre-rounded values during proportional allocation.  This avoids the
        //      same kind of problems arising from interaction with min/max that
        //      motivated the new algorithm in the first place.
        // 2. Use correct "nudge" amount when distributing roundoff space.   This
        //      comes into play at high DPI - greater than 134.
        // 3. Applies rounding only to real pixel values (not to ratios)
        private void SetFinalSizeMaxDiscrepancy(
            IReadOnlyList<DefinitionBase> definitions,
            double finalSize,
            bool columns)
        {
            int defCount = definitions.Count;
            int[] definitionIndices = DefinitionIndices;
            int minCount = 0, maxCount = 0;
            double takenSize = 0.0;
            double totalStarWeight = 0.0;
            int starCount = 0;      // number of unresolved *-definitions
            double scale = 1.0;   // scale factor applied to each *-weight;  negative means "Infinity is present"

            // Phase 1.  Determine the maximum *-weight and prepare to adjust *-weights
            double maxStar = 0.0;
            for (int i = 0; i < defCount; ++i)
            {
                DefinitionBase def = definitions[i];

                if (def.UserSize.IsStar)
                {
                    ++starCount;
                    def.MeasureSize = 1.0;  // meaning "not yet resolved in phase 3"
                    if (def.UserSize.Value > maxStar)
                    {
                        maxStar = def.UserSize.Value;
                    }
                }
            }

            if (Double.IsPositiveInfinity(maxStar))
            {
                // negative scale means one or more of the weights was Infinity
                scale = -1.0;
            }
            else if (starCount > 0)
            {
                // if maxStar * starCount > Double.Max, summing all the weights could cause
                // floating-point overflow.  To avoid that, scale the weights by a factor to keep
                // the sum within limits.  Choose a power of 2, to preserve precision.
                double power = Math.Floor(Math.Log(Double.MaxValue / maxStar / starCount, 2.0));
                if (power < 0.0)
                {
                    scale = Math.Pow(2.0, power - 4.0); // -4 is just for paranoia
                }
            }

            // normally Phases 2 and 3 execute only once.  But certain unusual combinations of weights
            // and constraints can defeat the algorithm, in which case we repeat Phases 2 and 3.
            // More explanation below...
            for (bool runPhase2and3 = true; runPhase2and3;)
            {
                // Phase 2.   Compute total *-weight W and available space S.
                // For *-items that have Min or Max constraints, compute the ratios used to decide
                // whether proportional space is too big or too small and add the item to the
                // corresponding list.  (The "min" list is in the first half of definitionIndices,
                // the "max" list in the second half.  DefinitionIndices has capacity at least
                // 2*defCount, so there's room for both lists.)
                totalStarWeight = 0.0;
                takenSize = 0.0;
                minCount = maxCount = 0;

                for (int i = 0; i < defCount; ++i)
                {
                    DefinitionBase def = definitions[i];

                    if (def.UserSize.IsStar)
                    {
                        Debug.Assert(!def.IsShared, "*-defs cannot be shared");

                        if (def.MeasureSize < 0.0)
                        {
                            takenSize += -def.MeasureSize;  // already resolved
                        }
                        else
                        {
                            double starWeight = StarWeight(def, scale);
                            totalStarWeight += starWeight;

                            if (def.MinSizeForArrange > 0.0)
                            {
                                // store ratio w/min in MeasureSize (for now)
                                definitionIndices[minCount++] = i;
                                def.MeasureSize = starWeight / def.MinSizeForArrange;
                            }

                            double effectiveMaxSize = Math.Max(def.MinSizeForArrange, def.UserMaxSize);
                            if (!Double.IsPositiveInfinity(effectiveMaxSize))
                            {
                                // store ratio w/max in SizeCache (for now)
                                definitionIndices[defCount + maxCount++] = i;
                                def.SizeCache = starWeight / effectiveMaxSize;
                            }
                        }
                    }
                    else
                    {
                        double userSize = 0;

                        switch (def.UserSize.GridUnitType)
                        {
                            case (GridUnitType.Pixel):
                                userSize = def.UserSize.Value;
                                break;

                            case (GridUnitType.Auto):
                                userSize = def.MinSizeForArrange;
                                break;
                        }

                        double userMaxSize;

                        if (def.IsShared)
                        {
                            //  overriding userMaxSize effectively prevents squishy-ness.
                            //  this is a "solution" to avoid shared definitions from been sized to
                            //  different final size at arrange time, if / when different grids receive
                            //  different final sizes.
                            userMaxSize = userSize;
                        }
                        else
                        {
                            userMaxSize = def.UserMaxSize;
                        }

                        def.SizeCache = Math.Max(def.MinSizeForArrange, Math.Min(userSize, userMaxSize));
                        takenSize += def.SizeCache;
                    }
                }

                // Phase 3.  Resolve *-items whose proportional sizes are too big or too small.
                int minCountPhase2 = minCount, maxCountPhase2 = maxCount;
                double takenStarWeight = 0.0;
                double remainingAvailableSize = finalSize - takenSize;
                double remainingStarWeight = totalStarWeight - takenStarWeight;

                MinRatioIndexComparer minRatioIndexComparer = new MinRatioIndexComparer(definitions);
                Array.Sort(definitionIndices, 0, minCount, minRatioIndexComparer);
                MaxRatioIndexComparer maxRatioIndexComparer = new MaxRatioIndexComparer(definitions);
                Array.Sort(definitionIndices, defCount, maxCount, maxRatioIndexComparer);

                while (minCount + maxCount > 0 && remainingAvailableSize > 0.0)
                {
                    // the calculation
                    //            remainingStarWeight = totalStarWeight - takenStarWeight
                    // is subject to catastrophic cancellation if the two terms are nearly equal,
                    // which leads to meaningless results.   Check for that, and recompute from
                    // the remaining definitions.   [This leads to quadratic behavior in really
                    // pathological cases - but they'd never arise in practice.]
                    const double starFactor = 1.0 / 256.0;      // lose more than 8 bits of precision -> recalculate
                    if (remainingStarWeight < totalStarWeight * starFactor)
                    {
                        takenStarWeight = 0.0;
                        totalStarWeight = 0.0;

                        for (int i = 0; i < defCount; ++i)
                        {
                            DefinitionBase def = definitions[i];
                            if (def.UserSize.IsStar && def.MeasureSize > 0.0)
                            {
                                totalStarWeight += StarWeight(def, scale);
                            }
                        }

                        remainingStarWeight = totalStarWeight - takenStarWeight;
                    }

                    double minRatio = (minCount > 0) ? definitions[definitionIndices[minCount - 1]].MeasureSize : Double.PositiveInfinity;
                    double maxRatio = (maxCount > 0) ? definitions[definitionIndices[defCount + maxCount - 1]].SizeCache : -1.0;

                    // choose the def with larger ratio to the current proportion ("max discrepancy")
                    double proportion = remainingStarWeight / remainingAvailableSize;
                    bool? chooseMin = Choose(minRatio, maxRatio, proportion);

                    // if no def was chosen, advance to phase 4;  the current proportion doesn't
                    // conflict with any min or max values.
                    if (!chooseMin.HasValue)
                    {
                        break;
                    }

                    // get the chosen definition and its resolved size
                    int resolvedIndex;
                    DefinitionBase resolvedDef;
                    double resolvedSize;
                    if (chooseMin == true)
                    {
                        resolvedIndex = definitionIndices[minCount - 1];
                        resolvedDef = definitions[resolvedIndex];
                        resolvedSize = resolvedDef.MinSizeForArrange;
                        --minCount;
                    }
                    else
                    {
                        resolvedIndex = definitionIndices[defCount + maxCount - 1];
                        resolvedDef = definitions[resolvedIndex];
                        resolvedSize = Math.Max(resolvedDef.MinSizeForArrange, resolvedDef.UserMaxSize);
                        --maxCount;
                    }

                    // resolve the chosen def, deduct its contributions from W and S.
                    // Defs resolved in phase 3 are marked by storing the negative of their resolved
                    // size in MeasureSize, to distinguish them from a pending def.
                    takenSize += resolvedSize;
                    resolvedDef.MeasureSize = -resolvedSize;
                    takenStarWeight += StarWeight(resolvedDef, scale);
                    --starCount;

                    remainingAvailableSize = finalSize - takenSize;
                    remainingStarWeight = totalStarWeight - takenStarWeight;

                    // advance to the next candidate defs, removing ones that have been resolved.
                    // Both counts are advanced, as a def might appear in both lists.
                    while (minCount > 0 && definitions[definitionIndices[minCount - 1]].MeasureSize < 0.0)
                    {
                        --minCount;
                        definitionIndices[minCount] = -1;
                    }
                    while (maxCount > 0 && definitions[definitionIndices[defCount + maxCount - 1]].MeasureSize < 0.0)
                    {
                        --maxCount;
                        definitionIndices[defCount + maxCount] = -1;
                    }
                }

                // decide whether to run Phase2 and Phase3 again.  There are 3 cases:
                // 1. There is space available, and *-defs remaining.  This is the
                //      normal case - move on to Phase 4 to allocate the remaining
                //      space proportionally to the remaining *-defs.
                // 2. There is space available, but no *-defs.  This implies at least one
                //      def was resolved as 'max', taking less space than its proportion.
                //      If there are also 'min' defs, reconsider them - we can give
                //      them more space.   If not, all the *-defs are 'max', so there's
                //      no way to use all the available space.
                // 3. We allocated too much space.   This implies at least one def was
                //      resolved as 'min'.  If there are also 'max' defs, reconsider
                //      them, otherwise the over-allocation is an inevitable consequence
                //      of the given min constraints.
                // Note that if we return to Phase2, at least one *-def will have been
                // resolved.  This guarantees we don't run Phase2+3 infinitely often.
                runPhase2and3 = false;
                if (starCount == 0 && takenSize < finalSize)
                {
                    // if no *-defs remain and we haven't allocated all the space, reconsider the defs
                    // resolved as 'min'.   Their allocation can be increased to make up the gap.
                    for (int i = minCount; i < minCountPhase2; ++i)
                    {
                        if (definitionIndices[i] >= 0)
                        {
                            DefinitionBase def = definitions[definitionIndices[i]];
                            def.MeasureSize = 1.0;      // mark as 'not yet resolved'
                            ++starCount;
                            runPhase2and3 = true;       // found a candidate, so re-run Phases 2 and 3
                        }
                    }
                }

                if (takenSize > finalSize)
                {
                    // if we've allocated too much space, reconsider the defs
                    // resolved as 'max'.   Their allocation can be decreased to make up the gap.
                    for (int i = maxCount; i < maxCountPhase2; ++i)
                    {
                        if (definitionIndices[defCount + i] >= 0)
                        {
                            DefinitionBase def = definitions[definitionIndices[defCount + i]];
                            def.MeasureSize = 1.0;      // mark as 'not yet resolved'
                            ++starCount;
                            runPhase2and3 = true;    // found a candidate, so re-run Phases 2 and 3
                        }
                    }
                }
            }

            // Phase 4.  Resolve the remaining defs proportionally.
            starCount = 0;
            for (int i = 0; i < defCount; ++i)
            {
                DefinitionBase def = definitions[i];

                if (def.UserSize.IsStar)
                {
                    if (def.MeasureSize < 0.0)
                    {
                        // this def was resolved in phase 3 - fix up its size
                        def.SizeCache = -def.MeasureSize;
                    }
                    else
                    {
                        // this def needs resolution, add it to the list, sorted by *-weight
                        definitionIndices[starCount++] = i;
                        def.MeasureSize = StarWeight(def, scale);
                    }
                }
            }

            if (starCount > 0)
            {
                StarWeightIndexComparer starWeightIndexComparer = new StarWeightIndexComparer(definitions);
                Array.Sort(definitionIndices, 0, starCount, starWeightIndexComparer);

                // compute the partial sums of *-weight, in increasing order of weight
                // for minimal loss of precision.
                totalStarWeight = 0.0;
                for (int i = 0; i < starCount; ++i)
                {
                    DefinitionBase def = definitions[definitionIndices[i]];
                    totalStarWeight += def.MeasureSize;
                    def.SizeCache = totalStarWeight;
                }

                // resolve the defs, in decreasing order of weight.
                for (int i = starCount - 1; i >= 0; --i)
                {
                    DefinitionBase def = definitions[definitionIndices[i]];
                    double resolvedSize = (def.MeasureSize > 0.0) ? Math.Max(finalSize - takenSize, 0.0) * (def.MeasureSize / def.SizeCache) : 0.0;

                    // min and max should have no effect by now, but just in case...
                    resolvedSize = Math.Min(resolvedSize, def.UserMaxSize);
                    resolvedSize = Math.Max(def.MinSizeForArrange, resolvedSize);

                    // Use the raw (unrounded) sizes to update takenSize, so that
                    // proportions are computed in the same terms as in phase 3;
                    // this avoids errors arising from min/max constraints.
                    takenSize += resolvedSize;
                    def.SizeCache = resolvedSize;
                }
            }

            // Phase 5.  Apply layout rounding.  We do this after fully allocating
            // unrounded sizes, to avoid breaking assumptions in the previous phases
            if (UseLayoutRounding)
            {
                // DpiScale dpiScale = GetDpi();
                // double dpi = columns ? dpiScale.DpiScaleX : dpiScale.DpiScaleY;
                var dpi = (VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1.0;
                double[] roundingErrors = RoundingErrors;
                double roundedTakenSize = 0;

                // round each of the allocated sizes, keeping track of the deltas
                for (int i = 0; i < definitions.Count; ++i)
                {
                    DefinitionBase def = definitions[i];
                    double roundedSize = LayoutHelper.RoundLayoutValue(def.SizeCache, dpi);
                    roundingErrors[i] = (roundedSize - def.SizeCache);
                    def.SizeCache = roundedSize;
                    roundedTakenSize += roundedSize;
                }

                // The total allocation might differ from finalSize due to rounding
                // effects.  Tweak the allocations accordingly.

                // Theoretical and historical note.  The problem at hand - allocating
                // space to columns (or rows) with *-weights, min and max constraints,
                // and layout rounding - has a long history.  Especially the special
                // case of 50 columns with min=1 and available space=435 - allocating
                // seats in the U.S. House of Representatives to the 50 states in
                // proportion to their population.  There are numerous algorithms
                // and papers dating back to the 1700's, including the book:
                // Balinski, M. and H. Young, Fair Representation, Yale University Press, New Haven, 1982.
                //
                // One surprising result of all this research is that *any* algorithm
                // will suffer from one or more undesirable features such as the
                // "population paradox" or the "Alabama paradox", where (to use our terminology)
                // increasing the available space by one pixel might actually decrease
                // the space allocated to a given column, or increasing the weight of
                // a column might decrease its allocation.   This is worth knowing
                // in case someone complains about this behavior;  it's not a bug so
                // much as something inherent to the problem.  Cite the book mentioned
                // above or one of the 100s of references, and resolve as WontFix.
                //
                // Fortunately, our scenarios tend to have a small number of columns (~10 or fewer)
                // each being allocated a large number of pixels (~50 or greater), and
                // people don't even notice the kind of 1-pixel anomalies that are
                // theoretically inevitable, or don't care if they do.  At least they shouldn't
                // care - no one should be using the results WPF's grid layout to make
                // quantitative decisions; its job is to produce a reasonable display, not
                // to allocate seats in Congress.
                //
                // Our algorithm is more susceptible to paradox than the one currently
                // used for Congressional allocation ("Huntington-Hill" algorithm), but
                // it is faster to run:  O(N log N) vs. O(S * N), where N=number of
                // definitions, S = number of available pixels.  And it produces
                // adequate results in practice, as mentioned above.
                //
                // To reiterate one point:  all this only applies when layout rounding
                // is in effect.  When fractional sizes are allowed, the algorithm
                // behaves as well as possible, subject to the min/max constraints
                // and precision of floating-point computation.  (However, the resulting
                // display is subject to anti-aliasing problems.   TANSTAAFL.)

                if (!MathUtilities.AreClose(roundedTakenSize, finalSize))
                {
                    // Compute deltas
                    for (int i = 0; i < definitions.Count; ++i)
                    {
                        definitionIndices[i] = i;
                    }

                    // Sort rounding errors
                    RoundingErrorIndexComparer roundingErrorIndexComparer = new RoundingErrorIndexComparer(roundingErrors);
                    Array.Sort(definitionIndices, 0, definitions.Count, roundingErrorIndexComparer);
                    double adjustedSize = roundedTakenSize;
                    double dpiIncrement = 1.0 / dpi;

                    if (roundedTakenSize > finalSize)
                    {
                        int i = definitions.Count - 1;
                        while ((adjustedSize > finalSize && !MathUtilities.AreClose(adjustedSize, finalSize)) && i >= 0)
                        {
                            DefinitionBase definition = definitions[definitionIndices[i]];
                            double final = definition.SizeCache - dpiIncrement;
                            final = Math.Max(final, definition.MinSizeForArrange);
                            if (final < definition.SizeCache)
                            {
                                adjustedSize -= dpiIncrement;
                            }
                            definition.SizeCache = final;
                            i--;
                        }
                    }
                    else if (roundedTakenSize < finalSize)
                    {
                        int i = 0;
                        while ((adjustedSize < finalSize && !MathUtilities.AreClose(adjustedSize, finalSize)) && i < definitions.Count)
                        {
                            DefinitionBase definition = definitions[definitionIndices[i]];
                            double final = definition.SizeCache + dpiIncrement;
                            final = Math.Max(final, definition.MinSizeForArrange);
                            if (final > definition.SizeCache)
                            {
                                adjustedSize += dpiIncrement;
                            }
                            definition.SizeCache = final;
                            i++;
                        }
                    }
                }
            }

            // Phase 6.  Compute final offsets
            definitions[0].FinalOffset = 0.0;
            for (int i = 0; i < definitions.Count; ++i)
            {
                definitions[(i + 1) % definitions.Count].FinalOffset = definitions[i].FinalOffset + definitions[i].SizeCache;
            }
        }

        /// <summary>
        /// Choose the ratio with maximum discrepancy from the current proportion.
        /// Returns:
        ///     true    if proportion fails a min constraint but not a max, or
        ///                 if the min constraint has higher discrepancy
        ///     false   if proportion fails a max constraint but not a min, or
        ///                 if the max constraint has higher discrepancy
        ///     null    if proportion doesn't fail a min or max constraint
        /// The discrepancy is the ratio of the proportion to the max- or min-ratio.
        /// When both ratios hit the constraint,  minRatio &lt; proportion &lt; maxRatio,
        /// and the minRatio has higher discrepancy if
        ///         (proportion / minRatio) &gt; (maxRatio / proportion)
        /// </summary>
        private static bool? Choose(double minRatio, double maxRatio, double proportion)
        {
            if (minRatio < proportion)
            {
                if (maxRatio > proportion)
                {
                    // compare proportion/minRatio : maxRatio/proportion, but
                    // do it carefully to avoid floating-point overflow or underflow
                    // and divide-by-0.
                    double minPower = Math.Floor(Math.Log(minRatio, 2.0));
                    double maxPower = Math.Floor(Math.Log(maxRatio, 2.0));
                    double f = Math.Pow(2.0, Math.Floor((minPower + maxPower) / 2.0));
                    if ((proportion / f) * (proportion / f) > (minRatio / f) * (maxRatio / f))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            else if (maxRatio > proportion)
            {
                return false;
            }

            return null;
        }

        /// <summary>
        /// Sorts row/column indices by rounding error if layout rounding is applied.
        /// </summary>
        /// <param name="x">Index, rounding error pair</param>
        /// <param name="y">Index, rounding error pair</param>
        /// <returns>1 if x.Value > y.Value, 0 if equal, -1 otherwise</returns>
        private static int CompareRoundingErrors(KeyValuePair<int, double> x, KeyValuePair<int, double> y)
        {
            if (x.Value < y.Value)
            {
                return -1;
            }
            else if (x.Value > y.Value)
            {
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Calculates final (aka arrange) size for given range.
        /// </summary>
        /// <param name="definitions">Array of definitions to process.</param>
        /// <param name="start">Start of the range.</param>
        /// <param name="count">Number of items in the range.</param>
        /// <returns>Final size.</returns>
        private static double GetFinalSizeForRange(
            IReadOnlyList<DefinitionBase> definitions,
            int start,
            int count)
        {
            double size = 0;
            int i = start + count - 1;

            do
            {
                size += definitions[i].SizeCache;
            } while (--i >= start);

            return (size);
        }

        /// <summary>
        /// Clears dirty state for the grid and its columns / rows
        /// </summary>
        private void SetValid()
        {
            if (_extData is { } extData)
            {
                //                for (int i = 0; i < PrivateColumnCount; ++i) DefinitionsU[i].SetValid ();
                //                for (int i = 0; i < PrivateRowCount; ++i) DefinitionsV[i].SetValid ();

                if (extData.TempDefinitions != null)
                {
                    //  TempDefinitions has to be cleared to avoid "memory leaks"
                    Array.Clear(extData.TempDefinitions, 0, Math.Max(DefinitionsU.Count, DefinitionsV.Count));
                    extData.TempDefinitions = null;
                }
            }
        }
 
        /// <summary>
        /// Synchronized ShowGridLines property with the state of the grid's visual collection
        /// by adding / removing GridLinesRenderer visual.
        /// Returns a reference to GridLinesRenderer visual or null.
        /// </summary>
        private GridLinesRenderer? EnsureGridLinesRenderer()
        {
            //
            //  synchronize the state
            //
            if (ShowGridLines && (_gridLinesRenderer == null))
            {
                _gridLinesRenderer = new GridLinesRenderer();
                VisualChildren.Add(_gridLinesRenderer);
            }

            if ((!ShowGridLines) && (_gridLinesRenderer != null))
            {
                VisualChildren.Remove(_gridLinesRenderer);
                _gridLinesRenderer = null;
            }

            return (_gridLinesRenderer);
        }

        /// <summary>
        /// SetFlags is used to set or unset one or multiple
        /// flags on the object.
        /// </summary>
        private void SetFlags(bool value, Flags flags)
        {
            _flags = value ? (_flags | flags) : (_flags & (~flags));
        }

        /// <summary>
        /// CheckFlags returns <c>true</c> if all the flags in the
        /// given bitmask are set on the object.
        /// </summary>
        private bool CheckFlags(Flags flags)
        {
            return _flags.HasAllFlags(flags);
        }

        private static void OnShowGridLinesPropertyChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            Grid grid = (Grid)d;

            if (grid._extData != null    // trivial grid is 1 by 1. there is no grid lines anyway
                && grid.ListenToNotifications)
            {
                grid.InvalidateArrange();
            }

            grid.SetFlags((bool)e.NewValue!, Flags.ShowGridLinesPropertyValue);
        }

        private static void OnSpacingPropertyChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            Grid grid = (Grid)d;

            if (grid._extData != null
                && grid.ListenToNotifications)
            {
                grid.CellsStructureDirty = true;
            }
        }

        private static void OnCellAttachedPropertyChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d is Visual child)
            {
                Grid? grid = child.GetVisualParent<Grid>();
                if (grid != null
                    && grid._extData != null
                    && grid.ListenToNotifications)
                {
                    grid.CellsStructureDirty = true;
                }
            }
        }

        /// <summary>
        /// Helper for Comparer methods.
        /// </summary>
        /// <returns>
        /// true if one or both of x and y are null, in which case result holds
        /// the relative sort order.
        /// </returns>
        private static bool CompareNullRefs([NotNullWhen(false)] object? x, [NotNullWhen(false)] object? y, out int result)
        {
            result = 2;

            if (x == null)
            {
                if (y == null)
                {
                    result = 0;
                }
                else
                {
                    result = -1;
                }
            }
            else
            {
                if (y == null)
                {
                    result = 1;
                }
            }

            return (result != 2);
        }

        /// <summary>
        /// Private version returning array of column definitions.
        /// </summary>
        private IReadOnlyList<DefinitionBase> DefinitionsU
        {
            get => _extData!.DefinitionsU!;
        }

        /// <summary>
        /// Private version returning array of row definitions.
        /// </summary>
        private IReadOnlyList<DefinitionBase> DefinitionsV
        {
            get => _extData!.DefinitionsV!;
        }

        /// <summary>
        /// Helper accessor to layout time array of definitions.
        /// </summary>
        private DefinitionBase?[] TempDefinitions
        {
            get
            {
                Debug.Assert(_extData is not null);

                var extData = _extData!;
                int requiredLength = Math.Max(DefinitionsU.Count, DefinitionsV.Count) * 2;

                if (extData.TempDefinitions == null
                    || extData.TempDefinitions.Length < requiredLength)
                {
                    WeakReference? tempDefinitionsWeakRef = (WeakReference?)Thread.GetData(s_tempDefinitionsDataSlot);
                    if (tempDefinitionsWeakRef == null)
                    {
                        extData.TempDefinitions = new DefinitionBase[requiredLength];
                        Thread.SetData(s_tempDefinitionsDataSlot, new WeakReference(extData.TempDefinitions));
                    }
                    else
                    {
                        extData.TempDefinitions = (DefinitionBase[]?)tempDefinitionsWeakRef.Target;
                        if (extData.TempDefinitions == null
                            || extData.TempDefinitions.Length < requiredLength)
                        {
                            extData.TempDefinitions = new DefinitionBase[requiredLength];
                            tempDefinitionsWeakRef.Target = extData.TempDefinitions;
                        }
                    }
                }
                return (extData.TempDefinitions);
            }
        }

        /// <summary>
        /// Helper accessor to definition indices.
        /// </summary>
        private int[] DefinitionIndices
        {
            get
            {
                int requiredLength = Math.Max(Math.Max(DefinitionsU.Count, DefinitionsV.Count), 1) * 2;

                if (_definitionIndices == null || _definitionIndices.Length < requiredLength)
                {
                    _definitionIndices = new int[requiredLength];
                }

                return _definitionIndices;
            }
        }

        /// <summary>
        /// Helper accessor to rounding errors.
        /// </summary>
        private double[] RoundingErrors
        {
            get
            {
                int requiredLength = Math.Max(DefinitionsU.Count, DefinitionsV.Count);

                if (_roundingErrors == null && requiredLength == 0)
                {
                    _roundingErrors = new double[1];
                }
                else if (_roundingErrors == null || _roundingErrors.Length < requiredLength)
                {
                    _roundingErrors = new double[requiredLength];
                }
                return _roundingErrors;
            }
        }

        /// <summary>
        /// Private version returning array of cells.
        /// </summary>
        private CellCache[] PrivateCells
        {
            get => _extData!.CellCachesCollection!;
        }

        /// <summary>
        /// Convenience accessor to ValidCellsStructure bit flag.
        /// </summary>
        private bool CellsStructureDirty
        {
            get => !CheckFlags(Flags.ValidCellsStructure);
            set => SetFlags(!value, Flags.ValidCellsStructure);
        }

        /// <summary>
        /// Convenience accessor to ListenToNotifications bit flag.
        /// </summary>
        private bool ListenToNotifications
        {
            get => CheckFlags(Flags.ListenToNotifications);
            set => SetFlags(value, Flags.ListenToNotifications);
        }

        /// <summary>
        /// Convenience accessor to SizeToContentU bit flag.
        /// </summary>
        private bool SizeToContentU
        {
            get => CheckFlags(Flags.SizeToContentU);
            set => SetFlags(value, Flags.SizeToContentU);
        }

        /// <summary>
        /// Convenience accessor to SizeToContentV bit flag.
        /// </summary>
        private bool SizeToContentV
        {
            get => CheckFlags(Flags.SizeToContentV);
            set => SetFlags(value, Flags.SizeToContentV);
        }

        /// <summary>
        /// Convenience accessor to HasStarCellsU bit flag.
        /// </summary>
        private bool HasStarCellsU
        {
            get => CheckFlags(Flags.HasStarCellsU);
            set => SetFlags(value, Flags.HasStarCellsU);
        }

        /// <summary>
        /// Convenience accessor to HasStarCellsV bit flag.
        /// </summary>
        private bool HasStarCellsV
        {
            get => CheckFlags(Flags.HasStarCellsV);
            set => SetFlags(value, Flags.HasStarCellsV);
        }

        /// <summary>
        /// Convenience accessor to HasGroup3CellsInAutoRows bit flag.
        /// </summary>
        private bool HasGroup3CellsInAutoRows
        {
            get => CheckFlags(Flags.HasGroup3CellsInAutoRows);
            set => SetFlags(value, Flags.HasGroup3CellsInAutoRows);
        }

        /// <summary>
        /// Returns *-weight, adjusted for scale computed during Phase 1
        /// </summary>
        private static double StarWeight(DefinitionBase def, double scale)
        {
            if (scale < 0.0)
            {
                // if one of the *-weights is Infinity, adjust the weights by mapping
                // Infinity to 1.0 and everything else to 0.0:  the infinite items share the
                // available space equally, everyone else gets nothing.
                return (Double.IsPositiveInfinity(def.UserSize.Value)) ? 1.0 : 0.0;
            }
            else
            {
                return def.UserSize.Value * scale;
            }
        }

        // Extended data instantiated on demand, for non-trivial case handling only
        private ExtendedData? _extData;

        // Grid validity / property caches dirtiness flags
        private Flags _flags;
        private GridLinesRenderer? _gridLinesRenderer;

        // Keeps track of definition indices.
        private int[]? _definitionIndices;

        // Stores unrounded values and rounding errors during layout rounding.
        private double[]? _roundingErrors;

        // 5 is an arbitrary constant chosen to end the measure loop
        private const int c_layoutLoopMaxCount = 5;

        private static readonly LocalDataStoreSlot s_tempDefinitionsDataSlot = Thread.AllocateDataSlot();
        private static readonly IComparer s_spanPreferredDistributionOrderComparer = new SpanPreferredDistributionOrderComparer();
        private static readonly IComparer s_spanMaxDistributionOrderComparer = new SpanMaxDistributionOrderComparer();
        private static readonly IComparer s_minRatioComparer = new MinRatioComparer();
        private static readonly IComparer s_maxRatioComparer = new MaxRatioComparer();
        private static readonly IComparer s_starWeightComparer = new StarWeightComparer();

        /// <summary>
        /// Extended data instantiated on demand, when grid handles non-trivial case.
        /// </summary>
        private class ExtendedData
        {
            internal ColumnDefinitions? ColumnDefinitions;  //  collection of column definitions (logical tree support)
            internal RowDefinitions? RowDefinitions;        //  collection of row definitions (logical tree support)
            internal IReadOnlyList<DefinitionBase>? DefinitionsU;    //  collection of column definitions used during calc
            internal IReadOnlyList<DefinitionBase>? DefinitionsV;    //  collection of row definitions used during calc
            internal CellCache[]? CellCachesCollection;              //  backing store for logical children
            internal int CellGroup1;                                //  index of the first cell in first cell group
            internal int CellGroup2;                                //  index of the first cell in second cell group
            internal int CellGroup3;                                //  index of the first cell in third cell group
            internal int CellGroup4;                                //  index of the first cell in forth cell group
            internal DefinitionBase?[]? TempDefinitions;              //  temporary array used during layout for various purposes
                                                                    //  TempDefinitions.Length == Max(definitionsU.Length, definitionsV.Length)
        }

        /// <summary>
        /// Grid validity / property caches dirtiness flags
        /// </summary>
        [Flags]
        private enum Flags
        {
            //
            //  the following flags let grid tracking dirtiness in more granular manner:
            //  * Valid???Structure flags indicate that elements were added or removed.
            //  * Valid???Layout flags indicate that layout time portion of the information
            //    stored on the objects should be updated.
            //
            ValidDefinitionsUStructure = 0x00000001,
            ValidDefinitionsVStructure = 0x00000002,
            ValidCellsStructure = 0x00000004,

            //
            //  boolean properties state
            //
            ShowGridLinesPropertyValue = 0x00000100,   //  show grid lines ?

            //
            //  boolean flags
            //
            ListenToNotifications = 0x00001000,   //  "0" when all notifications are ignored
            SizeToContentU = 0x00002000,   //  "1" if calculating to content in U direction
            SizeToContentV = 0x00004000,   //  "1" if calculating to content in V direction
            HasStarCellsU = 0x00008000,   //  "1" if at least one cell belongs to a Star column
            HasStarCellsV = 0x00010000,   //  "1" if at least one cell belongs to a Star row
            HasGroup3CellsInAutoRows = 0x00020000,   //  "1" if at least one cell of group 3 belongs to an Auto row
            MeasureOverrideInProgress = 0x00040000,   //  "1" while in the context of Grid.MeasureOverride
            ArrangeOverrideInProgress = 0x00080000,   //  "1" while in the context of Grid.ArrangeOverride
        }

        /// <summary>
        /// ShowGridLines property. This property is used mostly
        /// for simplification of visual debugging. When it is set
        /// to <c>true</c> grid lines are drawn to visualize location
        /// of grid lines.
        /// </summary>
        public static readonly StyledProperty<bool> ShowGridLinesProperty =
            AvaloniaProperty.Register<Grid, bool>(nameof(ShowGridLines));

        /// <summary>
        /// Defines the <see cref="RowSpacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> RowSpacingProperty =
            AvaloniaProperty.Register<Grid, double>(nameof(RowSpacing));

        /// <summary>
        /// Defines the <see cref="ColumnSpacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ColumnSpacingProperty =
            AvaloniaProperty.Register<Grid, double>(nameof(ColumnSpacingProperty));

        /// <summary>
        /// Column property. This is an attached property.
        /// Grid defines Column property, so that it can be set
        /// on any element treated as a cell. Column property
        /// specifies child's position with respect to columns.
        /// </summary>
        /// <remarks>
        /// <para> Columns are 0 - based. In order to appear in first column, element
        /// should have Column property set to <c>0</c>. </para>
        /// <para> Default value for the property is <c>0</c>. </para>
        /// </remarks>
        public static readonly AttachedProperty<int> ColumnProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, int>(
                "Column",
                defaultValue: 0,
                validate: v => v >= 0);

        /// <summary>
        /// Row property. This is an attached property.
        /// Grid defines Row, so that it can be set
        /// on any element treated as a cell. Row property
        /// specifies child's position with respect to rows.
        /// <remarks>
        /// <para> Rows are 0 - based. In order to appear in first row, element
        /// should have Row property set to <c>0</c>. </para>
        /// <para> Default value for the property is <c>0</c>. </para>
        /// </remarks>
        /// </summary>
        public static readonly AttachedProperty<int> RowProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, int>(
                "Row",
                defaultValue: 0,
                validate: v => v >= 0);

        /// <summary>
        /// ColumnSpan property. This is an attached property.
        /// Grid defines ColumnSpan, so that it can be set
        /// on any element treated as a cell. ColumnSpan property
        /// specifies child's width with respect to columns.
        /// Example, ColumnSpan == 2 means that child will span across two columns.
        /// </summary>
        /// <remarks>
        /// Default value for the property is <c>1</c>.
        /// </remarks>
        public static readonly AttachedProperty<int> ColumnSpanProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, int>(
                "ColumnSpan",
                defaultValue: 1,
                validate: v => v >= 0);

        /// <summary>
        /// RowSpan property. This is an attached property.
        /// Grid defines RowSpan, so that it can be set
        /// on any element treated as a cell. RowSpan property
        /// specifies child's height with respect to row grid lines.
        /// Example, RowSpan == 3 means that child will span across three rows.
        /// </summary>
        /// <remarks>
        /// Default value for the property is <c>1</c>.
        /// </remarks>
        public static readonly AttachedProperty<int> RowSpanProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, int>(
                "RowSpan",
                defaultValue: 1,
                validate: v => v >= 0);

        /// <summary>
        /// IsSharedSizeScope property marks scoping element for shared size.
        /// </summary>
        public static readonly AttachedProperty<bool> IsSharedSizeScopeProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, bool>(
                "IsSharedSizeScope");

        /// <summary>
        /// LayoutTimeSizeType is used internally and reflects layout-time size type.
        /// </summary>
        [Flags]
        internal enum LayoutTimeSizeType : byte
        {
            None = 0x00,
            Pixel = 0x01,
            Auto = 0x02,
            Star = 0x04,
        }

        /// <summary>
        /// CellCache stored calculated values of
        /// 1. attached cell positioning properties;
        /// 2. size type;
        /// 3. index of a next cell in the group;
        /// </summary>
        private struct CellCache
        {
            internal int ColumnIndex;
            internal int RowIndex;
            internal int ColumnSpan;
            internal int RowSpan;
            internal LayoutTimeSizeType SizeTypeU;
            internal LayoutTimeSizeType SizeTypeV;
            internal int Next;
            internal bool IsStarU => SizeTypeU.HasAllFlags(LayoutTimeSizeType.Star);
            internal bool IsAutoU => SizeTypeU.HasAllFlags(LayoutTimeSizeType.Auto);
            internal bool IsStarV => SizeTypeV.HasAllFlags(LayoutTimeSizeType.Star);
            internal bool IsAutoV => SizeTypeV.HasAllFlags(LayoutTimeSizeType.Auto);
        }

        /// <summary>
        /// Helper class for representing a key for a span in hashtable.
        /// </summary>
        private class SpanKey
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="start">Starting index of the span.</param>
            /// <param name="count">Span count.</param>
            /// <param name="u"><c>true</c> for columns; <c>false</c> for rows.</param>
            internal SpanKey(int start, int count, bool u)
            {
                _start = start;
                _count = count;
                _u = u;
            }

            /// <summary>
            /// <see cref="object.GetHashCode"/>
            /// </summary>
            public override int GetHashCode()
            {
                int hash = (_start ^ (_count << 2));

                if (_u) hash &= 0x7ffffff;
                else hash |= 0x8000000;

                return (hash);
            }

            /// <summary>
            /// <see cref="object.Equals(object)"/>
            /// </summary>
            public override bool Equals(object? obj)
            {
                SpanKey? sk = obj as SpanKey;
                return (sk != null
                        && sk._start == _start
                        && sk._count == _count
                        && sk._u == _u);
            }

            /// <summary>
            /// Returns start index of the span.
            /// </summary>
            internal int Start { get => (_start); }

            /// <summary>
            /// Returns span count.
            /// </summary>
            internal int Count { get => (_count); }

            /// <summary>
            /// Returns <c>true</c> if this is a column span.
            /// <c>false</c> if this is a row span.
            /// </summary>
            internal bool U { get => (_u); }

            private int _start;
            private int _count;
            private bool _u;
        }

        /// <summary>
        /// SpanPreferredDistributionOrderComparer.
        /// </summary>
        private class SpanPreferredDistributionOrderComparer : IComparer
        {
            public int Compare(object? x, object? y)
            {
                DefinitionBase? definitionX = x as DefinitionBase;
                DefinitionBase? definitionY = y as DefinitionBase;

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    if (definitionX.UserSize.IsAuto)
                    {
                        if (definitionY.UserSize.IsAuto)
                        {
                            result = definitionX.MinSize.CompareTo(definitionY.MinSize);
                        }
                        else
                        {
                            result = -1;
                        }
                    }
                    else
                    {
                        if (definitionY.UserSize.IsAuto)
                        {
                            result = +1;
                        }
                        else
                        {
                            result = definitionX.PreferredSize.CompareTo(definitionY.PreferredSize);
                        }
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// SpanMaxDistributionOrderComparer.
        /// </summary>
        private class SpanMaxDistributionOrderComparer : IComparer
        {
            public int Compare(object? x, object? y)
            {
                DefinitionBase? definitionX = x as DefinitionBase;
                DefinitionBase? definitionY = y as DefinitionBase;

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    if (definitionX.UserSize.IsAuto)
                    {
                        if (definitionY.UserSize.IsAuto)
                        {
                            result = definitionX.SizeCache.CompareTo(definitionY.SizeCache);
                        }
                        else
                        {
                            result = +1;
                        }
                    }
                    else
                    {
                        if (definitionY.UserSize.IsAuto)
                        {
                            result = -1;
                        }
                        else
                        {
                            result = definitionX.SizeCache.CompareTo(definitionY.SizeCache);
                        }
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// StarDistributionOrderIndexComparer.
        /// </summary>
        private class StarDistributionOrderIndexComparer : IComparer
        {
            private readonly IReadOnlyList<DefinitionBase> definitions;

            internal StarDistributionOrderIndexComparer(IReadOnlyList<DefinitionBase> definitions)
            {
                this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            }

            public int Compare(object? x, object? y)
            {
                int? indexX = x as int?;
                int? indexY = y as int?;

                DefinitionBase? definitionX = null;
                DefinitionBase? definitionY = null;

                if (indexX != null)
                {
                    definitionX = definitions[indexX.Value];
                }
                if (indexY != null)
                {
                    definitionY = definitions[indexY.Value];
                }

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    result = definitionX.SizeCache.CompareTo(definitionY.SizeCache);
                }

                return result;
            }
        }

        /// <summary>
        /// DistributionOrderComparer.
        /// </summary>
        private class DistributionOrderIndexComparer : IComparer
        {
            private readonly IReadOnlyList<DefinitionBase> definitions;

            internal DistributionOrderIndexComparer(IReadOnlyList<DefinitionBase> definitions)
            {
                this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            }

            public int Compare(object? x, object? y)
            {
                int? indexX = x as int?;
                int? indexY = y as int?;

                DefinitionBase? definitionX = null;
                DefinitionBase? definitionY = null;

                if (indexX != null)
                {
                    definitionX = definitions[indexX.Value];
                }
                if (indexY != null)
                {
                    definitionY = definitions[indexY.Value];
                }

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    double xprime = definitionX.SizeCache - definitionX.MinSizeForArrange;
                    double yprime = definitionY.SizeCache - definitionY.MinSizeForArrange;
                    result = xprime.CompareTo(yprime);
                }

                return result;
            }
        }

        /// <summary>
        /// RoundingErrorIndexComparer.
        /// </summary>
        private class RoundingErrorIndexComparer : IComparer
        {
            private readonly double[] errors;

            internal RoundingErrorIndexComparer(double[] errors)
            {
                this.errors = errors ?? throw new ArgumentNullException(nameof(errors));
            }

            public int Compare(object? x, object? y)
            {
                int? indexX = x as int?;
                int? indexY = y as int?;

                int result;

                if (!CompareNullRefs(indexX, indexY, out result))
                {
                    double errorX = errors[indexX.Value];
                    double errorY = errors[indexY.Value];
                    result = errorX.CompareTo(errorY);
                }

                return result;
            }
        }

        /// <summary>
        /// MinRatioComparer.
        /// Sort by w/min (stored in MeasureSize), descending.
        /// We query the list from the back, i.e. in ascending order of w/min.
        /// </summary>
        private class MinRatioComparer : IComparer
        {
            public int Compare(object? x, object? y)
            {
                DefinitionBase? definitionX = x as DefinitionBase;
                DefinitionBase? definitionY = y as DefinitionBase;

                int result;

                if (!CompareNullRefs(definitionY, definitionX, out result))
                {
                    result = definitionY.MeasureSize.CompareTo(definitionX.MeasureSize);
                }

                return result;
            }
        }

        /// <summary>
        /// MaxRatioComparer.
        /// Sort by w/max (stored in SizeCache), ascending.
        /// We query the list from the back, i.e. in descending order of w/max.
        /// </summary>
        private class MaxRatioComparer : IComparer
        {
            public int Compare(object? x, object? y)
            {
                DefinitionBase? definitionX = x as DefinitionBase;
                DefinitionBase? definitionY = y as DefinitionBase;

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    result = definitionX.SizeCache.CompareTo(definitionY.SizeCache);
                }

                return result;
            }
        }

        /// <summary>
        /// StarWeightComparer.
        /// Sort by *-weight (stored in MeasureSize), ascending.
        /// </summary>
        private class StarWeightComparer : IComparer
        {
            public int Compare(object? x, object? y)
            {
                DefinitionBase? definitionX = x as DefinitionBase;
                DefinitionBase? definitionY = y as DefinitionBase;

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    result = definitionX.MeasureSize.CompareTo(definitionY.MeasureSize);
                }

                return result;
            }
        }

        /// <summary>
        /// MinRatioIndexComparer.
        /// </summary>
        private class MinRatioIndexComparer : IComparer
        {
            private readonly IReadOnlyList<DefinitionBase> definitions;

            internal MinRatioIndexComparer(IReadOnlyList<DefinitionBase> definitions)
            {
                this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            }

            public int Compare(object? x, object? y)
            {
                int? indexX = x as int?;
                int? indexY = y as int?;

                DefinitionBase? definitionX = null;
                DefinitionBase? definitionY = null;

                if (indexX != null)
                {
                    definitionX = definitions[indexX.Value];
                }
                if (indexY != null)
                {
                    definitionY = definitions[indexY.Value];
                }

                int result;

                if (!CompareNullRefs(definitionY, definitionX, out result))
                {
                    result = definitionY.MeasureSize.CompareTo(definitionX.MeasureSize);
                }

                return result;
            }
        }

        /// <summary>
        /// MaxRatioIndexComparer.
        /// </summary>
        private class MaxRatioIndexComparer : IComparer
        {
            private readonly IReadOnlyList<DefinitionBase> definitions;

            internal MaxRatioIndexComparer(IReadOnlyList<DefinitionBase> definitions)
            {
                this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            }

            public int Compare(object? x, object? y)
            {
                int? indexX = x as int?;
                int? indexY = y as int?;

                DefinitionBase? definitionX = null;
                DefinitionBase? definitionY = null;

                if (indexX != null)
                {
                    definitionX = definitions[indexX.Value];
                }
                if (indexY != null)
                {
                    definitionY = definitions[indexY.Value];
                }

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    result = definitionX.SizeCache.CompareTo(definitionY.SizeCache);
                }

                return result;
            }
        }

        /// <summary>
        /// MaxRatioIndexComparer.
        /// </summary>
        private class StarWeightIndexComparer : IComparer
        {
            private readonly IReadOnlyList<DefinitionBase> definitions;

            internal StarWeightIndexComparer(IReadOnlyList<DefinitionBase> definitions)
            {
                this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            }

            public int Compare(object? x, object? y)
            {
                int? indexX = x as int?;
                int? indexY = y as int?;

                DefinitionBase? definitionX = null;
                DefinitionBase? definitionY = null;

                if (indexX != null)
                {
                    definitionX = definitions[indexX.Value];
                }
                if (indexY != null)
                {
                    definitionY = definitions[indexY.Value];
                }

                int result;

                if (!CompareNullRefs(definitionX, definitionY, out result))
                {
                    result = definitionX.MeasureSize.CompareTo(definitionY.MeasureSize);
                }

                return result;
            }
        }

        /// <summary>
        /// Helper for rendering grid lines.
        /// </summary>
        internal class GridLinesRenderer : Control
        {
            /// <summary>
            /// Static initialization
            /// </summary>
            static GridLinesRenderer()
            {
                var dashArray = new List<double>() { _dashLength, _dashLength };

                var ds1 = new DashStyle(dashArray, 0);
                _oddDashPen = new Pen(Brushes.Blue,
                                      _penWidth,
                                      lineCap: PenLineCap.Flat,
                                      dashStyle: ds1);

                var ds2 = new DashStyle(dashArray, _dashLength);
                _evenDashPen = new Pen(Brushes.Yellow,
                                       _penWidth,
                                       lineCap: PenLineCap.Flat,
                                       dashStyle: ds2);
            }

            /// <summary>
            /// UpdateRenderBounds.
            /// </summary>
            public sealed override void Render(DrawingContext drawingContext)
            {
                var grid = this.GetVisualParent<Grid>();

                if (grid == null || !grid.ShowGridLines)
                    return;

                for (int i = 1; i < grid.ColumnDefinitions.Count; ++i)
                {
                    DrawGridLine(
                        drawingContext,
                        grid.ColumnDefinitions[i].FinalOffset, 0.0,
                        grid.ColumnDefinitions[i].FinalOffset, _lastArrangeSize.Height);

                    if (grid.ColumnSpacing != 0)
                    {
                        DrawGridLine(
                            drawingContext,
                            grid.ColumnDefinitions[i].FinalOffset - grid.ColumnSpacing, 0.0,
                            grid.ColumnDefinitions[i].FinalOffset - grid.ColumnSpacing, _lastArrangeSize.Height);
                    }
                }

                for (int i = 1; i < grid.RowDefinitions.Count; ++i)
                {
                    DrawGridLine(
                        drawingContext,
                        0.0, grid.RowDefinitions[i].FinalOffset,
                        _lastArrangeSize.Width, grid.RowDefinitions[i].FinalOffset);

                    if (grid.RowSpacing != 0)
                    {
                        DrawGridLine(
                            drawingContext,
                            0.0, grid.RowDefinitions[i].FinalOffset - grid.RowSpacing,
                            _lastArrangeSize.Width, grid.RowDefinitions[i].FinalOffset - grid.RowSpacing);
                    }
                }
            }

            /// <summary>
            /// Draw single hi-contrast line.
            /// </summary>
            private static void DrawGridLine(
                DrawingContext drawingContext,
                double startX,
                double startY,
                double endX,
                double endY)
            {
                var start = new Point(startX, startY);
                var end = new Point(endX, endY);
                drawingContext.DrawLine(_oddDashPen, start, end);
                drawingContext.DrawLine(_evenDashPen, start, end);
            }

            internal void UpdateRenderBounds(Size arrangeSize)
            {
                _lastArrangeSize = arrangeSize;
                InvalidateVisual();
            }

            private static Size _lastArrangeSize;
            private const double _dashLength = 4.0;    //
            private const double _penWidth = 1.0;      //
            private static readonly Pen _oddDashPen;   //  first pen to draw dash
            private static readonly Pen _evenDashPen;  //  second pen to draw dash
        }
    }
}
