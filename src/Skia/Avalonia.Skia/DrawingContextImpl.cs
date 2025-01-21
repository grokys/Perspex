using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Utilities;
using Avalonia.Skia.Helpers;
using Avalonia.Utilities;
using SkiaSharp;
using ISceneBrush = Avalonia.Media.ISceneBrush;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia based drawing context.
    /// </summary>
    internal partial class DrawingContextImpl : IDrawingContextImpl,
        IDrawingContextWithAcrylicLikeSupport,
        IDrawingContextImplWithEffects
    {
        private IDisposable?[]? _disposables;
        // TODO: Get rid of this value, it's currently used to calculate intermediate sizes for tile brushes
        // but does so ignoring the current transform
        private readonly Vector _intermediateSurfaceDpi;
        private readonly Stack<(SKMatrix matrix, PaintWrapper paint)> _maskStack = new();
        private readonly Stack<double> _opacityStack = new();
        private readonly Stack<RenderOptions> _renderOptionsStack = new();
        private readonly Matrix? _postTransform;
        private double _currentOpacity = 1.0f;
        private readonly bool _disableSubpixelTextRendering;
        private Matrix? _currentTransform;
        private bool _disposed;
        private GRContext? _grContext;
        public GRContext? GrContext => _grContext;
        private readonly ISkiaGpu? _gpu;
        private readonly SKPaint _strokePaint = SKPaintCache.Shared.Get();
        private readonly SKPaint _fillPaint = SKPaintCache.Shared.Get();
        private readonly SKPaint _boxShadowPaint = SKPaintCache.Shared.Get();
        private static SKShader? s_acrylicNoiseShader;
        private readonly ISkiaGpuRenderSession? _session;
        private bool _leased;
        private bool _useOpacitySaveLayer;

        /// <summary>
        /// Context create info.
        /// </summary>
        public struct CreateInfo
        {
            /// <summary>
            /// Canvas to draw to.
            /// </summary>
            public SKCanvas? Canvas;

            /// <summary>
            /// Surface to draw to.
            /// </summary>
            public SKSurface? Surface;

            /// <summary>
            /// Makes DPI to be applied as a hidden matrix transform
            /// </summary>
            public bool ScaleDrawingToDpi;
            
            /// <summary>
            /// Dpi for intermediate surfaces
            /// </summary>
            public Vector Dpi;

            /// <summary>
            /// Render text without subpixel antialiasing.
            /// </summary>
            public bool DisableSubpixelTextRendering;

            /// <summary>
            /// GPU-accelerated context (optional)
            /// </summary>
            public GRContext? GrContext;

            /// <summary>
            /// Skia GPU provider context (optional)
            /// </summary>
            public ISkiaGpu? Gpu;

            public ISkiaGpuRenderSession? CurrentSession;
        }

        private class SkiaLeaseFeature : ISkiaSharpApiLeaseFeature
        {
            private readonly DrawingContextImpl _context;

            public SkiaLeaseFeature(DrawingContextImpl context)
            {
                _context = context;
            }

            public ISkiaSharpApiLease Lease()
            {
                _context.CheckLease();
                return new ApiLease(_context);
            }

            private class ApiLease : ISkiaSharpApiLease
            {
                private readonly DrawingContextImpl _context;
                private readonly SKMatrix _revertTransform;
                private bool _isDisposed;
                private bool _leased;

                public ApiLease(DrawingContextImpl context)
                {
                    _revertTransform = context.Canvas.TotalMatrix;
                    _context = context;
                    _context._leased = true;
                }

                void CheckLease()
                {
                    if (_leased)
                        throw new InvalidOperationException("The underlying graphics API is currently leased");
                }

                T CheckLease<T>(T rv)
                {
                    CheckLease();
                    return rv;
                }

                public SKCanvas SkCanvas => CheckLease(_context.Canvas);
                // GrContext is accessible during the lease since one might want to wrap native resources
                // Into Skia ones
                public GRContext? GrContext => _context.GrContext;
                public SKSurface? SkSurface => CheckLease(_context.Surface);
                public double CurrentOpacity => CheckLease(_context._currentOpacity);


                public void Dispose()
                {
                    if (!_isDisposed)
                    {
                        _context.Canvas.SetMatrix(_revertTransform);
                        _context._leased = false;
                        _isDisposed = true;
                    }
                }

                class PlatformApiLease : ISkiaSharpPlatformGraphicsApiLease
                {
                    private readonly ApiLease _parent;

                    public PlatformApiLease(ApiLease parent, IPlatformGraphicsContext context)
                    {
                        _parent = parent;
                        _parent.GrContext?.Flush();
                        Context = context;
                        _parent._leased = true;
                    }
                    
                    public void Dispose()
                    {
                        _parent._leased = false;
                        _parent.GrContext?.ResetContext();
                    }

                    public IPlatformGraphicsContext Context { get; }
                }
                
                public ISkiaSharpPlatformGraphicsApiLease? TryLeasePlatformGraphicsApi()
                {
                    CheckLease();
                    if (_context._gpu is ISkiaGpuWithPlatformGraphicsContext gpu &&
                        gpu.PlatformGraphicsContext is { } context)
                        return new PlatformApiLease(this, context);
                    return null;
                }
            }
        }
        
        /// <summary>
        /// Create new drawing context.
        /// </summary>
        /// <param name="createInfo">Create info.</param>
        /// <param name="disposables">Array of elements to dispose after drawing has finished.</param>
        public DrawingContextImpl(CreateInfo createInfo, params IDisposable?[]? disposables)
        {
            Canvas = createInfo.Canvas ?? createInfo.Surface?.Canvas
                ?? throw new ArgumentException("Invalid create info - no Canvas provided", nameof(createInfo));

            _intermediateSurfaceDpi = createInfo.Dpi;
            _disposables = disposables;
            _disableSubpixelTextRendering = createInfo.DisableSubpixelTextRendering;
            _grContext = createInfo.GrContext;
            _gpu = createInfo.Gpu;
            if (_grContext != null)
                Monitor.Enter(_grContext);
            Surface = createInfo.Surface;

            _session = createInfo.CurrentSession;

            
            if (createInfo.ScaleDrawingToDpi && !createInfo.Dpi.NearlyEquals(SkiaPlatform.DefaultDpi))
            {
                _postTransform =
                    Matrix.CreateScale(createInfo.Dpi.X / SkiaPlatform.DefaultDpi.X,
                        createInfo.Dpi.Y / SkiaPlatform.DefaultDpi.Y);
            }

            Transform = Matrix.Identity;

            var options = AvaloniaLocator.Current.GetService<SkiaOptions>();

            if(options != null)
            {
                _useOpacitySaveLayer = options.UseOpacitySaveLayer;
            }
        }
        
        /// <summary>
        /// Skia canvas.
        /// </summary>
        public SKCanvas Canvas { get; }
        public SKSurface? Surface { get; }

        public RenderOptions RenderOptions { get; set; }

        private void CheckLease()
        {
            if (_leased)
                throw new InvalidOperationException("The underlying graphics API is currently leased");
        }
        
        /// <inheritdoc />
        public void Clear(Color color)
        {
            CheckLease();
            Canvas.Clear(color.ToSKColor());
        }

        /// <inheritdoc />
        public void DrawBitmap(IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect)
        {
            CheckLease();
            var drawableImage = (IDrawableBitmapImpl)source;
            var s = sourceRect.ToSKRect();
            var d = destRect.ToSKRect();

            var paint = SKPaintCache.Shared.Get();

            paint.Color = new SKColor(255, 255, 255, (byte)(255 * opacity * _currentOpacity));
            paint.FilterQuality = RenderOptions.BitmapInterpolationMode.ToSKFilterQuality();
            paint.BlendMode = RenderOptions.BitmapBlendingMode.ToSKBlendMode();

            drawableImage.Draw(this, s, d, paint);
            SKPaintCache.Shared.ReturnReset(paint);
        }

        /// <inheritdoc />
        public void DrawBitmap(IBitmapImpl source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
        {
            CheckLease();
            PushOpacityMask(opacityMask, opacityMaskRect);
            DrawBitmap(source, 1, new Rect(0, 0, source.PixelSize.Width, source.PixelSize.Height), destRect);
            PopOpacityMask();
        }

        /// <inheritdoc />
        public void DrawLine(IPen? pen, Point p1, Point p2)
        {
            CheckLease();

            if (pen is not null
                && TryCreatePaint(_strokePaint, pen, new Rect(p1, p2).Normalize()) is { } stroke)
            {
                using (stroke)
                {
                    Canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, stroke.Paint);
                }
            }
        }

        /// <inheritdoc />
        public void DrawGeometry(IBrush? brush, IPen? pen, IGeometryImpl geometry)
        {
            CheckLease();
            var impl = (GeometryImpl) geometry;
            var rect = geometry.Bounds;

            if (brush is not null && impl.FillPath != null)
            {
                using (var fill = CreatePaint(_fillPaint, brush, rect))
                {
                    Canvas.DrawPath(impl.FillPath, fill.Paint);
                }
            }

            if (pen is not null
                && impl.StrokePath != null
                && TryCreatePaint(_strokePaint, pen, rect.Inflate(new Thickness(pen.Thickness / 2))) is { } stroke)
            {
                using (stroke)
                {
                    Canvas.DrawPath(impl.StrokePath, stroke.Paint);
                }
            }
        }

        private static float SkBlurRadiusToSigma(double radius) {
            if (radius <= 0)
                return 0.0f;
            return 0.288675f * (float)radius + 0.5f;
        }
        
        private struct BoxShadowFilter : IDisposable
        {
            public readonly SKPaint Paint;
            private readonly SKImageFilter? _filter;
            public readonly SKClipOperation ClipOperation;

            private BoxShadowFilter(SKPaint paint, SKImageFilter? filter, SKClipOperation clipOperation)
            {
                Paint = paint;
                _filter = filter;
                ClipOperation = clipOperation;
            }

            public static BoxShadowFilter Create(SKPaint paint, BoxShadow shadow, double opacity)
            {
                var ac = shadow.Color;

                var filter = SKImageFilter.CreateBlur(SkBlurRadiusToSigma(shadow.Blur), SkBlurRadiusToSigma(shadow.Blur));
                var color = new SKColor(ac.R, ac.G, ac.B, (byte)(ac.A * opacity));

                paint.Reset();
                paint.IsAntialias = true;
                paint.Color = color;
                paint.ImageFilter = filter;

                var clipOperation = shadow.IsInset ? SKClipOperation.Intersect : SKClipOperation.Difference;

                return new BoxShadowFilter(paint, filter, clipOperation);
            }

            public void Dispose()
            {
                Paint?.Reset();
                _filter?.Dispose();
            }
        }

        private static SKRect AreaCastingShadowInHole(
            SKRect hole_rect,
            float shadow_blur,
            float shadow_spread,
            float offsetX, float offsetY)
        {
            // Adapted from Chromium
            var bounds = hole_rect;

            bounds.Inflate(shadow_blur, shadow_blur);

            if (shadow_spread < 0)
                bounds.Inflate(-shadow_spread, -shadow_spread);

            var offset_bounds = bounds;
            offset_bounds.Offset(-offsetX, -offsetY);
            bounds.Union(offset_bounds);
            return bounds;
        }

        /// <inheritdoc />
        public void DrawRectangle(IExperimentalAcrylicMaterial? material, RoundedRect rect)
        {
            if (rect.Rect.Height <= 0 || rect.Rect.Width <= 0)
                return;
            CheckLease();
            
            var rc = rect.Rect.ToSKRect();
            SKRoundRect? skRoundRect = null;

            if (rect.IsRounded)
            {
                skRoundRect = SKRoundRectCache.Shared.Get();
                skRoundRect.SetRectRadii(rc,
                    new[]
                    {
                        rect.RadiiTopLeft.ToSKPoint(),
                        rect.RadiiTopRight.ToSKPoint(),
                        rect.RadiiBottomRight.ToSKPoint(),
                        rect.RadiiBottomLeft.ToSKPoint(),
                    });
            }

            if (material != null)
            {
                using (var paint = CreateAcrylicPaint(_fillPaint, material))
                {
                    if (skRoundRect is not null)
                    {
                        Canvas.DrawRoundRect(skRoundRect, paint.Paint);
                        SKRoundRectCache.Shared.Return(skRoundRect);
                    }
                    else
                    {
                        Canvas.DrawRect(rc, paint.Paint);
                    }

                }
            }
        }

        /// <inheritdoc />
        public void DrawRectangle(IBrush? brush, IPen? pen, RoundedRect rect, BoxShadows boxShadows = default)
        {
            if (rect.Rect.Height <= 0 || rect.Rect.Width <= 0)
                return;
            CheckLease();
            // Arbitrary chosen values
            // On OSX Skia breaks OpenGL context when asked to draw, e. g. (0, 0, 623, 6666600) rect
            if (rect.Rect.Height > 8192 || rect.Rect.Width > 8192)
                boxShadows = default;

            var rc = rect.Rect.ToSKRect();
            var isRounded = rect.IsRounded;
            var needRoundRect = rect.IsRounded || (boxShadows.HasInsetShadows);
            SKRoundRect? skRoundRect = null;
            if (needRoundRect)
            {
                skRoundRect = SKRoundRectCache.Shared.GetAndSetRadii(rc, rect);
            }

            foreach (var boxShadow in boxShadows)
            {
                if (boxShadow != default && !boxShadow.IsInset)
                {
                    using (var shadow = BoxShadowFilter.Create(_boxShadowPaint, boxShadow, _currentOpacity))
                    {
                        var spread = (float)boxShadow.Spread;
                        if (boxShadow.IsInset)
                            spread = -spread;

                        Canvas.Save();
                        if (isRounded)
                        {
                            var shadowRect = SKRoundRectCache.Shared.GetAndSetRadii(skRoundRect!.Rect, skRoundRect.Radii);
                            if (spread != 0)
                                shadowRect.Inflate(spread, spread);
                            Canvas.ClipRoundRect(skRoundRect,
                                shadow.ClipOperation, true);
                            
                            var oldTransform = Transform;
                            Transform = oldTransform * Matrix.CreateTranslation(boxShadow.OffsetX, boxShadow.OffsetY);
                            Canvas.DrawRoundRect(shadowRect, shadow.Paint);
                            Transform = oldTransform;
                            SKRoundRectCache.Shared.Return(shadowRect);
                        }
                        else
                        {
                            var shadowRect = rc;
                            if (spread != 0)
                                shadowRect.Inflate(spread, spread);
                            Canvas.ClipRect(rc, shadow.ClipOperation);
                            var oldTransform = Transform;
                            Transform = oldTransform * Matrix.CreateTranslation(boxShadow.OffsetX, boxShadow.OffsetY);
                            Canvas.DrawRect(shadowRect, shadow.Paint);
                            Transform = oldTransform;
                        }

                        RestoreCanvas();
                    }
                }
            }

            if (brush != null)
            {
                using (var fill = CreatePaint(_fillPaint, brush, rect.Rect))
                {
                    if (isRounded)
                    {
                        Canvas.DrawRoundRect(skRoundRect, fill.Paint);
                    }
                    else
                    {
                        Canvas.DrawRect(rc, fill.Paint);
                    }
                }
            }

            foreach (var boxShadow in boxShadows)
            {
                if (boxShadow != default && boxShadow.IsInset)
                {
                    using (var shadow = BoxShadowFilter.Create(_boxShadowPaint, boxShadow, _currentOpacity))
                    {
                        var spread = (float)boxShadow.Spread;
                        var offsetX = (float)boxShadow.OffsetX;
                        var offsetY = (float)boxShadow.OffsetY;
                        var outerRect = AreaCastingShadowInHole(rc, (float)boxShadow.Blur, spread, offsetX, offsetY);

                        Canvas.Save();
                        var shadowRect = SKRoundRectCache.Shared.GetAndSetRadii(skRoundRect!.Rect, skRoundRect.Radii);
                        if (spread != 0)
                            shadowRect.Deflate(spread, spread);
                        Canvas.ClipRoundRect(skRoundRect,
                            shadow.ClipOperation, true);
                        
                        var oldTransform = Transform;
                        Transform = oldTransform * Matrix.CreateTranslation(boxShadow.OffsetX, boxShadow.OffsetY);
                        using (var outerRRect = new SKRoundRect(outerRect))
                            Canvas.DrawRoundRectDifference(outerRRect, shadowRect, shadow.Paint);
                        Transform = oldTransform;
                        RestoreCanvas();
                        SKRoundRectCache.Shared.Return(shadowRect);
                    }
                }
            }

            if (pen is not null
                && TryCreatePaint(_strokePaint, pen, rect.Rect.Inflate(new Thickness(pen.Thickness / 2))) is { } stroke)
            {
                using (stroke)
                {
                    if (isRounded)
                    {
                        Canvas.DrawRoundRect(skRoundRect, stroke.Paint);
                    }
                    else
                    {
                        Canvas.DrawRect(rc, stroke.Paint);
                    }
                }
            }

            if (skRoundRect is not null)
                SKRoundRectCache.Shared.Return(skRoundRect);
        }

        /// <inheritdoc />
        public void DrawRegion(IBrush? brush, IPen? pen, IPlatformRenderInterfaceRegion region)
        {
            var r = (SkiaRegionImpl)region;
            if(r.IsEmpty)
                return;
            CheckLease();
            
            if (brush != null)
            {
                using (var fill = CreatePaint(_fillPaint, brush, r.Bounds.ToRectUnscaled()))
                {
                    Canvas.DrawRegion(r.Region, fill.Paint);
                }
            }

            if (pen is not null
                && TryCreatePaint(_strokePaint, pen, r.Bounds.ToRectUnscaled().Inflate(new Thickness(pen.Thickness / 2))) is { } stroke)
            {
                using (stroke)
                {
                    Canvas.DrawRegion(r.Region, stroke.Paint);
                }
            }
        }

        /// <inheritdoc />
        public void DrawEllipse(IBrush? brush, IPen? pen, Rect rect)
        {
            if (rect.Height <= 0 || rect.Width <= 0)
                return;
            CheckLease();
            
            var rc = rect.ToSKRect();

            if (brush != null)
            {
                using (var fill = CreatePaint(_fillPaint, brush, rect))
                {
                    Canvas.DrawOval(rc, fill.Paint);
                }
            }

            if (pen is not null
                && TryCreatePaint(_strokePaint, pen, rect.Inflate(new Thickness(pen.Thickness / 2))) is { } stroke)
            {
                using (stroke)
                {
                    Canvas.DrawOval(rc, stroke.Paint);
                }
            }
        }
       
        /// <inheritdoc />
        public void DrawGlyphRun(IBrush? foreground, IGlyphRunImpl glyphRun)
        {
            CheckLease();

            if (foreground is null)
            {
                return;
            }

            using (var paintWrapper = CreatePaint(_fillPaint, foreground, glyphRun.Bounds))
            {
                var glyphRunImpl = (GlyphRunImpl)glyphRun;

                var textRenderOptions = RenderOptions;

                if (_disableSubpixelTextRendering)
                {
                    switch (textRenderOptions.TextRenderingMode)
                    {
                        case TextRenderingMode.Unspecified
                            when textRenderOptions.EdgeMode == EdgeMode.Antialias || textRenderOptions.EdgeMode == EdgeMode.Unspecified:
                        case TextRenderingMode.SubpixelAntialias:
                            {
                                textRenderOptions = textRenderOptions with { TextRenderingMode = TextRenderingMode.Antialias };
                                break;
                            }
                    }
                }

                var textBlob = glyphRunImpl.GetTextBlob(textRenderOptions);

                Canvas.DrawText(textBlob, (float)glyphRun.BaselineOrigin.X,
                    (float)glyphRun.BaselineOrigin.Y, paintWrapper.Paint);
            }
        }

        /// <inheritdoc />
        public IDrawingContextLayerImpl CreateLayer(PixelSize size)
        {
            CheckLease();
            return CreateRenderTarget(size, true);
        }

        /// <inheritdoc />
        public void PushClip(Rect clip)
        {
            CheckLease();
            Canvas.Save();
            Canvas.ClipRect(clip.ToSKRect());
        }

        public void PushClip(RoundedRect clip)
        {
            CheckLease();
            Canvas.Save();

            // Get the rounded rectangle
            var rc = clip.Rect.ToSKRect();

            // Get a round rect from the cache.
            var roundRect = SKRoundRectCache.Shared.Get();

            roundRect.SetRectRadii(rc,
                new[]
                {
                    clip.RadiiTopLeft.ToSKPoint(), clip.RadiiTopRight.ToSKPoint(),
                    clip.RadiiBottomRight.ToSKPoint(), clip.RadiiBottomLeft.ToSKPoint(),
                });

            Canvas.ClipRoundRect(roundRect, antialias:true);

            // Should not need to reset as SetRectRadii overrides the values.
            SKRoundRectCache.Shared.Return(roundRect);
        }

        public void PushClip(IPlatformRenderInterfaceRegion region)
        {
            var r = ((SkiaRegionImpl)region).Region;
            CheckLease();
            Canvas.Save();
            Canvas.ClipRegion(r);
        }

        private void RestoreCanvas()
        {
            _currentTransform = null;
            Canvas.Restore();
        }
        
        /// <inheritdoc />
        public void PopClip()
        {
            CheckLease();
            RestoreCanvas();
        }

        public void PushLayer(Rect bounds)
        {
            CheckLease();
            Canvas.SaveLayer(bounds.ToSKRect(), null!);
        }

        public void PopLayer()
        {
            CheckLease();
            RestoreCanvas();
        }

        /// <inheritdoc />
        public void PushOpacity(double opacity, Rect? bounds)
        {
            CheckLease();

            _opacityStack.Push(_currentOpacity);

            var useOpacitySaveLayer = _useOpacitySaveLayer || RenderOptions.RequiresFullOpacityHandling == true;

            if (useOpacitySaveLayer)
            {
                opacity = _currentOpacity * opacity; //Take current multiplied opacity

                _currentOpacity = 1; //Opacity is applied via layering

                if (bounds.HasValue)
                {
                    var rect = bounds.Value.ToSKRect();
                    Canvas.SaveLayer(rect, new SKPaint { ColorF = new SKColorF(0, 0, 0, (float)opacity) });
                }
                else
                {
                    Canvas.SaveLayer(new SKPaint { ColorF = new SKColorF(0, 0, 0, (float)opacity) });
                }
            }
            else
            {
                _currentOpacity *= opacity;
            }
        }

        /// <inheritdoc />
        public void PopOpacity()
        {
            CheckLease();

            var useOpacitySaveLayer = _useOpacitySaveLayer || RenderOptions.RequiresFullOpacityHandling == true;

            if (useOpacitySaveLayer)
            {
                RestoreCanvas();
            }

            _currentOpacity = _opacityStack.Pop();
        }

        /// <inheritdoc />
        public void PushRenderOptions(RenderOptions renderOptions)
        {
            CheckLease();

            _renderOptionsStack.Push(RenderOptions);

            RenderOptions = RenderOptions.MergeWith(renderOptions);
        }

        public void PopRenderOptions()
        {
            RenderOptions = _renderOptionsStack.Pop();
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            if(_disposed)
                return;
            CheckLease();
            try
            {
                // Return leased paints.
                SKPaintCache.Shared.ReturnReset(_strokePaint);
                SKPaintCache.Shared.ReturnReset(_fillPaint);
                SKPaintCache.Shared.ReturnReset(_boxShadowPaint);

                if (_grContext != null)
                {
                    Monitor.Exit(_grContext);
                    _grContext = null;
                }

                if (_disposables != null)
                {
                    foreach (var disposable in _disposables)
                        disposable?.Dispose();
                    _disposables = null;
                }
            }
            finally
            {
                _disposed = true;
            }
        }

        /// <inheritdoc />
        public void PushGeometryClip(IGeometryImpl clip)
        {
            CheckLease();
            Canvas.Save();
            Canvas.ClipPath(((GeometryImpl)clip).FillPath, SKClipOperation.Intersect, true);
        }

        /// <inheritdoc />
        public void PopGeometryClip()
        {
            CheckLease();
            RestoreCanvas();
        }

        /// <inheritdoc />
        public void PushOpacityMask(IBrush mask, Rect bounds)
        {
            CheckLease();

            var paint = SKPaintCache.Shared.Get();

            Canvas.SaveLayer(bounds.ToSKRect(), paint);
            _maskStack.Push((Canvas.TotalMatrix, CreatePaint(paint, mask, bounds)));
        }

        /// <inheritdoc />
        public void PopOpacityMask()
        {
            CheckLease();

            var paint = SKPaintCache.Shared.Get();
            paint.BlendMode = SKBlendMode.DstIn;
            
            Canvas.SaveLayer(paint);
            SKPaintCache.Shared.ReturnReset(paint);
            
            var (transform, paintWrapper) = _maskStack.Pop();
            Canvas.SetMatrix(transform);
            using (paintWrapper)
            {
                Canvas.DrawPaint(paintWrapper.Paint);
            }
            // Return the paint wrapper's paint less the reset since the paint is already reset in the Dispose method above.
            SKPaintCache.Shared.Return(paintWrapper.Paint);

            RestoreCanvas();

            RestoreCanvas();
        }

        /// <inheritdoc />
        public Matrix Transform
        {
            get { return _currentTransform ??= Canvas.TotalMatrix.ToAvaloniaMatrix(); }
            set
            {
                CheckLease();
                if (_currentTransform == value)
                    return;

                _currentTransform = value;

                var transform = value;

                if (_postTransform.HasValue)
                {
                    transform *= _postTransform.Value;
                }

                Canvas.SetMatrix(transform.ToSKMatrix());
            }
        }

        public object? GetFeature(Type t)
        {
            if (t == typeof(ISkiaSharpApiLeaseFeature))
                return new SkiaLeaseFeature(this);
            return null;
        }

        /// <summary>
        /// Configure paint wrapper for using gradient brush.
        /// </summary>
        /// <param name="paintWrapper">Paint wrapper.</param>
        /// <param name="targetRect">Target rect.</param>
        /// <param name="gradientBrush">Gradient brush.</param>
        private static void ConfigureGradientBrush(ref PaintWrapper paintWrapper, Rect targetRect, IGradientBrush gradientBrush)
        {
            var tileMode = gradientBrush.SpreadMethod.ToSKShaderTileMode();
            var stopColors = gradientBrush.GradientStops.Select(s => s.Color.ToSKColor()).ToArray();
            var stopOffsets = gradientBrush.GradientStops.Select(s => (float)s.Offset).ToArray();

            switch (gradientBrush)
            {
                case ILinearGradientBrush linearGradient:
                    {
                        var start = linearGradient.StartPoint.ToPixels(targetRect).ToSKPoint();
                        var end = linearGradient.EndPoint.ToPixels(targetRect).ToSKPoint();

                        // would be nice to cache these shaders possibly?
                        if (linearGradient.Transform is null)
                        {
                            using (var shader =
                                SKShader.CreateLinearGradient(start, end, stopColors, stopOffsets, tileMode))
                            {
                                paintWrapper.Paint.Shader = shader;
                            }
                        }
                        else
                        {
                            var transformOrigin = linearGradient.TransformOrigin.ToPixels(targetRect);
                            var offset = Matrix.CreateTranslation(transformOrigin);
                            var transform = (-offset) * linearGradient.Transform.Value * (offset);

                            using (var shader =
                                SKShader.CreateLinearGradient(start, end, stopColors, stopOffsets, tileMode, transform.ToSKMatrix()))
                            {
                                paintWrapper.Paint.Shader = shader;
                            }
                        }

                        break;
                    }
                case IRadialGradientBrush radialGradient:
                    {
                        var centerPoint = radialGradient.Center.ToPixels(targetRect);
                        var center = centerPoint.ToSKPoint();

                        var radiusX = (radialGradient.RadiusX.ToValue(targetRect.Width));
                        var radiusY = (radialGradient.RadiusY.ToValue(targetRect.Height));

                        var originPoint = radialGradient.GradientOrigin.ToPixels(targetRect);

                        Matrix? transform = null;

                        if (radiusX != radiusY)
                            transform =
                                Matrix.CreateTranslation(-centerPoint)
                                * Matrix.CreateScale(1, radiusY / radiusX)
                                * Matrix.CreateTranslation(centerPoint);


                        if (radialGradient.Transform != null)
                        {
                            var transformOrigin = radialGradient.TransformOrigin.ToPixels(targetRect);
                            var offset = Matrix.CreateTranslation(transformOrigin);
                            var brushTransform = (-offset) * radialGradient.Transform.Value * (offset);
                            transform = transform.HasValue ? transform * brushTransform : brushTransform;
                        }

                        if (originPoint.Equals(centerPoint))
                        {
                            // when the origin is the same as the center the Skia RadialGradient acts the same as D2D
                            using (var shader =
                                       transform.HasValue
                                           ? SKShader.CreateRadialGradient(center, (float)radiusX, stopColors, stopOffsets, tileMode,
                                               transform.Value.ToSKMatrix())
                                           : SKShader.CreateRadialGradient(center, (float)radiusX, stopColors, stopOffsets, tileMode)
                                      )
                            {
                                paintWrapper.Paint.Shader = shader;
                            }
                        }
                        else
                        {
                            // when the origin is different to the center use a two point ConicalGradient to match the behaviour of D2D
                            if (radiusX != radiusY)
                                // Adjust the origin point for radiusX/Y transformation by reversing it
                                originPoint = originPoint.WithY(
                                    (originPoint.Y - centerPoint.Y) * radiusX / radiusY + centerPoint.Y);

                            var origin = originPoint.ToSKPoint();
                            var endOffset = 0.0;

                            // and then reverse the reference point of the stops
                            var reversedStops = new float[stopOffsets.Length];

                            for (var i = 0; i < stopOffsets.Length; i++)
                            {
                                var offset = stopOffsets[i];
                                if (endOffset < offset)
                                {
                                    endOffset = offset;
                                }
                                reversedStops[i] = offset;
                                if (reversedStops[i] > 0 && reversedStops[i] < 1)
                                {
                                    reversedStops[i] = Math.Abs(1 - offset);
                                }
                            }

                            var start = origin;
                            var radiusStart = 0f;
                            var end = center;
                            var radiusEnd = (float)radiusX;
                            var reverse = MathUtilities.AreClose(1, endOffset);

                            if (reverse)
                            {
                                (start, radiusStart, end, radiusEnd) = (end, radiusEnd, start, radiusStart);

                                // reverse the order of the stops to match D2D
                                var reversedColors = new SKColor[stopColors.Length];
                                Array.Copy(stopColors, reversedColors, stopColors.Length);
                                Array.Reverse(reversedColors);
                                stopColors = reversedColors;
                                stopOffsets = reversedStops;
                            }

                            // compose with a background colour of the final stop to match D2D's behaviour of filling with the final color
                            using (var shader = SKShader.CreateCompose(
                                       SKShader.CreateColor(stopColors[0]),
                                       transform.HasValue
                                           ? SKShader.CreateTwoPointConicalGradient(start, radiusStart, end, radiusEnd,
                                              stopColors, stopOffsets, tileMode, transform.Value.ToSKMatrix())
                                           : SKShader.CreateTwoPointConicalGradient(start, radiusStart, end, radiusEnd,
                                              stopColors, stopOffsets, tileMode)
                                        )
                                    )
                            {
                                paintWrapper.Paint.Shader = shader;
                            }
                        }

                        break;
                    }
                case IConicGradientBrush conicGradient:
                    {
                        var center = conicGradient.Center.ToPixels(targetRect).ToSKPoint();

                        // Skia's default is that angle 0 is from the right hand side of the center point
                        // but we are matching CSS where the vertical point above the center is 0.
                        var angle = (float)(conicGradient.Angle - 90);
                        var rotation = SKMatrix.CreateRotationDegrees(angle, center.X, center.Y);

                        if (conicGradient.Transform is { })
                        {

                            var transformOrigin = conicGradient.TransformOrigin.ToPixels(targetRect);
                            var offset = Matrix.CreateTranslation(transformOrigin);
                            var transform = (-offset) * conicGradient.Transform.Value * (offset);

                            rotation = rotation.PreConcat(transform.ToSKMatrix());
                        }

                        using (var shader =
                            SKShader.CreateSweepGradient(center, stopColors, stopOffsets, rotation))
                        {
                            paintWrapper.Paint.Shader = shader;
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// Configure paint wrapper for using tile brush.
        /// </summary>
        /// <param name="paintWrapper">Paint wrapper.</param>
        /// <param name="targetBox">Target bounding box.</param>
        /// <param name="tileBrush">Tile brush to use.</param>
        /// <param name="tileBrushImage">Tile brush image.</param>
        private void ConfigureTileBrush(ref PaintWrapper paintWrapper, Rect targetBox, ITileBrush tileBrush, IDrawableBitmapImpl tileBrushImage)
        {
            var calc = new TileBrushCalculator(tileBrush, tileBrushImage.PixelSize.ToSizeWithDpi(_intermediateSurfaceDpi), targetBox.Size);
            var intermediate = CreateRenderTarget(
                PixelSize.FromSizeWithDpi(calc.IntermediateSize, _intermediateSurfaceDpi), false);

            paintWrapper.AddDisposable(intermediate);

            using (var context = intermediate.CreateDrawingContext(true))
            {
                var sourceRect = new Rect(tileBrushImage.PixelSize.ToSizeWithDpi(96));
                var targetRect = new Rect(tileBrushImage.PixelSize.ToSizeWithDpi(_intermediateSurfaceDpi));

                context.Clear(Colors.Transparent);
                context.PushClip(calc.IntermediateClip);
                context.PushRenderOptions(RenderOptions);
                
                context.Transform = calc.IntermediateTransform;
                
                context.DrawBitmap(
                    tileBrushImage,
                    1,
                    sourceRect,
                    targetRect);

                context.PopRenderOptions();
                context.PopClip();
            }

            var tileTransform =
                tileBrush.TileMode != TileMode.None
                    ? SKMatrix.CreateTranslation(-(float)calc.DestinationRect.X, -(float)calc.DestinationRect.Y)
                    : SKMatrix.CreateIdentity();

            SKShaderTileMode tileX =
                tileBrush.TileMode == TileMode.None
                    ? SKShaderTileMode.Decal
                    : tileBrush.TileMode == TileMode.FlipX || tileBrush.TileMode == TileMode.FlipXY
                        ? SKShaderTileMode.Mirror
                        : SKShaderTileMode.Repeat;

            SKShaderTileMode tileY =
                tileBrush.TileMode == TileMode.None
                    ? SKShaderTileMode.Decal
                    : tileBrush.TileMode == TileMode.FlipY || tileBrush.TileMode == TileMode.FlipXY
                        ? SKShaderTileMode.Mirror
                        : SKShaderTileMode.Repeat;


            var image = intermediate.SnapshotImage();
            paintWrapper.AddDisposable(image);

            var paintTransform = default(SKMatrix);

            SKMatrix.Concat(
                ref paintTransform,
                tileTransform,
                SKMatrix.CreateScale((float)(96.0 / _intermediateSurfaceDpi.X), (float)(96.0 / _intermediateSurfaceDpi.Y)));

            if (tileBrush.Transform is { })
            {
                var origin = tileBrush.TransformOrigin.ToPixels(targetBox);
                var offset = Matrix.CreateTranslation(origin);
                var transform = (-offset) * tileBrush.Transform.Value * (offset);

                paintTransform = paintTransform.PreConcat(transform.ToSKMatrix());
            }

            if (tileBrush.DestinationRect.Unit == RelativeUnit.Relative)
                paintTransform =
                    paintTransform.PreConcat(SKMatrix.CreateTranslation((float)targetBox.X, (float)targetBox.Y));

            using (var shader = image.ToShader(tileX, tileY, paintTransform))
            {
                paintWrapper.Paint.Shader = shader;
            }
        }

        private void ConfigureSceneBrushContent(ref PaintWrapper paintWrapper, ISceneBrushContent content,
            Rect targetRect)
        {
            if(content.UseScalableRasterization)
                ConfigureSceneBrushContentWithPicture(ref paintWrapper, content, targetRect);
            else
                ConfigureSceneBrushContentWithSurface(ref paintWrapper, content, targetRect);
        }
        
        private void ConfigureSceneBrushContentWithSurface(ref PaintWrapper paintWrapper, ISceneBrushContent content,
            Rect targetRect)
        {
            var rect = content.Rect;
            var intermediateSize = rect.Size;

            if (intermediateSize.Width >= 1 && intermediateSize.Height >= 1)
            {
                using var intermediate = CreateRenderTarget(
                    PixelSize.FromSizeWithDpi(intermediateSize, _intermediateSurfaceDpi), false);

                using (var ctx = intermediate.CreateDrawingContext(true))
                {
                    ctx.PushRenderOptions(RenderOptions);
                    ctx.Clear(Colors.Transparent);
                    content.Render(ctx, rect.TopLeft == default ? null : Matrix.CreateTranslation(-rect.X, -rect.Y));
                    ctx.PopRenderOptions();
                }

                ConfigureTileBrush(ref paintWrapper, targetRect, content.Brush, intermediate);
            }
        }
        
        private void ConfigureSceneBrushContentWithPicture(ref PaintWrapper paintWrapper, ISceneBrushContent content,
            Rect targetRect)
        {
            // To understand what happens here, read
            // https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.tilebrush
            // and the rest of the docs
            
            // Avalonia follows WPF and WPF's brushes completely ignore whatever layout bounds visuals have, 
            // and instead are using content bounds, e. g.
            // ╔════════════════════════════════════╗  <--- target control
            // ║                                    ║       layout bounds
            // ║  ╔═════╗───────────┐ <--- content  ║
            // ║  ║     ║<- content │      bounds   ║
            // ║  ╚═════╝        ╔══╗               ║
            // ║  │  ^ content   ╚══╝               ║
            // ║  │ ╔═════╗content^ │               ║
            // ║  └─╚═════╝─────────┘               ║
            // ║                                    ║
            // ╚════════════════════════════════════╝
            //
            // Source Rect (aka ViewBox) is relative to the content bounds, not to the visual/drawing
            
            var contentRect = content.Rect;
            var sourceRect = content.Brush.SourceRect.ToPixels(contentRect);
            
            // Early escape
            if (contentRect.Size.Width <= 0 || contentRect.Size.Height <= 0
                || sourceRect.Size.Width <= 0 || sourceRect.Size.Height <= 0)
            {
                paintWrapper.Paint.Color = SKColor.Empty;
                return;
            }
            
            // We are moving the render area to make the top-left corner of the SourceRect (ViewBox) to be at (0,0)
            // of the tile
            var contentRenderTransform = Matrix.CreateTranslation(-sourceRect.X, -sourceRect.Y);
            
            // DestinationRect (aka Viewport) is specified relative to the target rect
            var destinationRect = content.Brush.DestinationRect.ToPixels(targetRect);
            
            // Tile size matches the destination rect size
            var tileSize = destinationRect.Size;
            
            // Apply transforms to stretch content to match the tile
            if (sourceRect.Size != tileSize)
            {
                // Stretch the content rect to match the tile size
                var scale = content.Brush.Stretch.CalculateScaling(tileSize, sourceRect.Size);

                // And move the resulting rect according to alignment rules
                var alignmentTranslate = TileBrushCalculator.CalculateTranslate(
                    content.Brush.AlignmentX,
                    content.Brush.AlignmentY, sourceRect.Size * scale, tileSize);

                contentRenderTransform = contentRenderTransform * Matrix.CreateScale(scale) *
                                         Matrix.CreateTranslation(alignmentTranslate);
            }
            
            // Pre-rasterize the tile into SKPicture
            using var pictureTarget = new PictureRenderTarget(_gpu, _grContext, _intermediateSurfaceDpi);
            using (var ctx = pictureTarget.CreateDrawingContext(tileSize, false))
            {
                ctx.PushRenderOptions(RenderOptions);
                content.Render(ctx, contentRenderTransform);
                ctx.PopRenderOptions();
            }
            using var tile = pictureTarget.GetPicture();
            
            // If there is no BrushTransform and destinationRect is at (0,0) we don't need any transforms
            Matrix shaderTransform = Matrix.Identity;
            
            // Apply Brush.Transform to SKShader
            if (content.Transform != null)
            {
                
                var transformOrigin = content.TransformOrigin.ToPixels(targetRect);
                var offset = Matrix.CreateTranslation(transformOrigin);
                shaderTransform = (-offset) * content.Transform.Value * (offset);
            }

            // Apply destinationRect position
            if (destinationRect.Position != default)
                shaderTransform *= Matrix.CreateTranslation(destinationRect.X, destinationRect.Y);
            
            // Create shader
            var (tileX, tileY) = GetTileModes(content.Brush.TileMode);
            using(var shader = tile.ToShader(tileX, tileY, shaderTransform.ToSKMatrix(), 
                      new SKRect(0, 0, tile.CullRect.Width, tile.CullRect.Height)))
            {
                paintWrapper.Paint.FilterQuality = SKFilterQuality.None;
                paintWrapper.Paint.Shader = shader;
            }
        }

        (SKShaderTileMode x, SKShaderTileMode y) GetTileModes(TileMode mode)
        {
            return (
                mode == TileMode.None
                    ? SKShaderTileMode.Decal
                    : mode == TileMode.FlipX || mode == TileMode.FlipXY
                        ? SKShaderTileMode.Mirror
                        : SKShaderTileMode.Repeat,


                mode == TileMode.None
                    ? SKShaderTileMode.Decal
                    : mode == TileMode.FlipY || mode == TileMode.FlipXY
                        ? SKShaderTileMode.Mirror
                        : SKShaderTileMode.Repeat);
        }

        private static SKColorFilter CreateAlphaColorFilter(double opacity)
        {
            if (opacity > 1)
                opacity = 1;
            var c = new byte[256];
            var a = new byte[256];
            for (var i = 0; i < 256; i++)
            {
                c[i] = (byte)i;
                a[i] = (byte)(i * opacity);
            }

            return SKColorFilter.CreateTable(a, c, c, c);
        }

        private static byte Blend(byte leftColor, byte leftAlpha, byte rightColor, byte rightAlpha)
        {
            var ca = leftColor / 255d;
            var aa = leftAlpha / 255d;
            var cb = rightColor / 255d;
            var ab = rightAlpha / 255d;
            var r = (ca * aa + cb * ab * (1 - aa)) / (aa + ab * (1 - aa));
            return (byte)(r * 255);
        }

        private static Color Blend(Color left, Color right)
        {
            var aa = left.A / 255d;
            var ab = right.A / 255d;
            return new Color(
                (byte)((aa + ab * (1 - aa)) * 255),
                Blend(left.R, left.A, right.R, right.A),
                Blend(left.G, left.A, right.G, right.A),
                Blend(left.B, left.A, right.B, right.A)                
            );
        }

        internal PaintWrapper CreateAcrylicPaint (SKPaint paint, IExperimentalAcrylicMaterial material)
        {
            var paintWrapper = new PaintWrapper(paint);

            paint.IsAntialias = true;

            var tintOpacity =
                material.BackgroundSource == AcrylicBackgroundSource.Digger ?
                material.TintOpacity : 1;

            const double noiseOpcity = 0.0225;

            var tintColor = material.TintColor;
            var tint = new SKColor(tintColor.R, tintColor.G, tintColor.B, tintColor.A);

            if (s_acrylicNoiseShader == null)
            {
                using (var stream = typeof(DrawingContextImpl).Assembly.GetManifestResourceStream("Avalonia.Skia.Assets.NoiseAsset_256X256_PNG.png"))
                using (var bitmap = SKBitmap.Decode(stream))
                {
                    s_acrylicNoiseShader = SKShader.CreateBitmap(bitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat)
                        .WithColorFilter(CreateAlphaColorFilter(noiseOpcity));
                }
            }

            using (var backdrop = SKShader.CreateColor(new SKColor(material.MaterialColor.R, material.MaterialColor.G, material.MaterialColor.B, material.MaterialColor.A)))
            using (var tintShader = SKShader.CreateColor(tint))
            using (var effectiveTint = SKShader.CreateCompose(backdrop, tintShader))
            using (var compose = SKShader.CreateCompose(effectiveTint, s_acrylicNoiseShader))
            {
                paint.Shader = compose;

                if (material.BackgroundSource == AcrylicBackgroundSource.Digger)
                {
                    paint.BlendMode = SKBlendMode.Src;
                }

                return paintWrapper;
            }
        }

        /// <summary>
        /// Creates paint wrapper for given brush.
        /// </summary>
        /// <param name="paint">The paint to wrap.</param>
        /// <param name="brush">Source brush.</param>
        /// <param name="targetRect">Target rect.</param>
        /// <returns>Paint wrapper for given brush.</returns>
        internal PaintWrapper CreatePaint(SKPaint paint, IBrush brush, Rect targetRect)
        {
            var paintWrapper = new PaintWrapper(paint);

            paint.IsAntialias = RenderOptions.EdgeMode != EdgeMode.Aliased;

            double opacity = brush.Opacity * (_useOpacitySaveLayer ? 1 :_currentOpacity);

            if (brush is ISolidColorBrush solid)
            {
                paint.Color = new SKColor(solid.Color.R, solid.Color.G, solid.Color.B, (byte) (solid.Color.A * opacity));

                return paintWrapper;
            }

            paint.Color = new SKColor(255, 255, 255, (byte) (255 * opacity));

            if (brush is IGradientBrush gradient)
            {
                ConfigureGradientBrush(ref paintWrapper, targetRect, gradient);

                return paintWrapper;
            }

            var tileBrush = brush as ITileBrush;
            var tileBrushImage = default(IDrawableBitmapImpl);

            if (brush is ISceneBrush sceneBrush)
            {
                using (var content = sceneBrush.CreateContent())
                {
                    if (content != null)
                    {
                        ConfigureSceneBrushContent(ref paintWrapper, content, targetRect);
                        return paintWrapper;
                    }
                    else
                        paint.Color = default;
                }
            }
            else if (brush is ISceneBrushContent sceneBrushContent)
            {
                ConfigureSceneBrushContent(ref paintWrapper, sceneBrushContent, targetRect);
                return paintWrapper;
            }
            else
            {
                tileBrushImage = (tileBrush as IImageBrush)?.Source?.Bitmap?.Item as IDrawableBitmapImpl;
            }

            if (tileBrush != null && tileBrushImage != null)
            {
                ConfigureTileBrush(ref paintWrapper, targetRect, tileBrush, tileBrushImage);
            }
            else
            {
                paint.Color = new SKColor(255, 255, 255, 0);
            }

            return paintWrapper;
        }

        /// <summary>
        /// Creates paint wrapper for given pen.
        /// </summary>
        /// <param name="paint">The paint to wrap.</param>
        /// <param name="pen">Source pen.</param>
        /// <param name="targetRect">Target rect.</param>
        /// <returns></returns>
        private PaintWrapper? TryCreatePaint(SKPaint paint, IPen pen, Rect targetRect)
        {
            // In Skia 0 thickness means - use hairline rendering
            // and for us it means - there is nothing rendered.
            if (pen.Brush is not { } brush || pen.Thickness == 0d)
            {
                return null;
            }

            var rv = CreatePaint(paint, brush, targetRect);

            paint.IsStroke = true;
            paint.StrokeWidth = (float) pen.Thickness;

            // Need to modify dashes due to Skia modifying their lengths
            // https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/paths/dots
            // TODO: Still something is off, dashes are now present, but don't look the same as D2D ones.

            paint.StrokeCap = pen.LineCap.ToSKStrokeCap();
            paint.StrokeJoin = pen.LineJoin.ToSKStrokeJoin();

            paint.StrokeMiter = (float) pen.MiterLimit;

            if (DrawingContextHelper.TryCreateDashEffect(pen, out var dashEffect))
            {
                paint.PathEffect = dashEffect;
                rv.AddDisposable(dashEffect);
            }

            return rv;
        }

        /// <summary>
        /// Create new render target compatible with this drawing context.
        /// </summary>
        /// <param name="pixelSize">The size of the render target.</param>
        /// <param name="isLayer">Whether the render target is being created for a layer.</param>
        /// <param name="format">Pixel format.</param>
        /// <returns></returns>
        private SurfaceRenderTarget CreateRenderTarget(PixelSize pixelSize, bool isLayer, PixelFormat? format = null)
        {
            var createInfo = new SurfaceRenderTarget.CreateInfo
            {
                Width = pixelSize.Width,
                Height = pixelSize.Height,
                Dpi = _intermediateSurfaceDpi,
                Format = format,
                DisableTextLcdRendering = isLayer ? _disableSubpixelTextRendering : true,
                GrContext = _grContext,
                Gpu = _gpu,
                Session = _session,
                DisableManualFbo = !isLayer,
            };

            return new SurfaceRenderTarget(createInfo);
        }        

        /// <summary>
        /// Skia cached paint state.
        /// </summary>
        private readonly struct PaintState : IDisposable
        {
            private readonly SKColor _color;
            private readonly SKShader _shader;
            private readonly SKPaint _paint;
            
            public PaintState(SKPaint paint, SKColor color, SKShader shader)
            {
                _paint = paint;
                _color = color;
                _shader = shader;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _paint.Color = _color;
                _paint.Shader = _shader;
            }
        }

        /// <summary>
        /// Skia paint wrapper.
        /// </summary>
        internal struct PaintWrapper : IDisposable
        {
            //We are saving memory allocations there
            public readonly SKPaint Paint;

            private IDisposable? _disposable1;
            private IDisposable? _disposable2;
            private IDisposable? _disposable3;

            public PaintWrapper(SKPaint paint)
            {
                Paint = paint;

                _disposable1 = null;
                _disposable2 = null;
                _disposable3 = null;
            }

            public IDisposable ApplyTo(SKPaint paint)
            {
                var state = new PaintState(paint, paint.Color, paint.Shader);

                paint.Color = Paint.Color;
                paint.Shader = Paint.Shader;

                return state;
            }

            /// <summary>
            /// Add new disposable to a wrapper.
            /// </summary>
            /// <param name="disposable">Disposable to add.</param>
            public void AddDisposable(IDisposable disposable)
            {
                if (_disposable1 == null)
                {
                    _disposable1 = disposable;
                }
                else if (_disposable2 == null)
                {
                    _disposable2 = disposable;
                }
                else if (_disposable3 == null)
                {
                    _disposable3 = disposable;
                }
                else
                {
                    Debug.Assert(false);

                    // ReSharper disable once HeuristicUnreachableCode
                    throw new InvalidOperationException(
                        "PaintWrapper disposable object limit reached. You need to add extra struct fields to support more disposables.");
                }
            }
            
            /// <inheritdoc />
            public void Dispose()
            {
                Paint?.Reset();
                _disposable1?.Dispose();
                _disposable2?.Dispose();
                _disposable3?.Dispose();
            }
        }
    }
}
