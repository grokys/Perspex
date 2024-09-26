﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;

using ShmSeg = System.UInt64;
using static Avalonia.X11.XLib;
using static Avalonia.X11.LibC;
using static Avalonia.X11.XShmExtensions.XShm;
using System.Runtime.InteropServices;

namespace Avalonia.X11.XShmExtensions;

class X11ShmFramebufferContext
{
    public X11ShmFramebufferContext(X11Window x11Window, IntPtr display, IntPtr windowXId, IntPtr renderHandle, IntPtr visual, int depth)
    {
        X11Window = x11Window;
        Display = display;
        WindowXId = windowXId;
        RenderHandle = renderHandle;
        Visual = visual;
        Depth = depth;
    }

    public X11Window X11Window { get; }

    public IntPtr Display { get; }

    public IntPtr WindowXId { get; }

    public IntPtr RenderHandle { get; }
    public IntPtr Visual { get; }
    public int Depth { get; }

    public void OnXShmCompletion(ShmSeg shmseg)
    {
        if (_shmImageDictionary.Remove(shmseg, out var image))
        {

        }
        else
        {
            // Unexpected case, all the X11ShmImage should be registered in the dictionary
        }
    }

    public void RegisterX11ShmImage(X11ShmImage image)
    {
        _shmImageDictionary[image.ShmSeg] = image;
    }

    private readonly Dictionary<ShmSeg, X11ShmImage> _shmImageDictionary = new Dictionary<ShmSeg, X11ShmImage>();
}

internal class X11ShmFramebufferSurface : IFramebufferPlatformSurface
{
    public X11ShmFramebufferSurface(X11Window x11Window, IntPtr display, IntPtr windowHandle, IntPtr renderHandle, IntPtr visual, int depth)
    {
        _context = new X11ShmFramebufferContext(x11Window, display, windowHandle, renderHandle, visual, depth);
    }

    private readonly X11ShmFramebufferContext _context;

    public IFramebufferRenderTarget CreateFramebufferRenderTarget()
    {
        return new X11ShmImageSwapchain(_context);
    }

    public unsafe void OnXShmCompletionEvent(XEvent @event)
    {
        var p = &@event;
        var xShmCompletionEvent = (XShmCompletionEvent*)p;
        ShmSeg shmseg = xShmCompletionEvent->shmseg;
        _context.OnXShmCompletion(shmseg);
    }
}

internal unsafe class X11ShmImage : IDisposable
{
    public X11ShmImage(PixelSize size, X11ShmImageManager x11ShmImageManager)
    {
        _x11ShmImageManager = x11ShmImageManager;
        // The XShmSegmentInfo struct will store in XImage, and it must pin the address.
        IntPtr pXShmSegmentInfo = Marshal.AllocHGlobal(Marshal.SizeOf<XShmSegmentInfo>());
        var pShmSegmentInfo = (XShmSegmentInfo*)pXShmSegmentInfo;
        PShmSegmentInfo = pShmSegmentInfo;

        var context = x11ShmImageManager.Context;
        var display = context.Display;
        var visual = context.Visual;

        const int ZPixmap = 2;

        var width = size.Width;
        var height = size.Height;

        Size = size;

        var depth = context.Depth;
        IntPtr data = 0;

        var shmImage = (XImage*)XShmCreateImage(display, visual, (uint) depth, ZPixmap, data, pShmSegmentInfo,
            (uint)width, (uint)height);
        ShmImage = shmImage;

        var mapLength = width * 4 * height;
        var shmgetResult = shmget(IPC_PRIVATE, mapLength, IPC_CREAT | 0777);
        pShmSegmentInfo->shmid = shmgetResult;

        var shmaddr = shmat(shmgetResult, IntPtr.Zero, 0);
        pShmSegmentInfo->shmaddr = (char*)shmaddr.ToPointer();
        shmImage->data = data = shmaddr;

        XShmAttach(display, pShmSegmentInfo);
    }

    private readonly X11ShmImageManager _x11ShmImageManager;

    public XImage* ShmImage { get; set; }
    public XShmSegmentInfo* PShmSegmentInfo { get; }
    public XShmSegmentInfo ShmSegmentInfo => *PShmSegmentInfo;
    public IntPtr ShmAddr => new IntPtr(PShmSegmentInfo->shmaddr);

