using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Animation;
using Avalonia.Reactive;

namespace Avalonia.Media;

internal partial class MediaContext
{
    private readonly MediaContextClock _clock;
    public IGlobalClock Clock => _clock;

    class MediaContextClock : IGlobalClock
    {
        private readonly MediaContext _parent;
        private readonly List<IObserver<TimeSpan>> _observers = new();
        private readonly List<IObserver<TimeSpan>> _newObservers = new();
        private Queue<Action<TimeSpan>> _queuedAnimationFrames = new();
        private Queue<Action<TimeSpan>> _queuedAnimationFramesNext = new();
        private TimeSpan _currentAnimationTimestamp;
        public bool HasNewSubscriptions => _newObservers.Count > 0;
        public bool HasSubscriptions => _observers.Count > 0 || _queuedAnimationFrames.Count > 0;

        public MediaContextClock(MediaContext parent)
        {
            _parent = parent;
        }

        public IDisposable Subscribe(IObserver<TimeSpan> observer)
        {
            _parent.ScheduleRender(false);
            _parent._dispatcher.VerifyAccess();
            _observers.Add(observer);
            _newObservers.Add(observer);
            return Disposable.Create(() =>
            {
                _parent._dispatcher.VerifyAccess();
                _observers.Remove(observer);
            });
        }
        
        public void RequestAnimationFrame(Action<TimeSpan> action)
        {
            _parent.ScheduleRender(false);
            _queuedAnimationFrames.Enqueue(action);
        }

        public void Pulse(TimeSpan now)
        {
            _newObservers.Clear();
            _currentAnimationTimestamp = now;
            
            // We are swapping the queues before enumeration
            (_queuedAnimationFrames, _queuedAnimationFramesNext) = (_queuedAnimationFramesNext, _queuedAnimationFrames);
            var animationFrames = _queuedAnimationFramesNext;
            while (animationFrames.TryDequeue(out var callback))
                callback(now);
            
            foreach (var observer in _observers.ToArray())
                observer.OnNext(_currentAnimationTimestamp);
        }
        
        public void PulseNewSubscriptions()
        {
            foreach (var observer in _newObservers.ToArray())
                observer.OnNext(_currentAnimationTimestamp);
            _newObservers.Clear();
        }

        public PlayState PlayState
        {
            get => PlayState.Run;
            set => throw new InvalidOperationException();
        }
    }

    public void RequestAnimationFrame(Action<TimeSpan> action) => _clock.RequestAnimationFrame(action);
}
