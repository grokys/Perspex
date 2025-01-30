using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Buffer = SharpDX.Direct3D11.Buffer;
using DxgiFactory1 = SharpDX.DXGI.Factory1;
using Matrix = SharpDX.Matrix;
using D3DDevice = SharpDX.Direct3D11.Device;
using FeatureLevel = SharpDX.Direct3D.FeatureLevel;
using Vector3 = SharpDX.Vector3;

namespace GpuInterop.D3DDemo;

public class D3D11DemoControl : DrawingSurfaceDemoBase
{
    private D3DDevice? _device;
    private D3D11Swapchain? _swapchain;
    private DeviceContext? _context;
    private Matrix _view;
    private PixelSize _lastSize;
    private Texture2D? _depthBuffer;
    private DepthStencilView? _depthView;
    private Matrix _proj;
    private Buffer? _constantBuffer;
    private readonly Stopwatch _st = Stopwatch.StartNew();

    protected override (bool success, string info) InitializeGraphicsResources(Compositor compositor,
        CompositionDrawingSurface surface, ICompositionGpuInterop gpuInterop)
    {
        if (gpuInterop.SupportedImageHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle) != true
            && gpuInterop.SupportedImageHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureNtHandle) != true)
            return (false, "DXGI shared handle import is not supported by the current graphics backend");

        var factory = new DxgiFactory1();
        using var adapter = factory.GetAdapter1(0);
        if(gpuInterop.DeviceLuid != null)
        {
            var bytes= BitConverter.GetBytes(adapter.Description1.Luid);
            if (!bytes.SequenceEqual(gpuInterop.DeviceLuid))
            {

            }
        }

        _device = new D3DDevice(adapter, DeviceCreationFlags.None, new[]
        {
            FeatureLevel.Level_12_1,
            FeatureLevel.Level_12_0,
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0,
            FeatureLevel.Level_10_0,
            FeatureLevel.Level_9_3,
            FeatureLevel.Level_9_2,
            FeatureLevel.Level_9_1,
        });
        _swapchain = new D3D11Swapchain(_device, gpuInterop, surface);
        _context = _device.ImmediateContext;
        _constantBuffer = D3DContent.CreateMesh(_device);
        _view = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
        return (true, $"D3D11 ({_device.FeatureLevel}) {adapter.Description1.Description}");
    }

    protected override void FreeGraphicsResources()
    {
        if (_swapchain is not null)
        {
            _swapchain.DisposeAsync().GetAwaiter().GetResult();
            _swapchain = null;
        }

        Utilities.Dispose(ref _depthView);
        Utilities.Dispose(ref _depthBuffer);
        Utilities.Dispose(ref _constantBuffer);
        Utilities.Dispose(ref _context);
        Utilities.Dispose(ref _device);
    }

    protected override bool SupportsDisco => true;

    protected override void RenderFrame(PixelSize pixelSize)
    {
        if (pixelSize == default)
            return;
        if (pixelSize != _lastSize)
        {
            _lastSize = pixelSize;
            Resize(pixelSize);
        }
        using (_swapchain!.BeginDraw(pixelSize, out var renderView))
        {
            
            _device!.ImmediateContext.OutputMerger.SetTargets(_depthView, renderView);
            var viewProj = Matrix.Multiply(_view, _proj);
            var context = _device.ImmediateContext;

            var now = _st.Elapsed.TotalSeconds * 5;
            var scaleX = (float)(1f + Disco * (Math.Sin(now) + 1) / 6);
            var scaleY = (float)(1f + Disco * (Math.Cos(now) + 1) / 8);
            var colorOff =(float) (Math.Sin(now) + 1) / 2 * Disco;
            
            
            // Clear views
            context.ClearDepthStencilView(_depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(renderView,
                new RawColor4(1 - colorOff, colorOff, (float)0.5 + colorOff / 2, 1));

            
            var ypr = Matrix4x4.CreateFromYawPitchRoll(Yaw, Pitch, Roll);
            // Update WorldViewProj Matrix
            var worldViewProj = Matrix.RotationX(Yaw) * Matrix.RotationY(Pitch)
                                                      * Matrix.RotationZ(Roll)
                                                      * Matrix.Scaling(new Vector3(scaleX, scaleY, 1))
                                                      * viewProj;
            worldViewProj.Transpose();
            context.UpdateSubresource(ref worldViewProj, _constantBuffer);

            // Draw the cube
            context.Draw(36, 0);
            
            
            _context!.Flush();
        }
    }

    private void Resize(PixelSize size)
    {
        Utilities.Dispose(ref _depthBuffer);

        if (_device is null)
            return;

        _depthBuffer = new Texture2D(_device,
            new Texture2DDescription()
            {
                Format = Format.D32_Float_S8X24_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = size.Width,
                Height = size.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });

        Utilities.Dispose(ref _depthView);
        _depthView = new DepthStencilView(_device, _depthBuffer);

        // Setup targets and viewport for rendering
        _device.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, size.Width, size.Height, 0.0f, 1.0f));
        
        // Setup new projection matrix with correct aspect ratio
        _proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, size.Width / (float) size.Height, 0.1f, 100.0f);
    }
}