    /// <summary>
    /// Returns false if we haven't got a completion event since the last Present, can call ProcessPendingEvents here
    /// </summary>
    public bool IsReady { get; set; }

    public PixelSize Size { get; }

    public ShmSeg ShmSeg => PShmSegmentInfo->shmseg;

    public void Reuse()
    {
        _x11ShmImageManager.TryReuse(this);
    }

    public void Dispose()
    {
        //XShmAttach(display, pShmSegmentInfo);
        // shmget

        Marshal.FreeHGlobal(new IntPtr(PShmSegmentInfo));
    }
}

internal class X11ShmImageManager : IDisposable
{
    public X11ShmImageManager(X11ShmFramebufferContext context)
    {
        Context = context;
    }

    public X11ShmFramebufferContext Context { get; }

    public Queue<X11ShmImage> AvailableQueue = new();

    public Queue<X11ShmImage> PresentationQueue = new();

    public PixelSize? LastSize { get; private set; }

    public X11ShmImage GetOrCreateImage(PixelSize size)
    {
        DrainPresentationQueue();

        if (LastSize != size)
        {
            foreach (var x11ShmImage in AvailableQueue)
            {
                x11ShmImage.Dispose();
            }
            AvailableQueue.Clear();
        }

        if (AvailableQueue.TryDequeue(out var image))
        {
        }
        else
        {
            // Check presentationQueue.Count < swapchainSize ?
            image = new X11ShmImage(size, this);
        }

        LastSize = size;

        image.IsReady = false;
        PresentationQueue.Enqueue(image);

        return image;
    }

    public void DrainPresentationQueue()
    {
        while (PresentationQueue.Count > 0 && PresentationQueue.Peek().IsReady)
        {
            AvailableQueue.Enqueue(PresentationQueue.Dequeue());
        }
    }

    public void TryReuse(X11ShmImage image)
    {
        if (_isDisposed)
        {
            image.Dispose();
            return;
        }

        if (image.Size != LastSize)
        {
            image.Dispose();
            return;
        }

        if (PresentationQueue.Contains(image))
        {
            image.IsReady = true;
        }
        else
        {
            image.Dispose();
        }
    }

    public void Dispose()
    {
        _isDisposed = true;
    }

    private bool _isDisposed;
}

class X11ShmLockedFramebuffer : ILockedFramebuffer
{
    public X11ShmLockedFramebuffer(X11ShmImage shmImage, X11ShmFramebufferContext context)
    {
        _context = context;
        X11ShmImage = shmImage;
    }

    public void Dispose()
    {
        // Send XShmImage and register it to handle the XShmCompletionEvent
        _context.RegisterX11ShmImage(X11ShmImage);
    }

    private readonly X11ShmFramebufferContext _context;

    public IntPtr Address { get; }
    public PixelSize Size { get; }
    public int RowBytes { get; }
    public Vector Dpi { get; }
    public PixelFormat Format { get; }
    public X11ShmImage X11ShmImage { get; }
}

internal class X11ShmImageSwapchain : IFramebufferRenderTarget
{
    public X11ShmImageSwapchain(X11ShmFramebufferContext context)
    {
        _context = context;
        X11ShmImageManager = new X11ShmImageManager(context);
    }

    public X11ShmImageManager X11ShmImageManager { get; }

    private readonly X11ShmFramebufferContext _context;

    public void Dispose()
    {
        X11ShmImageManager.Dispose();
    }

    public ILockedFramebuffer Lock()
    {
        /*
         1) gets the current window geometry, if it doesn't match the lastSize disposes images and clears queues
         2) calls DrainPresentationQueue();
         3) if there is an image in _availableQueue - use this image
         4) else if there are no images in _availableQueue and presentationQueue.Count < swapchainSize - create new image
         5) else synchronously wait for the first image from _presentationQueue and use it
         6) return a framebuffer associated with chosen image
         */
        var display = _context.Display;
        var xid = _context.WindowXId;
        XLockDisplay(display);
        XGetGeometry(display, xid, out var root, out var x, out var y, out var width, out var height,
            out var bw, out var d);
        XUnlockDisplay(display);

        var size = new PixelSize(width, height);
        var shmImage = X11ShmImageManager.GetOrCreateImage(size);

        return new X11ShmLockedFramebuffer(shmImage, _context);
    }
}
