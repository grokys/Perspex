using System;
using System.Diagnostics;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Input.GestureRecognizers
{
    public class ScrollGestureRecognizer : GestureRecognizer
    {
        // Pixels per second speed that is considered to be the stop of inertial scroll
        internal const double InertialScrollSpeedEnd = 5;
        public const double InertialResistance = 0.15;

        private bool _canHorizontallyScroll;
        private bool _canVerticallyScroll;
        private bool _isScrollInertiaEnabled;
        private Vector? _offset;
        private Size? _viewport;
        private Size? _extent;
        private readonly static int s_defaultScrollStartDistance = (int)((AvaloniaLocator.Current?.GetService<IPlatformSettings>()?.GetTapSize(PointerType.Touch).Height ?? 10) / 2);
        private int _scrollStartDistance = s_defaultScrollStartDistance;

        private bool _scrolling;
        private Point _trackedRootPoint;
        private IPointer? _tracking;
        private int _gestureId;
        private Point _pointerPressedPoint;
        private VelocityTracker? _velocityTracker;
        private Visual? _rootTarget;

        // Movement per second
        private Vector _inertia;
        private ulong? _lastMoveTimestamp;

        /// <summary>
        /// Defines the <see cref="CanHorizontallyScroll"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, bool> CanHorizontallyScrollProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, bool>(nameof(CanHorizontallyScroll), 
                o => o.CanHorizontallyScroll, (o, v) => o.CanHorizontallyScroll = v);

        /// <summary>
        /// Defines the <see cref="CanVerticallyScroll"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, bool> CanVerticallyScrollProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, bool>(nameof(CanVerticallyScroll),
                o => o.CanVerticallyScroll, (o, v) => o.CanVerticallyScroll = v);

        /// <summary>
        /// Defines the <see cref="IsScrollInertiaEnabled"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, bool> IsScrollInertiaEnabledProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, bool>(nameof(IsScrollInertiaEnabled),
                o => o.IsScrollInertiaEnabled, (o,v) => o.IsScrollInertiaEnabled = v);

        /// <summary>
        /// Defines the <see cref="ScrollStartDistance"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, int> ScrollStartDistanceProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, int>(nameof(ScrollStartDistance),
                o => o.ScrollStartDistance, (o, v) => o.ScrollStartDistance = v,
                unsetValue: s_defaultScrollStartDistance);

        /// <summary>
        /// Defines the <see cref="ScrollStartDistance"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, Vector?> OffsetProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, Vector?>(nameof(Offset),
                o => o.Offset, (o, v) => o.Offset = v,
                unsetValue: null);

        /// <summary>
        /// Defines the <see cref="ScrollStartDistance"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, Size?> ExtentProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, Size?>(nameof(Extent),
                o => o.Extent, (o, v) => o.Extent = v,
                unsetValue: null);

        /// <summary>
        /// Defines the <see cref="ScrollStartDistance"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, Size?> ViewportProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, Size?>(nameof(Viewport),
                o => o.Viewport, (o, v) => o.Viewport = v,
                unsetValue: null);

        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled horizontally.
        /// </summary>
        public bool CanHorizontallyScroll
        {
            get => _canHorizontallyScroll;
            set => SetAndRaise(CanHorizontallyScrollProperty, ref _canHorizontallyScroll, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled vertically.
        /// </summary>
        public bool CanVerticallyScroll
        {
            get => _canVerticallyScroll;
            set => SetAndRaise(CanVerticallyScrollProperty, ref _canVerticallyScroll, value);
        }

        /// <summary>
        /// Gets or sets whether the gesture should include inertia in it's behavior.
        /// </summary>
        public bool IsScrollInertiaEnabled
        {
            get => _isScrollInertiaEnabled;
            set => SetAndRaise(IsScrollInertiaEnabledProperty, ref _isScrollInertiaEnabled, value);
        }

        /// <summary>
        /// Gets or sets a value indicating the distance the pointer moves before scrolling is started
        /// </summary>
        public int ScrollStartDistance
        {
            get => _scrollStartDistance;
            set => SetAndRaise(ScrollStartDistanceProperty, ref _scrollStartDistance, value);
        }

        /// <summary>
        /// Gets the extent of the scrollable content.
        /// </summary>
        public Size? Extent
        {
            get => _extent;
            private set => SetAndRaise(ExtentProperty, ref _extent, value);
        }

        /// <summary>
        /// Gets or sets the current scroll offset.
        /// </summary>
        public Vector? Offset
        {
            get => _offset;
            private set => SetAndRaise(OffsetProperty, ref _offset, value);
        }

        /// <summary>
        /// Gets the size of the viewport on the scrollable content.
        /// </summary>
        public Size? Viewport
        {
            get => _viewport;
            private set => SetAndRaise(ViewportProperty, ref _viewport, value);
        }

        protected override void PointerPressed(PointerPressedEventArgs e)
        {
            if (e.Pointer.Type == PointerType.Touch || e.Pointer.Type == PointerType.Pen)
            {
                EndGesture();
                _tracking = e.Pointer;
                _gestureId = ScrollGestureEventArgs.GetNextFreeId();
                _rootTarget = (Visual?)(Target as Visual)?.VisualRoot;
                _trackedRootPoint = _pointerPressedPoint = e.GetPosition(_rootTarget);
                _velocityTracker = new VelocityTracker();
                _velocityTracker?.AddPosition(TimeSpan.FromMilliseconds(e.Timestamp), default);
            }
        }

        protected override void PointerMoved(PointerEventArgs e)
        {
            if (e.Pointer == _tracking)
            {
                var rootPoint = e.GetPosition(_rootTarget);
                if (!_scrolling)
                {
                    if (CanVerticallyScroll)
                    {
                        double delta = _trackedRootPoint.Y - rootPoint.Y;

                        if (Offset?.Y == 0 && delta < 0)
                            return;

                        if (Offset?.Y + Viewport?.Height - Extent?.Height == 0 && delta > 0)
                            return;

                        if (Math.Abs(delta) > ScrollStartDistance)
                            _scrolling = true;
                    }

                    if (CanHorizontallyScroll)
                    {
                        double delta = _trackedRootPoint.X - rootPoint.X;

                        if (Offset?.X == 0 && delta < 0)
                            return;

                        if (Offset?.X + Viewport?.Width - Extent?.Width == 0 && delta > 0)
                            return;

                        if (Math.Abs(delta) > ScrollStartDistance)
                            _scrolling = true;
                    }

                    if (_scrolling)
                    {                        
                        // Correct _trackedRootPoint with ScrollStartDistance, so scrolling does not start with a skip of ScrollStartDistance
                        _trackedRootPoint = new Point(
                            _trackedRootPoint.X - (_trackedRootPoint.X >= rootPoint.X ? ScrollStartDistance : -ScrollStartDistance),
                            _trackedRootPoint.Y - (_trackedRootPoint.Y >= rootPoint.Y ? ScrollStartDistance : -ScrollStartDistance));

                        Capture(e.Pointer);
                    }
                }

                if (_scrolling)
                {
                    var vector = _trackedRootPoint - rootPoint;

                    _velocityTracker?.AddPosition(TimeSpan.FromMilliseconds(e.Timestamp), _pointerPressedPoint - rootPoint);

                    _lastMoveTimestamp = e.Timestamp;
                    Target!.RaiseEvent(new ScrollGestureEventArgs(_gestureId, vector));
                    _trackedRootPoint = rootPoint;
                    e.Handled = true;
                }
            }
        }

        protected override void PointerCaptureLost(IPointer pointer)
        {
            if (pointer == _tracking) EndGesture();
        }

        void EndGesture()
        {
            _tracking = null;
            if (_scrolling)
            {
                _inertia = default;
                _scrolling = false;
                Target!.RaiseEvent(new ScrollGestureEndedEventArgs(_gestureId));
                _gestureId = 0;
                _lastMoveTimestamp = null;
                _rootTarget = null;
            }
            
        }


        protected override void PointerReleased(PointerReleasedEventArgs e)
        {
            if (e.Pointer == _tracking && _scrolling)
            {
                _inertia = _velocityTracker?.GetFlingVelocity().PixelsPerSecond ?? Vector.Zero;

                e.Handled = true;
                if (_inertia == default
                    || e.Timestamp == 0
                    || _lastMoveTimestamp == 0
                    || e.Timestamp - _lastMoveTimestamp > 200
                    || !IsScrollInertiaEnabled)
                    EndGesture();
                else
                {
                    _tracking = null;
                    var savedGestureId = _gestureId;
                    var st = Stopwatch.StartNew();
                    var lastTime = TimeSpan.Zero;
                    Target!.RaiseEvent(new ScrollGestureInertiaStartingEventArgs(_gestureId, _inertia));
                    DispatcherTimer.Run(() =>
                    {
                        // Another gesture has started, finish the current one
                        if (_gestureId != savedGestureId)
                        {
                            return false;
                        }

                        var elapsedSinceLastTick = st.Elapsed - lastTime;
                        lastTime = st.Elapsed;

                        var speed = _inertia * Math.Pow(InertialResistance, st.Elapsed.TotalSeconds);
                        var distance = speed * elapsedSinceLastTick.TotalSeconds;
                        var scrollGestureEventArgs = new ScrollGestureEventArgs(_gestureId, distance);
                        Target!.RaiseEvent(scrollGestureEventArgs);

                        if (!scrollGestureEventArgs.Handled || scrollGestureEventArgs.ShouldEndScrollGesture)
                        {
                            EndGesture();
                            return false;
                        }

                        // EndGesture using InertialScrollSpeedEnd only in the direction of scrolling
                        if (CanVerticallyScroll && CanHorizontallyScroll && Math.Abs(speed.X) < InertialScrollSpeedEnd && Math.Abs(speed.Y) <= InertialScrollSpeedEnd)
                        {
                            EndGesture();
                            return false;
                        }
                        else if (CanVerticallyScroll && Math.Abs(speed.Y) <= InertialScrollSpeedEnd)
                        {
                            EndGesture();
                            return false;
                        }
                        else if (CanHorizontallyScroll && Math.Abs(speed.X) < InertialScrollSpeedEnd)
                        {
                            EndGesture();
                            return false;
                        }

                        return true;
                    }, TimeSpan.FromMilliseconds(16), DispatcherPriority.Background);
                }
            }
        }
    }
}
