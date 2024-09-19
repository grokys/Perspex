using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Collections.Pooled;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// A renderer that utilizes <see cref="Avalonia.Rendering.Composition.Compositor"/> to render the visual tree 
/// </summary>
internal class CompositingRenderer : IRendererWithCompositor, IHitTester
{
    private readonly IRenderRoot _root;
    private readonly Compositor _compositor;
    private readonly RenderDataDrawingContext _recorder;
    private readonly HashSet<Visual> _dirty = new();
    private readonly HashSet<Visual> _recalculateChildren = new();
    private readonly Action _update;

    private bool _queuedUpdate;
    private bool _queuedSceneInvalidation;
    private bool _updating;
    private bool _isDisposed;

    internal CompositionTarget CompositionTarget { get; }
    
    /// <inheritdoc/>
    public RendererDiagnostics Diagnostics { get; }

    /// <inheritdoc />
    public Compositor Compositor => _compositor;

    /// <summary>
    /// Initializes a new instance of <see cref="CompositingRenderer"/>
    /// </summary>
    /// <param name="root">The render root using this renderer.</param>
    /// <param name="compositor">The associated compositors.</param>
    /// <param name="surfaces">
    /// A function returning the list of native platform's surfaces that can be consumed by rendering subsystems.
    /// </param>
    public CompositingRenderer(IRenderRoot root, Compositor compositor, Func<IEnumerable<object>> surfaces)
    {
        _root = root;
        _compositor = compositor;
        _recorder = new(compositor);
        CompositionTarget = compositor.CreateCompositionTarget(surfaces);
        CompositionTarget.Root = ((Visual)root).AttachToCompositor(compositor);
        _update = Update;
        Diagnostics = new RendererDiagnostics();
        Diagnostics.PropertyChanged += OnDiagnosticsPropertyChanged;
    }

    private void OnDiagnosticsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(RendererDiagnostics.DebugOverlays):
                CompositionTarget.DebugOverlays = Diagnostics.DebugOverlays;
                break;
        }
    }

    /// <inheritdoc/>
    public event EventHandler<SceneInvalidatedEventArgs>? SceneInvalidated;

    private void QueueUpdate()
    {
        if(_queuedUpdate)
            return;
        _queuedUpdate = true;
        _compositor.RequestCompositionUpdate(_update);
    }
    
    /// <inheritdoc/>
    public void AddDirty(Visual visual)
    {
        if (_isDisposed)
            return;
        if (_updating)
            throw new InvalidOperationException("Visual was invalidated during the render pass");
        _dirty.Add(visual);
        QueueUpdate();
    }

    /// <inheritdoc/>
    public IEnumerable<Visual> HitTest(Point p, Visual? root, Func<Visual, bool>? filter)
    {
        CompositionVisual? rootVisual = null;
        if (root != null)
        {
            if (root.CompositionVisual == null)
                yield break;
            rootVisual = root.CompositionVisual;
        }
        
        Func<CompositionVisual, bool>? f = null;
        if (filter != null)
            f = v =>
            {
                if (v is CompositionDrawListVisual dlv)
                    return filter(dlv.Visual);
                return true;
            };

        using var res = CompositionTarget.TryHitTest(p, rootVisual, f);
        if(res == null)
            yield break;
        foreach(var v in res)
        {
            if (v is CompositionDrawListVisual dv)
            {
                if (filter == null || filter(dv.Visual))
                    yield return dv.Visual;
            }
        }
    }

    /// <inheritdoc/>
    public Visual? HitTestFirst(Point p, Visual root, Func<Visual, bool>? filter)
    {
        // TODO: Optimize
        return HitTest(p, root, filter).FirstOrDefault();
    }

    /// <inheritdoc/>
    public void RecalculateChildren(Visual visual)
    {
        if (_isDisposed)
            return;
        if (_updating)
            throw new InvalidOperationException("Visual was invalidated during the render pass");
        _recalculateChildren.Add(visual);
        QueueUpdate();
    }

    private void UpdateCore()
    {
        _queuedUpdate = false;
        foreach (var visual in _dirty)
        {
            var comp = visual.CompositionVisual;
            if(comp == null)
                continue;
            
            visual.SynchronizeCompositionProperties();

            try
            {
                visual.Render(_recorder);
                comp.DrawList = _recorder.GetRenderResults();
            }
            finally
            {
                _recorder.Reset();
            }
            
            visual.SynchronizeCompositionChildVisuals();
        }
        foreach(var v in _recalculateChildren)
            if (!_dirty.Contains(v))
                v.SynchronizeCompositionChildVisuals();
        _dirty.Clear();
        _recalculateChildren.Clear();
        
        CompositionTarget.PixelSize = PixelSize.FromSizeRounded(_root.ClientSize, _root.RenderScaling);
        CompositionTarget.Scaling = _root.RenderScaling;
        
        var commit = _compositor.RequestCommitAsync();
        if (!_queuedSceneInvalidation)
        {
            _queuedSceneInvalidation = true;
            commit.ContinueWith(_ => Dispatcher.UIThread.Post(() =>
            {
                _queuedSceneInvalidation = false;
                SceneInvalidated?.Invoke(this, new SceneInvalidatedEventArgs(_root, new Rect(_root.ClientSize)));
            }, DispatcherPriority.Input), TaskContinuationOptions.ExecuteSynchronously);
        }
    }

    public void TriggerSceneInvalidatedForUnitTests(Rect rect) =>
        SceneInvalidated?.Invoke(this, new SceneInvalidatedEventArgs(_root, rect));
    
    private void Update()
    {
        if(_updating)
            return;
        _updating = true;
        try
        {
            UpdateCore();
        }
        finally
        {
            _updating = false;
        }
    }

    /// <inheritdoc />
    public void Resized(Size size)
    {
    }

    /// <inheritdoc />
    public void Paint(Rect rect) => Paint(rect, true);
    public void Paint(Rect rect, bool catchExceptions)
    {
        if (_isDisposed)
            return;

        QueueUpdate();
        CompositionTarget.RequestRedraw();
        MediaContext.Instance.ImmediateRenderRequested(CompositionTarget, catchExceptions);
    }

    /// <inheritdoc />
    public void Start()
    {
        if (_isDisposed)
            return;

        CompositionTarget.IsEnabled = true;
    }

    /// <inheritdoc />
    public void Stop()
        => CompositionTarget.IsEnabled = false;

    /// <inheritdoc />
    public ValueTask<object?> TryGetRenderInterfaceFeature(Type featureType)
        => Compositor.TryGetRenderInterfaceFeature(featureType);

    public bool IsDisposed => _isDisposed;
    
    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _dirty.Clear();
        _recalculateChildren.Clear();
        SceneInvalidated = null;

        Stop();

        MediaContext.Instance.SyncDisposeCompositionTarget(CompositionTarget);
    }
}
