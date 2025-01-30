using System;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.PullToRefresh;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a container control that provides a <see cref="RefreshVisualizer"/> and pull-to-refresh functionality for scrollable content.
    /// </summary>
    public class RefreshContainer : ContentControl
    {
        internal const int DefaultPullDimensionSize = 100;

        private bool _hasDefaultRefreshInfoProviderAdapter;

        private ScrollViewerIRefreshInfoProviderAdapter? _refreshInfoProviderAdapter;
        private RefreshInfoProvider? _refreshInfoProvider;
        private IDisposable? _visualizerSizeSubscription;
        private Grid? _visualizerPresenter;
        private RefreshVisualizer? _refreshVisualizer;
        private bool _hasDefaultRefreshVisualizer;

        /// <summary>
        /// Defines the <see cref="RefreshRequested"/> event.
        /// </summary>
        public static readonly RoutedEvent<RefreshRequestedEventArgs> RefreshRequestedEvent =
            RoutedEvent.Register<RefreshContainer, RefreshRequestedEventArgs>(nameof(RefreshRequested), RoutingStrategies.Bubble);

        internal static readonly DirectProperty<RefreshContainer, ScrollViewerIRefreshInfoProviderAdapter?> RefreshInfoProviderAdapterProperty =
            AvaloniaProperty.RegisterDirect<RefreshContainer, ScrollViewerIRefreshInfoProviderAdapter?>(nameof(RefreshInfoProviderAdapter),
                (s) => s.RefreshInfoProviderAdapter, (s, o) => s.RefreshInfoProviderAdapter = o);

        /// <summary>
        /// Defines the <see cref="Visualizer"/> event.
        /// </summary>
        public static readonly DirectProperty<RefreshContainer, RefreshVisualizer?> VisualizerProperty =
            AvaloniaProperty.RegisterDirect<RefreshContainer, RefreshVisualizer?>(nameof(Visualizer),
                s => s.Visualizer, (s, o) => s.Visualizer = o);

        /// <summary>
        /// Defines the <see cref="PullDirection"/> event.
        /// </summary>
        public static readonly StyledProperty<PullDirection> PullDirectionProperty =
            AvaloniaProperty.Register<RefreshContainer, PullDirection>(nameof(PullDirection), PullDirection.TopToBottom);

        internal ScrollViewerIRefreshInfoProviderAdapter? RefreshInfoProviderAdapter
        {
            get => _refreshInfoProviderAdapter; set
            {
                _hasDefaultRefreshInfoProviderAdapter = false;
                SetAndRaise(RefreshInfoProviderAdapterProperty, ref _refreshInfoProviderAdapter, value);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="RefreshVisualizer"/> for this container.
        /// </summary>
        public RefreshVisualizer? Visualizer
        {
            get => _refreshVisualizer; set
            {
                if (_refreshVisualizer != null)
                {
                    _visualizerSizeSubscription?.Dispose();
                    _refreshVisualizer.RefreshRequested -= Visualizer_RefreshRequested;
                }

                SetAndRaise(VisualizerProperty, ref _refreshVisualizer, value);
            }
        }

        /// <summary>
        /// Gets or sets a value that specifies the direction to pull to initiate a refresh.
        /// </summary>
        public PullDirection PullDirection
        {
            get => GetValue(PullDirectionProperty);
            set => SetValue(PullDirectionProperty, value);
        }

        /// <summary>
        /// Occurs when an update of the content has been initiated.
        /// </summary>
        public event EventHandler<RefreshRequestedEventArgs>? RefreshRequested
        {
            add => AddHandler(RefreshRequestedEvent, value);
            remove => RemoveHandler(RefreshRequestedEvent, value);
        }

        public RefreshContainer()
        {
            _hasDefaultRefreshInfoProviderAdapter = true;
            _refreshInfoProviderAdapter = new ScrollViewerIRefreshInfoProviderAdapter(PullDirection);
            RaisePropertyChanged(RefreshInfoProviderAdapterProperty, null, _refreshInfoProviderAdapter);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _visualizerPresenter = e.NameScope.Find<Grid>("PART_RefreshVisualizerPresenter");

            if (_refreshVisualizer == null)
            {
                _hasDefaultRefreshVisualizer = true;
                Visualizer = new RefreshVisualizer();
            }
            else
            {
                _hasDefaultRefreshVisualizer = false;
                RaisePropertyChanged(VisualizerProperty, default, _refreshVisualizer);
            }

            OnPullDirectionChanged();
        }

        private void OnVisualizerSizeChanged(Rect obj)
        {
            if (_hasDefaultRefreshInfoProviderAdapter && _refreshInfoProviderAdapter != null)
            {                
                _refreshInfoProviderAdapter.UpdateVisualizerSize(obj.Size);
                RaisePropertyChanged(RefreshInfoProviderAdapterProperty, null, _refreshInfoProviderAdapter);
            }
        }

        private void Visualizer_RefreshRequested(object? sender, RefreshRequestedEventArgs e)
        {
            var ev = new RefreshRequestedEventArgs(e.GetDeferral(), RefreshRequestedEvent);
            RaiseEvent(ev);
            ev.DecrementCount();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == RefreshInfoProviderAdapterProperty)
            {
                if (_refreshVisualizer != null)
                {
                    if (_refreshInfoProvider != null)
                    {
                        _refreshVisualizer.RefreshInfoProvider = _refreshInfoProvider;
                    }
                    else
                    {
                        if (RefreshInfoProviderAdapter != null && _refreshVisualizer != null)
                        {
                            _refreshInfoProvider = RefreshInfoProviderAdapter?.AdaptFromTree(this, _refreshVisualizer.Bounds.Size);

                            if (_refreshInfoProvider != null)
                            {
                                _refreshVisualizer.RefreshInfoProvider = _refreshInfoProvider;
                                RefreshInfoProviderAdapter?.SetAnimations(_refreshVisualizer);
                            }
                        }
                    }
                }
            }
            else if (change.Property == VisualizerProperty)
            {
                if (_visualizerPresenter != null)
                {
                    _visualizerPresenter.Children.Clear();
                    if (_refreshVisualizer != null)
                    {
                        _visualizerPresenter.Children.Add(_refreshVisualizer);
                    }
                }

                if (_refreshVisualizer != null)
                {
                    _refreshVisualizer.RefreshRequested += Visualizer_RefreshRequested;
                    _visualizerSizeSubscription = _refreshVisualizer.GetObservable(Control.BoundsProperty).Subscribe(OnVisualizerSizeChanged);
                }
            }
            else if (change.Property == PullDirectionProperty)
            {
                OnPullDirectionChanged();
            }
        }

        private void OnPullDirectionChanged()
        {
            if (_visualizerPresenter != null && _refreshVisualizer != null)
            {
                switch (PullDirection)
                {
                    case PullDirection.TopToBottom:
                        _visualizerPresenter.VerticalAlignment = Layout.VerticalAlignment.Top;
                        _visualizerPresenter.HorizontalAlignment = Layout.HorizontalAlignment.Stretch;
                        if (_hasDefaultRefreshVisualizer)
                        {
                            _refreshVisualizer.PullDirection = PullDirection.TopToBottom;
                            _refreshVisualizer.Height = DefaultPullDimensionSize;
                            _refreshVisualizer.Width = double.NaN;
                        }
                        break;
                    case PullDirection.BottomToTop:
                        _visualizerPresenter.VerticalAlignment = Layout.VerticalAlignment.Bottom;
                        _visualizerPresenter.HorizontalAlignment = Layout.HorizontalAlignment.Stretch;
                        if (_hasDefaultRefreshVisualizer)
                        {
                            _refreshVisualizer.PullDirection = PullDirection.BottomToTop;
                            _refreshVisualizer.Height = DefaultPullDimensionSize;
                            _refreshVisualizer.Width = double.NaN;
                        }
                        break;
                    case PullDirection.LeftToRight:
                        _visualizerPresenter.VerticalAlignment = Layout.VerticalAlignment.Stretch;
                        _visualizerPresenter.HorizontalAlignment = Layout.HorizontalAlignment.Left;
                        if (_hasDefaultRefreshVisualizer)
                        {
                            _refreshVisualizer.PullDirection = PullDirection.LeftToRight;
                            _refreshVisualizer.Width = DefaultPullDimensionSize;
                            _refreshVisualizer.Height = double.NaN;
                        }
                        break;
                    case PullDirection.RightToLeft:
                        _visualizerPresenter.VerticalAlignment = Layout.VerticalAlignment.Stretch;
                        _visualizerPresenter.HorizontalAlignment = Layout.HorizontalAlignment.Right;
                        if (_hasDefaultRefreshVisualizer)
                        {
                            _refreshVisualizer.PullDirection = PullDirection.RightToLeft;
                            _refreshVisualizer.Width = DefaultPullDimensionSize;
                            _refreshVisualizer.Height = double.NaN;
                        }
                        break;
                }

                if (_hasDefaultRefreshInfoProviderAdapter)
                {                
                    if (_hasDefaultRefreshVisualizer)
                    {                   
                        var size = new Size(_refreshVisualizer.Width, _refreshVisualizer.Height);
                        _refreshInfoProviderAdapter?.UpdateVisualizerSize(size);
                    }

                    _refreshInfoProviderAdapter?.UpdatePullDirection(PullDirection);
                    RaisePropertyChanged(RefreshInfoProviderAdapterProperty, null, _refreshInfoProviderAdapter);
                }
            }
        }

        /// <summary>
        /// Initiates an update of the content.
        /// </summary>
        public void RequestRefresh()
        {
            _refreshVisualizer?.RequestRefresh();
        }
    }
}
