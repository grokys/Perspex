using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Compatibility;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition
{
    /// <summary>
    /// The Compositor class manages communication between UI-thread and render-thread parts of the composition engine.
    /// It also serves as a factory to create UI-thread parts of various composition objects 
    /// </summary>
    public partial class Compositor
    {
        internal IRenderLoop Loop { get; }
        internal bool UseUiThreadForSynchronousCommits { get; }
        private readonly ServerCompositor _server;
        private CompositionBatch? _nextCommit;
        private readonly BatchStreamObjectPool<object?> _batchObjectPool;
        private readonly BatchStreamMemoryPool _batchMemoryPool;
        private readonly Queue<ICompositorSerializable> _objectSerializationQueue = new();
        private readonly HashSet<ICompositorSerializable> _objectSerializationHashSet = new();
        private Queue<Action> _invokeBeforeCommitWrite = new(), _invokeBeforeCommitRead = new();
        private readonly HashSet<IDisposable> _disposeOnNextBatch = new();
        internal ServerCompositor Server => _server;
        private CompositionBatch? _pendingBatch;
        private readonly object _pendingBatchLock = new();
        private readonly List<Action> _pendingServerCompositorJobs = new();
        private readonly List<Action> _pendingServerCompositorPostTargetJobs = new();
        private DiagnosticTextRenderer? _diagnosticTextRenderer;
        private readonly Action _triggerCommitRequested;

        internal IEasing DefaultEasing { get; }

        internal Dispatcher Dispatcher { get; }

        private DiagnosticTextRenderer? DiagnosticTextRenderer
        {
            get
            {
                if (_diagnosticTextRenderer == null)
                {
                    // We are running in some unit test context
                    if (AvaloniaLocator.Current.GetService<IFontManagerImpl>() == null)
                        return null;
                    _diagnosticTextRenderer = new(Typeface.Default.GlyphTypeface, 12.0);
                }

                return _diagnosticTextRenderer;
            }
        }

        internal event Action? AfterCommit;

        
        /// <summary>
        /// Creates a new compositor on a specified render loop that would use a particular GPU
        /// </summary>
        [PrivateApi]
        public Compositor(IPlatformGraphics? gpu, bool useUiThreadForSynchronousCommits = false)
            : this(RenderLoop.LocatorAutoInstance, gpu, useUiThreadForSynchronousCommits)
        {
        }

        internal Compositor(IRenderLoop loop, IPlatformGraphics? gpu, bool useUiThreadForSynchronousCommits = false, IAvnTimeProvider? timeProvider = null)
            : this(loop, gpu, useUiThreadForSynchronousCommits, MediaContext.Instance, false, Dispatcher.UIThread, timeProvider)
        {
        }

        internal Compositor(IRenderLoop loop, IPlatformGraphics? gpu,
            bool useUiThreadForSynchronousCommits,
            ICompositorScheduler scheduler, bool reclaimBuffersImmediately,
            Dispatcher dispatcher, IAvnTimeProvider? timeProvider = null, CompositionOptions? options = null)
        {
            options ??= AvaloniaLocator.Current.GetService<CompositionOptions>() ?? new();
            Loop = loop;
            UseUiThreadForSynchronousCommits = useUiThreadForSynchronousCommits;
            Dispatcher = dispatcher;
            _batchMemoryPool = new(reclaimBuffersImmediately);
            _batchObjectPool = new(reclaimBuffersImmediately);
            _server = new ServerCompositor(loop, gpu, options, _batchObjectPool, _batchMemoryPool, timeProvider ?? new StopwatchTimeProvider());
            _triggerCommitRequested = () => scheduler.CommitRequested(this);

            DefaultEasing = new SplineEasing(new KeySpline(0.25, 0.1, 0.25, 1.0));
        }

        /// <summary>
        /// Requests pending changes in the composition objects to be serialized and sent to the render thread
        /// </summary>
        /// <returns>A task that completes when sent changes are applied on the render thread</returns>
        public Task RequestCommitAsync() => RequestCompositionBatchCommitAsync().Processed;

        /// <summary>
        /// Requests pending changes in the composition objects to be serialized and sent to the render thread
        /// </summary>
        /// <returns>A CompositionBatch object that provides batch lifetime information</returns>
        public CompositionBatch RequestCompositionBatchCommitAsync()
        {
            Dispatcher.VerifyAccess();
            if (_nextCommit == null)
            {
                using var _ = NonPumpingLockHelper.Use();
                _nextCommit = new ();
                var pending = _pendingBatch;
                if (pending != null)
                    pending.Processed.ContinueWith(
                        _ => Dispatcher.Post(_triggerCommitRequested, DispatcherPriority.Send),
                        TaskContinuationOptions.ExecuteSynchronously);
                else
                    _triggerCommitRequested();
            }

            return _nextCommit;
        }

        internal CompositionBatch Commit()
        {
            try
            {
                return CommitCore();
            }
            finally
            {
                if (_invokeBeforeCommitWrite.Count > 0)
                    RequestCommitAsync();
                AfterCommit?.Invoke();
            }
        }
        
        CompositionBatch CommitCore()
        {
            Dispatcher.VerifyAccess();
            using var noPump = NonPumpingLockHelper.Use();
            
            var commit = _nextCommit ??= new();

            (_invokeBeforeCommitRead, _invokeBeforeCommitWrite) = (_invokeBeforeCommitWrite, _invokeBeforeCommitRead);
            while (_invokeBeforeCommitRead.Count > 0)
                _invokeBeforeCommitRead.Dequeue()();
            
            using (var writer = new BatchStreamWriter(_nextCommit.Changes, _batchMemoryPool, _batchObjectPool))
            {
                while(_objectSerializationQueue.TryDequeue(out var obj))
                {
                    var serverObject = obj.TryGetServer(this);
                    if (serverObject != null)
                    {
                        writer.WriteObject(serverObject);
                        obj.SerializeChanges(this, writer);
#if DEBUG_COMPOSITOR_SERIALIZATION
                        writer.Write(BatchStreamDebugMarkers.ObjectEndMagic);
                        writer.WriteObject(BatchStreamDebugMarkers.ObjectEndMarker);
#endif
                    }
                }
                _objectSerializationHashSet.Clear();

                if (_disposeOnNextBatch.Count != 0)
                {
                    writer.WriteObject(ServerCompositor.RenderThreadDisposeStartMarker);
                    writer.Write(_disposeOnNextBatch.Count);
                    foreach (var d in _disposeOnNextBatch)
                        writer.WriteObject(d);
                    _disposeOnNextBatch.Clear();
                }

                
                static void SerializeServerJobs(BatchStreamWriter writer, List<Action> list, object startMarker, object endMarker)
                {
                    if (list.Count > 0)
                    {
                        writer.WriteObject(startMarker);
                        foreach (var job in list)
                            writer.WriteObject(job);
                        writer.WriteObject(endMarker);
                    }
                    list.Clear();
                }

                SerializeServerJobs(writer, _pendingServerCompositorJobs, ServerCompositor.RenderThreadJobsStartMarker,
                    ServerCompositor.RenderThreadJobsEndMarker);
                SerializeServerJobs(writer, _pendingServerCompositorPostTargetJobs, ServerCompositor.RenderThreadPostTargetJobsStartMarker,
                    ServerCompositor.RenderThreadPostTargetJobsEndMarker);
            }

            _nextCommit.CommittedAt = Server.ServerNow;
            _server.EnqueueBatch(_nextCommit);
            
            lock (_pendingBatchLock)
            {
                _pendingBatch = _nextCommit;
                _pendingBatch.Processed.ContinueWith(t =>
                {
                    lock (_pendingBatchLock)
                    {
                        if (_pendingBatch.Processed == t)
                            _pendingBatch = null;
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
                _nextCommit = null;
                
                return commit;
            }
        }

        internal void RegisterForSerialization(ICompositorSerializable compositionObject)
        {
            Dispatcher.VerifyAccess();
            if(_objectSerializationHashSet.Add(compositionObject))
                _objectSerializationQueue.Enqueue(compositionObject);
            RequestCommitAsync();
        }

        internal void DisposeOnNextBatch(SimpleServerObject obj)
        {
            if (obj is IDisposable disposable && _disposeOnNextBatch.Add(disposable))
                RequestCommitAsync();
        }
        
        /// <summary>
        /// Enqueues a callback to be called before the next scheduled commit.
        /// If there is no scheduled commit it automatically schedules one
        /// This is useful for updating your composition tree objects after binding
        /// and layout passes have completed
        /// </summary>
        public void RequestCompositionUpdate(Action action)
        {
            Dispatcher.VerifyAccess();
            _invokeBeforeCommitWrite.Enqueue(action);
            RequestCommitAsync();
        }

        internal void PostServerJob(Action job, bool postTarget = false)
        {
            Dispatcher.VerifyAccess();
            (postTarget ? _pendingServerCompositorPostTargetJobs : _pendingServerCompositorJobs).Add(job);
            RequestCommitAsync();
        }

        internal Task InvokeServerJobAsync(Action job, bool postTarget = false) =>
            InvokeServerJobAsync<object?>(() =>
            {
                job();
                return null;
            }, postTarget);

        internal Task<T> InvokeServerJobAsync<T>(Func<T> job, bool postTarget = false)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            PostServerJob(() =>
            {
                try
                {
                    tcs.SetResult(job());
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            }, postTarget);
            return tcs.Task;
        }

        internal ValueTask<IReadOnlyDictionary<Type, object>> GetRenderInterfacePublicFeatures()
        {
            if (Server.AT_TryGetCachedRenderInterfaceFeatures() is { } rv)
                return new(rv);
            if (!Loop.RunsInBackground)
                return new(Server.RT_GetRenderInterfaceFeatures());
            return new(InvokeServerJobAsync(Server.RT_GetRenderInterfaceFeatures));
        }

        /// <summary>
        /// Attempts to query for a feature from the platform render interface
        /// </summary>
        public async ValueTask<object?> TryGetRenderInterfaceFeature(Type featureType)
        {
            (await GetRenderInterfacePublicFeatures().ConfigureAwait(false)).TryGetValue(featureType, out var rv);
            return rv;
        }

        public async Task<Bitmap> CreateCompositionVisualSnapshot(CompositionVisual visual, double scaling)
        {
            if (visual.Compositor != this)
                throw new InvalidOperationException();
            if (visual.Root == null)
                throw new InvalidOperationException();
            var impl = await InvokeServerJobAsync(() => _server.CreateCompositionVisualSnapshot(visual.Server, scaling, true), true);
            return new Bitmap(RefCountable.Create(impl));
        }
        
        /// <summary>
        /// Attempts to query for GPU interop feature from the platform render interface
        /// </summary>
        /// <returns></returns>
        public async ValueTask<ICompositionGpuInterop?> TryGetCompositionGpuInterop()
        {
            var externalObjects =
                (IExternalObjectsRenderInterfaceContextFeature?)await TryGetRenderInterfaceFeature(
                    typeof(IExternalObjectsRenderInterfaceContextFeature)).ConfigureAwait(false);

            if (externalObjects == null)
                return null;
            return new CompositionInterop(this, externalObjects);
        }

        internal bool UnitTestIsRegisteredForSerialization(ICompositorSerializable serializable) =>
            _objectSerializationHashSet.Contains(serializable);

        /// <summary>
        /// Attempts to get the Compositor instance that will be used by default for new TopLevels
        /// created by the current platform backend.
        ///
        /// This won't work for every single platform backend and backend settings, e. g. with web we'll need to have
        /// separate Compositor instances per output HTML canvas since they don't share OpenGL state.
        /// Another case where default compositor won't be available is our planned multithreaded rendering mode
        /// where each window would get its own Compositor instance
        ///
        /// This method is still useful for obtaining GPU device LUID to speed up initialization, but you should
        /// always check if default Compositor matches one used by our control once it gets attached to a TopLevel
        /// </summary>
        /// <returns></returns>
        public static Compositor? TryGetDefaultCompositor() => AvaloniaLocator.Current.GetService<Compositor>();
    }
    
    internal interface ICompositorScheduler
    {
        void CommitRequested(Compositor compositor);
    }
}
