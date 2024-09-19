using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Avalonia.Collections.Pooled;
using Avalonia.Diagnostics;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server
{
    /// <summary>
    /// Server-side counterpart of the <see cref="CompositionTarget"/>
    /// That's the place where we update visual transforms, track dirty rects and actually do rendering
    /// </summary>
    internal partial class ServerCompositionTarget : IDisposable
    {
        private readonly ServerCompositor _compositor;
        private readonly Func<IEnumerable<object>> _surfaces;
        private CompositionTargetOverlays _overlays;
        private static long s_nextId = 1;
        private IRenderTarget? _renderTarget;
        private PixelSize _layerSize;
        private IDrawingContextLayerImpl? _layer;
        private bool _updateRequested;
        private bool _redrawRequested;
        private bool _fullRedrawRequested;
        private bool _disposed;
        private readonly HashSet<ServerCompositionVisual> _attachedVisuals = new();
        private readonly Queue<ServerCompositionVisual> _adornerUpdateQueue = new();

        public long Id { get; }
        public ulong Revision { get; private set; }
        public ICompositionTargetDebugEvents? DebugEvents { get; set; }
        public ReadbackIndices Readback { get; } = new();
        public int RenderedVisuals { get; set; }

        public ServerCompositionTarget(ServerCompositor compositor, Func<IEnumerable<object>> surfaces,
            DiagnosticTextRenderer? diagnosticTextRenderer)
            : base(compositor)
        {
            _compositor = compositor;
            _surfaces = surfaces;
            _overlays = new CompositionTargetOverlays(this, diagnosticTextRenderer);
            var platformRender = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            DirtyRects = compositor.Options.UseRegionDirtyRectClipping == true &&
                         platformRender?.SupportsRegions == true
                ? new RegionDirtyRectTracker(platformRender)
                : new DirtyRectTracker();
            Id = Interlocked.Increment(ref s_nextId);
        }
        
        partial void OnIsEnabledChanged()
        {
            if (IsEnabled)
            {
                _compositor.AddCompositionTarget(this);
                foreach (var v in _attachedVisuals)
                    v.Activate();
            }
            else
            {
                _compositor.RemoveCompositionTarget(this);
                foreach (var v in _attachedVisuals)
                    v.Deactivate();
            }
        }

        partial void OnDebugOverlaysChanged()
        {
            _fullRedrawRequested = true;
            _overlays.OnChanged(DebugOverlays);
        }

        partial void DeserializeChangesExtra(BatchStreamReader c)
        {
            _redrawRequested = true;
            _fullRedrawRequested = true;
        }

        public void Render()
        {
            if (_disposed)
            {
                Compositor.RemoveCompositionTarget(this);
                return;
            }

            if (Root == null) 
                return;

            if (_renderTarget?.IsCorrupted == true)
            {
                _layer?.Dispose();
                _layer = null;
                _renderTarget.Dispose();
                _renderTarget = null;
                _redrawRequested = true;
            }

            try
            {
                _renderTarget ??= _compositor.CreateRenderTarget(_surfaces());
            }
            catch (RenderTargetNotReadyException)
            {
                return;
            }
            catch (RenderTargetCorruptedException)
            {
                return;
            }

            if (DirtyRects.IsEmpty && !_redrawRequested && !_updateRequested)
                return;

            Revision++;

            var rUpdateTiming = AvaloniaMetrics.BeginCompositorUpdate();
            var renderTiming = AvaloniaMetrics.BeginCompositorRender();

            var transform = Matrix.CreateScale(Scaling, Scaling);
            // Update happens in a separate phase to extend dirty rect if needed
            Root.Update(this, transform);

            while (_adornerUpdateQueue.Count > 0)
            {
                var adorner = _adornerUpdateQueue.Dequeue();
                adorner.Update(this, transform);
            }

            _updateRequested = false;
            Readback.CompleteWrite(Revision);

            rUpdateTiming.Dispose();

            if (!_redrawRequested)
                return;

            var renderTargetWithProperties = _renderTarget as IRenderTargetWithProperties;

            
            var needLayer = _overlays.RequireLayer // Check if we don't need overlays
                            // Check if render target can be rendered to directly and preserves the previous frame
                            || !(renderTargetWithProperties?.Properties.RetainsPreviousFrameContents == true
                                && renderTargetWithProperties?.Properties.IsSuitableForDirectRendering == true);
            
            using (var renderTargetContext = _renderTarget.CreateDrawingContextWithProperties(false, out var properties))
            {
                if(needLayer && (PixelSize != _layerSize || _layer == null || _layer.IsCorrupted))
                {
                    _layer?.Dispose();
                    _layer = null;
                    _layer = renderTargetContext.CreateLayer(PixelSize);
                    _layerSize = PixelSize;
                    DirtyRects.AddRect(new LtrbPixelRect(_layerSize));
                }
                else if (!needLayer)
                {
                    _layer?.Dispose();
                    _layer = null;
                }

                if (_fullRedrawRequested || (!needLayer && !properties.PreviousFrameIsRetained))
                {
                    DirtyRects.AddRect(new LtrbPixelRect(_layerSize));
                    _fullRedrawRequested = false;
                }

                if (!DirtyRects.IsEmpty)
                {
                    if (_layer != null)
                    {
                        using (var context = _layer.CreateDrawingContext(false))
                            RenderRootToContextWithClip(context, Root);

                        renderTargetContext.Clear(Colors.Transparent);
                        renderTargetContext.Transform = Matrix.Identity;
                        if (_layer.CanBlit)
                            _layer.Blit(renderTargetContext);
                        else
                        {
                            var rect = new PixelRect(default, PixelSize).ToRect(1);
                            renderTargetContext.DrawBitmap(_layer, 1, rect, rect);
                        }
                        renderTiming.Dispose();
                        _overlays.Draw(renderTargetContext, true);
                    }
                    else
                    {
                        RenderRootToContextWithClip(renderTargetContext, Root);
                        renderTiming.Dispose();
                        _overlays.Draw(renderTargetContext, false);
                    }
                }

                RenderedVisuals = 0;

                _redrawRequested = false;
                DirtyRects.Reset();
            }
        }

        void RenderRootToContextWithClip(IDrawingContextImpl context, ServerCompositionVisual root)
        {
            var useLayerClip = Compositor.Options.UseSaveLayerRootClip ?? false;
            
            using (DirtyRects.BeginDraw(context))
            {
                context.Clear(Colors.Transparent);
                if (useLayerClip)
                    context.PushLayer(DirtyRects.CombinedRect.ToRectUnscaled());

                using (var proxy = new CompositorDrawingContextProxy(context))
                {
                    var ctx = new ServerVisualRenderContext(proxy, DirtyRects, false, true);
                    root.Render(ctx, null);
                }

                if (useLayerClip)
                    context.PopLayer();
            }
        }
        
        public void RequestUpdate() => _updateRequested = true;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            using (_compositor.RenderInterface.EnsureCurrent())
            {
                if (_layer != null)
                {
                    _layer.Dispose();
                    _layer = null;
                }

                _renderTarget?.Dispose();
                _renderTarget = null;
            }
            _compositor.RemoveCompositionTarget(this);
        }

        public void AddVisual(ServerCompositionVisual visual)
        {
            if (_attachedVisuals.Add(visual) && IsEnabled)
                visual.Activate();
        }

        public void RemoveVisual(ServerCompositionVisual visual)
        {
            if (_attachedVisuals.Remove(visual) && IsEnabled)
                visual.Deactivate();
            if (visual.IsVisibleInFrame)
                AddDirtyRect(visual.TransformedOwnContentBounds);
        }

        public void EnqueueAdornerUpdate(ServerCompositionVisual visual) => _adornerUpdateQueue.Enqueue(visual);
    }
}
