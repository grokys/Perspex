using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Direct2D1.Media;
using Avalonia.Direct2D1.Media.Imaging;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using GlyphRun = Avalonia.Media.GlyphRun;
using Vortice.Direct3D11;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.WIC;
using Vortice.DXGI;
using PixelFormat = Avalonia.Platform.PixelFormat;
using System.Numerics;
using BitmapInterpolationMode = Avalonia.Media.Imaging.BitmapInterpolationMode;

namespace Avalonia
{
    public static class Direct2DApplicationExtensions
    {
        public static AppBuilder UseDirect2D1(this AppBuilder builder)
        {
            builder.UseRenderingSubsystem(Direct2D1.Direct2D1Platform.Initialize, "Direct2D1");
            return builder;
        }
    }
}

namespace Avalonia.Direct2D1
{
    internal class Direct2D1Platform : IPlatformRenderInterface
    {
        private static readonly Direct2D1Platform s_instance = new Direct2D1Platform();

        public static ID3D11Device Direct3D11Device { get; private set; }

        public static ID2D1Factory1 Direct2D1Factory { get; private set; }

        public static ID2D1Device Direct2D1Device { get; private set; }

        public static IDWriteFactory1 DirectWriteFactory { get; private set; }

        public static IWICImagingFactory ImagingFactory { get; private set; }

        public static IDXGIDevice1 DxgiDevice { get; private set; }

        private static readonly object s_initLock = new object();
        private static bool s_initialized = false;

        internal static void InitializeDirect2D()
        {
            lock (s_initLock)
            {
                if (s_initialized)
                {
                    return;
                }
#if DEBUG
                if (Debugger.IsAttached)
                {
                    try
                    {
                        Direct2D1Factory = D2D1.D2D1CreateFactory<ID2D1Factory1>(
                            Vortice.Direct2D1.FactoryType.MultiThreaded,
                            debugLevel: DebugLevel.Error);
                    }
                    catch
                    {
                        // ignore, retry below without the debug layer
                    }
                }
#endif
                if (Direct2D1Factory == null)
                {
                    Direct2D1Factory = D2D1.D2D1CreateFactory<ID2D1Factory1>(
                        Vortice.Direct2D1.FactoryType.MultiThreaded,
                        debugLevel: DebugLevel.None);
                }

                DirectWriteFactory = DWrite.DWriteCreateFactory<IDWriteFactory1>();

                ImagingFactory = new IWICImagingFactory();

                var featureLevels = new[]
                {
                    Vortice.Direct3D.FeatureLevel.Level_11_1,
                    Vortice.Direct3D.FeatureLevel.Level_11_0,
                    Vortice.Direct3D.FeatureLevel.Level_10_1,
                    Vortice.Direct3D.FeatureLevel.Level_10_0,
                    Vortice.Direct3D.FeatureLevel.Level_9_3,
                    Vortice.Direct3D.FeatureLevel.Level_9_2,
                    Vortice.Direct3D.FeatureLevel.Level_9_1,
                };

                Direct3D11Device = D3D11.D3D11CreateDevice(
                    Vortice.Direct3D.DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport,
                    featureLevels);

                DxgiDevice = Direct3D11Device.QueryInterface<IDXGIDevice1>();

                Direct2D1Device = Direct2D1Factory.CreateDevice(DxgiDevice);

                s_initialized = true;
            }
        }

        public static void Initialize()
        {
            SharpGen.Runtime.Configuration.EnableReleaseOnFinalizer = true;
            InitializeDirect2D();
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformRenderInterface>().ToConstant(s_instance)
                .Bind<IFontManagerImpl>().ToConstant(new FontManagerImpl())
                .Bind<ITextShaperImpl>().ToConstant(new TextShaperImpl());
        }

        private IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            foreach (var s in surfaces)
            {
                if (s is IPlatformHandle nativeWindow)
                {
                    if (nativeWindow.HandleDescriptor != "HWND")
                    {
                        throw new NotSupportedException("Don't know how to create a Direct2D1 renderer from " +
                                                        nativeWindow.HandleDescriptor);
                    }

                    return new HwndRenderTarget(nativeWindow);
                }
                if (s is IExternalDirect2DRenderTargetSurface external)
                {
                    return new ExternalRenderTarget(external);
                }

                if (s is IFramebufferPlatformSurface fb)
                {
                    return new FramebufferShimRenderTarget(fb);
                }
            }
            throw new NotSupportedException("Don't know how to create a Direct2D1 renderer from any of provided surfaces");
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
        {
            return new WicRenderTargetBitmapImpl(size, dpi);
        }

        public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat)
        {
            return new WriteableWicBitmapImpl(size, dpi, format, alphaFormat);
        }

