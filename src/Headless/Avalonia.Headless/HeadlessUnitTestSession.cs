using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.Threading;

namespace Avalonia.Headless;

/// <summary>
/// Headless unit test session that needs to be used by the actual testing framework.
/// All UI tests are supposed to be executed from one of the <see cref="Dispatch"/> methods to keep execution flow on the UI thread.
/// Disposing unit test session stops internal dispatcher loop. 
/// </summary>
[Unstable("This API is experimental and might be unstable. Use on your risk. API might or might not be changed in a minor update.")]
public sealed class HeadlessUnitTestSession : IDisposable
{
    private static readonly Dictionary<Assembly, HeadlessUnitTestSession> s_session = new();

    private readonly AppBuilder _appBuilder;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly BlockingCollection<(Action, ExecutionContext?)> _queue;
    private readonly Task _dispatchTask;

    internal const DynamicallyAccessedMemberTypes DynamicallyAccessed =
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods |
        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

    private HeadlessUnitTestSession(AppBuilder appBuilder, CancellationTokenSource cancellationTokenSource,
        BlockingCollection<(Action, ExecutionContext?)> queue, Task dispatchTask)
    {
        _appBuilder = appBuilder;
        _cancellationTokenSource = cancellationTokenSource;
        _queue = queue;
        _dispatchTask = dispatchTask;
    }

    /// <inheritdoc cref="DispatchCore{TResult}"/>
    public Task Dispatch(Action action, CancellationToken cancellationToken)
    {
        return DispatchCore(() =>
        {
            action();
            return Task.FromResult(0);
        }, false ,cancellationToken);
    }

    /// <inheritdoc cref="DispatchCore{TResult}"/>
    public Task<TResult> Dispatch<TResult>(Func<TResult> action, CancellationToken cancellationToken)
    {
        return DispatchCore(() => Task.FromResult(action()), false, cancellationToken);
    }

    /// <inheritdoc cref="DispatchCore{TResult}"/>
    public Task<TResult> Dispatch<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken)
    {
        return DispatchCore(action, false, cancellationToken);
    }

    /// <summary>
    /// Dispatch method queues an async operation on the dispatcher thread, creates a new application instance,
    /// setting app avalonia services, and runs <paramref name="action"/> parameter.
    /// </summary>
    /// <param name="action">Action to execute on the dispatcher thread with avalonia services.</param>
    /// <param name="captureExecutionContext">Whether dispatch should capture ExecutionContext.</param>
    /// <param name="cancellationToken">Cancellation token to cancel execution.</param>
    /// <exception cref="ObjectDisposedException">
    /// If global session was already cancelled and thread killed, it's not possible to dispatch any actions again
    /// </exception>
    internal Task<TResult> DispatchCore<TResult>(Func<Task<TResult>> action, bool captureExecutionContext, CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            throw new ObjectDisposedException("Session was already disposed.");
        }

        var token = _cancellationTokenSource.Token;
        var executionContext = captureExecutionContext ? ExecutionContext.Capture() : null;

        var tcs = new TaskCompletionSource<TResult>();
        _queue.Add((() =>
        {
            var cts = new CancellationTokenSource();
            using var globalCts = token.Register(s => ((CancellationTokenSource)s!).Cancel(), cts, true);
            using var localCts = cancellationToken.Register(s => ((CancellationTokenSource)s!).Cancel(), cts, true);

            try
            {
                using var application = EnsureApplication();
                var task = action();
                if (task.Status != TaskStatus.RanToCompletion)
                {
                    task.ContinueWith((_, s) =>
                            ((CancellationTokenSource)s!).Cancel(), cts,
                        TaskScheduler.FromCurrentSynchronizationContext());

                    if (cts.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(cts.Token);
                        return;
                    }

                    var frame = new DispatcherFrame();
                    using var innerCts = cts.Token.Register(() => frame.Continue = false, true);
                    Dispatcher.UIThread.PushFrame(frame);
                }

                var result = task.GetAwaiter().GetResult();
                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, executionContext));
        return tcs.Task;
    }

    private IDisposable EnsureApplication()
    {
        var scope = AvaloniaLocator.EnterScope();
        try
        {
            Dispatcher.ResetForUnitTests();
            _appBuilder.SetupUnsafe();
        }
        catch
        {
            scope.Dispose();
            throw;
        }

        return Disposable.Create(() =>
        {
            HeadlessTimeProvider.GetCurrent().Reset();
            scope.Dispose();
            Dispatcher.ResetForUnitTests();
            SynchronizationContext.SetSynchronizationContext(null);
        });
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _queue.CompleteAdding();
        _dispatchTask.Wait();
        _cancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Creates instance of <see cref="HeadlessUnitTestSession"/>. 
    /// </summary>
    /// <param name="entryPointType">
    /// Parameter from which <see cref="AppBuilder"/> should be created.
    /// It either needs to have BuildAvaloniaApp -> AppBuilder method or inherit Application.
    /// </param>
    public static HeadlessUnitTestSession StartNew(
        [DynamicallyAccessedMembers(DynamicallyAccessed)]
        Type entryPointType)
    {
        var tcs = new TaskCompletionSource<HeadlessUnitTestSession>();
        var cancellationTokenSource = new CancellationTokenSource();
        var queue = new BlockingCollection<(Action, ExecutionContext?)>();

        Task? task = null;
        task = Task.Run(() =>
        {
            try
            {
                var appBuilder = AppBuilder.Configure(entryPointType);

                // If windowing subsystem wasn't initialized by user, force headless with default parameters.
                if (appBuilder.WindowingSubsystemName != "Headless")
                {
                    appBuilder = appBuilder.UseHeadless(new AvaloniaHeadlessPlatformOptions());
                }

                // ReSharper disable once AccessToModifiedClosure
                tcs.SetResult(new HeadlessUnitTestSession(appBuilder, cancellationTokenSource, queue, task!));
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return;
            }

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var (action, executionContext) = queue.Take(cancellationTokenSource.Token);
                    if (executionContext is not null)
                    {
                        ExecutionContext.Run(executionContext, a => ((Action)a!).Invoke(), action);
                    }
                    else
                    {
                        action();   
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }
        });

        return tcs.Task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Creates a session from AvaloniaTestApplicationAttribute attribute or reuses any existing.
    /// If AvaloniaTestApplicationAttribute doesn't exist, empty application is used. 
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "AvaloniaTestApplicationAttribute attribute should preserve type information.")]
    public static HeadlessUnitTestSession GetOrStartForAssembly(Assembly? assembly)
    {
        assembly ??= typeof(HeadlessUnitTestSession).Assembly;

        lock (s_session)
        {
            if (!s_session.TryGetValue(assembly, out var session))
            {
                var appBuilderEntryPointType = assembly.GetCustomAttribute<AvaloniaTestApplicationAttribute>()
                    ?.AppBuilderEntryPointType;

                session = appBuilderEntryPointType is not null ?
                    StartNew(appBuilderEntryPointType) :
                    StartNew(typeof(Application));

                s_session.Add(assembly, session);
            }

            return session;
        }
    }
}
