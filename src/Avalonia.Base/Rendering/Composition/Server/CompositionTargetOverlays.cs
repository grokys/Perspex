using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using Avalonia.Diagnostics;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal class CompositionTargetOverlays
{
    private FpsCounter? _fpsCounter;
    private FrameTimeGraph? _renderTimeGraph;
    private FrameTimeGraph? _updateTimeGraph;
    private FrameTimeGraph? _layoutTimeGraph;
    private Rect? _oldFpsCounterRect;
    private readonly ServerCompositionTarget _target;
    private readonly DiagnosticTextRenderer? _diagnosticTextRenderer;
    private MeterListener? _meterListener;
    private double _lastMeasure, _lastArrange, _lastUpdate, _lastRender;

    public CompositionTargetOverlays(
        ServerCompositionTarget target,
        DiagnosticTextRenderer? diagnosticTextRenderer)
    {
        _target = target;
        _diagnosticTextRenderer = diagnosticTextRenderer;
    }

    private RendererDebugOverlays DebugOverlays { get; set; }

    private FpsCounter? FpsCounter
        => _fpsCounter ??= _diagnosticTextRenderer != null ? new FpsCounter(_diagnosticTextRenderer) : null;

    private FrameTimeGraph? LayoutTimeGraph
        => _layoutTimeGraph ??= CreateTimeGraph("Layout");

    private FrameTimeGraph? RenderTimeGraph
        => _renderTimeGraph ??= CreateTimeGraph("Render");
    
    private FrameTimeGraph? UpdateTimeGraph
        => _updateTimeGraph ??= CreateTimeGraph("RUpdate");

    public bool RequireLayer => DebugOverlays.HasAnyFlag(RendererDebugOverlays.DirtyRects);

    private FrameTimeGraph? CreateTimeGraph(string title)
    {
        if (_diagnosticTextRenderer == null)
            return null;
        return new FrameTimeGraph(360, new Size(360.0, 64.0), 1000.0 / 60.0, title, _diagnosticTextRenderer);
    }

    public void OnChanged(RendererDebugOverlays debugOverlays)
    {
        DebugOverlays = debugOverlays;
        _oldFpsCounterRect = null;

        if (_meterListener is null)
        {
            _meterListener = new MeterListener();
            _meterListener.SetMeasurementEventCallback<double>(MeterMeasurementCallback);
        }

        if ((DebugOverlays & RendererDebugOverlays.Fps) == 0)
        {
            _fpsCounter?.Reset();
        }

        if ((DebugOverlays & RendererDebugOverlays.LayoutTimeGraph) == 0)
        {
            _layoutTimeGraph?.Reset();
            _meterListener.DisableMeasurementEvents(AvaloniaMetrics.s_visualMeasure);
            _meterListener.DisableMeasurementEvents(AvaloniaMetrics.s_visualArrange);
        }
        else
        {
            _meterListener.EnableMeasurementEvents(AvaloniaMetrics.s_visualMeasure);
            _meterListener.EnableMeasurementEvents(AvaloniaMetrics.s_visualArrange);
        }

        if ((DebugOverlays & RendererDebugOverlays.RenderTimeGraph) == 0)
        {
            _renderTimeGraph?.Reset();
            _meterListener.DisableMeasurementEvents(AvaloniaMetrics.s_compositorRender);
            _meterListener.DisableMeasurementEvents(AvaloniaMetrics.s_compositorUpdate);
        }
        else
        {
            _meterListener.EnableMeasurementEvents(AvaloniaMetrics.s_compositorRender);
            _meterListener.EnableMeasurementEvents(AvaloniaMetrics.s_compositorUpdate);
        }
    }

    public void Draw(IDrawingContextImpl targetContext, bool hasLayer)
    {
        if (DebugOverlays != RendererDebugOverlays.None)
        {
            if (DebugOverlays.HasFlag(RendererDebugOverlays.DirtyRects))
                _target.DirtyRects.Visualize(targetContext);

            targetContext.Transform = Matrix.CreateScale(_target.Scaling, _target.Scaling);
            
            using (var immediate = new ImmediateDrawingContext(targetContext, false))
                DrawOverlays(immediate, hasLayer, _target.PixelSize.ToSize(_target.Scaling));
        }
    }

    private void DrawOverlays(ImmediateDrawingContext targetContext, bool hasLayer, Size logicalSize)
    {
        if (DebugOverlays.HasFlag(RendererDebugOverlays.Fps))
        {
            var nativeMem = ByteSizeHelper.ToString((ulong)(
                (_target.Compositor.BatchMemoryPool.CurrentUsage + _target.Compositor.BatchMemoryPool.CurrentPool) *
                _target.Compositor.BatchMemoryPool.BufferSize), false);
            var managedMem = ByteSizeHelper.ToString((ulong)(
                (_target.Compositor.BatchObjectPool.CurrentUsage + _target.Compositor.BatchObjectPool.CurrentPool) *
                _target.Compositor.BatchObjectPool.ArraySize *
                IntPtr.Size), false);

            _oldFpsCounterRect = FpsCounter?.RenderFps(targetContext,
                FormattableString.Invariant($"M:{managedMem} / N:{nativeMem} R:{_target.RenderedVisuals:0000}"),
                hasLayer, _oldFpsCounterRect);
        }

        var top = 0.0;

        void DrawTimeGraph(FrameTimeGraph? graph)
        {
            if (graph == null)
                return;
            var left = logicalSize.Width - graph.Size.Width - 8.0;
            top += 8.0;
            if (!hasLayer)
                targetContext.FillRectangle(Brushes.White, new Rect(left, top, graph.Size.Width, graph.Size.Height));
            using (targetContext.PushSetTransform(Matrix.CreateTranslation(left, top)))
                graph.Render(targetContext);
            top += graph.Size.Height;
        }

        if (DebugOverlays.HasFlag(RendererDebugOverlays.LayoutTimeGraph))
        {
            var layoutTotal = Interlocked.Exchange(ref _lastMeasure, 0)
                              + Interlocked.Exchange(ref _lastArrange, 0); 
            LayoutTimeGraph!.AddFrameValue(layoutTotal);
            DrawTimeGraph(LayoutTimeGraph);
        }

        if (DebugOverlays.HasFlag(RendererDebugOverlays.RenderTimeGraph))
        {
            LayoutTimeGraph!.AddFrameValue(Interlocked.Exchange(ref _lastRender, 0));
            LayoutTimeGraph!.AddFrameValue(Interlocked.Exchange(ref _lastUpdate, 0));
            DrawTimeGraph(RenderTimeGraph);
            DrawTimeGraph(UpdateTimeGraph);
        }
    }

    private void MeterMeasurementCallback(
        Instrument instrument,
        double measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        // Some events happen on UI thread.
        if (instrument == AvaloniaMetrics.s_visualMeasure)
        {
            Interlocked.Exchange(ref _lastMeasure, measurement);
        }
        else if (instrument == AvaloniaMetrics.s_visualArrange)
        {
            Interlocked.Exchange(ref _lastArrange, measurement);
        }
        else if (instrument == AvaloniaMetrics.s_compositorUpdate)
        {
            Interlocked.Exchange(ref _lastUpdate, measurement);
        }
        else if (instrument == AvaloniaMetrics.s_compositorRender)
        {
            Interlocked.Exchange(ref _lastRender, measurement);
        }
    }
}
