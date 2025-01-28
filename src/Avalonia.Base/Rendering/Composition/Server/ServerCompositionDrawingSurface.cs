using System;
using System.Runtime.ExceptionServices;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionDrawingSurface : ServerCompositionSurface, IDisposable
{
    private IRef<IBitmapImpl>? _bitmap;
    private IPlatformRenderInterfaceContext? _createdWithContext;
    public override IRef<IBitmapImpl>? Bitmap
    {
        get
        {
            // Failsafe to avoid consuming an image imported with a different context
            if (Compositor.RenderInterface.Value != _createdWithContext)
                return null;
            return _bitmap;
        }
    }

    public ServerCompositionDrawingSurface(ServerCompositor compositor) : base(compositor)
    {
    }

    void PerformSanityChecks(CompositionImportedGpuImage image)
    {
        // Failsafe to avoid consuming an image imported with a different context
        if (!image.IsUsable)
            throw new PlatformGraphicsContextLostException();

        // This should never happen, but check for it anyway to avoid a deadlock
        if (!image.ImportCompleted.IsCompleted)
            throw new InvalidOperationException("The import operation is not completed yet");

        // Rethrow the import here exception
        if (image.ImportCompleted.IsFaulted)
            image.ImportCompleted.GetAwaiter().GetResult();
    }

    void Update(IBitmapImpl newImage, IPlatformRenderInterfaceContext context)
    {
        _bitmap?.Dispose();
        _bitmap = RefCountable.Create(newImage);
        _createdWithContext = context;
        Changed?.Invoke();
    }

    public void UpdateWithAutomaticSync(CompositionImportedGpuImage image)
    {
        using (Compositor.RenderInterface.EnsureCurrent())
        {
            PerformSanityChecks(image);
            Update(image.Image.SnapshotWithAutomaticSync(), image.Context);
        }
    }
    
    public void UpdateWithKeyedMutex(CompositionImportedGpuImage image, 
        uint acquireIndex, uint releaseIndex)
    {
        using (Compositor.RenderInterface.EnsureCurrent())
        {
            PerformSanityChecks(image);
            Update(image.Image.SnapshotWithKeyedMutex(acquireIndex, releaseIndex), image.Context);
        }
    }

    public void UpdateWithSemaphores(CompositionImportedGpuImage image, CompositionImportedGpuSemaphore wait, CompositionImportedGpuSemaphore signal)
    {
        using (Compositor.RenderInterface.EnsureCurrent())
        {
            PerformSanityChecks(image);
            if (!wait.IsUsable || !signal.IsUsable)
                throw new PlatformGraphicsContextLostException();
            Update(image.Image.SnapshotWithSemaphores(wait.Semaphore, signal.Semaphore), image.Context);
        }
    }

    public void Dispose()
    {
        _bitmap?.Dispose();
    }
}
