using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Platform;

namespace Avalonia.Threading;

/// <summary>
/// Provides services for managing work items on a thread.
/// </summary>
/// <remarks>
/// In Avalonia, there is usually only a single <see cref="Dispatcher"/> in the application -
/// the one for the UI thread, retrieved via the <see cref="UIThread"/> property.
/// </remarks>
public partial class Dispatcher : IDispatcher
{
    private IDispatcherImpl _impl;
    internal object InstanceLock { get; } = new();
    private IControlledDispatcherImpl? _controlledImpl;
    private static Dispatcher? s_uiThread;
    private IDispatcherImplWithPendingInput? _pendingInputImpl;
    private readonly IDispatcherImplWithExplicitBackgroundProcessing? _backgroundProcessingImpl;

    private readonly AvaloniaSynchronizationContext?[] _priorityContexts =
        new AvaloniaSynchronizationContext?[DispatcherPriority.MaxValue - DispatcherPriority.MinValue + 1];

    internal Dispatcher(IDispatcherImpl impl)
    {
        _impl = impl;
        impl.Timer += OnOSTimer;
        impl.Signaled += Signaled;
        _controlledImpl = _impl as IControlledDispatcherImpl;
        _pendingInputImpl = _impl as IDispatcherImplWithPendingInput;
        _backgroundProcessingImpl = _impl as IDispatcherImplWithExplicitBackgroundProcessing;
        _maximumInputStarvationTime = _backgroundProcessingImpl == null ?
            MaximumInputStarvationTimeInFallbackMode :
            MaximumInputStarvationTimeInExplicitProcessingExplicitMode;
        if (_backgroundProcessingImpl != null)
            _backgroundProcessingImpl.ReadyForBackgroundProcessing += OnReadyForExplicitBackgroundProcessing;

        _unhandledExceptionEventArgs = new DispatcherUnhandledExceptionEventArgs(this);
        _exceptionFilterEventArgs = new DispatcherUnhandledExceptionFilterEventArgs(this);
    }
    
    public static Dispatcher UIThread => s_uiThread ??= CreateUIThreadDispatcher();
    public bool SupportsRunLoops => _controlledImpl != null;

    private static Dispatcher CreateUIThreadDispatcher()
    {
        var impl = AvaloniaLocator.Current.GetService<IDispatcherImpl>();
        if (impl == null)
        {
            var platformThreading = AvaloniaLocator.Current.GetService<IPlatformThreadingInterface>();
            if (platformThreading != null)
                impl = new LegacyDispatcherImpl(platformThreading);
            else
                impl = new NullDispatcherImpl();
        }
        return new Dispatcher(impl);
    }

    /// <summary>
    /// Checks that the current thread is the UI thread.
    /// </summary>
    public bool CheckAccess() => _impl.CurrentThreadIsLoopThread;

    /// <summary>
    /// Checks that the current thread is the UI thread and throws if not.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The current thread is not the UI thread.
    /// </exception>
    public void VerifyAccess()
    {
        if (!CheckAccess())
        {
            // Used to inline VerifyAccess.
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void ThrowVerifyAccess()
                => throw new InvalidOperationException("Call from invalid thread");
            ThrowVerifyAccess();
        }
    }

    internal AvaloniaSynchronizationContext GetContextWithPriority(DispatcherPriority priority)
    {
        DispatcherPriority.Validate(priority, nameof(priority));
        var index = priority - DispatcherPriority.MinValue;
        return _priorityContexts[index] ??= new(this, priority);
    }

    /// <summary>
    /// Creates an awaitable object that asynchronously resumes execution on the dispatcher.
    /// </summary>
    /// <returns>
    /// An awaitable object that asynchronously resumes execution on the dispatcher.
    /// </returns>
    /// <remarks>
    /// This method is equivalent to calling the <see cref="Resume(DispatcherPriority)"/> method
    /// and passing in <see cref="DispatcherPriority.Background"/>.
    /// </remarks>
    public DispatcherPriorityAwaitable Resume() =>
        Resume(DispatcherPriority.Background);

    /// <summary>``
    /// Creates an awaitable object that asynchronously resumes execution on the dispatcher. The work that occurs
    /// when control returns to the code awaiting the result of this method is scheduled with the specified priority.
    /// </summary>
    /// <param name="priority">The priority at which to schedule the continuation.</param>
    /// <returns>
    /// An awaitable object that asynchronously resumes execution on the dispatcher.
    /// </returns>
    public DispatcherPriorityAwaitable Resume(DispatcherPriority priority)
    {
        DispatcherPriority.Validate(priority, nameof(priority));
        return new(this, priority);
    }

    /// <summary>
    /// Creates an awaitable object that asynchronously yields control back to the current dispatcher
    /// and provides an opportunity for the dispatcher to process other events.
    /// </summary>
    /// <returns>
    /// An awaitable object that asynchronously yields control back to the current dispatcher
    /// and provides an opportunity for the dispatcher to process other events.
    /// </returns>
    /// <remarks>
    /// This method is equivalent to calling the <see cref="Yield(DispatcherPriority)"/> method
    /// and passing in <see cref="DispatcherPriority.Background"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// The current thread is not the UI thread.
    /// </exception>
    public static DispatcherPriorityAwaitable Yield() =>
        Yield(DispatcherPriority.Background);

    /// <summary>
    /// Creates an awaitable object that asynchronously yields control back to the current dispatcher
    /// and provides an opportunity for the dispatcher to process other events. The work that occurs when
    /// control returns to the code awaiting the result of this method is scheduled with the specified priority.
    /// </summary>
    /// <param name="priority">The priority at which to schedule the continuation.</param>
    /// <returns>
    /// An awaitable object that asynchronously yields control back to the current dispatcher
    /// and provides an opportunity for the dispatcher to process other events.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The current thread is not the UI thread.
    /// </exception>
    public static DispatcherPriorityAwaitable Yield(DispatcherPriority priority)
    {
        // When multiple dispatcher are supported by Avalonia
        // there should be a member to fetch the current Dispatcher.
        // But for now there's only one and it's fine to request it.
        var current = UIThread;
        current.VerifyAccess();
        return current.Resume(priority);
    }
}
