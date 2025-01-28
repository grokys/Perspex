using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Avalonia.Compatibility;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;

namespace Avalonia.Rendering.Composition.Server
{
    /// <summary>
    /// Server-side counterpart of the <see cref="Compositor"/>.
    /// 1) manages deserialization of changes received from the UI thread
    /// 2) triggers animation ticks
    /// 3) asks composition targets to render themselves
    /// </summary>
    internal partial class ServerCompositor : IRenderLoopTask
    {
        private readonly IRenderLoop _renderLoop;

        private readonly Queue<CompositionBatch> _batches = new Queue<CompositionBatch>();
        private readonly Queue<Action> _receivedJobQueue = new();
        private readonly Queue<Action> _receivedPostTargetJobQueue = new();
        public long LastBatchId { get; private set; }
        public IAvnTimeProvider TimeProvider { get; }
        public TimeSpan ServerNow { get; private set; }
        private readonly List<ServerCompositionTarget> _activeTargets = new();
        internal BatchStreamObjectPool<object?> BatchObjectPool;
        internal BatchStreamMemoryPool BatchMemoryPool;
        private readonly object _lock = new object();
        private readonly long _serverStartedAt;
        private Thread? _safeThread;
        private bool _uiThreadIsInsideRender;
        public PlatformRenderInterfaceContextManager RenderInterface { get; }
        internal static readonly object RenderThreadDisposeStartMarker = new();
        internal static readonly object RenderThreadJobsStartMarker = new();
        internal static readonly object RenderThreadJobsEndMarker = new();
        internal static readonly object RenderThreadPostTargetJobsStartMarker = new();
        internal static readonly object RenderThreadPostTargetJobsEndMarker = new();
        public CompositionOptions Options { get; }
        public ServerCompositorAnimations Animations { get; }

        public ServerCompositor(IRenderLoop renderLoop, IPlatformGraphics? platformGraphics,
            CompositionOptions options,
            BatchStreamObjectPool<object?> batchObjectPool, BatchStreamMemoryPool batchMemoryPool,
            IAvnTimeProvider timeProvider)
        {
            Options = options;
            Animations = new();
            _renderLoop = renderLoop;
            RenderInterface = new PlatformRenderInterfaceContextManager(platformGraphics);
            RenderInterface.ContextDisposed += RT_OnContextDisposed;
            RenderInterface.ContextCreated += RT_OnContextCreated;
            BatchObjectPool = batchObjectPool;
            BatchMemoryPool = batchMemoryPool;
            TimeProvider = timeProvider;
            _serverStartedAt = TimeProvider.GetTimestamp();
            _renderLoop.Add(this);
        }

        public void EnqueueBatch(CompositionBatch batch)
        {
            lock (_batches) 
                _batches.Enqueue(batch);
        }

        internal void UpdateServerTime() => ServerNow = TimeSpan.FromMilliseconds(
            TimeProvider.GetElapsedMilliseconds(_serverStartedAt, TimeProvider.GetTimestamp()));

        readonly List<CompositionBatch> _reusableToNotifyProcessedList = new();
        readonly List<CompositionBatch> _reusableToNotifyRenderedList = new();

        void ApplyPendingBatches()
        {
            while (true)
            {
                CompositionBatch batch;
                lock (_batches)
                {
                    if(_batches.Count == 0)
                        break;
                    batch = _batches.Dequeue();
                }

                using (var stream = new BatchStreamReader(batch.Changes, BatchMemoryPool, BatchObjectPool))
                {
                    while (!stream.IsObjectEof)
                    {
                        var readObject = stream.ReadObject();
                        if (readObject == RenderThreadJobsStartMarker)
                        {
                            ReadServerJobs(stream, _receivedJobQueue, RenderThreadJobsEndMarker);
                            continue;
                        }
                        if (readObject == RenderThreadPostTargetJobsStartMarker)
                        {
                            ReadServerJobs(stream, _receivedPostTargetJobQueue, RenderThreadPostTargetJobsEndMarker);
                            continue;
                        }

                        if (readObject == RenderThreadDisposeStartMarker)
                        {
                            ReadDisposeJobs(stream);
                            continue;
                        }
                        
                        var target = (SimpleServerObject)readObject!;
                        target.DeserializeChanges(stream, batch);
#if DEBUG_COMPOSITOR_SERIALIZATION
                        if (stream.ReadObject() != BatchStreamDebugMarkers.ObjectEndMarker)
                            throw new InvalidOperationException(
                                $"Object {target.GetType()} failed to deserialize properly on object stream");
                        if(stream.Read<Guid>() != BatchStreamDebugMarkers.ObjectEndMagic)
                            throw new InvalidOperationException(
                                $"Object {target.GetType()} failed to deserialize properly on data stream");
#endif
                    }
                }

                _reusableToNotifyProcessedList.Add(batch);
                LastBatchId = batch.SequenceId;
            }
        }

