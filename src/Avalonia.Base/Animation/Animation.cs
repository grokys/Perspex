using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Data;
using Avalonia.Metadata;
using Avalonia.Threading;

namespace Avalonia.Animation
{
    /// <summary>
    /// Tracks the progress of an animation.
    /// </summary>
    public sealed partial class Animation : AvaloniaObject, IAnimation
    {
        /// <summary>
        /// Defines the <see cref="Duration"/> property.
        /// </summary>
        public static readonly DirectProperty<Animation, TimeSpan> DurationProperty =
            AvaloniaProperty.RegisterDirect<Animation, TimeSpan>(
                nameof(Duration),
                o => o._duration,
                (o, v) => o._duration = v);

        /// <summary>
        /// Defines the <see cref="IterationCount"/> property.
        /// </summary>
        public static readonly DirectProperty<Animation, IterationCount> IterationCountProperty =
            AvaloniaProperty.RegisterDirect<Animation, IterationCount>(
                nameof(IterationCount),
                o => o._iterationCount,
                (o, v) => o._iterationCount = v);

        /// <summary>
        /// Defines the <see cref="PlaybackDirection"/> property.
        /// </summary>
        public static readonly DirectProperty<Animation, PlaybackDirection> PlaybackDirectionProperty =
            AvaloniaProperty.RegisterDirect<Animation, PlaybackDirection>(
                nameof(PlaybackDirection),
                o => o._playbackDirection,
                (o, v) => o._playbackDirection = v);

        /// <summary>
        /// Defines the <see cref="FillMode"/> property.
        /// </summary>
        public static readonly DirectProperty<Animation, FillMode> FillModeProperty =
            AvaloniaProperty.RegisterDirect<Animation, FillMode>(
                nameof(FillMode),
                o => o._fillMode,
                (o, v) => o._fillMode = v);

        /// <summary>
        /// Defines the <see cref="Easing"/> property.
        /// </summary>
        public static readonly DirectProperty<Animation, Easing> EasingProperty =
            AvaloniaProperty.RegisterDirect<Animation, Easing>(
                nameof(Easing),
                o => o._easing,
                (o, v) => o._easing = v);

        /// <summary>
        /// Defines the <see cref="Delay"/> property.
        /// </summary>
        public static readonly DirectProperty<Animation, TimeSpan> DelayProperty =
            AvaloniaProperty.RegisterDirect<Animation, TimeSpan>(
                nameof(Delay),
                o => o._delay,
                (o, v) => o._delay = v);

        /// <summary>
        /// Defines the <see cref="DelayBetweenIterations"/> property.
        /// </summary>
        public static readonly DirectProperty<Animation, TimeSpan> DelayBetweenIterationsProperty =
            AvaloniaProperty.RegisterDirect<Animation, TimeSpan>(
                nameof(DelayBetweenIterations),
                o => o._delayBetweenIterations,
                (o, v) => o._delayBetweenIterations = v);

        /// <summary>
        /// Defines the <see cref="SpeedRatio"/> property.
        /// </summary>
        public static readonly DirectProperty<Animation, double> SpeedRatioProperty =
            AvaloniaProperty.RegisterDirect<Animation, double>(
                nameof(SpeedRatio),
                o => o._speedRatio,
                (o, v) => o._speedRatio = v,
                defaultBindingMode: BindingMode.TwoWay);

        private TimeSpan _duration;
        private IterationCount _iterationCount = new IterationCount(1);
        private PlaybackDirection _playbackDirection;
        private FillMode _fillMode;
        private Easing _easing = new LinearEasing();
        private TimeSpan _delay = TimeSpan.Zero;
        private TimeSpan _delayBetweenIterations = TimeSpan.Zero;
        private double _speedRatio = 1d;

        /// <summary>
        /// Gets or sets the active time of this animation.
        /// </summary>
        public TimeSpan Duration
        {
            get { return _duration; }
            set { SetAndRaise(DurationProperty, ref _duration, value); }
        }

