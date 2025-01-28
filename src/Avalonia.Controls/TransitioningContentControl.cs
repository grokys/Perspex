using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Avalonia.Controls;

/// <summary>
/// Displays <see cref="ContentControl.Content"/> according to an <see cref="IDataTemplate"/>,
/// using a <see cref="PageTransition"/> to move between the old and new content. 
/// </summary>
public class TransitioningContentControl : ContentControl
{
    private CancellationTokenSource? _currentTransition;
    private ContentPresenter? _lastPresenter;
    private ContentPresenter? _presenter2;
    private bool _isFirstFull;
    private bool _shouldAnimate;

    /// <summary>
    /// Defines the <see cref="PageTransition"/> property.
    /// </summary>
    public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
        AvaloniaProperty.Register<TransitioningContentControl, IPageTransition?>(
            nameof(PageTransition),
            defaultValue: new ImmutableCrossFade(TimeSpan.FromMilliseconds(125)));

    /// <summary>
    /// Defines the <see cref="IsTransitionReversed"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsTransitionReversedProperty =
        AvaloniaProperty.Register<TransitioningContentControl, bool>(
            nameof(IsTransitionReversed),
            defaultValue: false);

    /// <summary>
    /// Defines the <see cref="TransitionCompleted"/> routed event.
    /// </summary>
    public static readonly RoutedEvent<TransitionCompletedEventArgs> TransitionCompletedEvent =
        RoutedEvent.Register<TransitioningContentControl, TransitionCompletedEventArgs>(
            nameof(TransitionCompleted),
            RoutingStrategies.Direct);

    /// <summary>
    /// Gets or sets the animation played when content appears and disappears.
    /// </summary>
    public IPageTransition? PageTransition
    {
        get => GetValue(PageTransitionProperty);
        set => SetValue(PageTransitionProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the control will be animated in the reverse direction.
    /// </summary>
    /// <remarks>May not apply to all transitions.</remarks>
    public bool IsTransitionReversed
    {
        get => GetValue(IsTransitionReversedProperty);
        set => SetValue(IsTransitionReversedProperty, value);
    }

    /// <summary>
    /// Raised when the old content isn't needed anymore by the control, because the transition has completed.
    /// </summary>
    public event EventHandler<TransitionCompletedEventArgs> TransitionCompleted
    {
        add => AddHandler(TransitionCompletedEvent, value);
        remove => RemoveHandler(TransitionCompletedEvent, value);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var result = base.ArrangeOverride(finalSize);

        if (_shouldAnimate)
        {
            _currentTransition?.Cancel();

            if (_presenter2 is not null &&
                Presenter is { } presenter &&
                PageTransition is { } transition)
            {   
                _shouldAnimate = false;
                
                var cancel = new CancellationTokenSource();
                _currentTransition = cancel;

                var from = _isFirstFull ? _presenter2 : presenter;
                var to = _isFirstFull ? presenter : _presenter2;
                var fromContent = from.Content;
                var toContent = to.Content;

                transition.Start(from, to, !IsTransitionReversed, cancel.Token).ContinueWith(task =>
                {
                    OnTransitionCompleted(new TransitionCompletedEventArgs(
                        fromContent, toContent, task.Status == TaskStatus.RanToCompletion && !cancel.IsCancellationRequested));

                    if (!cancel.IsCancellationRequested)
                    {
                        HideOldPresenter();
                    }
                }, DispatcherTaskScheduler.UIThread);
            }

            _shouldAnimate = false;
        }

        return result;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateContent(false);
    }

    protected override bool RegisterContentPresenter(ContentPresenter presenter)
    {
        if (base.RegisterContentPresenter(presenter))
        {
            return true;
        }

        if (presenter is ContentPresenter p &&
            p.Name == "PART_ContentPresenter2")
        {
            _presenter2 = p;
            _presenter2.IsVisible = false;
            UpdateContent(false);
            return true;
        }

        return false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ContentProperty)
        {
            UpdateContent(true);
            return;
        }

        base.OnPropertyChanged(change);
    }

    private void UpdateContent(bool withTransition)
    {
        if (VisualRoot is null || _presenter2 is null || Presenter is null)
        {
            return;
        }

        var currentPresenter = _isFirstFull ? _presenter2 : Presenter;
        var fromContent = _lastPresenter?.Content;
        var toContent = Content;

        if (_lastPresenter != null &&
            _lastPresenter != currentPresenter &&
            _lastPresenter.Content == toContent)
        {
            _lastPresenter.Content = null;
        }

        currentPresenter.Content = toContent;
        currentPresenter.IsVisible = true;
        _lastPresenter = currentPresenter;

        _isFirstFull = !_isFirstFull;

        if (PageTransition is not null && withTransition)
        {
            _shouldAnimate = true;
            InvalidateArrange();
        }
        else
        {
            HideOldPresenter();
            OnTransitionCompleted(new TransitionCompletedEventArgs(fromContent, toContent, false));
        }
    }

    private void HideOldPresenter()
    {
        var oldPresenter = _isFirstFull ? _presenter2 : Presenter;
        if (oldPresenter is not null)
        {
            oldPresenter.Content = null;
            oldPresenter.IsVisible = false;
        }
    }

    private void OnTransitionCompleted(TransitionCompletedEventArgs e)
        => RaiseEvent(e);

    private class ImmutableCrossFade : IPageTransition
    {
        private readonly CrossFade _inner;

        public ImmutableCrossFade(TimeSpan duration) => _inner = new CrossFade(duration);

        public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
        {
            return _inner.Start(from, to, cancellationToken);
        }
    }
}
