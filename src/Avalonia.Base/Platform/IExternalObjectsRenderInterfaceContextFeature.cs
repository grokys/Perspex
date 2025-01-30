using System;
using System.Collections.Generic;
using Avalonia.Metadata;
using Avalonia.Rendering.Composition;

namespace Avalonia.Platform;

[Unstable]
public interface IExternalObjectsRenderInterfaceContextFeature
{
    /// <summary>
    /// Returns the list of image handle types supported by the current GPU backend, see <see cref="KnownPlatformGraphicsExternalImageHandleTypes"/>
    /// </summary>
    IReadOnlyList<string> SupportedImageHandleTypes { get; }
    
    /// <summary>
    /// Returns the list of semaphore types supported by the current GPU backend, see <see cref="KnownPlatformGraphicsExternalSemaphoreHandleTypes"/>
    /// </summary>
    IReadOnlyList<string> SupportedSemaphoreTypes { get; }

    IPlatformRenderInterfaceImportedImage ImportImage(IPlatformHandle handle,
        PlatformGraphicsExternalImageProperties properties);

    IPlatformRenderInterfaceImportedImage ImportImage(ICompositionImportableSharedGpuContextImage image);

    IPlatformRenderInterfaceImportedSemaphore ImportSemaphore(IPlatformHandle handle);
    CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType);
    public byte[]? DeviceUuid { get; }
    public byte[]? DeviceLuid { get; }
}

[Unstable]
public interface IPlatformRenderInterfaceImportedObject : IDisposable
{
    
}

[Unstable]
public interface IPlatformRenderInterfaceImportedImage : IPlatformRenderInterfaceImportedObject
{
    IBitmapImpl SnapshotWithKeyedMutex(uint acquireIndex, uint releaseIndex);
    IBitmapImpl SnapshotWithSemaphores(IPlatformRenderInterfaceImportedSemaphore waitForSemaphore,
        IPlatformRenderInterfaceImportedSemaphore signalSemaphore);

    IBitmapImpl SnapshotWithAutomaticSync();
}

[Unstable]
public interface IPlatformRenderInterfaceImportedSemaphore : IPlatformRenderInterfaceImportedObject
{
    
}
