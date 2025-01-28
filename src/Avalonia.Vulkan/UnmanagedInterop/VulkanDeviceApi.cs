using System;
using Avalonia.SourceGenerator;
using Avalonia.Vulkan.Interop;
using uint32_t = System.UInt32;
using uint64_t = System.UInt64;
using VkBool32 = System.UInt32;
using VkDeviceSize = System.UInt64;

namespace Avalonia.Vulkan.UnmanagedInterop;


internal unsafe partial class VulkanDeviceApi
{
    public VulkanDeviceApi(IVulkanDevice device)
    {
        Initialize(name =>
        {
            var addr = device.Instance.GetDeviceProcAddress(device.Handle, name);
            if (addr != IntPtr.Zero)
                return addr;
            return device.Instance.GetInstanceProcAddress(device.Instance.Handle, name);
        });
    }

    [GetProcAddress("vkCreateFence")]
    public partial VkResult CreateFence(VkDevice device,
        ref VkFenceCreateInfo pCreateInfo,
        IntPtr pAllocator,
        out VkFence pFence);

    [GetProcAddress("vkDestroyFence")]
    public partial void DestroyFence(VkDevice device, VkFence fence, IntPtr pAllocator);

    [GetProcAddress("vkCreateCommandPool")]
    public partial VkResult CreateCommandPool(VkDevice device, ref VkCommandPoolCreateInfo pCreateInfo,
        IntPtr pAllocator, out VkCommandPool pCommandPool);

    [GetProcAddress("vkDestroyCommandPool")]
    public partial void DestroyCommandPool(VkDevice device, VkCommandPool pool, IntPtr pAllocator);

    [GetProcAddress("vkAllocateCommandBuffers")]
    public partial VkResult AllocateCommandBuffers(VkDevice device,
        ref VkCommandBufferAllocateInfo pAllocateInfo, VkCommandBuffer* pCommandBuffers);

    [GetProcAddress("vkFreeCommandBuffers")]
    public partial void FreeCommandBuffers(VkDevice device, VkCommandPool commandPool, uint32_t commandBufferCount,
        VkCommandBuffer* pCommandBuffers);

    [GetProcAddress("vkWaitForFences")]
    public partial VkResult WaitForFences(VkDevice device, uint32_t fenceCount, VkFence* pFences, VkBool32 waitAll,
        uint64_t timeout);

    [GetProcAddress("vkGetFenceStatus")]
    public partial VkResult GetFenceStatus(VkDevice device, VkFence fence);
    
    [GetProcAddress("vkBeginCommandBuffer")]
    public partial VkResult BeginCommandBuffer(VkCommandBuffer commandBuffer, ref VkCommandBufferBeginInfo pBeginInfo);

    [GetProcAddress("vkEndCommandBuffer")]
    public partial VkResult EndCommandBuffer(VkCommandBuffer commandBuffer);

    [GetProcAddress("vkCreateSemaphore")]
    public partial VkResult CreateSemaphore(VkDevice device, ref VkSemaphoreCreateInfo pCreateInfo,
        IntPtr pAllocator, out VkSemaphore pSemaphore);

    [GetProcAddress("vkDestroySemaphore")]
    public partial void DestroySemaphore(VkDevice device, VkSemaphore semaphore, IntPtr pAllocator);

    [GetProcAddress("vkResetFences")]
    public partial VkResult ResetFences(VkDevice device, uint32_t fenceCount, VkFence* pFences);

    [GetProcAddress("vkQueueSubmit")]
    public partial VkResult QueueSubmit(VkQueue queue, uint32_t submitCount, VkSubmitInfo* pSubmits,
        VkFence fence);

    [GetProcAddress("vkCreateImage")]
    public partial VkResult CreateImage(VkDevice device, ref VkImageCreateInfo pCreateInfo, IntPtr pAllocator,
        out VkImage pImage);
    
    [GetProcAddress("vkDestroyImage")]
    public partial void DestroyImage(VkDevice device, VkImage image, IntPtr pAllocator);

    [GetProcAddress("vkGetImageMemoryRequirements")]
    public partial void GetImageMemoryRequirements(VkDevice device, VkImage image,
        out VkMemoryRequirements pMemoryRequirements);

