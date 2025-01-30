using System;
using System.Collections.Generic;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;

namespace Avalonia.Vulkan;

[Unstable]
public interface IVulkanContextExternalObjectsFeature
{
    IReadOnlyList<string> SupportedImageHandleTypes { get; }
    IReadOnlyList<string> SupportedSemaphoreTypes { get; }
    byte[]? DeviceUuid { get; }
    byte[]? DeviceLuid { get; }
    CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType);
    IVulkanExternalImage ImportImage(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties);
    IVulkanExternalSemaphore ImportSemaphore(IPlatformHandle handle);
}

[Unstable]
public interface IVulkanExternalSemaphore : IDisposable
{
    ulong Handle { get; }
    void SubmitWaitSemaphore();
    void SubmitSignalSemaphore();
}

[Unstable]
public interface IVulkanExternalImage : IDisposable
{
    VulkanImageInfo Info { get; }

    void SubmitKeyedMutex(uint acquireIndex, uint releaseIndex);
}
