using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace GpuInterop.D3DDemo;

public class D3D11DemoControl : DrawingSurfaceDemoBase
{
    private ID3D11Device? _device;
    private D3D11Swapchain? _swapchain;
    private ID3D11DeviceContext? _context;
    private Matrix4x4 _view;
    private PixelSize _lastSize;
    private ID3D11Texture2D? _depthBuffer;
    private ID3D11DepthStencilView? _depthView;
    private Matrix4x4 _proj;
    private ID3D11Buffer? _constantBuffer;
    private readonly Stopwatch _st = Stopwatch.StartNew();

    protected override (bool success, string info) InitializeGraphicsResources(Compositor compositor,
        CompositionDrawingSurface surface, ICompositionGpuInterop interop)
    {
        if (interop.SupportedImageHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes
                .D3D11TextureGlobalSharedHandle) != true)
            return (false, "DXGI shared handle import is not supported by the current graphics backend");

        DXGI.CreateDXGIFactory1<IDXGIFactory1>(out var factory);
        if (factory is null)
        {
            return (false, "Failed to create DXGI factory");
        }
        factory.EnumAdapters(0, out var adapter);
        if (adapter is null)
        {
            factory.Dispose();
            return (false, "Failed to enumerate DXGI adapter");
        }
        try
        {
            D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, DeviceCreationFlags.None,
            [
                FeatureLevel.Level_12_1,
                FeatureLevel.Level_12_0,
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_10_0,
                FeatureLevel.Level_9_3,
                FeatureLevel.Level_9_2,
                FeatureLevel.Level_9_1,
            ], out _device);

            if (_device is null)
            {
                return (false, "Failed to create D3D11 device");
            }

            _swapchain = new D3D11Swapchain(_device, interop, surface);
            _context = _device.ImmediateContext;
            _constantBuffer = D3DContent.CreateMesh(_device);
            _view = Matrix4x4.CreateLookAtLeftHanded(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
            return (true, $"D3D11 ({_device.FeatureLevel}) {adapter.Description.Description}");
        }
        finally
        {
            adapter.Dispose();
            factory.Dispose();
        }
    }

    protected override void FreeGraphicsResources()
    {
        if (_swapchain is not null)
        {
            _swapchain.DisposeAsync().GetAwaiter().GetResult();
            _swapchain = null;
        }

        _depthView?.Dispose();
        _depthBuffer?.Dispose();
        _constantBuffer?.Dispose();
        _context?.Dispose();
        _device?.Dispose();
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

            _device!.ImmediateContext.OMSetRenderTargets(renderView, _depthView);
            var viewProj = Matrix4x4.Multiply(_view, _proj);
            var context = _device.ImmediateContext;

            var now = _st.Elapsed.TotalSeconds * 5;
            var scaleX = (float)(1f + Disco * (Math.Sin(now) + 1) / 6);
            var scaleY = (float)(1f + Disco * (Math.Cos(now) + 1) / 8);
            var colorOff = (float)(Math.Sin(now) + 1) / 2 * Disco;


            // Clear views
            context.ClearDepthStencilView(_depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(renderView,
                new Vortice.Mathematics.Color4(1 - colorOff, colorOff, (float)0.5 + colorOff / 2, 1));


            var ypr = Matrix4x4.CreateFromYawPitchRoll(Yaw, Pitch, Roll);
            // Update WorldViewProj Matrix
            var worldViewProj = Matrix4x4.Transpose(Matrix4x4.CreateRotationX(Yaw) * Matrix4x4.CreateRotationY(Pitch)
                                                      * Matrix4x4.CreateRotationZ(Roll)
                                                      * Matrix4x4.CreateScale(new Vector3(scaleX, scaleY, 1))
                                                      * viewProj);
            context.UpdateSubresource(worldViewProj, _constantBuffer!);

            // Draw the cube
            context.Draw(36, 0);


            _context!.Flush();
        }
    }

    private void Resize(PixelSize size)
    {
        _depthBuffer?.Dispose();

        if (_device is null)
            return;

        _depthBuffer = _device.CreateTexture2D(
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
                CPUAccessFlags = CpuAccessFlags.None,
                MiscFlags = ResourceOptionFlags.None
            });

        _depthView?.Dispose();
        _depthView = _device.CreateDepthStencilView(_depthBuffer);

        // Setup targets and viewport for rendering
        _device.ImmediateContext.RSSetViewport(0, 0, size.Width, size.Height, 0.0f, 1.0f);

        // Setup new projection matrix with correct aspect ratio
        _proj = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded((float)Math.PI / 4.0f, size.Width / (float)size.Height, 0.1f, 100.0f);
    }
}