    [GetProcAddress("vkAllocateMemory")]
    public partial VkResult AllocateMemory(VkDevice device, ref VkMemoryAllocateInfo pAllocateInfo, IntPtr pAllocator,
        out VkDeviceMemory pMemory);
    
    [GetProcAddress("vkFreeMemory")]
    public partial void FreeMemory(VkDevice device, VkDeviceMemory memory, IntPtr pAllocator);

    [GetProcAddress("vkBindImageMemory")]
    public partial VkResult BindImageMemory(VkDevice device, VkImage image, VkDeviceMemory memory,
        VkDeviceSize memoryOffset);

    [GetProcAddress("vkCreateImageView")]
    public partial VkResult CreateImageView(VkDevice device, ref VkImageViewCreateInfo pCreateInfo, IntPtr pAllocator,
        out VkImageView pView);
    
    [GetProcAddress("vkDestroyImageView")]
    public partial void DestroyImageView(VkDevice device, VkImageView imageView, IntPtr pAllocator);
    

    [GetProcAddress("vkCmdPipelineBarrier")]
    public partial void CmdPipelineBarrier(VkCommandBuffer commandBuffer,
        VkPipelineStageFlags srcStageMask,
        VkPipelineStageFlags dstStageMask,
        VkDependencyFlags dependencyFlags,
        uint32_t memoryBarrierCount,
        IntPtr pMemoryBarriers,
        uint32_t bufferMemoryBarrierCount,
        IntPtr pBufferMemoryBarriers,
        uint32_t imageMemoryBarrierCount,
        VkImageMemoryBarrier* pImageMemoryBarriers);
    
    [GetProcAddress("vkCreateSwapchainKHR")]
    public partial VkResult CreateSwapchainKHR(VkDevice device, ref VkSwapchainCreateInfoKHR pCreateInfo,
        IntPtr pAllocator, out VkSwapchainKHR pSwapchain);
    
    [GetProcAddress("vkDestroySwapchainKHR")]
    public partial void DestroySwapchainKHR(VkDevice device, VkSwapchainKHR swapchain, IntPtr pAllocator);

    [GetProcAddress("vkGetSwapchainImagesKHR")]
    public partial VkResult GetSwapchainImagesKHR(VkDevice device, VkSwapchainKHR swapchain, ref uint32_t pSwapchainImageCount,
        VkImage* pSwapchainImages);

    [GetProcAddress("vkDeviceWaitIdle")]
    public partial VkResult DeviceWaitIdle(VkDevice device);

    [GetProcAddress("vkQueueWaitIdle")]
    public partial VkResult QueueWaitIdle(VkQueue queue);

    [GetProcAddress("vkAcquireNextImageKHR")]
    public partial VkResult AcquireNextImageKHR(VkDevice device, VkSwapchainKHR swapchain, uint64_t timeout,
        VkSemaphore semaphore, VkFence fence, out uint32_t pImageIndex);

    [GetProcAddress("vkCmdBlitImage")]
    public partial void CmdBlitImage(VkCommandBuffer commandBuffer, VkImage srcImage, VkImageLayout srcImageLayout,
        VkImage dstImage, VkImageLayout dstImageLayout, uint32_t regionCount, VkImageBlit* pRegions, VkFilter filter);

    [GetProcAddress("vkQueuePresentKHR")]
    public partial VkResult vkQueuePresentKHR(VkQueue queue, ref VkPresentInfoKHR pPresentInfo);

    [GetProcAddress("vkImportSemaphoreFdKHR", true)]
    public partial VkResult ImportSemaphoreFdKHR(VkDevice device, VkImportSemaphoreFdInfoKHR* pImportSemaphoreFdInfo);

    [GetProcAddress("vkImportSemaphoreWin32HandleKHR", true)]
    public partial VkResult ImportSemaphoreWin32HandleKHR(VkDevice device,
        VkImportSemaphoreWin32HandleInfoKHR* pImportSemaphoreWin32HandleInfo);

    [GetProcAddress("vkGetMemoryWin32HandlePropertiesKHR")]
    public partial VkResult GetMemoryWin32HandlePropertiesKHR(VkDevice device, VkExternalMemoryHandleTypeFlagBits handleType, nint handle, VkMemoryWin32HandlePropertiesKHR* pMemoryWin32HandleProperties);
}
