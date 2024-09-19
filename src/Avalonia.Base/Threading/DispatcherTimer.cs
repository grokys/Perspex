using System;
using Avalonia.Reactive;

namespace Avalonia.Threading;

/// <summary>
///     A timer that is integrated into the Dispatcher queues, and will
///     be processed after a given amount of time at a specified priority.
/// </summary>
public partial class DispatcherTimer
{
    internal static int ActiveTimersCount { get; private set; }

    /// <summary>
    ///     Creates a timer that uses theUI thread's Dispatcher2 to
    ///     process the timer event at background priority.
    /// </summary>
    public DispatcherTimer() : this(DispatcherPriority.Background)
    {
    }

    /// <summary>
    ///     Creates a timer that uses the UI thread's Dispatcher2 to
    ///     process the timer event at the specified priority.
    /// </summary>
    /// <param name="priority">
    ///     The priority to process the timer at.
    /// </param>
    public DispatcherTimer(DispatcherPriority priority) : this(Threading.Dispatcher.UIThread, priority,
        TimeSpan.FromMilliseconds(0))
    {
    }

    /// <summary>
    ///     Creates a timer that uses the specified Dispatcher2 to
    ///     process the timer event at the specified priority.
    /// </summary>
    /// <param name="priority">
    ///     The priority to process the timer at.
    /// </param>
    /// <param name="dispatcher">
    ///     The dispatcher to use to process the timer.
    /// </param>
    internal DispatcherTimer(DispatcherPriority priority, Dispatcher dispatcher) : this(dispatcher, priority,
        TimeSpan.FromMilliseconds(0))
    {
    }