        /// <summary>
        /// Gets or sets the repeat count for this animation.
        /// </summary>
        public IterationCount IterationCount
        {
            get { return _iterationCount; }
            set { SetAndRaise(IterationCountProperty, ref _iterationCount, value); }
        }

        /// <summary>
        /// Gets or sets the playback direction for this animation.
        /// </summary>
        public PlaybackDirection PlaybackDirection
        {
            get { return _playbackDirection; }
            set { SetAndRaise(PlaybackDirectionProperty, ref _playbackDirection, value); }
        }

        /// <summary>
        /// Gets or sets the value fill mode for this animation.
        /// </summary> 
        public FillMode FillMode
        {
            get { return _fillMode; }
            set { SetAndRaise(FillModeProperty, ref _fillMode, value); }
        }

        /// <summary>
        /// Gets or sets the easing function to be used for this animation.
        /// </summary>
        public Easing Easing
        {
            get { return _easing; }
            set { SetAndRaise(EasingProperty, ref _easing, value); }
        }

        /// <summary> 
        /// Gets or sets the initial delay time for this animation. 
        /// </summary> 
        public TimeSpan Delay
        {
            get { return _delay; }
            set { SetAndRaise(DelayProperty, ref _delay, value); }
        }

        /// <summary> 
        /// Gets or sets the delay time in between iterations.
        /// </summary> 
        public TimeSpan DelayBetweenIterations
        {
            get { return _delayBetweenIterations; }
            set { SetAndRaise(DelayBetweenIterationsProperty, ref _delayBetweenIterations, value); }
        }

        /// <summary>
        /// Gets or sets the speed multiple for this animation.
        /// </summary> 
        public double SpeedRatio
        {
            get { return _speedRatio; }
            set { SetAndRaise(SpeedRatioProperty, ref _speedRatio, value); }
        }

        /// <summary>
        /// Gets the children of the <see cref="Animation"/>.
        /// </summary>
        [Content]
        public KeyFrames Children { get; } = new KeyFrames();

        // Store values for the Animator attached properties for IAnimationSetter objects.
        private static readonly Dictionary<IAnimationSetter, (Type Type, Func<IAnimator> Factory)> s_animators = new();

        /// <summary>
        /// Gets the value of the Animator attached property for a setter.
        /// </summary>
        /// <param name="setter">The animation setter.</param>
        /// <returns>The property animator type.</returns>
        internal static (Type Type, Func<IAnimator> Factory)? GetAnimator(IAnimationSetter setter)
        {
            if (s_animators.TryGetValue(setter, out var type))
            {
                return type;
            }
            return null;
        }

        private (IList<IAnimator> Animators, IList<IDisposable> subscriptions) InterpretKeyframes(Animatable control)
        {
            var handlerList = new Dictionary<(Type type, AvaloniaProperty Property), Func<IAnimator>>();
            var animatorKeyFrames = new List<AnimatorKeyFrame>();
            var subscriptions = new List<IDisposable>();

            foreach (var keyframe in Children)
            {
                foreach (var setter in keyframe.Setters)
                {
                    if (setter.Property is null)
                    {
                        throw new InvalidOperationException("No Setter property assigned.");
                    }

                    var handler = Animation.GetAnimator(setter) ?? GetAnimatorType(setter.Property);

                    if (handler == null)
                    {
                        throw new InvalidOperationException($"No animator registered for the property {setter.Property}. Add an animator to the Animation.Animators collection that matches this property to animate it.");
                    }

                    var (type, factory) = handler.Value;

                    if (!handlerList.ContainsKey((type, setter.Property)))
                        handlerList[(type, setter.Property)] = factory;

                    var cue = keyframe.Cue;

                    if (keyframe.TimingMode == KeyFrameTimingMode.TimeSpan)
                    {
                        cue = new Cue(keyframe.KeyTime.TotalSeconds / Duration.TotalSeconds);
                    }

                    var newKF = new AnimatorKeyFrame(type, factory, cue, keyframe.KeySpline);

                    subscriptions.Add(newKF.BindSetter(setter, control));

                    animatorKeyFrames.Add(newKF);
                }
            }

            animatorKeyFrames.Sort(static (x, y) => x.Cue.CueValue.CompareTo(y.Cue.CueValue));

            var newAnimatorInstances = new List<IAnimator>();

            foreach (var handler in handlerList)
            {
                var newInstance = handler.Value();
                newInstance.Property = handler.Key.Property;
                newAnimatorInstances.Add(newInstance);
            }

            foreach (var keyframe in animatorKeyFrames)
            {
                var animator = newAnimatorInstances.First(a => a.GetType() == keyframe.AnimatorType &&
                                                             a.Property == keyframe.Property);

                if (animator.Count == 0 && FillMode is FillMode.Backward or FillMode.Both)
                    keyframe.FillBefore = true;

                animator.Add(keyframe);
            }

            if (FillMode is FillMode.Forward or FillMode.Both)
            {
                foreach (var newAnimatorInstance in newAnimatorInstances)
                {
                    if (newAnimatorInstance.Count > 0)
                        newAnimatorInstance[newAnimatorInstance.Count - 1].FillAfter = true;
                }
            }

            return (newAnimatorInstances, subscriptions);
        }

