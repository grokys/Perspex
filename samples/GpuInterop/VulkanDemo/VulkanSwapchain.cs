using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Vulkan;
using Silk.NET.Vulkan;

namespace GpuInterop.VulkanDemo;

class VulkanSwapchain : SwapchainBase<VulkanSwapchainImage>
{
    private readonly VulkanContext _vk;

    public VulkanSwapchain(VulkanContext vk, ICompositionGpuInterop interop, CompositionDrawingSurface target) : base(interop, target)
    {
        _vk = vk;
    }

    protected override VulkanSwapchainImage CreateImage(PixelSize size)
    {
        return new VulkanSwapchainImage(_vk, size, Interop, Target);
    }

    public IDisposable BeginDraw(PixelSize size, out VulkanImage image)
    {
        _vk.Pool.FreeUsedCommandBuffers();
        var rv = BeginDrawCore(size, out var swapchainImage);
        image = swapchainImage.Image;
        return rv;
    }
}

class VulkanSwapchainImage : ISwapchainImage
{
    private readonly VulkanContext _vk;
    private readonly ICompositionGpuInterop _interop;
    private readonly CompositionDrawingSurface _target;
    private readonly VulkanImage _image;
    private readonly VulkanSemaphorePair _semaphorePair;
    private ICompositionImportedGpuSemaphore? _availableSemaphore, _renderCompletedSemaphore;
    private ICompositionImportedGpuImage? _importedImage;
    private Task? _lastPresent;
    public VulkanImage Image => _image;
    private bool _initial = true;

    public VulkanSwapchainImage(VulkanContext vk, PixelSize size, ICompositionGpuInterop interop, CompositionDrawingSurface target)
    {
        _vk = vk;
        _interop = interop;
        _target = target;
        Size = size;
        _image = new VulkanImage(vk, (uint)Format.R8G8B8A8Unorm, size, true, interop.SupportedImageHandleTypes);
        _semaphorePair = new VulkanSemaphorePair(vk, true);
    }

    public async ValueTask DisposeAsync()
    {
        if (LastPresent != null)
            await LastPresent;
        if (_importedImage != null)
            await _importedImage.DisposeAsync();
        if (_availableSemaphore != null)
            await _availableSemaphore.DisposeAsync();
        if (_renderCompletedSemaphore != null)
            await _renderCompletedSemaphore.DisposeAsync();
        _semaphorePair.Dispose();
        _image.Dispose();
    }

    public PixelSize Size { get; }

    public Task? LastPresent => _lastPresent;

    public void BeginDraw()
    {
        var buffer = _vk.Pool.CreateCommandBuffer();
        buffer.BeginRecording();

        _image.TransitionLayout(buffer.InternalHandle, 
            ImageLayout.Undefined, AccessFlags.None,
            ImageLayout.ColorAttachmentOptimal, AccessFlags.ColorAttachmentReadBit);

        if(_image.IsDirectXBacked)
            buffer.Submit(null,null,null, null, new VulkanCommandBufferPool.VulkanCommandBuffer.KeyedMutexSubmitInfo
            {
                AcquireKey = 0,
                DeviceMemory = _image.DeviceMemory
            });
        else if (_initial)
        {
            _initial = false;
            buffer.Submit();
        }
        else
            buffer.Submit(new[] { _semaphorePair.ImageAvailableSemaphore },
                new[]
                {
                    PipelineStageFlags.AllGraphicsBit
                });
    }
    
    public void Present()
    {
        var buffer = _vk.Pool.CreateCommandBuffer();
        buffer.BeginRecording();
        _image.TransitionLayout(buffer.InternalHandle, ImageLayout.TransferSrcOptimal, AccessFlags.TransferWriteBit);
        
        if (_image.IsDirectXBacked)
        {
            buffer.Submit(null, null, null, null,
                new VulkanCommandBufferPool.VulkanCommandBuffer.KeyedMutexSubmitInfo
                {
                    DeviceMemory = _image.DeviceMemory, ReleaseKey = 1
                });
        }
        else
            buffer.Submit(null, null, new[] { _semaphorePair.RenderFinishedSemaphore });

        if (!_image.IsDirectXBacked)
        {
            _availableSemaphore ??= _interop.ImportSemaphore(_semaphorePair.Export(false));
            
            _renderCompletedSemaphore ??= _interop.ImportSemaphore(_semaphorePair.Export(true));
        }

        _importedImage ??= _interop.ImportImage(_image.Export(),
            new PlatformGraphicsExternalImageProperties
            {
                Format = PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm,
                Width = Size.Width,
                Height = Size.Height,
                MemorySize = _image.MemorySize
            });

        if (_image.IsDirectXBacked)
            _lastPresent = _target.UpdateWithKeyedMutexAsync(_importedImage, 1, 0);
        else
            _lastPresent = _target.UpdateWithSemaphoresAsync(_importedImage, _renderCompletedSemaphore!, _availableSemaphore!);
    }
}
