using System;
using System.Collections.Generic;
using Avalonia.Reactive;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using System.Linq;
using Avalonia.Layout;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Presents a scrolling view of content inside a <see cref="ScrollViewer"/>.
    /// </summary>
    public class ScrollContentPresenter : ContentPresenter, IScrollable, IScrollAnchorProvider
    {
        private const double EdgeDetectionTolerance = 0.1;

        /// <summary>
        /// Defines the <see cref="CanHorizontallyScroll"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> CanHorizontallyScrollProperty =
            AvaloniaProperty.Register<ScrollContentPresenter, bool>(nameof(CanHorizontallyScroll));

        /// <summary>
        /// Defines the <see cref="CanVerticallyScroll"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> CanVerticallyScrollProperty =
            AvaloniaProperty.Register<ScrollContentPresenter, bool>(nameof(CanVerticallyScroll));

        /// <summary>
        /// Defines the <see cref="Extent"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollContentPresenter, Size> ExtentProperty =
            ScrollViewer.ExtentProperty.AddOwner<ScrollContentPresenter>(
                o => o.Extent);

        /// <summary>
        /// Defines the <see cref="Offset"/> property.
        /// </summary>
        public static readonly StyledProperty<Vector> OffsetProperty =
            ScrollViewer.OffsetProperty.AddOwner<ScrollContentPresenter>(new(coerce: ScrollViewer.CoerceOffset));

        /// <summary>
        /// Defines the <see cref="Viewport"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollContentPresenter, Size> ViewportProperty =
            ScrollViewer.ViewportProperty.AddOwner<ScrollContentPresenter>(
                o => o.Viewport);

        /// <summary>
        /// Defines the <see cref="HorizontalSnapPointsType"/> property.
        /// </summary>
        public static readonly StyledProperty<SnapPointsType> HorizontalSnapPointsTypeProperty =
            ScrollViewer.HorizontalSnapPointsTypeProperty.AddOwner<ScrollContentPresenter>();

        /// <summary>
        /// Defines the <see cref="VerticalSnapPointsType"/> property.
        /// </summary>
        public static readonly StyledProperty<SnapPointsType> VerticalSnapPointsTypeProperty =
           ScrollViewer.VerticalSnapPointsTypeProperty.AddOwner<ScrollContentPresenter>();

        /// <summary>
        /// Defines the <see cref="HorizontalSnapPointsAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<SnapPointsAlignment> HorizontalSnapPointsAlignmentProperty =
            ScrollViewer.HorizontalSnapPointsAlignmentProperty.AddOwner<ScrollContentPresenter>();

        /// <summary>
        /// Defines the <see cref="VerticalSnapPointsAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<SnapPointsAlignment> VerticalSnapPointsAlignmentProperty =
            ScrollViewer.VerticalSnapPointsAlignmentProperty.AddOwner<ScrollContentPresenter>();

        /// <summary>
        /// Defines the <see cref="IsScrollChainingEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsScrollChainingEnabledProperty =
            ScrollViewer.IsScrollChainingEnabledProperty.AddOwner<ScrollContentPresenter>();

        private bool _arranging;
        private Size _extent;
        private IDisposable? _logicalScrollSubscription;
        private Size _viewport;
        private Dictionary<int, Vector>? _activeLogicalGestureScrolls;
        private Dictionary<int, Vector>? _scrollGestureSnapPoints;
        private HashSet<Control>? _anchorCandidates;
        private Control? _anchorElement;
        private Rect _anchorElementBounds;
        private bool _isAnchorElementDirty;
        private bool _areVerticalSnapPointsRegular;
        private bool _areHorizontalSnapPointsRegular;
        private IReadOnlyList<double>? _horizontalSnapPoints;
        private double _horizontalSnapPoint;
        private IReadOnlyList<double>? _verticalSnapPoints;
        private double _verticalSnapPoint;
        private double _verticalSnapPointOffset;
        private double _horizontalSnapPointOffset;
        private CompositeDisposable? _ownerSubscriptions;
        private ScrollViewer? _owner;
        private IScrollSnapPointsInfo? _scrollSnapPointsInfo;
        private bool _isSnapPointsUpdated;

        /// <summary>
        /// Initializes static members of the <see cref="ScrollContentPresenter"/> class.
        /// </summary>
        static ScrollContentPresenter()
        {
            ClipToBoundsProperty.OverrideDefaultValue(typeof(ScrollContentPresenter), true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollContentPresenter"/> class.
        /// </summary>
        public ScrollContentPresenter()
        {
            AddHandler(RequestBringIntoViewEvent, BringIntoViewRequested);
            AddHandler(Gestures.ScrollGestureEvent, OnScrollGesture);
            AddHandler(Gestures.ScrollGestureEndedEvent, OnScrollGestureEnded);
            AddHandler(Gestures.ScrollGestureInertiaStartingEvent, OnScrollGestureInertiaStartingEnded);

            this.GetObservable(ChildProperty).Subscribe(UpdateScrollableSubscription);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled horizontally.
        /// </summary>
        public bool CanHorizontallyScroll
        {
            get => GetValue(CanHorizontallyScrollProperty);
            set => SetValue(CanHorizontallyScrollProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled horizontally.
        /// </summary>
        public bool CanVerticallyScroll
        {
            get => GetValue(CanVerticallyScrollProperty);
            set => SetValue(CanVerticallyScrollProperty, value);
        }

        /// <summary>
        /// Gets the extent of the scrollable content.
        /// </summary>
        public Size Extent
        {
            get => _extent;
            private set => SetAndRaise(ExtentProperty, ref _extent, value);
        }

        /// <summary>
        /// Gets or sets the current scroll offset.
        /// </summary>
        public Vector Offset
        {
            get => GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }

        /// <summary>
        /// Gets the size of the viewport on the scrollable content.
        /// </summary>
        public Size Viewport
        {
            get => _viewport;
            private set => SetAndRaise(ViewportProperty, ref _viewport, value);
        }

        /// <summary>
        /// Gets or sets how scroll gesture reacts to the snap points along the horizontal axis.
        /// </summary>
        public SnapPointsType HorizontalSnapPointsType
        {
            get => GetValue(HorizontalSnapPointsTypeProperty);
            set => SetValue(HorizontalSnapPointsTypeProperty, value);
        }

        /// <summary>
        /// Gets or sets how scroll gesture reacts to the snap points along the vertical axis.
        /// </summary>
        public SnapPointsType VerticalSnapPointsType
        {
            get => GetValue(VerticalSnapPointsTypeProperty);
            set => SetValue(VerticalSnapPointsTypeProperty, value);
        }

        /// <summary>
        /// Gets or sets how the existing snap points are horizontally aligned versus the initial viewport.
        /// </summary>
        public SnapPointsAlignment HorizontalSnapPointsAlignment
        {
            get => GetValue(HorizontalSnapPointsAlignmentProperty);
            set => SetValue(HorizontalSnapPointsAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets how the existing snap points are vertically aligned versus the initial viewport.
        /// </summary>
        public SnapPointsAlignment VerticalSnapPointsAlignment
        {
            get => GetValue(VerticalSnapPointsAlignmentProperty);
            set => SetValue(VerticalSnapPointsAlignmentProperty, value);
        }

        /// <summary>
        ///  Gets or sets if scroll chaining is enabled. The default value is true.
        /// </summary>
        /// <remarks>
        ///  After a user hits a scroll limit on an element that has been nested within another scrollable element,
        /// you can specify whether that parent element should continue the scrolling operation begun in its child element.
        /// This is called scroll chaining.
        /// </remarks>
        public bool IsScrollChainingEnabled
        {
            get => GetValue(IsScrollChainingEnabledProperty);
            set => SetValue(IsScrollChainingEnabledProperty, value);
        }

        /// <inheritdoc/>
        Control? IScrollAnchorProvider.CurrentAnchor
        {
            get
            {
                EnsureAnchorElementSelection();
                return _anchorElement;
            }
        }

        /// <summary>
        /// Attempts to bring a portion of the target visual into view by scrolling the content.
        /// </summary>
        /// <param name="target">The target visual.</param>
        /// <param name="targetRect">The portion of the target visual to bring into view.</param>
        /// <returns>True if the scroll offset was changed; otherwise false.</returns>
        public bool BringDescendantIntoView(Visual target, Rect targetRect)
        {
            if (Child?.IsEffectivelyVisible != true)
            {
                return false;
            }

            var scrollable = Child as ILogicalScrollable;
            var control = target as Control;

            if (scrollable?.IsLogicalScrollEnabled == true && control != null)
            {
                return scrollable.BringIntoView(control, targetRect);
            }

            var transform = target.TransformToVisual(Child);

            if (transform == null)
            {
                return false;
            }

            var rect = targetRect.TransformToAABB(transform.Value);
            var offset = Offset;

            if (rect.Bottom > offset.Y + Viewport.Height)
            {
                offset = offset.WithY((rect.Bottom - Viewport.Height) + Child.Margin.Top);
            }

            if (rect.Y < offset.Y)
            {
                offset = offset.WithY(rect.Y);
            }

            if (rect.Right > offset.X + Viewport.Width)
            {
                offset = offset.WithX((rect.Right - Viewport.Width) + Child.Margin.Left);
            }

            if (rect.X < offset.X)
            {
                offset = offset.WithX(rect.X);
            }

            if (Offset.NearlyEquals(offset))
            {
                return false;
            }

            var oldOffset = Offset;
            SetCurrentValue(OffsetProperty, offset);

            // It's possible that the Offset coercion has changed the offset back to its previous value,
            // this is common for floating point rounding errors.
            return !Offset.NearlyEquals(oldOffset);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            AttachToScrollViewer();
        }

        /// <summary>
        /// Locates the first <see cref="ScrollViewer"/> ancestor and binds to it. Properties which have been set through other means are not bound.
        /// </summary>
        /// <remarks>
        /// This method is automatically called when the control is attached to a visual tree.
        /// </remarks>
        internal void AttachToScrollViewer()
        {
            var owner = this.FindAncestorOfType<ScrollViewer>();

            if (owner == null)
            {
                _owner = null;
                _ownerSubscriptions?.Dispose();
                _ownerSubscriptions = null;
                return;
            }

            if (owner == _owner)
            {
                return;
            }

            _ownerSubscriptions?.Dispose();
            _owner = owner;

            var subscriptionDisposables = new IDisposable?[]
            {
                IfUnset(CanHorizontallyScrollProperty, p => Bind(p, owner.GetObservable(ScrollViewer.HorizontalScrollBarVisibilityProperty, NotDisabled), Data.BindingPriority.Template)),
                IfUnset(CanVerticallyScrollProperty, p => Bind(p, owner.GetObservable(ScrollViewer.VerticalScrollBarVisibilityProperty, NotDisabled), Data.BindingPriority.Template)),
                IfUnset(OffsetProperty, p => Bind(p, owner.GetBindingObservable(ScrollViewer.OffsetProperty), Data.BindingPriority.Template)),
                IfUnset(IsScrollChainingEnabledProperty, p => Bind(p, owner.GetBindingObservable(ScrollViewer.IsScrollChainingEnabledProperty), Data.BindingPriority.Template)),
                IfUnset(ContentProperty, p => Bind(p, owner.GetBindingObservable(ContentProperty), Data.BindingPriority.Template)),
            }.Where(d => d != null).Cast<IDisposable>().ToArray();

            _ownerSubscriptions = new CompositeDisposable(subscriptionDisposables);

            static bool NotDisabled(ScrollBarVisibility v) => v != ScrollBarVisibility.Disabled;

            IDisposable? IfUnset<T>(T property, Func<T, IDisposable> func) where T : AvaloniaProperty => IsSet(property) ? null : func(property);
        }

        /// <inheritdoc/>
        void IScrollAnchorProvider.RegisterAnchorCandidate(Control element)
        {
            if (!this.IsVisualAncestorOf(element))
            {
                throw new InvalidOperationException(
                    "An anchor control must be a visual descendent of the ScrollContentPresenter.");
            }

            _anchorCandidates ??= new();
            _anchorCandidates.Add(element);
            _isAnchorElementDirty = true;
        }

        /// <inheritdoc/>
        void IScrollAnchorProvider.UnregisterAnchorCandidate(Control element)
        {
            _anchorCandidates?.Remove(element);
            _isAnchorElementDirty = true;

            if (_anchorElement == element)
            {
                _anchorElement = null;
            }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_logicalScrollSubscription != null || Child == null)
            {
                return base.MeasureOverride(availableSize);
            }

            var constraint = new Size(
                CanHorizontallyScroll ? double.PositiveInfinity : availableSize.Width,
                CanVerticallyScroll ? double.PositiveInfinity : availableSize.Height);

            Child.Measure(constraint);

            if (!_isSnapPointsUpdated)
            {
                _isSnapPointsUpdated = true;
                UpdateSnapPoints();
            }

            return Child.DesiredSize.Constrain(availableSize);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_logicalScrollSubscription != null || Child == null)
            {
                return base.ArrangeOverride(finalSize);
            }

            return ArrangeWithAnchoring(finalSize);
        }

        private Size ArrangeWithAnchoring(Size finalSize)
        {
            var size = new Size(
                CanHorizontallyScroll ? Math.Max(Child!.DesiredSize.Width, finalSize.Width) : finalSize.Width,
                CanVerticallyScroll ? Math.Max(Child!.DesiredSize.Height, finalSize.Height) : finalSize.Height);

            Vector TrackAnchor()
            {
                // If we have an anchor and its position relative to Child has changed during the
                // arrange then that change wasn't just due to scrolling (as scrolling doesn't adjust
                // relative positions within Child).
                if (_anchorElement != null &&
                    TranslateBounds(_anchorElement, Child!, out var updatedBounds) &&
                    updatedBounds.Position != _anchorElementBounds.Position)
                {
                    var offset = updatedBounds.Position - _anchorElementBounds.Position;
                    return offset;
                }

                return default;
            }

            var isAnchoring = Offset.X >= EdgeDetectionTolerance || Offset.Y >= EdgeDetectionTolerance;

            if (isAnchoring)
            {
                // Calculate the new anchor element if necessary.
                EnsureAnchorElementSelection();

                // Do the arrange.
                ArrangeOverrideImpl(size, -Offset);

                // If the anchor moved during the arrange, we need to adjust the offset and do another arrange.
                var anchorShift = TrackAnchor();

                if (anchorShift != default)
                {
                    var newOffset = Offset + anchorShift;
                    var newExtent = Extent;
                    var maxOffset = new Vector(Extent.Width - Viewport.Width, Extent.Height - Viewport.Height);

                    if (newOffset.X > maxOffset.X)
                    {
                        newExtent = newExtent.WithWidth(newOffset.X + Viewport.Width);
                    }

                    if (newOffset.Y > maxOffset.Y)
                    {
                        newExtent = newExtent.WithHeight(newOffset.Y + Viewport.Height);
                    }

                    Extent = newExtent;

                    try
                    {
                        _arranging = true;
                        SetCurrentValue(OffsetProperty, newOffset);
                    }
                    finally
                    {
                        _arranging = false;
                    }

                    ArrangeOverrideImpl(size, -Offset);
                }
            }
            else
            {
                ArrangeOverrideImpl(size, -Offset);
            }

            Viewport = finalSize;
            Extent = ComputeExtent(finalSize);
            _isAnchorElementDirty = true;

            return finalSize;
        }

        private Size ComputeExtent(Size viewportSize)
        {
            var childMargin = Child!.Margin;

            if (Child.UseLayoutRounding)
            {
                var scale = LayoutHelper.GetLayoutScale(Child);
                childMargin = LayoutHelper.RoundLayoutThickness(childMargin, scale, scale);
            }

            var extent = Child!.Bounds.Size.Inflate(childMargin);

            if (MathUtilities.AreClose(extent.Width, viewportSize.Width, LayoutHelper.LayoutEpsilon))
                extent = extent.WithWidth(viewportSize.Width);

            if (MathUtilities.AreClose(extent.Height, viewportSize.Height, LayoutHelper.LayoutEpsilon))
                extent = extent.WithHeight(viewportSize.Height);

            return extent;
        }

        private void OnScrollGesture(object? sender, ScrollGestureEventArgs e)
        {
            if (Extent.Height > Viewport.Height || Extent.Width > Viewport.Width)
            {
                var scrollable = Child as ILogicalScrollable;
                var isLogical = scrollable?.IsLogicalScrollEnabled == true;
                var logicalScrollItemSize = new Vector(1, 1);

                double x = Offset.X;
                double y = Offset.Y;

                Vector delta = default;
                if (isLogical)
                    _activeLogicalGestureScrolls?.TryGetValue(e.Id, out delta);
                delta += AdjustDeltaForFlowDirection(e.Delta, FlowDirection);

                if (isLogical && scrollable is object)
                {
                    logicalScrollItemSize = Bounds.Size / scrollable.Viewport;
                }

                if (Extent.Height > Viewport.Height)
                {
                    double dy;
                    if (isLogical)
                    {
                        var logicalUnits = delta.Y / logicalScrollItemSize.Y;
                        delta = delta.WithY(delta.Y - logicalUnits * logicalScrollItemSize.Y);
                        dy = logicalUnits;
                    }
                    else
                        dy = delta.Y;


                    y += dy;
                    y = Math.Max(y, 0);
                    y = Math.Min(y, Extent.Height - Viewport.Height);
                }

                if (Extent.Width > Viewport.Width)
                {
                    double dx;
                    if (isLogical)
                    {
                        var logicalUnits = delta.X / logicalScrollItemSize.X;
                        delta = delta.WithX(delta.X - logicalUnits * logicalScrollItemSize.X);
                        dx = logicalUnits;
                    }
                    else
                        dx = delta.X;
                    x += dx;
                    x = Math.Max(x, 0);
                    x = Math.Min(x, Extent.Width - Viewport.Width);
                }

                if (isLogical)
                {
                    if (_activeLogicalGestureScrolls == null)
                        _activeLogicalGestureScrolls = new Dictionary<int, Vector>();
                    _activeLogicalGestureScrolls[e.Id] = delta;
                }

                Vector newOffset = new Vector(x, y);

                if (_scrollGestureSnapPoints?.TryGetValue(e.Id, out var snapPoint) == true)
                {
                    double xOffset = x;
                    double yOffset = y;

                    if (HorizontalSnapPointsType != SnapPointsType.None)
                    {
                        xOffset = delta.X < 0 ? Math.Max(snapPoint.X, newOffset.X) : Math.Min(snapPoint.X, newOffset.X);
                    }

                    if (VerticalSnapPointsType != SnapPointsType.None)
                    {
                        yOffset = delta.Y < 0 ? Math.Max(snapPoint.Y, newOffset.Y) : Math.Min(snapPoint.Y, newOffset.Y);
                    }

                    newOffset = new Vector(xOffset, yOffset);
                }

                bool offsetChanged = newOffset != Offset;
                SetCurrentValue(OffsetProperty, newOffset);

                e.Handled = !IsScrollChainingEnabled || offsetChanged;

                e.ShouldEndScrollGesture = !IsScrollChainingEnabled && !offsetChanged;
            }
        }

        private void OnScrollGestureEnded(object? sender, ScrollGestureEndedEventArgs e)
        {
            _activeLogicalGestureScrolls?.Remove(e.Id);
            _scrollGestureSnapPoints?.Remove(e.Id);

            SetCurrentValue(OffsetProperty, SnapOffset(Offset));
        }

        private void OnScrollGestureInertiaStartingEnded(object? sender, ScrollGestureInertiaStartingEventArgs e)
        {
            var scrollable = Content;

            if (Content is ItemsControl itemsControl)
                scrollable = itemsControl.Presenter?.Panel;

            if (scrollable is not IScrollSnapPointsInfo)
                return;

            if (_scrollGestureSnapPoints == null)
                _scrollGestureSnapPoints = new Dictionary<int, Vector>();

            var offset = Offset;

            if (HorizontalSnapPointsType != SnapPointsType.None && VerticalSnapPointsType != SnapPointsType.None)
            {
                return;
            }

            double xDistance = 0;
            double yDistance = 0;

            if (HorizontalSnapPointsType != SnapPointsType.None)
            {
                xDistance = HorizontalSnapPointsType == SnapPointsType.Mandatory ? GetDistance(e.Inertia.X) : 0;
            }

            if (VerticalSnapPointsType != SnapPointsType.None)
            {
                yDistance = VerticalSnapPointsType == SnapPointsType.Mandatory ? GetDistance(e.Inertia.Y) : 0;
            }

            offset = new Vector(offset.X + xDistance, offset.Y + yDistance);

            _scrollGestureSnapPoints.Add(e.Id, SnapOffset(offset));

            double GetDistance(double speed)
            {
                var time = Math.Log(ScrollGestureRecognizer.InertialScrollSpeedEnd / Math.Abs(speed)) / Math.Log(ScrollGestureRecognizer.InertialResistance);

                double timeElapsed = 0, distance = 0, step = 0;

                while (timeElapsed <= time)
                {
                    double s = speed * Math.Pow(ScrollGestureRecognizer.InertialResistance, timeElapsed);
                    distance += (s * step);

                    timeElapsed += 0.016f;
                    step = 0.016f;
                }

                return distance;
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            if (Extent.Height > Viewport.Height || Extent.Width > Viewport.Width)
            {
                var scrollable = Child as ILogicalScrollable;
                var isLogical = scrollable?.IsLogicalScrollEnabled == true;

                var x = Offset.X;
                var y = Offset.Y;
                var delta = e.Delta;

                // KeyModifiers.Shift should scroll in horizontal direction. This does not work on every platform. 
                // If Shift-Key is pressed and X is close to 0 we swap the Vector.
                if (e.KeyModifiers == KeyModifiers.Shift && MathUtilities.IsZero(delta.X))
                {
                    delta = new Vector(delta.Y, delta.X);
                }
                else
                {
                    delta = AdjustDeltaForFlowDirection(delta, FlowDirection);
                }

                if (Extent.Height > Viewport.Height)
                {
                    double height = isLogical ? scrollable!.ScrollSize.Height : 50;
                    y += -delta.Y * height;
                    y = Math.Max(y, 0);
                    y = Math.Min(y, Extent.Height - Viewport.Height);
                }

                if (Extent.Width > Viewport.Width)
                {
                    double width = isLogical ? scrollable!.ScrollSize.Width : 50;
                    x += -delta.X * width;
                    x = Math.Max(x, 0);
                    x = Math.Min(x, Extent.Width - Viewport.Width);
                }

                Vector newOffset = SnapOffset(new Vector(x, y), delta, true);

                bool offsetChanged = newOffset != Offset;
                SetCurrentValue(OffsetProperty, newOffset);

                e.Handled = !IsScrollChainingEnabled || offsetChanged;
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == OffsetProperty)
            {
                if (!_arranging)
                {
                    InvalidateArrange();
                }

                _owner?.SetCurrentValue(OffsetProperty, change.GetNewValue<Vector>());
            }
            else if (change.Property == ChildProperty)
            {
                ChildChanged(change);
            }
            else if (change.Property == HorizontalSnapPointsAlignmentProperty ||
                change.Property == VerticalSnapPointsAlignmentProperty)
            {
                UpdateSnapPoints();
            }
            else if (change.Property == ExtentProperty)
            {
                if (_owner != null)
                {
                    _owner.Extent = change.GetNewValue<Size>();
                }
                CoerceValue(OffsetProperty);
            }
            else if (change.Property == ViewportProperty)
            {
                if (_owner != null)
                {
                    _owner.Viewport = change.GetNewValue<Size>();
                }
                CoerceValue(OffsetProperty);
            }

            base.OnPropertyChanged(change);
        }

        private void ScrollSnapPointsInfoSnapPointsChanged(object? sender, Interactivity.RoutedEventArgs e)
        {
            UpdateSnapPoints();
        }

        private void BringIntoViewRequested(object? sender, RequestBringIntoViewEventArgs e)
        {
            if (e.TargetObject is not null)
                e.Handled = BringDescendantIntoView(e.TargetObject, e.TargetRect);
        }

        private void ChildChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateScrollableSubscription((Control?)e.NewValue);

            if (e.OldValue != null)
            {
                SetCurrentValue(OffsetProperty, default);
            }
        }

        private void UpdateScrollableSubscription(Control? child)
        {
            var scrollable = child as ILogicalScrollable;

            _logicalScrollSubscription?.Dispose();
            _logicalScrollSubscription = null;

            if (scrollable != null)
            {
                scrollable.ScrollInvalidated += ScrollInvalidated;

                if (scrollable.IsLogicalScrollEnabled)
                {
                    _logicalScrollSubscription = new CompositeDisposable(
                        this.GetObservable(CanHorizontallyScrollProperty)
                            .Subscribe(x => scrollable.CanHorizontallyScroll = x),
                        this.GetObservable(CanVerticallyScrollProperty)
                            .Subscribe(x => scrollable.CanVerticallyScroll = x),
                        this.GetObservable(OffsetProperty)
                            .Skip(1).Subscribe(x => scrollable.Offset = x),
                        Disposable.Create(() => scrollable.ScrollInvalidated -= ScrollInvalidated));
                    UpdateFromScrollable(scrollable);
                }
            }
        }

        private void ScrollInvalidated(object? sender, EventArgs e)
        {
            UpdateFromScrollable((ILogicalScrollable)sender!);
        }

        private void UpdateFromScrollable(ILogicalScrollable scrollable)
        {
            var logicalScroll = _logicalScrollSubscription != null;

            if (logicalScroll != scrollable.IsLogicalScrollEnabled)
            {
                UpdateScrollableSubscription(Child);
                SetCurrentValue(OffsetProperty, default);
                InvalidateMeasure();
            }
            else if (scrollable.IsLogicalScrollEnabled)
            {
                Viewport = scrollable.Viewport;
                Extent = scrollable.Extent;
                SetCurrentValue(OffsetProperty, scrollable.Offset);
            }
        }

        private void EnsureAnchorElementSelection()
        {
            if (!_isAnchorElementDirty || _anchorCandidates is null)
            {
                return;
            }

            _anchorElement = null;
            _anchorElementBounds = default;
            _isAnchorElementDirty = false;

            var bestCandidate = default(Control);
            var bestCandidateDistance = double.MaxValue;

            // Find the anchor candidate that is scrolled closest to the top-left of this
            // ScrollContentPresenter.
            foreach (var element in _anchorCandidates)
            {
                if (element.IsVisible && GetViewportBounds(element, out var bounds))
                {
                    var distance = (Vector)bounds.Position;
                    var candidateDistance = Math.Abs(distance.Length);

                    if (candidateDistance < bestCandidateDistance)
                    {
                        bestCandidate = element;
                        bestCandidateDistance = candidateDistance;
                    }
                }
            }

            if (bestCandidate != null)
            {
                // We have a candidate, calculate its bounds relative to Child. Because these
                // bounds aren't relative to the ScrollContentPresenter itself, if they change
                // then we know it wasn't just due to scrolling.
                var unscrolledBounds = TranslateBounds(bestCandidate, Child!);
                _anchorElement = bestCandidate;
                _anchorElementBounds = unscrolledBounds;
            }
        }

        private bool GetViewportBounds(Control element, out Rect bounds)
        {
            if (TranslateBounds(element, Child!, out var childBounds))
            {
                // We want the bounds relative to the new Offset, regardless of whether the child
                // control has actually been arranged to this offset yet, so translate first to the
                // child control and then apply Offset rather than translating directly to this
                // control.
                var thisBounds = new Rect(Bounds.Size);
                bounds = new Rect(childBounds.Position - Offset, childBounds.Size);
                return bounds.Intersects(thisBounds);
            }

            bounds = default;
            return false;
        }

        private Rect TranslateBounds(Control control, Control to)
        {
            if (TranslateBounds(control, to, out var bounds))
            {
                return bounds;
            }

            throw new InvalidOperationException("The control's bounds could not be translated to the requested control.");
        }

        private bool TranslateBounds(Control control, Control to, out Rect bounds)
        {
            if (!control.IsVisible)
            {
                bounds = default;
                return false;
            }

            var p = control.TranslatePoint(default, to);
            bounds = p.HasValue ? new Rect(p.Value, control.Bounds.Size) : default;
            return p.HasValue;
        }

        private void UpdateSnapPoints()
        {
            var scrollable = GetScrollSnapPointsInfo(Content);

            if (scrollable is IScrollSnapPointsInfo scrollSnapPointsInfo)
            {
                _areVerticalSnapPointsRegular = scrollSnapPointsInfo.AreVerticalSnapPointsRegular;
                _areHorizontalSnapPointsRegular = scrollSnapPointsInfo.AreHorizontalSnapPointsRegular;

                if (!_areVerticalSnapPointsRegular)
                {
                    _verticalSnapPoints = scrollSnapPointsInfo.GetIrregularSnapPoints(Layout.Orientation.Vertical, VerticalSnapPointsAlignment);
                }
                else
                {
                    _verticalSnapPoints = new List<double>();
                    _verticalSnapPoint = scrollSnapPointsInfo.GetRegularSnapPoints(Layout.Orientation.Vertical, VerticalSnapPointsAlignment, out _verticalSnapPointOffset);

                }

                if (!_areHorizontalSnapPointsRegular)
                {
                    _horizontalSnapPoints = scrollSnapPointsInfo.GetIrregularSnapPoints(Layout.Orientation.Horizontal, HorizontalSnapPointsAlignment);
                }
                else
                {
                    _horizontalSnapPoints = new List<double>();
                    _horizontalSnapPoint = scrollSnapPointsInfo.GetRegularSnapPoints(Layout.Orientation.Horizontal, HorizontalSnapPointsAlignment, out _horizontalSnapPointOffset);
                }
            }
            else
            {
                _horizontalSnapPoints = new List<double>();
                _verticalSnapPoints = new List<double>();
            }
        }

        private Vector SnapOffset(Vector offset, Vector direction = default, bool snapToNext = false)
        {
            var scrollable = GetScrollSnapPointsInfo(Content);

            if (scrollable is null || (VerticalSnapPointsType == SnapPointsType.None && HorizontalSnapPointsType == SnapPointsType.None))
                return offset;

            var diff = GetAlignmentDiff();

            if (VerticalSnapPointsType != SnapPointsType.None && (_areVerticalSnapPointsRegular || _verticalSnapPoints?.Count > 0) && (!snapToNext || snapToNext && direction.Y != 0))
            {
                var estimatedOffset = new Vector(offset.X, offset.Y + diff.Y);
                double previousSnapPoint = 0, nextSnapPoint = 0, midPoint = 0;

                if (_areVerticalSnapPointsRegular)
                {
                    previousSnapPoint = (int)(estimatedOffset.Y / _verticalSnapPoint) * _verticalSnapPoint + _verticalSnapPointOffset;
                    nextSnapPoint = previousSnapPoint + _verticalSnapPoint;
                    midPoint = (previousSnapPoint + nextSnapPoint) / 2;
                }
                else if (_verticalSnapPoints?.Count > 0)
                {
                    (previousSnapPoint, nextSnapPoint) = FindNearestSnapPoint(_verticalSnapPoints, estimatedOffset.Y);
                    midPoint = (previousSnapPoint + nextSnapPoint) / 2;
                }

                var nearestSnapPoint = snapToNext ? (direction.Y > 0 ? previousSnapPoint : nextSnapPoint) :
                    estimatedOffset.Y < midPoint ? previousSnapPoint : nextSnapPoint;

                offset = new Vector(offset.X, nearestSnapPoint - diff.Y);
            }

            if (HorizontalSnapPointsType != SnapPointsType.None && (_areHorizontalSnapPointsRegular || _horizontalSnapPoints?.Count > 0) && (!snapToNext || snapToNext && direction.X != 0))
            {
                var estimatedOffset = new Vector(offset.X + diff.X, offset.Y);
                double previousSnapPoint = 0, nextSnapPoint = 0, midPoint = 0;

                if (_areHorizontalSnapPointsRegular)
                {
                    previousSnapPoint = (int)(estimatedOffset.X / _horizontalSnapPoint) * _horizontalSnapPoint + _horizontalSnapPointOffset;
                    nextSnapPoint = previousSnapPoint + _horizontalSnapPoint;
                    midPoint = (previousSnapPoint + nextSnapPoint) / 2;
                }
                else if (_horizontalSnapPoints?.Count > 0)
                {
                    (previousSnapPoint, nextSnapPoint) = FindNearestSnapPoint(_horizontalSnapPoints, estimatedOffset.X);
                    midPoint = (previousSnapPoint + nextSnapPoint) / 2;
                }

                var nearestSnapPoint = snapToNext ? (direction.X > 0 ? previousSnapPoint : nextSnapPoint) :
                    estimatedOffset.X < midPoint ? previousSnapPoint : nextSnapPoint;

                offset = new Vector(nearestSnapPoint - diff.X, offset.Y);
            }

            Vector GetAlignmentDiff()
            {
                var vector = default(Vector);

                switch (VerticalSnapPointsAlignment)
                {
                    case SnapPointsAlignment.Center:
                        vector += new Vector(0, Viewport.Height / 2);
                        break;
                    case SnapPointsAlignment.Far:
                        vector += new Vector(0, Viewport.Height);
                        break;
                }

                switch (HorizontalSnapPointsAlignment)
                {
                    case SnapPointsAlignment.Center:
                        vector += new Vector(Viewport.Width / 2, 0);
                        break;
                    case SnapPointsAlignment.Far:
                        vector += new Vector(Viewport.Width, 0);
                        break;
                }

                return vector;
            }

            return offset;
        }

        private static (double previous, double next) FindNearestSnapPoint(IReadOnlyList<double> snapPoints, double value)
        {
            var point = snapPoints.BinarySearch(value, Comparer<double>.Default);

            double previousSnapPoint, nextSnapPoint;

            if (point < 0)
            {
                point = ~point;

                previousSnapPoint = snapPoints[Math.Max(0, point - 1)];
                nextSnapPoint = point >= snapPoints.Count ? snapPoints.Last() : snapPoints[Math.Max(0, point)];
            }
            else
            {
                previousSnapPoint = nextSnapPoint = snapPoints[Math.Max(0, point)];
            }

            return (previousSnapPoint, nextSnapPoint);
        }

        private IScrollSnapPointsInfo? GetScrollSnapPointsInfo(object? content)
        {
            var scrollable = content;

            if (Content is ItemsControl itemsControl)
                scrollable = itemsControl.Presenter?.Panel;

            if (Content is ItemsPresenter itemsPresenter)
                scrollable = itemsPresenter.Panel;

            var snapPointsInfo = scrollable as IScrollSnapPointsInfo;

            if (snapPointsInfo != _scrollSnapPointsInfo)
            {
                if (_scrollSnapPointsInfo != null)
                {
                    _scrollSnapPointsInfo.VerticalSnapPointsChanged -= ScrollSnapPointsInfoSnapPointsChanged;
                    _scrollSnapPointsInfo.HorizontalSnapPointsChanged -= ScrollSnapPointsInfoSnapPointsChanged;
                }

                _scrollSnapPointsInfo = snapPointsInfo;

                if (_scrollSnapPointsInfo != null)
                {
                    _scrollSnapPointsInfo.VerticalSnapPointsChanged += ScrollSnapPointsInfoSnapPointsChanged;
                    _scrollSnapPointsInfo.HorizontalSnapPointsChanged += ScrollSnapPointsInfoSnapPointsChanged;
                }
            }

            return snapPointsInfo;
        }

        private static Vector AdjustDeltaForFlowDirection(Vector delta, Media.FlowDirection flowDirection)
        {
            if (flowDirection == Media.FlowDirection.RightToLeft)
            {
                return delta.WithX(-delta.X);
            }
            return delta;
        }
    }
}