        IDisposable IAnimation.Apply(Animatable control, IClock? clock, IObservable<bool> match, Action? onComplete)
            => Apply(control, clock, match, onComplete);
        
        /// <inheritdoc/>
        internal IDisposable Apply(Animatable control, IClock? clock, IObservable<bool> match, Action? onComplete)
        {
            var (animators, subscriptions) = InterpretKeyframes(control);
            if (animators.Count == 1)
            {
                var subscription = animators[0].Apply(this, control, clock, match, onComplete);

                if (subscription is not null)
                {
                    subscriptions.Add(subscription);
                }
            }
            else
            {
                var completionTasks = onComplete != null ? new List<Task>() : null;
                foreach (IAnimator animator in animators)
                {
                    Action? animatorOnComplete = null;
                    if (onComplete != null)
                    {
                        var tcs = new TaskCompletionSource<object?>();
                        animatorOnComplete = () => tcs.SetResult(null);
                        completionTasks!.Add(tcs.Task);
                    }

                    var subscription = animator.Apply(this, control, clock, match, animatorOnComplete);

                    if (subscription is not null)
                    {
                        subscriptions.Add(subscription);
                    }
                }

                if (onComplete != null)
                {
                    Task.WhenAll(completionTasks!)
                        .ContinueWith((_, state) => ((Action)state!).Invoke()
                            , onComplete
                            , DispatcherTaskScheduler.UIThread
                            );
                }
            }
            return new CompositeDisposable(subscriptions);
        }

        public Task RunAsync(Animatable control, CancellationToken cancellationToken = default) =>
            RunAsync(control, null, cancellationToken);
        
        /// <inheritdoc/>
        internal Task RunAsync(Animatable control, IClock? clock)
        {
            return RunAsync(control, clock, default);
        }

        Task IAnimation.RunAsync(Animatable control, IClock? clock, CancellationToken cancellationToken)
            => RunAsync(control, clock, cancellationToken);
        
        /// <inheritdoc/>
        internal Task RunAsync(Animatable control, IClock? clock, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            var run = new TaskCompletionSource<object?>();

            if (this.IterationCount == IterationCount.Infinite)
                run.SetException(new InvalidOperationException("Looping animations must not use the Run method."));

            IDisposable? subscriptions = null, cancellation = null;
            subscriptions = this.Apply(control, clock, Observable.Return(true), () =>
            {
                run.TrySetResult(null);
                subscriptions?.Dispose();
                cancellation?.Dispose();
            });

            cancellation = cancellationToken.Register(() =>
            {
                run.TrySetResult(null);
                subscriptions?.Dispose();
                cancellation?.Dispose();
            });

            return run.Task;
        }
    }
}
