using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Diagnostics;

internal static class AvaloniaMetrics
{
    internal const string MeterName = "Avalonia";
    private static readonly Meter s_meter = new(MeterName);

    internal static readonly Histogram<double> s_compositorRender = s_meter
        .CreateHistogram<double>(
            "avalonia.compositor.render.time", "ms");
    internal static readonly Histogram<double> s_compositorUpdate = s_meter
        .CreateHistogram<double>(
            "avalonia.compositor.update.time", "ms");

    internal static readonly Histogram<double> s_visualMeasure = s_meter
        .CreateHistogram<double>(
            "avalonia.visual.measure.time", "ms");
    internal static readonly Histogram<double> s_visualArrange = s_meter
        .CreateHistogram<double>(
            "avalonia.visual.arrange.time", "ms");
    private static readonly ObservableUpDownCounter<int> s_visualHandlerCount = s_meter
        .CreateObservableUpDownCounter(
            "avalonia.visual.handler.count",
            () => Interactive.TotalHandlersCount,
            "{handler}");
    private static readonly ObservableUpDownCounter<int> s_visualCount = s_meter
        .CreateObservableUpDownCounter(
            "avalonia.visual.count",
            () => Visual.RootedVisualChildrenCount,
            "{visual}");

    public static HistogramReportDisposable BeginCompositorRender() => new(s_compositorRender);
    public static HistogramReportDisposable BeginCompositorUpdate() => new(s_compositorUpdate);
    public static HistogramReportDisposable BeginVisualMeasure() => new(s_visualMeasure);
    public static HistogramReportDisposable BeginVisualArrange() => new(s_visualArrange);

    internal readonly ref struct HistogramReportDisposable
    {
        private readonly Histogram<double> _histogram;
        private readonly long _timestamp;

        public HistogramReportDisposable(Histogram<double> histogram)
        {
            _histogram = histogram;
            if (histogram.Enabled)
            {
                _timestamp = Stopwatch.GetTimestamp();
            }
        }

        public void Dispose()
        {
            if (_timestamp > 0)
            {
                _histogram.Record(StopwatchHelper.GetElapsedTimeMs(_timestamp));
            }
        }
    }
}