    /// <summary>
    ///     Creates a timer that uses the UI thread's Dispatcher2 to
    ///     process the timer event at the specified priority after the specified timeout.
    /// </summary>
    /// <param name="interval">
    ///     The interval to tick the timer after.
    /// </param>
    /// <param name="priority">
    ///     The priority to process the timer at.
    /// </param>
    /// <param name="callback">
    ///     The callback to call when the timer ticks.
    /// </param>
    public DispatcherTimer(TimeSpan interval, DispatcherPriority priority, EventHandler callback)
        : this(Threading.Dispatcher.UIThread, priority, interval)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        Tick += callback;
        Start();
    }

    /// <summary>
    ///     Gets the dispatcher this timer is associated with.
    /// </summary>
    public Dispatcher Dispatcher
    {
        get { return _dispatcher; }
    }

    /// <summary>
    ///     Gets or sets whether the timer is running.
    /// </summary>
    public bool IsEnabled
    {
        get { return _isEnabled; }

        set
        {
            lock (_instanceLock)
            {
                if (!value && _isEnabled)
                {
                    Stop();
                }
                else if (value && !_isEnabled)
                {
                    Start();
                }
            }
        }
    }

    /// <summary>
    ///     Gets or sets the time between timer ticks.
    /// </summary>
    public TimeSpan Interval
    {
        get { return _interval; }

        set
        {
            bool updateOSTimer = false;

            if (value.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "TimeSpan period must be greater than or equal to zero.");

            if (value.TotalMilliseconds > Int32.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "TimeSpan period must be less than or equal to Int32.MaxValue.");

            lock (_instanceLock)
            {
                _interval = value;

                if (_isEnabled)
                {
                    DueTimeInMs = _dispatcher.Now + (long)_interval.TotalMilliseconds;
                    updateOSTimer = true;
                }
            }

            if (updateOSTimer)
            {
                _dispatcher.RescheduleTimers();
            }
        }
    }

    /// <summary>
    ///     Starts the timer.
    /// </summary>
    public void Start()
    {
        lock (_instanceLock)
        {
            if (!_isEnabled)
            {
                _isEnabled = true;
                ActiveTimersCount++;

                Restart();
            }
        }
    }

    /// <summary>
    ///     Stops the timer.
    /// </summary>
    public void Stop()
    {
        bool updateOSTimer = false;

        lock (_instanceLock)
        {
            if (_isEnabled)
            {
                _isEnabled = false;
                ActiveTimersCount--;
                updateOSTimer = true;

                // If the operation is in the queue, abort it.
                if (_operation != null)
                {
                    _operation.Abort();
                    _operation = null;
                }
            }
        }

        if (updateOSTimer)
        {
            _dispatcher.RemoveTimer(this);
        }
    }
    
    /// <summary>
    /// Starts a new timer.
    /// </summary>
    /// <param name="action">
    /// The method to call on timer tick. If the method returns false, the timer will stop.
    /// </param>
    /// <param name="interval">The interval at which to tick.</param>
    /// <param name="priority">The priority to use.</param>
    /// <returns>An <see cref="IDisposable"/> used to cancel the timer.</returns>
    public static IDisposable Run(Func<bool> action, TimeSpan interval, DispatcherPriority priority = default)
    {
        var timer = new DispatcherTimer(priority) { Interval = interval };

        timer.Tick += (s, e) =>
        {
            if (!action())
            {
                timer.Stop();
            }
        };

        timer.Start();

        return Disposable.Create(() => timer.Stop());
    }
    
    /// <summary>
    /// Runs a method once, after the specified interval.
    /// </summary>
    /// <param name="action">
    /// The method to call after the interval has elapsed.
    /// </param>
    /// <param name="interval">The interval after which to call the method.</param>
    /// <param name="priority">The priority to use.</param>
    /// <returns>An <see cref="IDisposable"/> used to cancel the timer.</returns>
    public static IDisposable RunOnce(
        Action action,
        TimeSpan interval,
        DispatcherPriority priority = default)
    {
        interval = (interval != TimeSpan.Zero) ? interval : TimeSpan.FromTicks(1);
            
        var timer = new DispatcherTimer(priority) { Interval = interval };

        timer.Tick += (s, e) =>
        {
            action();
            timer.Stop();
        };

        timer.Start();

        return Disposable.Create(() => timer.Stop());
    }

    /// <summary>
    ///     Occurs when the specified timer interval has elapsed and the
    ///     timer is enabled.
    /// </summary>
    public event EventHandler? Tick;

    /// <summary>
    ///     Any data that the caller wants to pass along with the timer.
    /// </summary>
    public object? Tag { get; set; }


    internal DispatcherTimer(Dispatcher dispatcher, DispatcherPriority priority, TimeSpan interval)
    {
        if (dispatcher == null)
        {
            throw new ArgumentNullException(nameof(dispatcher));
        }

        DispatcherPriority.Validate(priority, "priority");
        if (priority == DispatcherPriority.Inactive)
        {
            throw new ArgumentException("Specified priority is not valid.", nameof(priority));
        }

        if (interval.TotalMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(interval), "TimeSpan period must be greater than or equal to zero.");

        if (interval.TotalMilliseconds > Int32.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(interval),
                "TimeSpan period must be less than or equal to Int32.MaxValue.");


        _dispatcher = dispatcher;
        _priority = priority;
        _interval = interval;
    }

    private void Restart()
    {
        lock (_instanceLock)
        {
            if (_operation != null)
            {
                // Timer has already been restarted, e.g. Start was called form the Tick handler.
                return;
            }

            // BeginInvoke a new operation.
            _operation = _dispatcher.InvokeAsync(FireTick, DispatcherPriority.Inactive);

            DueTimeInMs = _dispatcher.Now + (long)_interval.TotalMilliseconds;

            if (_interval.TotalMilliseconds == 0 && _dispatcher.CheckAccess())
            {
                // shortcut - just promote the item now
                Promote();
            }
            else
            {
                _dispatcher.AddTimer(this);
            }
        }
    }

    internal void Promote() // called from Dispatcher
    {
        lock (_instanceLock)
        {
            // Simply promote the operation to it's desired priority.
            if (_operation != null)
            {
                _operation.Priority = _priority;
            }
        }
    }

    private void FireTick()
    {
        // The operation has been invoked, so forget about it.
        _operation = null;

        // The dispatcher thread is calling us because item's priority
        // was changed from inactive to something else.
        if (Tick != null)
        {
            Tick(this, EventArgs.Empty);
        }

        // If we are still enabled, start the timer again.
        if (_isEnabled)
        {
            Restart();
        }
    }

    // This is the object we use to synchronize access.
    private readonly object _instanceLock = new object();

    // Note: We cannot BE a dispatcher-affinity object because we can be
    // created by a worker thread.  We are still associated with a
    // dispatcher (where we post the item) but we can be accessed
    // by any thread.
    private readonly Dispatcher _dispatcher;

    private readonly DispatcherPriority _priority; // NOTE: should be Priority
    private TimeSpan _interval;
    private DispatcherOperation? _operation;
    private bool _isEnabled;

    // used by Dispatcher
    internal long DueTimeInMs { get; private set; }
}
