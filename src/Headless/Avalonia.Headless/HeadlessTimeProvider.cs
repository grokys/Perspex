using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Compatibility;
using Avalonia.Controls.Platform;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Headless;

internal class HeadlessTimeProvider : TimeProvider, IAvnTimeProvider
{
    private readonly bool _autoStart;

    public static HeadlessTimeProvider GetCurrent() =>
        AvaloniaLocator.Current.GetRequiredService<HeadlessTimeProvider>();

    private readonly object _sync = new();

    private bool _isRunning;
    private TimeSpan _snapshotTime;

    private TimeProvider? _nested;
    private long _nestedStartTimestamp;

    public HeadlessTimeProvider(bool autoStart)
    {
        _autoStart = autoStart;
        Reset();
    }

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    
    public override long GetTimestamp()
    {
        lock (_sync)
        {
            return (_snapshotTime + GetTimeAfterStarted()).Ticks;
        }
    }

    public double GetElapsedMilliseconds(long startingTimestamp, long endingTimestamp)
    {
        return base.GetElapsedTime(startingTimestamp, endingTimestamp).TotalMilliseconds;
    }

    private void Pause()
    {
        lock (_sync)
        {
            _snapshotTime += GetTimeAfterStarted();
            _isRunning = false;
        }
    }

    private void Play()
    {
        lock (_sync)
        {
            _isRunning = true;
            _nestedStartTimestamp = _nested?.GetTimestamp() ?? default;
        }
    }

    public void Pulse(TimeSpan time)
    {
        lock (_sync)
        {
            _snapshotTime += time;
        }
    }

    public void SetNested(TimeProvider? timeProvider)
    {
        lock (_sync)
        {
            if (timeProvider == _nested)
            {
                return;
            }

            Pause();
            _nested = timeProvider;
            // Force running state, when custom time provider is set.
            if (_nested != null)
            {
                Play();
            }
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            Pause();
            _snapshotTime = default;
            if (_autoStart)
                SetNested(System);
        }
    }

    private TimeSpan GetTimeAfterStarted()
    {
        lock (_sync)
        {
            return _isRunning && _nested is not null ? _nested.GetElapsedTime(_nestedStartTimestamp) : default;
        }
    }
}
