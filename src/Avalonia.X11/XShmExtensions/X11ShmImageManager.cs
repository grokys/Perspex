﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Avalonia.X11.XShmExtensions;

internal class X11ShmImageManager : IDisposable
{
    public X11ShmImageManager(X11ShmFramebufferContext context)
    {
        Context = context;
    }

    public X11ShmFramebufferContext Context { get; }

    private Queue<X11ShmImage> AvailableQueue { get; } = new Queue<X11ShmImage>();

    public PixelSize? LastSize { get; private set; }

    private readonly object _lock = new();

    public X11ShmImage GetOrCreateImage(PixelSize size)
    {
        lock (_lock)
        {
            if (LastSize != size)
            {
                ClearAvailableQueue();
            }

            LastSize = size;
        }

        if (Context.ShouldRenderOnUiThread)
        {
            // If the render thread and the UI thread are the same, then synchronous waiting cannot be performed here. This is because synchronous waiting would block the UI thread, preventing it from receiving subsequent render completion events, and ultimately causing the UI thread to become unresponsive.
        }
        else if (_presentationCount > Context.MaxXShmSwapchainFrameCount)
        {
            // Specifically, allowing one additional frame beyond the maximum render limit is beneficial. This is because at any given moment, one frame might be in the process of being returned, and another might be currently rendering. Therefore, adding an extra frame in preparation for rendering can maximize rendering efficiency.
            SpinWait.SpinUntil(() => _presentationCount <= Context.MaxXShmSwapchainFrameCount);
        }

#nullable enable
        X11ShmImage? image = null;
        lock (_lock)
        {
            while (AvailableQueue.TryDequeue(out image))
            {
                if (image.Size != size)
                {
                    image.Dispose();
                    image = null;
                }
                else
                {
                    X11ShmDebugLogger.WriteLine(
                        $"[X11ShmImageManager][GetOrCreateImage] Get X11ShmImage from AvailableQueue.");
                    break;
                }
            }
        }

        if (image is null)
        {
            image = new X11ShmImage(size, this);
        }
#nullable disable

        var currentPresentationCount = Interlocked.Increment(ref _presentationCount);
        _ = currentPresentationCount;
        X11ShmDebugLogger.WriteLine(
            $"[X11ShmImageManager][GetOrCreateImage] PresentationCount={currentPresentationCount}");

        return image;
    }

    private int _presentationCount;

    public void OnXShmCompletion(X11ShmImage image)
    {
        var currentPresentationCount = Interlocked.Decrement(ref _presentationCount);
        _ = currentPresentationCount;
        X11ShmDebugLogger.WriteLine(
            $"[X11ShmImageManager][OnXShmCompletion] PresentationCount={currentPresentationCount}");

        if (_isDisposed)
        {
            image.Dispose();
            return;
        }

        lock (_lock)
        {
            if (image.Size != LastSize)
            {
                image.Dispose();
                return;
            }

            AvailableQueue.Enqueue(image);
        }
    }

    public void Dispose()
    {
        _isDisposed = true;

        lock (_lock)
        {
            ClearAvailableQueue();
        }
    }

    private void ClearAvailableQueue()
    {
        while (AvailableQueue.TryDequeue(out var x11ShmImage))
        {
            x11ShmImage.Dispose();
        }
    }

    private bool _isDisposed;
}
