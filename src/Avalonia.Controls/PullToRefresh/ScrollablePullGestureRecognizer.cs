using System;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;

namespace Avalonia.Controls.PullToRefresh
{
    internal class ScrollablePullGestureRecognizer : GestureRecognizer
    {
        private int _gestureId;
        private bool _pullInProgress;

        private double _delta = 1;

        private Point _initialPosition;
        private IPointer? _tracking;

        /// <summary>
        /// Defines the <see cref="PullDirection"/> property.
        /// </summary>
        public static readonly StyledProperty<PullDirection> PullDirectionProperty =
            AvaloniaProperty.Register<ScrollablePullGestureRecognizer, PullDirection>(nameof(PullDirection));

        public PullDirection PullDirection
        {
            get => GetValue(PullDirectionProperty);
            set => SetValue(PullDirectionProperty, value);
        }

        public ScrollablePullGestureRecognizer(PullDirection pullDirection)
        {
            PullDirection = pullDirection;
        }

        public ScrollablePullGestureRecognizer() { }

        protected override void PointerCaptureLost(IPointer pointer)
        {
            if (_tracking == pointer)
            {
                EndPull();
            }
        }

        protected override void PointerPressed(PointerPressedEventArgs e)
        {
            if (Target != null && Target is Visual visual && (e.Pointer.Type == PointerType.Touch || e.Pointer.Type == PointerType.Pen))
            {
                _tracking = e.Pointer;
                _initialPosition = e.GetPosition(visual);
            }
        }

        protected override void PointerMoved(PointerEventArgs e)
        {
            if (_tracking != e.Pointer)
                return;

            if (Target is Visual visual && visual is IScrollable scrollable && CanPull(scrollable))
            {
                var currentPosition = e.GetPosition(visual);

                var delta = CalculateDelta(currentPosition);

                bool pulling = delta.Y > 0 || delta.X > 0;
                _pullInProgress = (_pullInProgress, pulling) switch
                {
                    (false, false) => false,
                    (false, true) => BeginPull(e, delta),
                    (true, true) => HandlePull(e, delta),
                    (true, false) => EndPull(),
                };
            }
        }

        protected override void PointerReleased(PointerReleasedEventArgs e)
        {
            if (_pullInProgress == true)
            {
                EndPull();
            }

            _tracking = null;
            _initialPosition = default;
            _pullInProgress = false;
        }

        private bool BeginPull(PointerEventArgs e, Vector delta)
        {
            _gestureId = PullGestureEventArgs.GetNextFreeId();
            return HandlePull(e, delta);
        }

        private bool HandlePull(PointerEventArgs e, Vector delta)
        {
            Capture(e.Pointer);

            var pullEventArgs = new PullGestureEventArgs(_gestureId, delta, PullDirection);
            Target?.RaiseEvent(pullEventArgs);

            e.Handled = pullEventArgs.Handled;
            return true;
        }

        private bool EndPull()
        {
            Target?.RaiseEvent(new PullGestureEndedEventArgs(_gestureId, PullDirection));
            return false;
        }

        private Vector CalculateDelta(Point currentPosition) => PullDirection switch
        {
            PullDirection.TopToBottom => new Vector(0, currentPosition.Y - _initialPosition.Y),
            PullDirection.BottomToTop => new Vector(0, _initialPosition.Y - currentPosition.Y),
            PullDirection.LeftToRight => new Vector(currentPosition.X - _initialPosition.X, 0),
            PullDirection.RightToLeft => new Vector(_initialPosition.X - currentPosition.X, 0),
            _ => default,
        };

        private bool CanPull(IScrollable scrollable) => PullDirection switch
        {
            PullDirection.TopToBottom => scrollable.Offset.Y < _delta,
            PullDirection.BottomToTop => Math.Abs(scrollable.Offset.Y + scrollable.Viewport.Height - scrollable.Extent.Height) <= _delta,
            PullDirection.LeftToRight => scrollable.Offset.X < _delta,
            PullDirection.RightToLeft => Math.Abs(scrollable.Offset.X + scrollable.Viewport.Width - scrollable.Extent.Width) <= _delta,
            _ => false,
        };
    }
}
