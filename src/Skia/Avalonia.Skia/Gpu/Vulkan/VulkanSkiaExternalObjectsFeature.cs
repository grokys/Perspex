using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Vulkan;
using SkiaSharp;

namespace Avalonia.Skia.Vulkan;

internal class VulkanSkiaExternalObjectsFeature : IExternalObjectsRenderInterfaceContextFeature
{
    private readonly VulkanSkiaGpu _gpu;
    private readonly IVulkanPlatformGraphicsContext _context;
    private readonly IVulkanContextExternalObjectsFeature _feature;

    public VulkanSkiaExternalObjectsFeature(VulkanSkiaGpu gpu,
        IVulkanPlatformGraphicsContext context, IVulkanContextExternalObjectsFeature feature)
    {
        _gpu = gpu;
        _context = context;
        _feature = feature;
    }


    public IPlatformRenderInterfaceImportedImage ImportImage(IPlatformHandle handle,
        PlatformGraphicsExternalImageProperties properties) =>
        new Image(_gpu, _feature.ImportImage(handle, properties), properties);

    public IPlatformRenderInterfaceImportedSemaphore ImportSemaphore(IPlatformHandle handle) => new Semaphore(_feature.ImportSemaphore(handle));

    class Semaphore : IPlatformRenderInterfaceImportedSemaphore
    {
        private IVulkanExternalSemaphore? _inner;

        public Semaphore(IVulkanExternalSemaphore inner)
        {
            _inner = inner;
        }

        public void Dispose()
        {
            _inner?.Dispose();
            _inner = null;
        }

        public IVulkanExternalSemaphore Inner =>
            _inner ?? throw new ObjectDisposedException(nameof(IVulkanExternalSemaphore));

    }

    class Image : IPlatformRenderInterfaceImportedImage
    {
        private readonly VulkanSkiaGpu _gpu;
        private IVulkanExternalImage? _inner;
        private readonly PlatformGraphicsExternalImageProperties _properties;

        public Image(VulkanSkiaGpu gpu, IVulkanExternalImage inner, PlatformGraphicsExternalImageProperties properties)
        {
            _gpu = gpu;
            _inner = inner;
            _properties = properties;
        }

        public void Dispose()
        {
            _inner?.Dispose();
            _inner = null;
        }

        public IBitmapImpl SnapshotWithKeyedMutex(uint acquireIndex, uint releaseIndex)
        {
            var info = _inner!.Info;
            _gpu.GrContext.ResetContext();

            var imageInfo = new GRVkImageInfo
            {
                CurrentQueueFamily = _gpu.Vulkan.Device.GraphicsQueueFamilyIndex,
                Format = info.Format,
                Image = (ulong)info.Handle,
                ImageLayout = info.Layout,
                ImageTiling = info.Tiling,
                ImageUsageFlags = info.UsageFlags,
                LevelCount = info.LevelCount,
                SampleCount = info.SampleCount,
                Protected = info.IsProtected,
                Alloc = new GRVkAlloc
                {
                    Memory = (ulong)info.MemoryHandle,
                    Size = info.MemorySize
                }
            };
            using var renderTarget = new GRBackendRenderTarget(_properties.Width, _properties.Height, 1, imageInfo);
            using var surface = SKSurface.Create(_gpu.GrContext, renderTarget,
                _properties.TopLeftOrigin ? GRSurfaceOrigin.TopLeft : GRSurfaceOrigin.BottomLeft,
                _properties.Format == PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm
                    ? SKColorType.Rgba8888
                    : SKColorType.Bgra8888, SKColorSpace.CreateSrgb());
            var image = surface.Snapshot();
            _gpu.GrContext.Flush();
            //surface.Canvas.Flush();
            _inner.SubmitKeyedMutex(acquireIndex, releaseIndex);

            return new ImmutableBitmap(image);
        }

        public IBitmapImpl SnapshotWithSemaphores(IPlatformRenderInterfaceImportedSemaphore waitForSemaphore, IPlatformRenderInterfaceImportedSemaphore signalSemaphore)
        {
            var info = _inner!.Info;

            _gpu.GrContext.ResetContext();
            ((Semaphore)waitForSemaphore).Inner.SubmitWaitSemaphore();
            var imageInfo = new GRVkImageInfo
            {
                CurrentQueueFamily = _gpu.Vulkan.Device.GraphicsQueueFamilyIndex,
                Format = info.Format,
                Image = (ulong)info.Handle,
                ImageLayout = info.Layout,
                ImageTiling = info.Tiling,
                ImageUsageFlags = info.UsageFlags,
                LevelCount = info.LevelCount,
                SampleCount = info.SampleCount,
                Protected = info.IsProtected,
                Alloc = new GRVkAlloc
                {
                    Memory = (ulong)info.MemoryHandle,
                    Size = info.MemorySize
                }
            };
            using var renderTarget = new GRBackendRenderTarget(_properties.Width, _properties.Height, 1, imageInfo);
            using var surface = SKSurface.Create(_gpu.GrContext, renderTarget,
                _properties.TopLeftOrigin ? GRSurfaceOrigin.TopLeft : GRSurfaceOrigin.BottomLeft,
                _properties.Format == PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm
                    ? SKColorType.Rgba8888
                    : SKColorType.Bgra8888, SKColorSpace.CreateSrgb());
            var image = surface.Snapshot();
            _gpu.GrContext.Flush();
            //surface.Canvas.Flush();

            ((Semaphore)signalSemaphore).Inner.SubmitSignalSemaphore();

            return new ImmutableBitmap(image);
        }

        public IBitmapImpl SnapshotWithAutomaticSync() => throw new NotSupportedException();
    }

    public IPlatformRenderInterfaceImportedImage ImportImage(ICompositionImportableSharedGpuContextImage image) => throw new System.NotSupportedException();

    public CompositionGpuImportedImageSynchronizationCapabilities
        GetSynchronizationCapabilities(string imageHandleType) => _feature.GetSynchronizationCapabilities(imageHandleType);

    public byte[]? DeviceUuid => _feature.DeviceUuid;
    public byte[]? DeviceLuid => _feature.DeviceLuid;

    public IReadOnlyList<string> SupportedImageHandleTypes => _feature.SupportedImageHandleTypes;
    public IReadOnlyList<string> SupportedSemaphoreTypes => _feature.SupportedSemaphoreTypes;
}
