using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Threading;

public partial class Dispatcher
{
    /// <summary>
    ///     Executes the specified Action synchronously on the thread that
    ///     the Dispatcher was created on.
    /// </summary>
    /// <param name="callback">
    ///     An Action delegate to invoke through the dispatcher.
    /// </param>
    /// <remarks>
    ///     Note that the default priority is DispatcherPriority.Send.
    /// </remarks>
    public void Invoke(Action callback)
    {
        Invoke(callback, DispatcherPriority.Send, CancellationToken.None, TimeSpan.FromMilliseconds(-1));
    }

    /// <summary>
    ///     Executes the specified Action synchronously on the thread that
    ///     the Dispatcher was created on.
    /// </summary>
    /// <param name="callback">
    ///     An Action delegate to invoke through the dispatcher.
    /// </param>
    /// <param name="priority">
    ///     The priority that determines in what order the specified
    ///     callback is invoked relative to the other pending operations
    ///     in the Dispatcher.
    /// </param>
    public void Invoke(Action callback, DispatcherPriority priority)
    {
        Invoke(callback, priority, CancellationToken.None, TimeSpan.FromMilliseconds(-1));
    }

    /// <summary>
    ///     Executes the specified Action synchronously on the thread that
    ///     the Dispatcher was created on.
    /// </summary>
    /// <param name="callback">
    ///     An Action delegate to invoke through the dispatcher.
    /// </param>
    /// <param name="priority">
    ///     The priority that determines in what order the specified
    ///     callback is invoked relative to the other pending operations
    ///     in the Dispatcher.
    /// </param>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used to cancel the operation.
    ///     If the operation has not started, it will be aborted when the
    ///     cancellation token is canceled.  If the operation has started,
    ///     the operation can cooperate with the cancellation request.
    /// </param>
    public void Invoke(Action callback, DispatcherPriority priority, CancellationToken cancellationToken)
    {
        Invoke(callback, priority, cancellationToken, TimeSpan.FromMilliseconds(-1));
    }

    /// <summary>
    ///     Executes the specified Action synchronously on the thread that
    ///     the Dispatcher was created on.
    /// </summary>
    /// <param name="callback">
    ///     An Action delegate to invoke through the dispatcher.
    /// </param>
    /// <param name="priority">
    ///     The priority that determines in what order the specified
    ///     callback is invoked relative to the other pending operations
    ///     in the Dispatcher.
    /// </param>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used to cancel the operation.
    ///     If the operation has not started, it will be aborted when the
    ///     cancellation token is canceled.  If the operation has started,
    ///     the operation can cooperate with the cancellation request.
    /// </param>
    /// <param name="timeout">
    ///     The minimum amount of time to wait for the operation to start.
    ///     Once the operation has started, it will complete before this method
    ///     returns.
    /// </param>
    public void Invoke(Action callback, DispatcherPriority priority, CancellationToken cancellationToken,
        TimeSpan timeout)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        DispatcherPriority.Validate(priority, "priority");

        if (timeout.TotalMilliseconds < 0 &&
            timeout != TimeSpan.FromMilliseconds(-1))
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        // Fast-Path: if on the same thread, and invoking at Send priority,
        // and the cancellation token is not already canceled, then just
        // call the callback directly.
        if (!cancellationToken.IsCancellationRequested && priority == DispatcherPriority.Send && CheckAccess())
        {
            using (AvaloniaSynchronizationContext.Ensure(this, priority))
                callback();
            return;
        }

