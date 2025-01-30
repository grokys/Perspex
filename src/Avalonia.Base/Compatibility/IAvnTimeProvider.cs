using System;
using System.Diagnostics;

namespace Avalonia.Compatibility;

/// Minimal netstandard2.0 compatible abstraction over TimeProvider.
internal interface IAvnTimeProvider
{
    long GetTimestamp();
    double GetElapsedMilliseconds(long startingTimestamp, long endingTimestamp);
}

internal class StopwatchTimeProvider : IAvnTimeProvider
{
    private static readonly double s_conversionFactor = (double)TimeSpan.TicksPerSecond / (Stopwatch.Frequency * TimeSpan.TicksPerMillisecond);

    public double GetElapsedMilliseconds(long startingTimestamp, long endingTimestamp) => (endingTimestamp - startingTimestamp) * s_conversionFactor;
    public long GetTimestamp() => Stopwatch.GetTimestamp();
}