        public IGeometryImpl CreateEllipseGeometry(Rect rect) => new EllipseGeometryImpl(rect);
        public IGeometryImpl CreateLineGeometry(Point p1, Point p2) => new LineGeometryImpl(p1, p2);
        public IGeometryImpl CreateRectangleGeometry(Rect rect) => new RectangleGeometryImpl(rect);
        public IStreamGeometryImpl CreateStreamGeometry() => new StreamGeometryImpl();
        public IGeometryImpl CreateGeometryGroup(FillRule fillRule, IReadOnlyList<IGeometryImpl> children) => new GeometryGroupImpl(fillRule, children);
        public IGeometryImpl CreateCombinedGeometry(GeometryCombineMode combineMode, IGeometryImpl g1, IGeometryImpl g2) => new CombinedGeometryImpl(combineMode, g1, g2);
        public IGlyphRunImpl CreateGlyphRun(IGlyphTypeface glyphTypeface, double fontRenderingEmSize, IReadOnlyList<GlyphInfo> glyphInfos, Point baselineOrigin)
        {
            return new GlyphRunImpl(glyphTypeface, fontRenderingEmSize, glyphInfos, baselineOrigin);
        }

        class D2DApi : IPlatformRenderInterfaceContext
        {
            private readonly Direct2D1Platform _platform;

            public D2DApi(Direct2D1Platform platform)
            {
                _platform = platform;
            }
            public object TryGetFeature(Type featureType) => null;

            public void Dispose()
            {
            }

            public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces) => _platform.CreateRenderTarget(surfaces);
            public bool IsLost => false;
            public IReadOnlyDictionary<Type, object> PublicFeatures { get; } = new Dictionary<Type, object>();
        }

        public IPlatformRenderInterfaceContext CreateBackendContext(IPlatformGraphicsContext graphicsContext) =>
            new D2DApi(this);

        public IGeometryImpl BuildGlyphRunGeometry(GlyphRun glyphRun)
        {
            if (glyphRun.GlyphTypeface is not GlyphTypefaceImpl glyphTypeface)
            {
                throw new InvalidOperationException("PlatformImpl can't be null.");
            }

            var pathGeometry = Direct2D1Factory.CreatePathGeometry();

            using (var sink = pathGeometry.Open())
            {
                var glyphInfos = glyphRun.GlyphInfos;
                var glyphs = new ushort[glyphInfos.Count];

                for (int i = 0; i < glyphInfos.Count; i++)
                {
                    glyphs[i] = glyphInfos[i].GlyphIndex;
                }

                glyphTypeface.FontFace.GetGlyphRunOutline((float)glyphRun.FontRenderingEmSize, glyphs, null, null, false, !glyphRun.IsLeftToRight, sink);

                sink.Close();
            }

            var (baselineOriginX, baselineOriginY) = glyphRun.BaselineOrigin;

            var transformedGeometry = Direct2D1Factory.CreateTransformedGeometry(
                pathGeometry,
                new Matrix3x2(1.0f, 0.0f, 0.0f, 1.0f, (float)baselineOriginX, (float)baselineOriginY));

            return new TransformedGeometryWrapper(transformedGeometry);
        }

        private class TransformedGeometryWrapper : GeometryImpl
        {
            public TransformedGeometryWrapper(ID2D1TransformedGeometry geometry) : base(geometry)
            {

            }
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmap(string fileName)
        {
            return new WicBitmapImpl(fileName);
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmap(Stream stream)
        {
            return new WicBitmapImpl(stream);
        }

        public IWriteableBitmapImpl LoadWriteableBitmapToWidth(Stream stream, int width,
            BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new WriteableWicBitmapImpl(stream, width, true, interpolationMode);
        }

        public IWriteableBitmapImpl LoadWriteableBitmapToHeight(Stream stream, int height,
            BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new WriteableWicBitmapImpl(stream, height, false, interpolationMode);
        }

        public IWriteableBitmapImpl LoadWriteableBitmap(string fileName)
        {
            return new WriteableWicBitmapImpl(fileName);
        }

        public IWriteableBitmapImpl LoadWriteableBitmap(Stream stream)
        {
            return new WriteableWicBitmapImpl(stream);
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new WicBitmapImpl(stream, width, true, interpolationMode);
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new WicBitmapImpl(stream, height, false, interpolationMode);
        }

        /// <inheritdoc />
        public IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            // https://github.com/sharpdx/SharpDX/issues/959 blocks implementation.
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            return new WicBitmapImpl(format, alphaFormat, data, size, dpi, stride);
        }

        public bool SupportsIndividualRoundRects => false;

        public AlphaFormat DefaultAlphaFormat => AlphaFormat.Premul;

        public PixelFormat DefaultPixelFormat => PixelFormat.Bgra8888;
        public bool IsSupportedBitmapPixelFormat(PixelFormat format) =>
            format == PixelFormats.Bgra8888 
            || format == PixelFormats.Rgba8888;

        public bool SupportsRegions => false;
        public IPlatformRenderInterfaceRegion CreateRegion() => throw new NotSupportedException();
    }
}