        // Slow-Path: go through the queue.
        DispatcherOperation operation = new DispatcherOperation(this, priority, callback, false);
        InvokeImpl(operation, cancellationToken, timeout);
    }

    /// <summary>
    ///     Executes the specified Func&lt;TResult&gt; synchronously on the
    ///     thread that the Dispatcher was created on.
    /// </summary>
    /// <typeparam name="TResult">The type of the <paramref name="callback"/> return value.</typeparam>
    /// <param name="callback">
    ///     A Func&lt;TResult&gt; delegate to invoke through the dispatcher.
    /// </param>
    /// <returns>
    ///     The return value from the delegate being invoked.
    /// </returns>
    /// <remarks>
    ///     Note that the default priority is DispatcherPriority.Send.
    /// </remarks>
    public TResult Invoke<TResult>(Func<TResult> callback)
    {
        return Invoke(callback, DispatcherPriority.Send, CancellationToken.None, TimeSpan.FromMilliseconds(-1));
    }

    /// <summary>
    ///     Executes the specified Func&lt;TResult&gt; synchronously on the
    ///     thread that the Dispatcher was created on.
    /// </summary>
    /// <typeparam name="TResult">The type of the <paramref name="callback"/> return value.</typeparam>
    /// <param name="callback">
    ///     A Func&lt;TResult&gt; delegate to invoke through the dispatcher.
    /// </param>
    /// <param name="priority">
    ///     The priority that determines in what order the specified
    ///     callback is invoked relative to the other pending operations
    ///     in the Dispatcher.
    /// </param>
    /// <returns>
    ///     The return value from the delegate being invoked.
    /// </returns>
    public TResult Invoke<TResult>(Func<TResult> callback, DispatcherPriority priority)
    {
        return Invoke(callback, priority, CancellationToken.None, TimeSpan.FromMilliseconds(-1));
    }

    /// <summary>
    ///     Executes the specified Func&lt;TResult&gt; synchronously on the
    ///     thread that the Dispatcher was created on.
    /// </summary>
    /// <typeparam name="TResult">The type of the <paramref name="callback"/> return value.</typeparam>
    /// <param name="callback">
    ///     A Func&lt;TResult&gt; delegate to invoke through the dispatcher.
    /// </param>
    /// <param name="priority">
    ///     The priority that determines in what order the specified
    ///     callback is invoked relative to the other pending operations
    ///     in the Dispatcher.
    /// </param>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used to cancel the operation.
    ///     If the operation has not started, it will be aborted when the
    ///     cancellation token is canceled.  If the operation has started,
    ///     the operation can cooperate with the cancellation request.
    /// </param>
    /// <returns>
    ///     The return value from the delegate being invoked.
    /// </returns>
    public TResult Invoke<TResult>(Func<TResult> callback, DispatcherPriority priority,
        CancellationToken cancellationToken)
    {
        return Invoke(callback, priority, cancellationToken, TimeSpan.FromMilliseconds(-1));
    }

    /// <summary>
    ///     Executes the specified Func&lt;TResult&gt; synchronously on the
    ///     thread that the Dispatcher was created on.
    /// </summary>
    /// <typeparam name="TResult">The type of the <paramref name="callback"/> return value.</typeparam>
    /// <param name="callback">
    ///     A Func&lt;TResult&gt; delegate to invoke through the dispatcher.
    /// </param>
    /// <param name="priority">
    ///     The priority that determines in what order the specified
    ///     callback is invoked relative to the other pending operations
    ///     in the Dispatcher.
    /// </param>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used to cancel the operation.
    ///     If the operation has not started, it will be aborted when the
    ///     cancellation token is canceled.  If the operation has started,
    ///     the operation can cooperate with the cancellation request.
    /// </param>
    /// <param name="timeout">
    ///     The minimum amount of time to wait for the operation to start.
    ///     Once the operation has started, it will complete before this method
    ///     returns.
    /// </param>
    /// <returns>
    ///     The return value from the delegate being invoked.
    /// </returns>
    public TResult Invoke<TResult>(Func<TResult> callback, DispatcherPriority priority,
        CancellationToken cancellationToken, TimeSpan timeout)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        DispatcherPriority.Validate(priority, "priority");

        if (timeout.TotalMilliseconds < 0 &&
            timeout != TimeSpan.FromMilliseconds(-1))
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        // Fast-Path: if on the same thread, and invoking at Send priority,
        // and the cancellation token is not already canceled, then just
        // call the callback directly.
        if (!cancellationToken.IsCancellationRequested && priority == DispatcherPriority.Send && CheckAccess())
        {
            using (AvaloniaSynchronizationContext.Ensure(this, priority))
                return callback();
        }

        // Slow-Path: go through the queue.
        DispatcherOperation<TResult> operation = new DispatcherOperation<TResult>(this, priority, callback);
        return (TResult)InvokeImpl(operation, cancellationToken, timeout)!;
    }

    /// <summary>
    ///     Executes the specified Action asynchronously on the thread
    ///     that the Dispatcher was created on.
    /// </summary>
    /// <param name="callback">
    ///     An Action delegate to invoke through the dispatcher.
    /// </param>
    /// <returns>
    ///     An operation representing the queued delegate to be invoked.
    /// </returns>
    /// <remarks>
    ///     Note that the default priority is DispatcherPriority.Default.
    /// </remarks>
    public DispatcherOperation InvokeAsync(Action callback)
    {
        return InvokeAsync(callback, default, CancellationToken.None);
    }

    /// <summary>
    ///     Executes the specified Action asynchronously on the thread
    ///     that the Dispatcher was created on.
    /// </summary>
    /// <param name="callback">
    ///     An Action delegate to invoke through the dispatcher.
    /// </param>
    /// <param name="priority">
    ///     The priority that determines in what order the specified
    ///     callback is invoked relative to the other pending operations
    ///     in the Dispatcher.
    /// </param>
    /// <returns>
    ///     An operation representing the queued delegate to be invoked.
    /// </returns>
    /// <returns>
    ///     An operation representing the queued delegate to be invoked.
    /// </returns>
    public DispatcherOperation InvokeAsync(Action callback, DispatcherPriority priority)
    {
        return InvokeAsync(callback, priority, CancellationToken.None);
    }

    /// <summary>
    ///     Executes the specified Action asynchronously on the thread
    ///     that the Dispatcher was created on.
    /// </summary>
    /// <param name="callback">
    ///     An Action delegate to invoke through the dispatcher.
    /// </param>
    /// <param name="priority">
    ///     The priority that determines in what order the specified
    ///     callback is invoked relative to the other pending operations
    ///     in the Dispatcher.
    /// </param>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used to cancel the operation.
    ///     If the operation has not started, it will be aborted when the
    ///     cancellation token is canceled.  If the operation has started,
    ///     the operation can cooperate with the cancellation request.
    /// </param>
    /// <returns>
    ///     An operation representing the queued delegate to be invoked.
    /// </returns>
    public DispatcherOperation InvokeAsync(Action callback, DispatcherPriority priority,
        CancellationToken cancellationToken)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        DispatcherPriority.Validate(priority, "priority");

        DispatcherOperation operation = new DispatcherOperation(this, priority, callback, false);
        InvokeAsyncImpl(operation, cancellationToken);

        return operation;
    }

    /// <summary>
    ///     Executes the specified Func&lt;TResult&gt; asynchronously on the
    ///     thread that the Dispatcher was created on.
    /// </summary>
    /// <typeparam name="TResult">The type of the <paramref name="callback"/> return value.</typeparam>
    /// <param name="callback">
    ///     A Func&lt;TResult&gt; delegate to invoke through the dispatcher.
    /// </param>
    /// <returns>
    ///     An operation representing the queued delegate to be invoked.
    /// </returns>
    /// <remarks>
    ///     Note that the default priority is DispatcherPriority.Default.
    /// </remarks>
    public DispatcherOperation<TResult> InvokeAsync<TResult>(Func<TResult> callback)
    {
        return InvokeAsync(callback, DispatcherPriority.Default, CancellationToken.None);
    }

    /// <summary>
    ///     Executes the specified Func&lt;TResult&gt; asynchronously on the
    ///     thread that the Dispatcher was created on.
    /// </summary>
    /// <param name="callback">
    ///     A Func&lt;TResult&gt; delegate to invoke through the dispatcher.
    /// </param>
    /// <param name="priority">
    ///     The priority that determines in what order the specified
    ///     callback is invoked relative to the other pending operations
    ///     in the Dispatcher.
    /// </param>
    /// <returns>
    ///     An operation representing the queued delegate to be invoked.
    /// </returns>
    public DispatcherOperation<TResult> InvokeAsync<TResult>(Func<TResult> callback, DispatcherPriority priority)
    {
        return InvokeAsync(callback, priority, CancellationToken.None);
    }

    /// <summary>
    ///     Executes the specified Func&lt;TResult&gt; asynchronously on the
    ///     thread that the Dispatcher was created on.
    /// </summary>
    /// <typeparam name="TResult">The type of the <paramref name="callback"/> return value.</typeparam>
    /// <param name="callback">
    ///     A Func&lt;TResult&gt; delegate to invoke through the dispatcher.
    /// </param>
    /// <param name="priority">
    ///     The priority that determines in what order the specified
    ///     callback is invoked relative to the other pending operations
    ///     in the Dispatcher.
    /// </param>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used to cancel the operation.
    ///     If the operation has not started, it will be aborted when the
    ///     cancellation token is canceled.  If the operation has started,
    ///     the operation can cooperate with the cancellation request.
    /// </param>
    /// <returns>
    ///     An operation representing the queued delegate to be invoked.
    /// </returns>
    public DispatcherOperation<TResult> InvokeAsync<TResult>(Func<TResult> callback, DispatcherPriority priority,
        CancellationToken cancellationToken)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        DispatcherPriority.Validate(priority, "priority");

        DispatcherOperation<TResult> operation = new DispatcherOperation<TResult>(this, priority, callback);
        InvokeAsyncImpl(operation, cancellationToken);

        return operation;
    }

    internal void InvokeAsyncImpl(DispatcherOperation operation, CancellationToken cancellationToken)
    {
        bool succeeded = false;

        // Could be a non-dispatcher thread, lock to read
        lock (InstanceLock)
        {
            if (!cancellationToken.IsCancellationRequested &&
                !_hasShutdownFinished &&
                !Environment.HasShutdownStarted)
            {
                // Add the operation to the work queue
                _queue.Enqueue(operation.Priority, operation);

                // Make sure we will wake up to process this operation.
                succeeded = RequestProcessing();

                if (!succeeded)
                {
                    // Dequeue the item since we failed to request
                    // processing for it.  Note we will mark it aborted
                    // below.
                    _queue.RemoveItem(operation);
                }
            }
        }

        if (succeeded == true)
        {
            // We have enqueued the operation.  Register a callback
            // with the cancellation token to abort the operation
            // when cancellation is requested.
            if (cancellationToken.CanBeCanceled)
            {
                CancellationTokenRegistration cancellationRegistration =
                    cancellationToken.Register(s => ((DispatcherOperation)s!).Abort(), operation);

                // Revoke the cancellation when the operation is done.
                operation.Aborted += (s, e) => cancellationRegistration.Dispose();
                operation.Completed += (s, e) => cancellationRegistration.Dispose();
            }
        }
        else
        {
            // We failed to enqueue the operation, and the caller that
            // created the operation does not expose it before we return,
            // so it is safe to modify the operation outside of the lock.
            // Just mark the operation as aborted, which we can safely
            // return to the user.
            operation.DoAbort();
        }
    }


    private object? InvokeImpl(DispatcherOperation operation, CancellationToken cancellationToken, TimeSpan timeout)
    {
        object? result = null;

        Debug.Assert(timeout.TotalMilliseconds >= 0 || timeout == TimeSpan.FromMilliseconds(-1));
        Debug.Assert(operation.Priority != DispatcherPriority.Send || !CheckAccess()); // should be handled by caller

        if (!cancellationToken.IsCancellationRequested)
        {
            // This operation must be queued since it was invoked either to
            // another thread, or at a priority other than Send.
            InvokeAsyncImpl(operation, cancellationToken);

            CancellationToken ctTimeout = CancellationToken.None;
            CancellationTokenRegistration ctTimeoutRegistration = new CancellationTokenRegistration();
            CancellationTokenSource? ctsTimeout = null;

            if (timeout.TotalMilliseconds >= 0)
            {
                // Create a CancellationTokenSource that will abort the
                // operation after the timeout.  Note that this does not
                // cancel the operation, just abort it if it is still pending.
                ctsTimeout = new CancellationTokenSource(timeout);
                ctTimeout = ctsTimeout.Token;
                ctTimeoutRegistration = ctTimeout.Register(s => ((DispatcherOperation)s!).Abort(), operation);
            }


            // We have already registered with the cancellation tokens
            // (both provided by the user, and one for the timeout) to
            // abort the operation when they are canceled.  If the
            // operation has already started when the timeout expires,
            // we still wait for it to complete.  This is different
            // than simply waiting on the operation with a timeout
            // because we are the ones queuing the dispatcher
            // operation, not the caller.  We can't leave the operation
            // in a state that it might execute if we return that it did not
            // invoke.
            try
            {
                operation.Wait();

                Debug.Assert(operation.Status == DispatcherOperationStatus.Completed ||
                             operation.Status == DispatcherOperationStatus.Aborted);

                // Old async semantics return from Wait without
                // throwing an exception if the operation was aborted.
                // There is no need to test the timeout condition, since
                // the old async semantics would just return the result,
                // which would be null.

                // This should not block because either the operation
                // is using the old async semantics, or the operation
                // completed successfully.
                result = operation.GetResult();
            }
            catch (OperationCanceledException)
            {
                Debug.Assert(operation.Status == DispatcherOperationStatus.Aborted);

                // New async semantics will throw an exception if the
                // operation was aborted.  Here we convert that
                // exception into a timeout exception if the timeout
                // has expired (admittedly a weak relationship
                // assuming causality).
                if (ctTimeout.IsCancellationRequested)
                {
                    // The operation was canceled because of the
                    // timeout, throw a TimeoutException instead.
                    throw new TimeoutException();
                }
                else
                {
                    // The operation was canceled from some other reason.
                    throw;
                }
            }
            finally
            {
                ctTimeoutRegistration.Dispose();
                if (ctsTimeout != null)
                {
                    ctsTimeout.Dispose();
                }
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public void Post(Action action, DispatcherPriority priority = default)
    {
        _ = action ?? throw new ArgumentNullException(nameof(action));
        InvokeAsyncImpl(new DispatcherOperation(this, priority, action, true), CancellationToken.None);
    }

    /// <summary>
    ///     Executes the specified Func&lt;Task&gt; asynchronously on the
    ///     thread that the Dispatcher was created on
    /// </summary>
    /// <param name="callback">
    ///     A Func&lt;Task&gt; delegate to invoke through the dispatcher.
    /// </param>
    /// <returns>
    ///     An task that completes after the task returned from callback finishes.
    /// </returns>
    public Task InvokeAsync(Func<Task> callback) => InvokeAsync(callback, DispatcherPriority.Default);
    
    /// <summary>
    ///     Executes the specified Func&lt;Task&gt; asynchronously on the
    ///     thread that the Dispatcher was created on
    /// </summary>
    /// <param name="callback">
    ///     A Func&lt;Task&gt; delegate to invoke through the dispatcher.
    /// </param>
    /// <param name="priority">
    ///     The priority that determines in what order the specified
    ///     callback is invoked relative to the other pending operations
    ///     in the Dispatcher.
    /// </param>
    /// <returns>
    ///     An task that completes after the task returned from callback finishes
    /// </returns>
    public Task InvokeAsync(Func<Task> callback, DispatcherPriority priority)
    {
        _ = callback ?? throw new ArgumentNullException(nameof(callback));
        return InvokeAsync<Task>(callback, priority).GetTask().Unwrap();
    }

    /// <summary>
    ///     Executes the specified Func&lt;Task&lt;TResult&gt;&gt; asynchronously on the
    ///     thread that the Dispatcher was created on
    /// </summary>
    /// <param name="action">
    ///     A Func&lt;Task&lt;TResult&gt;&gt; delegate to invoke through the dispatcher.
    /// </param>
    /// <returns>
    ///     An task that completes after the task returned from callback finishes
    /// </returns>
    public Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> action) =>
        InvokeAsync(action, DispatcherPriority.Default);
    
    /// <summary>
    ///     Executes the specified Func&lt;Task&lt;TResult&gt;&gt; asynchronously on the
    ///     thread that the Dispatcher was created on
    /// </summary>
    /// <param name="action">
    ///     A Func&lt;Task&lt;TResult&gt;&gt; delegate to invoke through the dispatcher.
    /// </param>
    /// <param name="priority">
    ///     The priority that determines in what order the specified
    ///     callback is invoked relative to the other pending operations
    ///     in the Dispatcher.
    /// </param>
    /// <returns>
    ///     An task that completes after the task returned from callback finishes
    /// </returns>
    public Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> action, DispatcherPriority priority)
    {
        _ = action ?? throw new ArgumentNullException(nameof(action));
        return InvokeAsync<Task<TResult>>(action, priority).GetTask().Unwrap();
    }

    /// <summary>
    /// Posts an action that will be invoked on the dispatcher thread.
    /// </summary>
    /// <param name="action">The method.</param>
    /// <param name="arg">The argument of method to call.</param>
    /// <param name="priority">The priority with which to invoke the method.</param>
    public void Post(SendOrPostCallback action, object? arg, DispatcherPriority priority = default)
    {
        _ = action ?? throw new ArgumentNullException(nameof(action));
        InvokeAsyncImpl(new SendOrPostCallbackDispatcherOperation(this, priority, action, arg, true), CancellationToken.None);
    }
    
    /// <summary>
    /// Sends an action that will be invoked on the dispatcher thread.
    /// </summary>
    /// <param name="action">The method.</param>
    /// <param name="arg">The argument of method to call.</param>
    /// <param name="priority">The priority with which to invoke the method. If null, Send is default.</param>
    /// <remarks>
    /// When on the same thread with Send priority, callback is executed immediately, without changing synchronization context.
    /// </remarks>
    internal void Send(SendOrPostCallback action, object? arg = null, DispatcherPriority? priority = null)
    {
        _ = action ?? throw new ArgumentNullException(nameof(action));
        priority ??= DispatcherPriority.Send;

        if (priority == DispatcherPriority.Send && CheckAccess())
        {
            try
            {
                action(arg);
            }
            catch (Exception ex) when (ExceptionFilter(ex))
            {
                if (!CatchException(ex))
                    throw;
            }
        }
        else
        {
            InvokeImpl(new SendOrPostCallbackDispatcherOperation(this, priority.Value, action, arg, true),
                CancellationToken.None,
                TimeSpan.FromMilliseconds(-1));
        }
    }

    /// <summary>
    /// Returns a task that would invoke continuation on specified dispatcher priority
    /// </summary>
    public async Task AwaitWithPriority(Task task, DispatcherPriority priority)
    {
        await Resume(priority);
        await task;
    }
    
    /// <summary>
    /// Returns a task that would invoke continuation on specified dispatcher priority
    /// </summary>
    public async Task<T> AwaitWithPriority<T>(Task<T> task, DispatcherPriority priority)
    {
        await Resume(priority);
        return await task;
    }
}
