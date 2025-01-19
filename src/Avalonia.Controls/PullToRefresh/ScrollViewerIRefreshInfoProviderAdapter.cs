using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using Avalonia.VisualTree;

namespace Avalonia.Controls.PullToRefresh
{
    internal class ScrollViewerIRefreshInfoProviderAdapter
    {
        private const int MaxSearchDepth = 10;
        private const int InitialOffsetThreshold = 1;

        private PullDirection _refreshPullDirection;
        private ScrollViewer? _scrollViewer;
        private RefreshInfoProvider? _refreshInfoProvider;
        private ScrollablePullGestureRecognizer? _pullGestureRecognizer;
        private InputElement? _interactionSource;
        private bool _isVisualizerInteractionSourceAttached;

        public ScrollViewerIRefreshInfoProviderAdapter(PullDirection pullDirection)
        {
            _refreshPullDirection = pullDirection;
        }

        public RefreshInfoProvider? AdaptFromTree(Visual root, Size? refreshVIsualizerSize)
        {
            if (root is ScrollViewer scrollViewer)
            {
                return Adapt(scrollViewer, refreshVIsualizerSize);
            }
            else
            {
                int depth = 0;
                while (depth < MaxSearchDepth)
                {
                    var scroll = AdaptFromTreeRecursiveHelper(root, depth);

                    if (scroll != null)
                    {
                        return Adapt(scroll, refreshVIsualizerSize);
                    }

                    depth++;
                }
            }

            ScrollViewer? AdaptFromTreeRecursiveHelper(Visual root, int depth)
            {
                if (depth == 0)
                {
                    foreach (var child in root.VisualChildren)
                    {
                        if (child is ScrollViewer viewer)
                        {
                            return viewer;
                        }
                    }
                }
                else
                {
                    foreach (var child in root.VisualChildren)
                    {
                        var viewer = AdaptFromTreeRecursiveHelper(child, depth - 1);
                        if (viewer != null)
                        {
                            return viewer;
                        }
                    }
                }

                return null;
            }

            return null;
        }

        public RefreshInfoProvider Adapt(ScrollViewer adaptee, Size? refreshVIsualizerSize)
        {
            if (adaptee == null)
            {
                throw new ArgumentNullException(nameof(adaptee), "Adaptee cannot be null");
            }

            if (_scrollViewer != null)
            {
                CleanUpScrollViewer();
            }

            if (_refreshInfoProvider != null && _interactionSource != null)
            {
                _interactionSource.RemoveHandler(Gestures.PullGestureEvent, _refreshInfoProvider.InteractingStateEntered);
                _interactionSource.RemoveHandler(Gestures.PullGestureEndedEvent, _refreshInfoProvider.InteractingStateExited);
            }

            _refreshInfoProvider = null;
            _scrollViewer = adaptee;

            if (_scrollViewer.Content == null)
            {
                throw new ArgumentException("Adaptee's content property cannot be null.", nameof(adaptee));
            }

            var content = adaptee.Content as Visual;

            if (content == null)
            {
                throw new ArgumentException("Adaptee's content property must be a Visual", nameof(adaptee));
            }

            if (content.GetVisualParent() == null)
            {
                _scrollViewer.Loaded += ScrollViewer_Loaded;
            }
            else
            {
                ScrollViewer_Loaded(null, null);

                if (content.Parent is not InputElement)
                {
                    throw new ArgumentException("Adaptee's content's parent must be a InputElement", nameof(adaptee));
                }
            }

            _refreshInfoProvider = new RefreshInfoProvider(_refreshPullDirection, refreshVIsualizerSize, ElementComposition.GetElementVisual(content));

            _pullGestureRecognizer = new ScrollablePullGestureRecognizer(_refreshPullDirection);

            if (_interactionSource != null)
            {
                _interactionSource.GestureRecognizers.Add(_pullGestureRecognizer);
                _interactionSource.AddHandler(Gestures.PullGestureEvent, _refreshInfoProvider.InteractingStateEntered);
                _interactionSource.AddHandler(Gestures.PullGestureEndedEvent, _refreshInfoProvider.InteractingStateExited);
                _isVisualizerInteractionSourceAttached = true;
            }

            _scrollViewer.PointerPressed += ScrollViewer_PointerPressed;
            _scrollViewer.PointerReleased += ScrollViewer_PointerReleased;
            _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;

            return _refreshInfoProvider;
        }

