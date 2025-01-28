using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using D3DDevice = SharpDX.Direct3D11.Device;
using DxgiResource = SharpDX.DXGI.Resource;
using DxgiResource1 = SharpDX.DXGI.Resource1;

namespace GpuInterop.D3DDemo;

class D3D11Swapchain : SwapchainBase<D3D11SwapchainImage>
{
    private readonly D3DDevice _device;

    public D3D11Swapchain(D3DDevice device, ICompositionGpuInterop interop, CompositionDrawingSurface target)
        : base(interop, target)
    {
        _device = device;
    }

    protected override D3D11SwapchainImage CreateImage(PixelSize size) => new(_device, size, Interop, Target);

    public IDisposable BeginDraw(PixelSize size, out RenderTargetView view)
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
    private readonly Texture2D _texture;
    private readonly KeyedMutex _mutex;
    private readonly IntPtr _handle;
    private PlatformGraphicsExternalImageProperties _properties;
    private ICompositionImportedGpuImage? _imported;
    public Task? LastPresent { get; private set; }
    public RenderTargetView RenderTargetView { get; }
    public bool IsVulkanBacked { get; }

    public D3D11SwapchainImage(D3DDevice device, PixelSize size,
        ICompositionGpuInterop interop,
        CompositionDrawingSurface target)
    {
        if (!interop.SupportedImageHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle))
        {
            IsVulkanBacked = true;
        }

        Size = size;
        _interop = interop;
        _target = target;
        _texture = new Texture2D(device,
            new Texture2DDescription
            {
                Format = Format.R8G8B8A8_UNorm,
                Width = size.Width,
                Height = size.Height,
                ArraySize = 1,
                MipLevels = 1,
                SampleDescription = new SampleDescription { Count = 1, Quality = 0 },
                CpuAccessFlags = default,
                OptionFlags = IsVulkanBacked ? ResourceOptionFlags.SharedNthandle | ResourceOptionFlags.SharedKeyedmutex : ResourceOptionFlags.SharedKeyedmutex,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
            });
        _mutex = _texture.QueryInterface<KeyedMutex>();
        if (IsVulkanBacked)
        {
            using (var res = _texture.QueryInterface<DxgiResource1>())
                _handle = res.CreateSharedHandle(null, SharedResourceFlags.Read | SharedResourceFlags.Write);
        }
        else
        {
            using (var res = _texture.QueryInterface<DxgiResource>())
                _handle = res.SharedHandle;
        }

        _properties = new PlatformGraphicsExternalImageProperties
        {
            Width = size.Width,
            Height = size.Height,
            Format = PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm
        };

        RenderTargetView = new RenderTargetView(device, _texture);
    }

    public void BeginDraw()
    {
        _mutex.Acquire(0, int.MaxValue);
    }

    public void Present()
    {
        _mutex.Release(1);
        _imported ??= _interop.ImportImage(
            new PlatformHandle(_handle, 
                IsVulkanBacked ? KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureNtHandle: KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle),
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
        //_mutex.Dispose();
        _texture.Dispose();
    }
}
