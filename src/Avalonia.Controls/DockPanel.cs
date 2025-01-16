// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/) 
// 
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the available docking modes for a control in a <see cref="DockPanel"/>.
    /// </summary>
    public enum Dock
    {
        Left = 0,
        Bottom,
        Right,
        Top
    }

    /// <summary>
    /// A panel which arranges its children at the top, bottom, left, right or center.
    /// </summary>
    public class DockPanel : Panel
    {
        /// <summary>
        /// Defines the Dock attached property.
        /// </summary>
        public static readonly AttachedProperty<Dock> DockProperty =
            AvaloniaProperty.RegisterAttached<DockPanel, Control, Dock>("Dock");

        /// <summary>
        /// Defines the <see cref="LastChildFill"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> LastChildFillProperty =
            AvaloniaProperty.Register<DockPanel, bool>(
                nameof(LastChildFill),
                defaultValue: true);

        /// <summary>
        /// Identifies the Padding dependency property.
        /// </summary>
        /// <returns>The identifier for the <see cref="Padding"/> dependency property.</returns>
        public static readonly StyledProperty<Thickness> PaddingProperty =
            AvaloniaProperty.Register<DockPanel, Thickness>(
                nameof(Padding));

        /// <summary>
        /// Identifies the HorizontalSpacing dependency property.
        /// </summary>
        /// <returns>The identifier for the <see cref="HorizontalSpacing"/> dependency property.</returns>
        public static readonly StyledProperty<double> HorizontalSpacingProperty =
            AvaloniaProperty.Register<DockPanel, double>(
                nameof(HorizontalSpacing));

        /// <summary>
        /// Identifies the VerticalSpacing dependency property.
        /// </summary>
        /// <returns>The identifier for the <see cref="VerticalSpacing"/> dependency property.</returns>
        public static readonly StyledProperty<double> VerticalSpacingProperty =
                AvaloniaProperty.Register<DockPanel, double>(
                    nameof(VerticalSpacing));

        /// <summary>
        /// Initializes static members of the <see cref="DockPanel"/> class.
        /// </summary>
        static DockPanel()
        {
            AffectsParentMeasure<DockPanel>(DockProperty);
            AffectsMeasure<DockPanel>(LastChildFillProperty, PaddingProperty, HorizontalSpacingProperty, VerticalSpacingProperty);
        }

        /// <summary>
        /// Gets the value of the Dock attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The Dock attached property.</returns>
        public static Dock GetDock(Control control)
        {
            return control.GetValue(DockProperty);
        }

        /// <summary>
        /// Sets the value of the Dock attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value of the Dock property.</param>
        public static void SetDock(Control control, Dock value)
        {
            control.SetValue(DockProperty, value);
        }

        /// <summary>
        /// Gets or sets a value which indicates whether the last child of the 
        /// <see cref="DockPanel"/> fills the remaining space in the panel.
        /// </summary>
        public bool LastChildFill
        {
            get => GetValue(LastChildFillProperty);
            set => SetValue(LastChildFillProperty, value);
        }

        /// <summary>
        /// Gets or sets the distance between the border and its child object.
        /// </summary>
        /// <returns>
        /// The dimensions of the space between the border and its child as a Thickness value.
        /// Thickness is a structure that stores dimension values using pixel measures.
        /// </returns>
        public Thickness Padding
        {
            get => GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        /// <summary>
        /// Gets or sets the horizontal distance between the child objects.
        /// </summary>
        public double HorizontalSpacing
        {
            get => GetValue(HorizontalSpacingProperty);
            set => SetValue(HorizontalSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical distance between the child objects.
        /// </summary>
        public double VerticalSpacing
        {
            get => GetValue(VerticalSpacingProperty);
            set => SetValue(VerticalSpacingProperty, value);
        }


        /// <summary>
        /// Updates DesiredSize of the DockPanel.  Called by parent Control.  This is the first pass of layout.
        /// </summary>
        /// <remarks>
        /// Children are measured based on their sizing properties and <see cref="Dock" />.  
        /// Each child is allowed to consume all the space on the side on which it is docked; Left/Right docked
        /// children are granted all vertical space for their entire width, and Top/Bottom docked children are
        /// granted all horizontal space for their entire height.
        /// </remarks>
        /// <param name="availableSize">Constraint size is an "upper limit" that the return value should not exceed.</param>
        /// <returns>The Panel's desired size.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var parentWidth = 0.0;
            var parentHeight = 0.0;
            var accumulatedWidth = Padding.Left + Padding.Right;
            var accumulatedHeight = Padding.Top + Padding.Bottom;

            var leftSpacing = false;
            var topSpacing = false;
            var rightSpacing = false;
            var bottomSpacing = false;
            var childrenCount = LastChildFill ? Children.Count - 1 : Children.Count;

            for (var index = 0; index < childrenCount; ++index)
            {
                var child = Children[index];
                var childConstraint = new Size(
                    GetPositiveOrZero(availableSize.Width - accumulatedWidth),
                    GetPositiveOrZero(availableSize.Height - accumulatedHeight));

                child.Measure(childConstraint);
                var childDesiredSize = child.DesiredSize;

                switch (child.GetValue(DockProperty))
                {
                    case Dock.Left:
                        leftSpacing = true;
                        parentHeight = Math.Max(parentHeight, accumulatedHeight + childDesiredSize.Height);
                        if (childConstraint.Width is not 0)
                            accumulatedWidth += HorizontalSpacing;
                        accumulatedWidth += childDesiredSize.Width;
                        break;

                    case Dock.Right:
                        rightSpacing = true;
                        parentHeight = Math.Max(parentHeight, accumulatedHeight + childDesiredSize.Height);
                        if (childConstraint.Width is not 0)
                            accumulatedWidth += HorizontalSpacing;
                        accumulatedWidth += childDesiredSize.Width;
                        break;

                    case Dock.Top:
                        topSpacing = true;
                        parentWidth = Math.Max(parentWidth, accumulatedWidth + childDesiredSize.Width);
                        if (childConstraint.Height is not 0)
                            accumulatedHeight += VerticalSpacing;
                        accumulatedHeight += childDesiredSize.Height;
                        break;

                    case Dock.Bottom:
                        bottomSpacing = true;
                        parentWidth = Math.Max(parentWidth, accumulatedWidth + childDesiredSize.Width);
                        if (childConstraint.Height is not 0)
                            accumulatedHeight += VerticalSpacing;
                        accumulatedHeight += childDesiredSize.Height;
                        break;
                }
            }

            if (LastChildFill)
            {
                var child = Children[Children.Count - 1];
                var childConstraint = new Size(
                    GetPositiveOrZero(availableSize.Width - accumulatedWidth),
                    GetPositiveOrZero(availableSize.Height - accumulatedHeight));

                child.Measure(childConstraint);
                var childDesiredSize = child.DesiredSize;
                parentHeight = Math.Max(parentHeight, accumulatedHeight + childDesiredSize.Height);
                parentWidth = Math.Max(parentWidth, accumulatedWidth + childDesiredSize.Width);
                accumulatedHeight += childDesiredSize.Height;
                accumulatedWidth += childDesiredSize.Width;
            }
            else
            {
                if (leftSpacing || rightSpacing)
                    accumulatedWidth -= HorizontalSpacing;
                if (bottomSpacing || topSpacing)
                    accumulatedHeight -= VerticalSpacing;
            }

            parentWidth = Math.Min(availableSize.Width, Math.Max(parentWidth, accumulatedWidth));
            parentHeight = Math.Min(availableSize.Height, Math.Max(parentHeight, accumulatedHeight));
            return new Size(parentWidth, parentHeight);
        }

        /// <summary>
        /// DockPanel computes a position and final size for each of its children based upon their
        /// <see cref="Dock" /> enum and sizing properties.
        /// </summary>
        /// <param name="finalSize">Size that DockPanel will assume to position children.</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count is 0)
                return finalSize;

            var currentBounds = new Rect(
                Padding.Left,
                Padding.Top,
                GetPositiveOrZero(finalSize.Width - Padding.Left - Padding.Right),
                GetPositiveOrZero(finalSize.Height - Padding.Top - Padding.Bottom));
            var childrenCount = LastChildFill ? Children.Count - 1 : Children.Count;

            for (var index = 0; index < childrenCount; ++index)
            {
                var child = Children[index];
                var dock = child.GetValue(DockProperty);
                double width, height;
                switch (dock)
                {
                    case Dock.Left:

                        width = Math.Min(child.DesiredSize.Width, currentBounds.Width);
                        child.Arrange(currentBounds.WithWidth(width));
                        width += HorizontalSpacing;
                        currentBounds = new Rect(currentBounds.X + width, currentBounds.Y, GetPositiveOrZero(currentBounds.Width - width), currentBounds.Height);

                        break;
                    case Dock.Top:

                        height = Math.Min(child.DesiredSize.Height, currentBounds.Height);
                        child.Arrange(currentBounds.WithHeight(height));
                        height += VerticalSpacing;
                        currentBounds = new Rect(currentBounds.X, currentBounds.Y + height, currentBounds.Width, GetPositiveOrZero(currentBounds.Height - height));

                        break;
                    case Dock.Right:

                        width = Math.Min(child.DesiredSize.Width, currentBounds.Width);
                        child.Arrange(new Rect(currentBounds.X + currentBounds.Width - width, currentBounds.Y, width, currentBounds.Height));
                        width += HorizontalSpacing;
                        currentBounds = currentBounds.WithWidth(GetPositiveOrZero(currentBounds.Width - width));

                        break;
                    case Dock.Bottom:

                        height = Math.Min(child.DesiredSize.Height, currentBounds.Height);
                        child.Arrange(new Rect(currentBounds.X, currentBounds.Y + currentBounds.Height - height, currentBounds.Width, height));
                        height += VerticalSpacing;
                        currentBounds = currentBounds.WithHeight(GetPositiveOrZero(currentBounds.Height - height));

                        break;
                }
            }

            if (LastChildFill)
            {
                var child = Children[Children.Count - 1];
                child.Arrange(new Rect(currentBounds.X, currentBounds.Y, currentBounds.Width, currentBounds.Height));
            }

            return finalSize;
        }

        private static double GetPositiveOrZero(double value) => Math.Max(value, 0);
    }
}