        void ReadServerJobs(BatchStreamReader reader, Queue<Action> queue, object endMarker)
        {
            object? readObject;
            while ((readObject = reader.ReadObject()) != endMarker)
                queue.Enqueue((Action)readObject!);
        }

        void ReadDisposeJobs(BatchStreamReader reader)
        {
            var count = reader.Read<int>();
            while (count > 0)
            {
                (reader.ReadObject() as IDisposable)?.Dispose();
                count--;
            }
        }

        void ExecuteServerJobs(Queue<Action> queue)
        {
            while(queue.Count > 0)
                try
                {
                    queue.Dequeue()();
                }
                catch
                {
                    // Ignore
                }
        }

        void NotifyBatchesProcessed()
        {
            foreach (var batch in _reusableToNotifyProcessedList) 
                batch.NotifyProcessed();

            foreach (var batch in _reusableToNotifyProcessedList)
                _reusableToNotifyRenderedList.Add(batch);

            _reusableToNotifyProcessedList.Clear();
        }
        
        void NotifyBatchesRendered()
        {
            foreach (var batch in _reusableToNotifyRenderedList) 
                batch.NotifyRendered();

            _reusableToNotifyRenderedList.Clear();
        }

        public void Render() => Render(true);
        public void Render(bool catchExceptions)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                if (_uiThreadIsInsideRender)
                    throw new InvalidOperationException("Reentrancy is not supported");
                _uiThreadIsInsideRender = true;
                try
                {
                    using (Dispatcher.UIThread.DisableProcessing()) 
                        RenderReentrancySafe(catchExceptions);
                }
                finally
                {
                    _uiThreadIsInsideRender = false;
                }
            }
            else
                RenderReentrancySafe(catchExceptions);
        }
        
        private void RenderReentrancySafe(bool catchExceptions)
        {
            lock (_lock)
            {
                try
                {
                    try
                    {
                        _safeThread = Thread.CurrentThread;
                        RenderCore(catchExceptions);
                    }
                    finally
                    {
                        NotifyBatchesRendered();
                    }
                }
                finally
                {
                    _safeThread = null;
                }
            }
        }
        
        private void RenderCore(bool catchExceptions)
        {
            UpdateServerTime();
            ApplyPendingBatches();
            NotifyBatchesProcessed();

            Animations.Process();


            ApplyEnqueuedRenderResourceChanges();
            
            try
            {
                if(!RenderInterface.IsReady)
                    return;
                RenderInterface.EnsureValidBackendContext();
                ExecuteServerJobs(_receivedJobQueue);
                foreach (var t in _activeTargets)
                    t.Render();
                ExecuteServerJobs(_receivedPostTargetJobQueue);
            }
            catch (Exception e) when(RT_OnContextLostExceptionFilterObserver(e) && catchExceptions)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "Exception when rendering: {Error}", e);
            }
        }

        public void AddCompositionTarget(ServerCompositionTarget target)
        {
            _activeTargets.Add(target);
        }

        public void RemoveCompositionTarget(ServerCompositionTarget target)
        {
            _activeTargets.Remove(target);
        }
        
        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            using (RenderInterface.EnsureCurrent())
                return RenderInterface.CreateRenderTarget(surfaces);
        }

        public bool CheckAccess() => _safeThread == Thread.CurrentThread;
        public void VerifyAccess()
        {
            if (!CheckAccess())
                throw new InvalidOperationException("This object can be only accessed under compositor lock");
        }
    }
}
