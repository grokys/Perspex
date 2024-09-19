using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Layout
{
    /// <summary>
    /// Manages measuring and arranging of controls.
    /// </summary>
    [PrivateApi]
    public class LayoutManager : ILayoutManager, IDisposable
    {
        private const int MaxPasses = 10;
        private readonly Layoutable _owner;
        private readonly LayoutQueue<Layoutable> _toMeasure = new LayoutQueue<Layoutable>(v => !v.IsMeasureValid);
        private readonly LayoutQueue<Layoutable> _toArrange = new LayoutQueue<Layoutable>(v => !v.IsArrangeValid);
        private readonly List<Layoutable> _toArrangeAfterMeasure = new();
        private List<EffectiveViewportChangedListener>? _effectiveViewportChangedListeners;
        private bool _disposed;
        private bool _queued;
        private bool _running;
        private int _totalPassCount;
        private readonly Action _invokeOnRender;

        public LayoutManager(ILayoutRoot owner)
        {
            _owner = owner as Layoutable ?? throw new ArgumentNullException(nameof(owner));
            _invokeOnRender = ExecuteQueuedLayoutPass;
        }

        public virtual event EventHandler? LayoutUpdated;

        /// <inheritdoc/>
        public virtual void InvalidateMeasure(Layoutable control)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            Dispatcher.UIThread.VerifyAccess();

            if (_disposed)
            {
                return;
            }

            if (!control.IsAttachedToVisualTree)
            {
#if DEBUG
                throw new AvaloniaInternalException(
                    "LayoutManager.InvalidateMeasure called on a control that is detached from the visual tree.");
#else
                return;
#endif
            }

            if (control.VisualRoot != _owner)
            {
                throw new ArgumentException("Attempt to call InvalidateMeasure on wrong LayoutManager.");
            }

            _toMeasure.Enqueue(control);
            QueueLayoutPass();
        }

        /// <inheritdoc/>
        public virtual void InvalidateArrange(Layoutable control)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            Dispatcher.UIThread.VerifyAccess();

            if (_disposed)
            {
                return;
            }

            if (!control.IsAttachedToVisualTree)
            {
#if DEBUG
                throw new AvaloniaInternalException(
                    "LayoutManager.InvalidateArrange called on a control that is detached from the visual tree.");
#else
                return;
#endif
            }

            if (control.VisualRoot != _owner)
            {
                throw new ArgumentException("Attempt to call InvalidateArrange on wrong LayoutManager.");
            }

            _toArrange.Enqueue(control);
            QueueLayoutPass();
        }

        internal void ExecuteQueuedLayoutPass()
        {
            if (!_queued)
            {
                return;
            }
            
            ExecuteLayoutPass();
        }

        /// <inheritdoc/>
        public virtual void ExecuteLayoutPass()
        {
            Dispatcher.UIThread.VerifyAccess();

            if (_disposed)
            {
                return;
            }

            if (!_running)
            {
                const LogEventLevel timingLogLevel = LogEventLevel.Information;
                var startingTimestamp = 0L;

                Logger.TryGet(timingLogLevel, LogArea.Layout)?.Log(
                    this,
                    "Started layout pass. To measure: {Measure} To arrange: {Arrange}",
                    _toMeasure.Count,
                    _toArrange.Count);

                _toMeasure.BeginLoop(MaxPasses);
                _toArrange.BeginLoop(MaxPasses);

                try
                {
                    _running = true;
                    ++_totalPassCount;

                    for (var pass = 0; pass < MaxPasses; ++pass)
                    {
                        InnerLayoutPass();

                        if (!RaiseEffectiveViewportChanged())
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    _running = false;
                }

                _toMeasure.EndLoop();
                _toArrange.EndLoop();

                Logger.TryGet(timingLogLevel, LogArea.Layout)?.Log(this, "Layout pass finished in {Time}", StopwatchHelper.GetElapsedTime(startingTimestamp));
            }

            _queued = false;
            LayoutUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public virtual void ExecuteInitialLayoutPass()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _running = true;
                Measure(_owner);
                Arrange(_owner);
            }
            finally
            {
                _running = false;
            }

            // Running the initial layout pass may have caused some control to be invalidated
            // so run a full layout pass now (this usually due to scrollbars; its not known
            // whether they will need to be shown until the layout pass has run and if the
            // first guess was incorrect the layout will need to be updated).
            ExecuteLayoutPass();
        }

        public void Dispose()
        {
            _disposed = true;
            _toMeasure.Dispose();
            _toArrange.Dispose();
        }

        void ILayoutManager.RegisterEffectiveViewportListener(Layoutable control)
        {
            _effectiveViewportChangedListeners ??= new List<EffectiveViewportChangedListener>();
            _effectiveViewportChangedListeners.Add(new EffectiveViewportChangedListener(control));
        }

        void ILayoutManager.UnregisterEffectiveViewportListener(Layoutable control)
        {
            if (_effectiveViewportChangedListeners is object)
            {
                for (var i = _effectiveViewportChangedListeners.Count - 1; i >= 0; --i)
                {
                    if (_effectiveViewportChangedListeners[i].Listener == control)
                    {
                        _effectiveViewportChangedListeners.RemoveAt(i);
                    }
                }
            }
        }

        private void InnerLayoutPass()
        {
            for (var pass = 0; pass < MaxPasses; ++pass)
            {
                ExecuteMeasurePass();
                ExecuteArrangePass();

                if (_toMeasure.Count == 0)
                {
                    break;
                }
            }
        }

        private void ExecuteMeasurePass()
        {
            using var _ = AvaloniaMetrics.BeginVisualMeasure();
            while (_toMeasure.Count > 0)
            {
                var control = _toMeasure.Dequeue();

                if (!control.IsMeasureValid)
                {
                    Measure(control);
                }

                _toArrange.Enqueue(control);
            }
        }

        private void ExecuteArrangePass()
        {
            using var _ = AvaloniaMetrics.BeginVisualArrange();
            while (_toArrange.Count > 0)
            {
                var control = _toArrange.Dequeue();

                if (!control.IsArrangeValid)
                {
                    if (Arrange(control) == ArrangeResult.AncestorMeasureInvalid)
                        _toArrangeAfterMeasure.Add(control);
                }
            }

            foreach (var i in _toArrangeAfterMeasure)
                InvalidateArrange(i);
            _toArrangeAfterMeasure.Clear();
        }

        private bool Measure(Layoutable control)
        {
            if (!control.IsVisible || !control.IsAttachedToVisualTree)
                return false;

            // Controls closest to the visual root need to be arranged first. We don't try to store
            // ordered invalidation lists, instead we traverse the tree upwards, measuring the
            // controls closest to the root first. This has been shown by benchmarks to be the
            // fastest and most memory-efficient algorithm.
            if (control.VisualParent is Layoutable parent)
            {
                if (!Measure(parent))
                    return false;
            }

            // If the control being measured has IsMeasureValid == true here then its measure was
            // handed by an ancestor and can be ignored. The measure may have also caused the
            // control to be removed.
            if (!control.IsMeasureValid)
            {
                if (control is ILayoutRoot root)
                {
                    control.Measure(Size.Infinity);
                }
                else if (control.PreviousMeasure.HasValue)
                {
                    control.Measure(control.PreviousMeasure.Value);
                }
            }

            return true;
        }

        private ArrangeResult Arrange(Layoutable control)
        {
            if (!control.IsVisible || !control.IsAttachedToVisualTree)
                return ArrangeResult.NotVisible;

            if (control.VisualParent is Layoutable parent)
            {
                if (Arrange(parent) is var parentResult && parentResult != ArrangeResult.Arranged)
                    return parentResult;
            }

            if (!control.IsMeasureValid)
                return ArrangeResult.AncestorMeasureInvalid;

            if (!control.IsArrangeValid)
            {
                if (control is IEmbeddedLayoutRoot embeddedRoot)
                    control.Arrange(new Rect(embeddedRoot.AllocatedSize));
                else if (control is ILayoutRoot root)
                    control.Arrange(new Rect(control.DesiredSize));
                else if (control.PreviousArrange != null)
                {
                    // Has been observed that PreviousArrange sometimes is null, probably a bug somewhere else.
                    // Condition observed: control.VisualParent is Scrollbar, control is Border.
                    control.Arrange(control.PreviousArrange.Value);
                }
            }

            return ArrangeResult.Arranged;
        }

        private void QueueLayoutPass()
        {
            if (!_queued && !_running)
            {
                _queued = true;
                MediaContext.Instance.BeginInvokeOnRender(_invokeOnRender);
            }
        }

        private bool RaiseEffectiveViewportChanged()
        {
            var startCount = _toMeasure.Count + _toArrange.Count;

            if (_effectiveViewportChangedListeners is object)
            {
                var count = _effectiveViewportChangedListeners.Count;
                var pool = ArrayPool<EffectiveViewportChangedListener>.Shared;
                var listeners = pool.Rent(count);

                _effectiveViewportChangedListeners.CopyTo(listeners);

                try
                {
                    for (var i = 0; i < count; ++i)
                    {
                        var l = listeners[i];

                        if (!l.Listener.IsAttachedToVisualTree)
                        {
                            continue;
                        }

                        var viewport = CalculateEffectiveViewport(l.Listener);

                        if (viewport != l.Viewport)
                        {
                            l.Listener.RaiseEffectiveViewportChanged(new EffectiveViewportChangedEventArgs(viewport));
                            l.Viewport = viewport;
                        }
                    }
                }
                finally
                {
                    pool.Return(listeners, clearArray: true);
                }
            }

            return startCount != _toMeasure.Count + _toArrange.Count;
        }

        private Rect CalculateEffectiveViewport(Visual control)
        {
            var viewport = new Rect(0, 0, double.PositiveInfinity, double.PositiveInfinity);
            CalculateEffectiveViewport(control, control, ref viewport);
            return viewport;
        }

        private void CalculateEffectiveViewport(Visual target, Visual control, ref Rect viewport)
        {
            // Recurse until the top level control.
            if (control.VisualParent is object)
            {
                CalculateEffectiveViewport(target, control.VisualParent, ref viewport);
            }
            else
            {
                viewport = new Rect(control.Bounds.Size);
            }

            // Apply the control clip bounds if it's not the target control. We don't apply it to
            // the target control because it may itself be clipped to bounds and if so the viewport
            // we calculate would be of no use.
            if (control != target && control.ClipToBounds)
            {
                viewport = control.Bounds.Intersect(viewport);
            }

            // Translate the viewport into this control's coordinate space.
            viewport = viewport.Translate(-control.Bounds.Position);

            if (control != target && control.RenderTransform is { } transform)
            {
                if (transform.Value.TryInvert(out var invertedMatrix))
                {
                    var origin = control.RenderTransformOrigin.ToPixels(control.Bounds.Size);
                    var offset = Matrix.CreateTranslation(origin);
                    viewport = viewport.TransformToAABB(-offset * invertedMatrix * offset);
                }
                else
                    viewport = default;
            }
        }

        private class EffectiveViewportChangedListener
        {
            public EffectiveViewportChangedListener(Layoutable listener)
            {
                Listener = listener;
            }

            public Layoutable Listener { get; }
            public Rect? Viewport { get; set; }
        }

        private enum ArrangeResult
        {
            Arranged,
            NotVisible,
            AncestorMeasureInvalid,
        }
    }
}
