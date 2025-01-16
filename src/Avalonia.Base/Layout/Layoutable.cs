using System;
using Avalonia.Logging;
using Avalonia.Reactive;
using Avalonia.Styling;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Layout
{
    /// <summary>
    /// Defines how a control aligns itself horizontally in its parent control.
    /// </summary>
    public enum HorizontalAlignment
    {
        /// <summary>
        /// The control stretches to fill the width of the parent control.
        /// </summary>
        Stretch,

        /// <summary>
        /// The control aligns itself to the left of the parent control.
        /// </summary>
        Left,

        /// <summary>
        /// The control centers itself in the parent control.
        /// </summary>
        Center,

        /// <summary>
        /// The control aligns itself to the right of the parent control.
        /// </summary>
        Right,
    }

    /// <summary>
    /// Defines how a control aligns itself vertically in its parent control.
    /// </summary>
    public enum VerticalAlignment
    {
        /// <summary>
        /// The control stretches to fill the height of the parent control.
        /// </summary>
        Stretch,

        /// <summary>
        /// The control aligns itself to the top of the parent control.
        /// </summary>
        Top,

        /// <summary>
        /// The control centers itself within the parent control.
        /// </summary>
        Center,

        /// <summary>
        /// The control aligns itself to the bottom of the parent control.
        /// </summary>
        Bottom,
    }

    /// <summary>
    /// Implements layout-related functionality for a control.
    /// </summary>
    public class Layoutable : Visual
    {
        /// <summary>
        /// Defines the <see cref="DesiredSize"/> property.
        /// </summary>
        public static readonly DirectProperty<Layoutable, Size> DesiredSizeProperty =
            AvaloniaProperty.RegisterDirect<Layoutable, Size>(nameof(DesiredSize), o => o.DesiredSize);

        /// <summary>
        /// Defines the <see cref="Width"/> property.
        /// </summary>
        public static readonly StyledProperty<double> WidthProperty =
            AvaloniaProperty.Register<Layoutable, double>(nameof(Width), double.NaN, validate: ValidateDimension);

        /// <summary>
        /// Defines the <see cref="Height"/> property.
        /// </summary>
        public static readonly StyledProperty<double> HeightProperty =
            AvaloniaProperty.Register<Layoutable, double>(nameof(Height), double.NaN, validate: ValidateDimension);

        /// <summary>
        /// Defines the <see cref="MinWidth"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MinWidthProperty =
            AvaloniaProperty.Register<Layoutable, double>(nameof(MinWidth), validate: ValidateMinimumDimension);

        /// <summary>
        /// Defines the <see cref="MaxWidth"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaxWidthProperty =
            AvaloniaProperty.Register<Layoutable, double>(nameof(MaxWidth), double.PositiveInfinity, validate: ValidateMaximumDimension);

        /// <summary>
        /// Defines the <see cref="MinHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MinHeightProperty =
            AvaloniaProperty.Register<Layoutable, double>(nameof(MinHeight), validate: ValidateMinimumDimension);

        /// <summary>
        /// Defines the <see cref="MaxHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaxHeightProperty =
            AvaloniaProperty.Register<Layoutable, double>(nameof(MaxHeight), double.PositiveInfinity, validate: ValidateMaximumDimension);

        /// <summary>
        /// Defines the <see cref="Margin"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> MarginProperty =
            AvaloniaProperty.Register<Layoutable, Thickness>(nameof(Margin));

        /// <summary>
        /// Defines the <see cref="HorizontalAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalAlignmentProperty =
            AvaloniaProperty.Register<Layoutable, HorizontalAlignment>(nameof(HorizontalAlignment));

        /// <summary>
        /// Defines the <see cref="VerticalAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalAlignmentProperty =
            AvaloniaProperty.Register<Layoutable, VerticalAlignment>(nameof(VerticalAlignment));

        /// <summary>
        /// Defines the <see cref="UseLayoutRounding"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> UseLayoutRoundingProperty =
            AvaloniaProperty.Register<Layoutable, bool>(nameof(UseLayoutRounding), defaultValue: true, inherits: true);

        private bool _measuring;
        private Size? _previousMeasure;
        private Rect? _previousArrange;
        private EventHandler<EffectiveViewportChangedEventArgs>? _effectiveViewportChanged;
        private EventHandler? _layoutUpdated;
        private bool _isAttachingToVisualTree;

        /// <summary>
        /// Initializes static members of the <see cref="Layoutable"/> class.
        /// </summary>
        static Layoutable()
        {
            AffectsMeasure<Layoutable>(
                WidthProperty,
                HeightProperty,
                MinWidthProperty,
                MaxWidthProperty,
                MinHeightProperty,
                MaxHeightProperty,
                MarginProperty,
                HorizontalAlignmentProperty,
                VerticalAlignmentProperty);
        }

        private static bool ValidateDimension(double value) => double.IsNaN(value) || ValidateMinimumDimension(value);
        private static bool ValidateMinimumDimension(double value) => !double.IsPositiveInfinity(value) && ValidateMaximumDimension(value);
        private static bool ValidateMaximumDimension(double value) => value >= 0;

        /// <summary>
        /// Occurs when the element's effective viewport changes.
        /// </summary>
        public event EventHandler<EffectiveViewportChangedEventArgs>? EffectiveViewportChanged
        {
            add
            {
                if (_effectiveViewportChanged is null && VisualRoot is ILayoutRoot r && !_isAttachingToVisualTree)
                {
                    r.LayoutManager.RegisterEffectiveViewportListener(this);
                }

                _effectiveViewportChanged += value;
            }

            remove
            {
                _effectiveViewportChanged -= value;

                if (_effectiveViewportChanged is null && VisualRoot is ILayoutRoot r)
                {
                    r.LayoutManager.UnregisterEffectiveViewportListener(this);
                }
            }
        }

        /// <summary>
        /// Occurs when a layout pass completes for the control.
        /// </summary>
        public event EventHandler? LayoutUpdated
        {
            add
            {
                if (_layoutUpdated is null && VisualRoot is ILayoutRoot r && !_isAttachingToVisualTree)
                {
                    r.LayoutManager.LayoutUpdated += LayoutManagedLayoutUpdated;
                }

                _layoutUpdated += value;
            }

            remove
            {
                _layoutUpdated -= value;

                if (_layoutUpdated is null && VisualRoot is ILayoutRoot r)
                {
                    r.LayoutManager.LayoutUpdated -= LayoutManagedLayoutUpdated;
                }
            }
        }

        /// <summary>
        /// Executes a layout pass.
        /// </summary>
        /// <remarks>
        /// You should not usually need to call this method explictly, the layout manager will
        /// schedule layout passes itself.
        /// </remarks>
        public void UpdateLayout() => (this.GetVisualRoot() as ILayoutRoot)?.LayoutManager?.ExecuteLayoutPass();

        /// <summary>
        /// Gets or sets the width of the element.
        /// </summary>
        public double Width
        {
            get { return GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the height of the element.
        /// </summary>
        public double Height
        {
            get { return GetValue(HeightProperty); }
            set { SetValue(HeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimum width of the element.
        /// </summary>
        public double MinWidth
        {
            get { return GetValue(MinWidthProperty); }
            set { SetValue(MinWidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum width of the element.
        /// </summary>
        public double MaxWidth
        {
            get { return GetValue(MaxWidthProperty); }
            set { SetValue(MaxWidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimum height of the element.
        /// </summary>
        public double MinHeight
        {
            get { return GetValue(MinHeightProperty); }
            set { SetValue(MinHeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum height of the element.
        /// </summary>
        public double MaxHeight
        {
            get { return GetValue(MaxHeightProperty); }
            set { SetValue(MaxHeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the margin around the element.
        /// </summary>
        public Thickness Margin
        {
            get { return GetValue(MarginProperty); }
            set { SetValue(MarginProperty, value); }
        }

        /// <summary>
        /// Gets or sets the element's preferred horizontal alignment in its parent.
        /// </summary>
        public HorizontalAlignment HorizontalAlignment
        {
            get { return GetValue(HorizontalAlignmentProperty); }
            set { SetValue(HorizontalAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the element's preferred vertical alignment in its parent.
        /// </summary>
        public VerticalAlignment VerticalAlignment
        {
            get { return GetValue(VerticalAlignmentProperty); }
            set { SetValue(VerticalAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets the size that this element computed during the measure pass of the layout process.
        /// </summary>
        public Size DesiredSize
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether the control's layout measure is valid.
        /// </summary>
        public bool IsMeasureValid
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether the control's layouts arrange is valid.
        /// </summary>
        public bool IsArrangeValid
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a value that determines whether the element should be snapped to pixel
        /// boundaries at layout time.
        /// </summary>
        public bool UseLayoutRounding
        {
            get { return GetValue(UseLayoutRoundingProperty); }
            set { SetValue(UseLayoutRoundingProperty, value); }
        }

        /// <summary>
        /// Gets the available size passed in the previous layout pass, if any.
        /// </summary>
        internal Size? PreviousMeasure => _previousMeasure;

        /// <summary>
        /// Gets the layout rect passed in the previous layout pass, if any.
        /// </summary>
        internal Rect? PreviousArrange => _previousArrange;

        /// <summary>
        /// Creates the visual children of the control, if necessary
        /// </summary>
        public virtual void ApplyTemplate()
        {
        }

        /// <summary>
        /// Carries out a measure of the control.
        /// </summary>
        /// <param name="availableSize">The available size for the control.</param>
        public void Measure(Size availableSize)
        {
            if (double.IsNaN(availableSize.Width) || double.IsNaN(availableSize.Height))
            {
                throw new InvalidOperationException("Cannot call Measure using a size with NaN values.");
            }

            if (!IsMeasureValid || _previousMeasure != availableSize)
            {
                var previousDesiredSize = DesiredSize;
                var desiredSize = default(Size);

                IsMeasureValid = true;

                try
                {
                    _measuring = true;
                    desiredSize = MeasureCore(availableSize);
                }
                finally
                {
                    _measuring = false;
                }

                if (IsInvalidSize(desiredSize))
                {
                    throw new InvalidOperationException("Invalid size returned for Measure.");
                }

                DesiredSize = desiredSize;
                _previousMeasure = availableSize;

                Logger.TryGet(LogEventLevel.Verbose, LogArea.Layout)?.Log(this, "Measure requested {DesiredSize}", DesiredSize);

                if (DesiredSize != previousDesiredSize)
                {
                    this.GetVisualParent<Layoutable>()?.ChildDesiredSizeChanged(this);
                }
            }
        }

        /// <summary>
        /// Arranges the control and its children.
        /// </summary>
        /// <param name="rect">The control's new bounds.</param>
        public void Arrange(Rect rect)
        {
            if (IsInvalidRect(rect))
            {
                throw new InvalidOperationException("Invalid Arrange rectangle.");
            }

            if (!IsMeasureValid)
            {
                Measure(_previousMeasure ?? rect.Size);
            }

            if (!IsArrangeValid || _previousArrange != rect)
            {
                Logger.TryGet(LogEventLevel.Verbose, LogArea.Layout)?.Log(this, "Arrange to {Rect} ", rect);

                IsArrangeValid = true;
                ArrangeCore(rect);
                _previousArrange = rect;
            }
        }

        /// <summary>
        /// Invalidates the measurement of the control and queues a new layout pass.
        /// </summary>
        public void InvalidateMeasure()
        {
            if (IsMeasureValid)
            {
                Logger.TryGet(LogEventLevel.Verbose, LogArea.Layout)?.Log(this, "Invalidated measure");

                IsMeasureValid = false;
                IsArrangeValid = false;

                if (IsAttachedToVisualTree)
                {
                    (VisualRoot as ILayoutRoot)?.LayoutManager.InvalidateMeasure(this);
                    InvalidateVisual();
                }
                OnMeasureInvalidated();
            }
        }

        /// <summary>
        /// Invalidates the arrangement of the control and queues a new layout pass.
        /// </summary>
        public void InvalidateArrange()
        {
            if (IsArrangeValid)
            {
                Logger.TryGet(LogEventLevel.Verbose, LogArea.Layout)?.Log(this, "Invalidated arrange");

                IsArrangeValid = false;
                (VisualRoot as ILayoutRoot)?.LayoutManager?.InvalidateArrange(this);
                InvalidateVisual();
            }
        }

        /// <inheritdoc/>
        internal void ChildDesiredSizeChanged(Layoutable control)
        {
            if (!_measuring)
            {
                InvalidateMeasure();
            }
        }

        internal void RaiseEffectiveViewportChanged(EffectiveViewportChangedEventArgs e)
        {
            _effectiveViewportChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// Marks a property as affecting the control's measurement.
        /// </summary>
        /// <typeparam name="T">The control which the property affects.</typeparam>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// After a call to this method in a control's static constructor, any change to the
        /// property will cause <see cref="InvalidateMeasure"/> to be called on the element.
        /// </remarks>
        protected static void AffectsMeasure<T>(params AvaloniaProperty[] properties)
            where T : Layoutable
        {
            var invalidateObserver = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
                static e => (e.Sender as T)?.InvalidateMeasure());

            foreach (var property in properties)
            {
                property.Changed.Subscribe(invalidateObserver);
            }
        }

        /// <summary>
        /// Marks a property as affecting the control's arrangement.
        /// </summary>
        /// <typeparam name="T">The control which the property affects.</typeparam>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// After a call to this method in a control's static constructor, any change to the
        /// property will cause <see cref="InvalidateArrange"/> to be called on the element.
        /// </remarks>
        protected static void AffectsArrange<T>(params AvaloniaProperty[] properties)
            where T : Layoutable
        {
            var invalidate = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
                static e => (e.Sender as T)?.InvalidateArrange());

            foreach (var property in properties)
            {
                property.Changed.Subscribe(invalidate);
            }
        }

        /// <summary>
        /// The default implementation of the control's measure pass.
        /// </summary>
        /// <param name="availableSize">The size available to the control.</param>
        /// <returns>The desired size for the control.</returns>
        /// <remarks>
        /// This method calls <see cref="MeasureOverride(Size)"/> which is probably the method you
        /// want to override in order to modify a control's arrangement.
        /// </remarks>
        protected virtual Size MeasureCore(Size availableSize)
        {
            if (IsVisible)
            {
                var margin = Margin;
                var useLayoutRounding = UseLayoutRounding;
                var scale = 1.0;

                if (useLayoutRounding)
                {
                    scale = LayoutHelper.GetLayoutScale(this);
                    margin = LayoutHelper.RoundLayoutThickness(margin, scale, scale);
                }

                ApplyStyling();
                ApplyTemplate();

                var constrainedSize = LayoutHelper.ApplyLayoutConstraints(
                    this,
                    availableSize.Deflate(margin));

                var isContainer = false;
                ContainerType containerType = ContainerType.Normal;

                if(this is IContainer container && container.ContainerType != ContainerType.Normal)
                {
                    isContainer = true;
                    containerType = container.ContainerType;
                    container.QueryProvider.SetSize(constrainedSize.Width, constrainedSize.Height, containerType);
                }

                var measured = MeasureOverride(constrainedSize);

                double width, height;

                if (isContainer)
                {
                    switch (containerType)
                    {
                        case ContainerType.Width:
                            width = double.IsInfinity(constrainedSize.Width) ? measured.Width : constrainedSize.Width;
                            height = measured.Height;
                            break;
                        case ContainerType.Height:
                            width = measured.Width;
                            height = double.IsInfinity(constrainedSize.Height) ? measured.Height : constrainedSize.Height;
                            break;
                        case ContainerType.WidthAndHeight:
                            width = double.IsInfinity(constrainedSize.Width) ? measured.Width : constrainedSize.Width;
                            height = double.IsInfinity(constrainedSize.Height) ? measured.Height : constrainedSize.Height;
                            break;
                        default:
                            width = measured.Width;
                            height = measured.Height;
                            break;
                    }
                }
                else
                {
                    width = measured.Width;
                    height = measured.Height;
                }

                {
                    double widthCache = Width;

                    if (!double.IsNaN(widthCache))
                    {
                        width = widthCache;
                    }
                }

                width = Math.Min(width, MaxWidth);
                width = Math.Max(width, MinWidth);

                {
                    double heightCache = Height;

                    if (!double.IsNaN(heightCache))
                    {
                        height = heightCache;
                    }
                }

                height = Math.Min(height, MaxHeight);
                height = Math.Max(height, MinHeight);

                if (useLayoutRounding)
                {
                    (width, height) = LayoutHelper.RoundLayoutSizeUp(new Size(width, height), scale, scale);
                }

                width = Math.Min(width, availableSize.Width);
                height = Math.Min(height, availableSize.Height);

                return NonNegative(new Size(width, height).Inflate(margin));
            }
            else
            {
                return new Size();
            }
        }

        /// <summary>
        /// Measures the control and its child elements as part of a layout pass.
        /// </summary>
        /// <param name="availableSize">The size available to the control.</param>
        /// <returns>The desired size for the control.</returns>
        protected virtual Size MeasureOverride(Size availableSize)
        {
            double width = 0;
            double height = 0;

            var visualChildren = VisualChildren;
            var visualCount = visualChildren.Count;

            for (var i = 0; i < visualCount; i++)
            {
                Visual visual = visualChildren[i];

                if (visual is Layoutable layoutable)
                {
                    layoutable.Measure(availableSize);
                    width = Math.Max(width, layoutable.DesiredSize.Width);
                    height = Math.Max(height, layoutable.DesiredSize.Height);
                }
            }

            return new Size(width, height);
        }

        /// <summary>
        /// The default implementation of the control's arrange pass.
        /// </summary>
        /// <param name="finalRect">The control's new bounds.</param>
        /// <remarks>
        /// This method calls <see cref="ArrangeOverride(Size)"/> which is probably the method you
        /// want to override in order to modify a control's arrangement.
        /// </remarks>
        protected virtual void ArrangeCore(Rect finalRect)
        {
            if (IsVisible)
            {
                var useLayoutRounding = UseLayoutRounding;
                var scale = LayoutHelper.GetLayoutScale(this);

                var margin = Margin;
                var originX = finalRect.X + margin.Left;
                var originY = finalRect.Y + margin.Top;

                // Margin has to be treated separately because the layout rounding function is not linear
                // f(a + b) != f(a) + f(b)
                // If the margin isn't pre-rounded some sizes will be offset by 1 pixel in certain scales.
                if (useLayoutRounding)
                {
                    margin = LayoutHelper.RoundLayoutThickness(margin, scale, scale);
                }

                var availableSizeMinusMargins = new Size(
                    Math.Max(0, finalRect.Width - margin.Left - margin.Right),
                    Math.Max(0, finalRect.Height - margin.Top - margin.Bottom));
                var horizontalAlignment = HorizontalAlignment;
                var verticalAlignment = VerticalAlignment;
                var size = availableSizeMinusMargins;

                if (horizontalAlignment != HorizontalAlignment.Stretch)
                {
                    size = size.WithWidth(Math.Min(size.Width, DesiredSize.Width - margin.Left - margin.Right));
                }

                if (verticalAlignment != VerticalAlignment.Stretch)
                {
                    size = size.WithHeight(Math.Min(size.Height, DesiredSize.Height - margin.Top - margin.Bottom));
                }

                size = LayoutHelper.ApplyLayoutConstraints(this, size);

                if (useLayoutRounding)
                {
                    size = LayoutHelper.RoundLayoutSizeUp(size, scale, scale);
                    availableSizeMinusMargins = LayoutHelper.RoundLayoutSizeUp(availableSizeMinusMargins, scale, scale);
                }

                size = ArrangeOverride(size).Constrain(size);

                switch (horizontalAlignment)
                {
                    case HorizontalAlignment.Center:
                    case HorizontalAlignment.Stretch:
                        originX += (availableSizeMinusMargins.Width - size.Width) / 2;
                        break;
                    case HorizontalAlignment.Right:
                        originX += availableSizeMinusMargins.Width - size.Width;
                        break;
                }

                switch (verticalAlignment)
                {
                    case VerticalAlignment.Center:
                    case VerticalAlignment.Stretch:
                        originY += (availableSizeMinusMargins.Height - size.Height) / 2;
                        break;
                    case VerticalAlignment.Bottom:
                        originY += availableSizeMinusMargins.Height - size.Height;
                        break;
                }

                if (useLayoutRounding)
                {
                    originX = LayoutHelper.RoundLayoutValue(originX, scale);
                    originY = LayoutHelper.RoundLayoutValue(originY, scale);
                }

                Bounds = new Rect(originX, originY, size.Width, size.Height);
            }
        }

        /// <summary>
        /// Positions child elements as part of a layout pass.
        /// </summary>
        /// <param name="finalSize">The size available to the control.</param>
        /// <returns>The actual size used.</returns>
        protected virtual Size ArrangeOverride(Size finalSize)
        {
            var arrangeRect = new Rect(finalSize);

            var visualChildren = VisualChildren;
            var visualCount = visualChildren.Count;

            for (var i = 0; i < visualCount; i++)
            {
                Visual visual = visualChildren[i];

                if (visual is Layoutable layoutable)
                {
                    layoutable.Arrange(arrangeRect);
                }
            }

            return finalSize;
        }

        internal sealed override void InvalidateStyles(bool recurse)
        {
            base.InvalidateStyles(recurse);
            InvalidateMeasure();
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            _isAttachingToVisualTree = true;
            try
            {
                base.OnAttachedToVisualTreeCore(e);
            }
            finally
            {
                _isAttachingToVisualTree = false;
            }

            if (e.Root is ILayoutRoot r)
            {
                if (_layoutUpdated is object)
                {
                    r.LayoutManager.LayoutUpdated += LayoutManagedLayoutUpdated;
                }

                if (_effectiveViewportChanged is object)
                {
                    r.LayoutManager.RegisterEffectiveViewportListener(this);
                }
            }
        }

        protected override void OnDetachedFromVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            if (e.Root is ILayoutRoot r)
            {
                if (_layoutUpdated is object)
                {
                    r.LayoutManager.LayoutUpdated -= LayoutManagedLayoutUpdated;
                }

                if (_effectiveViewportChanged is object)
                {
                    r.LayoutManager.UnregisterEffectiveViewportListener(this);
                }
            }

            base.OnDetachedFromVisualTreeCore(e);
        }

        /// <summary>
        /// Called by InvalidateMeasure
        /// </summary>
        protected virtual void OnMeasureInvalidated()
        {
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsVisibleProperty)
            {
                DesiredSize = default;

                // All changes to visibility cause the parent element to be notified.
                this.GetVisualParent<Layoutable>()?.ChildDesiredSizeChanged(this);

                if (change.GetNewValue<bool>())
                {
                    // We only invalidate ourselves when visibility is changed to true.
                    InvalidateMeasure();

                    // If any descendant had its measure/arrange invalidated while we were hidden,
                    // they will need to be registered with the layout manager now that they
                    // are again effectively visible. If IsEffectivelyVisible becomes an observable
                    // property then we can piggy-pack on that; for the moment we do this manually.
                    if (VisualRoot is ILayoutRoot layoutRoot)
                    {
                        var count = VisualChildren.Count;

                        for (var i = 0; i < count; ++i)
                        {
                            (VisualChildren[i] as Layoutable)?.AncestorBecameVisible(layoutRoot.LayoutManager);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected sealed override void OnVisualParentChanged(Visual? oldParent, Visual? newParent)
        {
            LayoutHelper.InvalidateSelfAndChildrenMeasure(this);

            base.OnVisualParentChanged(oldParent, newParent);
        }

        private protected override void OnControlThemeChanged()
        {
            base.OnControlThemeChanged();
            InvalidateMeasure();
        }

        internal override void OnTemplatedParentControlThemeChanged()
        {
            base.OnTemplatedParentControlThemeChanged();
            InvalidateMeasure();
        }

        private void AncestorBecameVisible(ILayoutManager layoutManager)
        {
            if (!IsVisible)
                return;

            if (!IsMeasureValid)
            {
                layoutManager.InvalidateMeasure(this);
                InvalidateVisual();
            }
            else if (!IsArrangeValid)
            {
                layoutManager.InvalidateArrange(this);
                InvalidateVisual();
            }

            var count = VisualChildren.Count;

            for (var i = 0; i < count; ++i)
            {
                (VisualChildren[i] as Layoutable)?.AncestorBecameVisible(layoutManager);
            }
        }

        /// <summary>
        /// Called when the layout manager raises a LayoutUpdated event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void LayoutManagedLayoutUpdated(object? sender, EventArgs e) => _layoutUpdated?.Invoke(this, e);

        /// <summary>
        /// Tests whether any of a <see cref="Rect"/>'s properties include negative values,
        /// a NaN or Infinity.
        /// </summary>
        /// <param name="rect">The rect.</param>
        /// <returns>True if the rect is invalid; otherwise false.</returns>
        private static bool IsInvalidRect(Rect rect)
        {
            return rect.Width < 0 || rect.Height < 0 ||
                double.IsInfinity(rect.X) || double.IsInfinity(rect.Y) ||
                double.IsInfinity(rect.Width) || double.IsInfinity(rect.Height) ||
                double.IsNaN(rect.X) || double.IsNaN(rect.Y) ||
                double.IsNaN(rect.Width) || double.IsNaN(rect.Height);
        }

        /// <summary>
        /// Tests whether any of a <see cref="Size"/>'s properties include negative values,
        /// a NaN or Infinity.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns>True if the size is invalid; otherwise false.</returns>
        private static bool IsInvalidSize(Size size)
        {
            return size.Width < 0 || size.Height < 0 ||
                double.IsInfinity(size.Width) || double.IsInfinity(size.Height) ||
                double.IsNaN(size.Width) || double.IsNaN(size.Height);
        }

        /// <summary>
        /// Ensures neither component of a <see cref="Size"/> is negative.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns>The non-negative size.</returns>
        private static Size NonNegative(Size size)
        {
            return new Size(Math.Max(size.Width, 0), Math.Max(size.Height, 0));
        }
    }
}