        private void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (_isVisualizerInteractionSourceAttached && _refreshInfoProvider != null && _refreshInfoProvider.IsInteractingForRefresh)
            {
                if (!IsWithinOffsetThreashold())
                {
                    _refreshInfoProvider.IsInteractingForRefresh = false;
                }
            }
        }

        public void SetAnimations(RefreshVisualizer refreshVisualizer)
        {
            var visualizerComposition = ElementComposition.GetElementVisual(refreshVisualizer);
            if (visualizerComposition != null)
            {
                var compositor = visualizerComposition.Compositor;

                var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
                offsetAnimation.Target = "Offset";
                offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
                offsetAnimation.Duration = TimeSpan.FromMilliseconds(150);

                var animation = compositor.CreateImplicitAnimationCollection();
                animation["Offset"] = offsetAnimation;
                visualizerComposition.ImplicitAnimations = animation;
            }

            if(_scrollViewer != null)
            {
                var scollContentComposition = ElementComposition.GetElementVisual(_scrollViewer);

                if(scollContentComposition != null)
                {
                    var compositor = scollContentComposition.Compositor;

                    var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
                    offsetAnimation.Target = "Offset";
                    offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
                    offsetAnimation.Duration = TimeSpan.FromMilliseconds(150);

                    var animation = compositor.CreateImplicitAnimationCollection();
                    animation["Offset"] = offsetAnimation;
                    scollContentComposition.ImplicitAnimations = animation;
                }
            }
        }

        private void ScrollViewer_Loaded(object? sender, RoutedEventArgs? e)
        {
            var content = _scrollViewer?.Content as Visual;
            if (content == null)
            {
                throw new ArgumentException("Adaptee's content property must be a Visual", nameof(_scrollViewer));
            }

            if (content.Parent is not InputElement parent)
            {
                throw new ArgumentException("Adaptee's content parent must be an InputElement", nameof(_scrollViewer));
            }

            MakeInteractionSource(parent);

            if (_scrollViewer != null)
            {
                _scrollViewer.Loaded -= ScrollViewer_Loaded;
            }
        }

        private void MakeInteractionSource(InputElement? element)
        {
            _interactionSource = element;

            if (_pullGestureRecognizer != null && _refreshInfoProvider != null)
            {
                element?.GestureRecognizers.Add(_pullGestureRecognizer);
                _interactionSource?.AddHandler(Gestures.PullGestureEvent, _refreshInfoProvider.InteractingStateEntered);
                _interactionSource?.AddHandler(Gestures.PullGestureEndedEvent, _refreshInfoProvider.InteractingStateExited);
                _isVisualizerInteractionSourceAttached = true;
            }
        }

        private void ScrollViewer_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_refreshInfoProvider != null)
            {
                _refreshInfoProvider.IsInteractingForRefresh = false;
            }
        }

        private void ScrollViewer_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_refreshInfoProvider != null)
            {
                _refreshInfoProvider.PeekingMode = !IsWithinOffsetThreashold();
            }
        }

        private bool IsWithinOffsetThreashold()
        {
            if (_scrollViewer != null)
            {
                var offset = _scrollViewer.Offset;

                switch (_refreshPullDirection)
                {
                    case PullDirection.TopToBottom:
                        return offset.Y < InitialOffsetThreshold;
                    case PullDirection.LeftToRight:
                        return offset.X < InitialOffsetThreshold;
                    case PullDirection.RightToLeft:
                        return offset.X > _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width - InitialOffsetThreshold;
                    case PullDirection.BottomToTop:
                        return offset.Y > _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height - InitialOffsetThreshold;
                }
            }

            return false;
        }

        public void UpdatePullDirection(PullDirection pullDirection)
        {
            _refreshPullDirection = pullDirection;

            if (_refreshInfoProvider != null)
            {
                _refreshInfoProvider.PullDirection = pullDirection;
            }

            if (_pullGestureRecognizer != null)
            {
                _pullGestureRecognizer.PullDirection = pullDirection;
            }
        }

        public void UpdateVisualizerSize(Size? refreshVisualizerSize)
        {
            if (_refreshInfoProvider != null)
            {
                _refreshInfoProvider.RefreshVisualizerSize = refreshVisualizerSize ?? default;
            }
        }

        private void CleanUpScrollViewer()
        {
            if (_scrollViewer != null)
            {
                _scrollViewer.PointerPressed -= ScrollViewer_PointerPressed;
                _scrollViewer.PointerReleased -= ScrollViewer_PointerReleased;
                _scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
            }
        }
    }
}
