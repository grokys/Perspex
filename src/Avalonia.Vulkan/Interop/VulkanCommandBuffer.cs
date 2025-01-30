using System;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan.Interop;

internal class VulkanCommandBuffer : IDisposable
{
    private readonly VulkanCommandBufferPool _pool;
    private VkCommandBuffer _handle;
    private readonly IVulkanPlatformGraphicsContext _context;
    private VulkanFence _fence;
    private bool _hasEnded;
    private bool _hasStarted;
    public VkCommandBuffer Handle => _handle;

    public VulkanCommandBuffer(VulkanCommandBufferPool pool, VkCommandBuffer handle, IVulkanPlatformGraphicsContext context)
    {
        _pool = pool;
        _handle = handle;
        _context = context;
        _fence = new VulkanFence(context, VkFenceCreateFlags.VK_FENCE_CREATE_SIGNALED_BIT);
    }

    public unsafe void Dispose()
    {
        if (_fence.Handle.Handle != 0)
            _fence.Wait();
        _fence.Dispose();
        if (_handle.Handle != IntPtr.Zero)
        {
            VkCommandBuffer buf = _handle;
            _context.DeviceApi.FreeCommandBuffers(_context.DeviceHandle, _pool.Handle, 1, &buf);
            _handle = default;
        }
    }

    public bool IsFinished => _fence.IsSignaled;

    public void BeginRecording()
    {
        if (_hasStarted)
            return;

        var beginInfo = new VkCommandBufferBeginInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO,
            flags = VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT
        };

        _context.DeviceApi.BeginCommandBuffer(_handle, ref beginInfo).ThrowOnError("vkBeginCommandBuffer");
        _hasStarted = true;
    }

    public void EndRecording()
    {
        if (_hasStarted && !_hasEnded)
        {
            _context.DeviceApi.EndCommandBuffer(_handle).ThrowOnError("vkEndCommandBuffer");
            _hasEnded = true;
        }
    }

    public void Submit()
    {
        Submit(null, null, null);
    }

    public class KeyedMutexSubmitInfo
    {
        public ulong? AcquireKey { get; set; }
        public ulong? ReleaseKey { get; set; }
        public VkDeviceMemory DeviceMemory { get; set; }
    }

    public unsafe void Submit(
        ReadOnlySpan<VulkanSemaphore> waitSemaphores,
        ReadOnlySpan<VkPipelineStageFlags> waitDstStageMask,
        ReadOnlySpan<VulkanSemaphore> signalSemaphores,
        VulkanFence? fence = null,
        KeyedMutexSubmitInfo? keyedMutex = null)
    {
        
        EndRecording();
        VkFence fenceHandle = (fence ?? _fence).Handle;
        _context.DeviceApi.ResetFences(_context.DeviceHandle, 1, &fenceHandle)
            .ThrowOnError("vkResetFences");
        
        var pWaitSempaphores = stackalloc VkSemaphore[waitSemaphores.Length];
        for (var c = 0; c < waitSemaphores.Length; c++)
            pWaitSempaphores[c] = waitSemaphores[c].Handle;
        
        var pSignalSemaphores = stackalloc VkSemaphore[signalSemaphores.Length];
        for (var c = 0; c < signalSemaphores.Length; c++)
            pSignalSemaphores[c] = signalSemaphores[c].Handle;

        ulong acquireKey = keyedMutex?.AcquireKey ?? 0, releaseKey = keyedMutex?.ReleaseKey ?? 0;
        VkDeviceMemory devMem = keyedMutex?.DeviceMemory ?? default;
        uint timeout = uint.MaxValue;
        VkWin32KeyedMutexAcquireReleaseInfoKHR mutex = default;
        if (keyedMutex != null)
            mutex = new VkWin32KeyedMutexAcquireReleaseInfoKHR
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_WIN32_KEYED_MUTEX_ACQUIRE_RELEASE_INFO_KHR,
                acquireCount = keyedMutex.AcquireKey.HasValue ? 1u : 0u,
                releaseCount = keyedMutex.ReleaseKey.HasValue ? 1u : 0u,
                pAcquireKeys = &acquireKey,
                pReleaseKeys = &releaseKey,
                pAcquireSyncs = &devMem,
                pReleaseSyncs = &devMem,
                pAcquireTimeouts = &timeout
            };

        VkCommandBuffer commandBuffer = _handle;
        fixed (VkPipelineStageFlags* flags = waitDstStageMask)
        {
            var submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO,
                pNext = keyedMutex != null ? &mutex : null,
                waitSemaphoreCount = (uint)waitSemaphores.Length,
                pWaitSemaphores = pWaitSempaphores,
                signalSemaphoreCount = (uint)signalSemaphores.Length,
                pSignalSemaphores = pSignalSemaphores,
                commandBufferCount = 1,
                pCommandBuffers = &commandBuffer,
                pWaitDstStageMask = flags
            };
            _context.DeviceApi.QueueSubmit(_context.MainQueueHandle, 1, &submitInfo, fenceHandle)
                .ThrowOnError("vkQueueSubmit");
        }
        _pool.AddSubmittedCommandBuffer(this);
    }
}
