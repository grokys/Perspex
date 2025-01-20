using System;
using System.Runtime.CompilerServices;

namespace Avalonia.Threading;

/// <summary>
/// Represents an awaitable object that asynchronously yields control back to the current dispatcher
/// and provides an opportunity for the dispatcher to process other events.
/// </summary>
/// <remarks>
/// The <see cref="Dispatcher.Yield"/> method returns a <see cref="DispatcherPriorityAwaitable"/>.
/// </remarks>
public readonly struct DispatcherPriorityAwaitable
{
	private readonly Dispatcher _dispatcher;
	private readonly DispatcherPriority _priority;

	internal DispatcherPriorityAwaitable(Dispatcher dispatcher, DispatcherPriority priority)
	{
		_dispatcher = dispatcher;
		_priority = priority;
	}

	/// <summary>
	/// Returns an object that waits for the completion of an asynchronous task.
	/// </summary>
	public DispatcherPriorityAwaiter GetAwaiter() =>
		new(_dispatcher, _priority);
}

/// <summary>
/// Represents an object that waits for the completion of an asynchronous task.
/// </summary>
public readonly struct DispatcherPriorityAwaiter : INotifyCompletion
{
	private readonly Dispatcher _dispatcher;
	private readonly DispatcherPriority _priority;

	internal DispatcherPriorityAwaiter(Dispatcher dispatcher, DispatcherPriority priority)
	{
		_dispatcher = dispatcher;
		_priority = priority;
	}

	/// <inheritdoc/>
	public void GetResult() { }

	/// </inheritdoc/>
	public bool IsCompleted => false;

	/// <inheritdoc/>
	public void OnCompleted(Action completion)
	{
		if (_dispatcher is null)
			throw new InvalidOperationException($"The {nameof(DispatcherPriorityAwaiter)} was not configured with a valid {nameof(Dispatcher)}");

		_dispatcher.Post(completion, _priority);
	}
}
