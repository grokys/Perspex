using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;

namespace Avalonia.Controls.PullToRefresh
{
    internal class RefreshInfoProvider : Interactive
    {
        internal const double DefaultExecutionRatio = 0.8;

        private PullDirection _refreshPullDirection;
        private Size _refreshVisualizerSize;

        private readonly CompositionVisual? _visual;
        private bool _isInteractingForRefresh;
        private double _interactionRatio;
        private bool _entered;

        public static readonly  DirectProperty<RefreshInfoProvider, bool> IsInteractingForRefreshProperty =
            AvaloniaProperty.RegisterDirect<RefreshInfoProvider, bool>(nameof(IsInteractingForRefresh),
                s => s.IsInteractingForRefresh, (s, o) => s.IsInteractingForRefresh = o);


        public static readonly DirectProperty<RefreshInfoProvider, double> ExecutionRatioProperty =
            AvaloniaProperty.RegisterDirect<RefreshInfoProvider, double>(nameof(ExecutionRatio),
                s => s.ExecutionRatio);

        public static readonly DirectProperty<RefreshInfoProvider, double> InteractionRatioProperty =
            AvaloniaProperty.RegisterDirect<RefreshInfoProvider, double>(nameof(InteractionRatio),
                s => s.InteractionRatio, (s, o) => s.InteractionRatio = o);

        public static readonly DirectProperty<RefreshInfoProvider, PullDirection> PullDirectionProperty =
            AvaloniaProperty.RegisterDirect<RefreshInfoProvider, PullDirection>(nameof(PullDirection),
                s => s.PullDirection, (s, o) => s.PullDirection = o);

        public static readonly DirectProperty<RefreshInfoProvider, Size> RefreshVisualizerSizeProperty =
            AvaloniaProperty.RegisterDirect<RefreshInfoProvider, Size>(nameof(RefreshVisualizerSize),
                s => s.RefreshVisualizerSize, (s, o) => s.RefreshVisualizerSize = o);

        /// <summary>
        /// Defines the <see cref="RefreshStarted"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> RefreshStartedEvent =
            RoutedEvent.Register<RefreshInfoProvider, RoutedEventArgs>(nameof(RefreshStarted), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="RefreshCompleted"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> RefreshCompletedEvent =
            RoutedEvent.Register<RefreshInfoProvider, RoutedEventArgs>(nameof(RefreshCompleted), RoutingStrategies.Bubble);

        public bool PeekingMode { get; internal set; }

        public bool IsInteractingForRefresh
        {
            get => _isInteractingForRefresh;
            internal set
            {
                var isInteractingForRefresh = value && !PeekingMode;

                if (isInteractingForRefresh != _isInteractingForRefresh)
                {
                    SetAndRaise(IsInteractingForRefreshProperty, ref _isInteractingForRefresh, isInteractingForRefresh);
                }
            }
        }

        public double InteractionRatio
        {
            get => _interactionRatio;
            set => SetAndRaise(InteractionRatioProperty, ref _interactionRatio, value);
        }

        public Size RefreshVisualizerSize
        {
            get => _refreshVisualizerSize;
            set => SetAndRaise(RefreshVisualizerSizeProperty, ref _refreshVisualizerSize, value);
        }

        public PullDirection PullDirection
        {
            get => _refreshPullDirection;
            set => SetAndRaise(PullDirectionProperty, ref _refreshPullDirection, value);
        }

        public double ExecutionRatio
        {
            get => DefaultExecutionRatio;
        }

        internal CompositionVisual? Visual => _visual;

        public event EventHandler<RoutedEventArgs> RefreshStarted
        {
            add => AddHandler(RefreshStartedEvent, value);
            remove => RemoveHandler(RefreshStartedEvent, value);
        }

        public event EventHandler<RoutedEventArgs> RefreshCompleted
        {
            add => AddHandler(RefreshCompletedEvent, value);
            remove => RemoveHandler(RefreshCompletedEvent, value);
        }

        internal void InteractingStateEntered(object? sender, PullGestureEventArgs e)
        {
            if (!_entered)
            {
                IsInteractingForRefresh = true;
                _entered = true;
            }

            ValuesChanged(e.Delta);
        }

        internal void InteractingStateExited(object? sender, PullGestureEndedEventArgs e)
        {
            IsInteractingForRefresh = false;
            _entered = false;

            ValuesChanged(default);
        }


        public RefreshInfoProvider(PullDirection refreshPullDirection, Size? refreshVIsualizerSize, CompositionVisual? visual)
        {
            _refreshPullDirection = refreshPullDirection;
            _refreshVisualizerSize = refreshVIsualizerSize ?? default;
            _visual = visual;
        }

        public void OnRefreshStarted()
        {
            RaiseEvent(new RoutedEventArgs(RefreshStartedEvent));
        }

        public void OnRefreshCompleted()
        {
            RaiseEvent(new RoutedEventArgs(RefreshCompletedEvent));
        }

        internal void ValuesChanged(Vector value)
        {
            switch (_refreshPullDirection)
            {
                case PullDirection.TopToBottom:
                case PullDirection.BottomToTop:
                    InteractionRatio = _refreshVisualizerSize.Height == 0 ? 1 : Math.Min(1, value.Y / _refreshVisualizerSize.Height);
                    break;
                case PullDirection.LeftToRight:
                case PullDirection.RightToLeft:
                    InteractionRatio = _refreshVisualizerSize.Height == 0 ? 1 : Math.Min(1, value.X / _refreshVisualizerSize.Width);
                    break;
            }
        }
    }
}
