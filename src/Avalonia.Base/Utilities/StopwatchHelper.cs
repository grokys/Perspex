using System;
using System.Diagnostics;

namespace Avalonia.Utilities;

/// <summary>
/// Allows using <see cref="Stopwatch"/> as timestamps without allocating.
/// </summary>
/// <remarks>Equivalent to Stopwatch.GetElapsedTime in .NET 7.</remarks>
internal static class StopwatchHelper
{
    private static readonly double s_timestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
    private static readonly double s_timestampToMs = s_timestampToTicks / TimeSpan.TicksPerMillisecond;

    public static TimeSpan GetElapsedTime(long startingTimestamp)
        => GetElapsedTime(startingTimestamp, Stopwatch.GetTimestamp());

    public static TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp)
        => new((long)((endingTimestamp - startingTimestamp) * s_timestampToTicks));

    public static double GetElapsedTimeMs(long startingTimestamp)
        => (Stopwatch.GetTimestamp() - startingTimestamp) * s_timestampToMs;
}
