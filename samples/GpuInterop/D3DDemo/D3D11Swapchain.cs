using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace GpuInterop.D3DDemo;

class D3D11Swapchain : SwapchainBase<D3D11SwapchainImage>
{
    private readonly ID3D11Device _device;

    public D3D11Swapchain(ID3D11Device device, ICompositionGpuInterop interop, CompositionDrawingSurface target)
        : base(interop, target)
    {
        _device = device;
    }

    protected override D3D11SwapchainImage CreateImage(PixelSize size) => new(_device, size, Interop, Target);

    public IDisposable BeginDraw(PixelSize size, out ID3D11RenderTargetView view)
    {
        var rv = BeginDrawCore(size, out var image);
        view = image.RenderTargetView;
        return rv;
    }
}

public class D3D11SwapchainImage : ISwapchainImage
{
    public PixelSize Size { get; }
    private readonly ICompositionGpuInterop _interop;
    private readonly CompositionDrawingSurface _target;
    private readonly ID3D11Texture2D _texture;
    private readonly IDXGIKeyedMutex _mutex;
    private readonly IntPtr _handle;
    private PlatformGraphicsExternalImageProperties _properties;
    private ICompositionImportedGpuImage? _imported;
    public Task? LastPresent { get; private set; }
    public ID3D11RenderTargetView RenderTargetView { get; }

    public D3D11SwapchainImage(ID3D11Device device, PixelSize size,
        ICompositionGpuInterop interop,
        CompositionDrawingSurface target)
    {
        Size = size;
        _interop = interop;
        _target = target;
        _texture = device.CreateTexture2D(
            new Texture2DDescription
            {
                Format = Format.R8G8B8A8_UNorm,
                Width = size.Width,
                Height = size.Height,
                ArraySize = 1,
                MipLevels = 1,
                SampleDescription = new SampleDescription { Count = 1, Quality = 0 },
                CPUAccessFlags = default,
                MiscFlags = ResourceOptionFlags.SharedKeyedMutex,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
            });
        _mutex = _texture.QueryInterface<IDXGIKeyedMutex>();
        using (var res = _texture.QueryInterface<IDXGIResource>())
            _handle = res.SharedHandle;
        _properties = new PlatformGraphicsExternalImageProperties
        {
            Width = size.Width, Height = size.Height, Format = PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm
        };

        RenderTargetView = device.CreateRenderTargetView(_texture);
    }

    public void BeginDraw()
    {
        _mutex.AcquireSync(0, int.MaxValue);
    }

    public void Present()
    {
        _mutex.ReleaseSync(1);
        _imported ??= _interop.ImportImage(
            new PlatformHandle(_handle, KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle),
            _properties);
        LastPresent = _target.UpdateWithKeyedMutexAsync(_imported, 1, 0);
    }


    public async ValueTask DisposeAsync()
    {
        if (LastPresent != null)
            try
            {
                await LastPresent;
            }
            catch
            {
                // Ignore
            }

        RenderTargetView.Dispose();
        _mutex.Dispose();
        _texture.Dispose();
    }
}
