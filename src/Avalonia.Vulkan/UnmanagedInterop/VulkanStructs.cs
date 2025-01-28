// ReSharper disable IdentifierTypo

// ReSharper disable InconsistentNaming
// ReSharper disable NotAccessedField.Global
#pragma warning disable CS0649
#pragma warning disable CS0169
#pragma warning disable CA1823
using System;
using uint32_t = System.UInt32;
using VkSampleCountFlags = System.UInt32;
using int32_t = System.Int32;
using VkBool32 = System.UInt32;
using uint8_t = System.Byte;
using size_t = System.IntPtr;
using VkDeviceSize = System.UInt64;
// ReSharper disable RedundantUnsafeContext

namespace Avalonia.Vulkan.UnmanagedInterop
{
    struct VkInstance
    {
        public IntPtr Handle;
        public VkInstance(IntPtr handle)
        {
            Handle = handle;
        }
    }

    struct VkPhysicalDevice
    {
        public IntPtr Handle;
        public VkPhysicalDevice(IntPtr handle)
        {
            Handle = handle;
        }
    }

    struct VkDevice
    {
        public IntPtr Handle;

        public VkDevice(IntPtr handle)
        {
            Handle = handle;
        }
    }

    struct VkSwapchainKHR
    {
        public ulong Handle;
    }

    struct VkSemaphore
    {
        public ulong Handle;
    }

    struct VkFence
    {
        public ulong Handle;
    }

    struct VkImage
    {
        public ulong Handle;
    }
    
    struct VkImageView
    {
        public ulong Handle;
    }
    
    struct VkDeviceMemory
    {
        public ulong Handle;
    }

    struct VkQueue
    {
        public IntPtr Handle;
        public VkQueue(IntPtr handle)
        {
            Handle = handle;
        }
    }

    struct VkCommandPool
    {
        public ulong Handle;
    }
    
    struct VkCommandBuffer
    {
        public IntPtr Handle;
    }

    struct VkSurfaceKHR
    {
        public ulong Handle;

        public VkSurfaceKHR(ulong handle)
        {
            Handle = handle;
        }
    }
    
    struct VkDebugUtilsMessengerEXT
    {
        public IntPtr Handle;
    }

    unsafe struct VkLayerProperties
    {
        public fixed byte layerName[256];
        public uint32_t specVersion;
        public uint32_t implementationVersion;
        public fixed byte description[256];
    }

    unsafe struct VkDebugUtilsLabelEXT
    {
        public VkStructureType sType;
        public IntPtr pNext;
        public IntPtr pLabelName;
        public fixed float color[4];
    }

    struct VkDebugUtilsObjectNameInfoEXT
    {
        public VkStructureType sType;
        public IntPtr pNext;
        public VkObjectType objectType;
        public ulong objectHandle;
        public IntPtr pObjectName;
    }

    unsafe struct VkDebugUtilsMessengerCallbackDataEXT
    {
        public VkStructureType sType;
        public IntPtr pNext;
        public uint flags;
        public IntPtr pMessageIdName;
        public int32_t messageIdNumber;
        public IntPtr pMessage;
        public uint32_t queueLabelCount;
        public VkDebugUtilsLabelEXT* pQueueLabels;
        public uint32_t cmdBufLabelCount;
        public VkDebugUtilsLabelEXT* pCmdBufLabels;
        public uint32_t objectCount;
        public VkDebugUtilsObjectNameInfoEXT* pObjects;
    }

    unsafe delegate VkBool32 VkDebugUtilsMessengerCallbackEXTDelegate(
        VkDebugUtilsMessageSeverityFlagsEXT messageSeverity,
        VkDebugUtilsMessageTypeFlagsEXT messageTypes,
        VkDebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* pUserData);

    struct VkDebugUtilsMessengerCreateInfoEXT
    {
        public VkStructureType sType;
        public IntPtr pNext;
        public uint flags;
        public VkDebugUtilsMessageSeverityFlagsEXT messageSeverity;
        public VkDebugUtilsMessageTypeFlagsEXT messageType;
        public IntPtr pfnUserCallback;
        public IntPtr pUserData;
    }

    unsafe struct VkInstanceCreateInfo
    {
        public VkStructureType sType;
        public IntPtr pNext;
        public int flags;
        public VkApplicationInfo* pApplicationInfo;
        public uint32_t enabledLayerCount;
        public byte** ppEnabledLayerNames;
        public uint32_t enabledExtensionCount;
        public byte** ppEnabledExtensionNames;
    }

    unsafe struct VkApplicationInfo
    {
        public VkStructureType sType;
        public void* pNext;
        public byte* pApplicationName;
        public uint32_t applicationVersion;
        public byte* pEngineName;
        public uint32_t engineVersion;
        public uint32_t apiVersion;
    }

    unsafe struct VkPhysicalDeviceSparseProperties
    {
        public VkBool32 residencyStandard2DBlockShape;
        public VkBool32 residencyStandard2DMultisampleBlockShape;
        public VkBool32 residencyStandard3DBlockShape;
        public VkBool32 residencyAlignedMipSize;
        public VkBool32 residencyNonResidentStrict;
    }

    unsafe struct VkPhysicalDeviceProperties
    {
        public uint32_t apiVersion;
        public uint32_t driverVersion;
        public uint32_t vendorID;
        public uint32_t deviceID;
        public VkPhysicalDeviceType deviceType;
        public fixed byte deviceName[256];
        public fixed uint8_t pipelineCacheUUID[16];
        public VkPhysicalDeviceLimits limits;
        public VkPhysicalDeviceSparseProperties sparseProperties;
    }
    
    unsafe struct VkPhysicalDeviceProperties2 {
        public VkStructureType               sType;
        public void*                         pNext;
        public VkPhysicalDeviceProperties    properties;
    }
    
    unsafe struct VkPhysicalDeviceIDProperties {
        public VkStructureType    sType;
        public void*              pNext;
        public fixed uint8_t            deviceUUID[16];
        public fixed uint8_t            driverUUID[16];
        public fixed uint8_t            deviceLUID[8];
        public uint32_t           deviceNodeMask;
        public VkBool32           deviceLUIDValid;
    } 
    
    unsafe struct VkPhysicalDeviceLimits
    {
        public uint32_t maxImageDimension1D;
        public uint32_t maxImageDimension2D;
        public uint32_t maxImageDimension3D;
        public uint32_t maxImageDimensionCube;
        public uint32_t maxImageArrayLayers;
        public uint32_t maxTexelBufferElements;
        public uint32_t maxUniformBufferRange;
        public uint32_t maxStorageBufferRange;
        public uint32_t maxPushConstantsSize;
        public uint32_t maxMemoryAllocationCount;
        public uint32_t maxSamplerAllocationCount;
        public VkDeviceSize bufferImageGranularity;
        public VkDeviceSize sparseAddressSpaceSize;
        public uint32_t maxBoundDescriptorSets;
        public uint32_t maxPerStageDescriptorSamplers;
        public uint32_t maxPerStageDescriptorUniformBuffers;
        public uint32_t maxPerStageDescriptorStorageBuffers;
        public uint32_t maxPerStageDescriptorSampledImages;
        public uint32_t maxPerStageDescriptorStorageImages;
        public uint32_t maxPerStageDescriptorInputAttachments;
        public uint32_t maxPerStageResources;
        public uint32_t maxDescriptorSetSamplers;
        public uint32_t maxDescriptorSetUniformBuffers;
        public uint32_t maxDescriptorSetUniformBuffersDynamic;
        public uint32_t maxDescriptorSetStorageBuffers;
        public uint32_t maxDescriptorSetStorageBuffersDynamic;
        public uint32_t maxDescriptorSetSampledImages;
        public uint32_t maxDescriptorSetStorageImages;
        public uint32_t maxDescriptorSetInputAttachments;
        public uint32_t maxVertexInputAttributes;
        public uint32_t maxVertexInputBindings;
        public uint32_t maxVertexInputAttributeOffset;
        public uint32_t maxVertexInputBindingStride;
        public uint32_t maxVertexOutputComponents;
        public uint32_t maxTessellationGenerationLevel;
        public uint32_t maxTessellationPatchSize;
        public uint32_t maxTessellationControlPerVertexInputComponents;
        public uint32_t maxTessellationControlPerVertexOutputComponents;
        public uint32_t maxTessellationControlPerPatchOutputComponents;
        public uint32_t maxTessellationControlTotalOutputComponents;
        public uint32_t maxTessellationEvaluationInputComponents;
        public uint32_t maxTessellationEvaluationOutputComponents;
        public uint32_t maxGeometryShaderInvocations;
        public uint32_t maxGeometryInputComponents;
        public uint32_t maxGeometryOutputComponents;
        public uint32_t maxGeometryOutputVertices;
        public uint32_t maxGeometryTotalOutputComponents;
        public uint32_t maxFragmentInputComponents;
        public uint32_t maxFragmentOutputAttachments;
        public uint32_t maxFragmentDualSrcAttachments;
        public uint32_t maxFragmentCombinedOutputResources;
        public uint32_t maxComputeSharedMemorySize;
        public fixed uint32_t maxComputeWorkGroupCount[3];
        public uint32_t maxComputeWorkGroupInvocations;
        public fixed uint32_t maxComputeWorkGroupSize[3];
        public uint32_t subPixelPrecisionBits;
        public uint32_t subTexelPrecisionBits;
        public uint32_t mipmapPrecisionBits;
        public uint32_t maxDrawIndexedIndexValue;
        public uint32_t maxDrawIndirectCount;
        public float maxSamplerLodBias;
        public float maxSamplerAnisotropy;
        public uint32_t maxViewports;
        public fixed uint32_t maxViewportDimensions[2];
        public fixed float viewportBoundsRange[2];
        public uint32_t viewportSubPixelBits;
        public size_t minMemoryMapAlignment;
        public VkDeviceSize minTexelBufferOffsetAlignment;
        public VkDeviceSize minUniformBufferOffsetAlignment;
        public VkDeviceSize minStorageBufferOffsetAlignment;
        public int32_t minTexelOffset;
        public uint32_t maxTexelOffset;
        public int32_t minTexelGatherOffset;
        public uint32_t maxTexelGatherOffset;
        public float minInterpolationOffset;
        public float maxInterpolationOffset;
        public uint32_t subPixelInterpolationOffsetBits;
        public uint32_t maxFramebufferWidth;
        public uint32_t maxFramebufferHeight;
        public uint32_t maxFramebufferLayers;
        public VkSampleCountFlags framebufferColorSampleCounts;
        public VkSampleCountFlags framebufferDepthSampleCounts;
        public VkSampleCountFlags framebufferStencilSampleCounts;
        public VkSampleCountFlags framebufferNoAttachmentsSampleCounts;
        public uint32_t maxColorAttachments;
        public VkSampleCountFlags sampledImageColorSampleCounts;
        public VkSampleCountFlags sampledImageIntegerSampleCounts;
        public VkSampleCountFlags sampledImageDepthSampleCounts;
        public VkSampleCountFlags sampledImageStencilSampleCounts;
        public VkSampleCountFlags storageImageSampleCounts;
        public uint32_t maxSampleMaskWords;
        public VkBool32 timestampComputeAndGraphics;
        public float timestampPeriod;
        public uint32_t maxClipDistances;
        public uint32_t maxCullDistances;
        public uint32_t maxCombinedClipAndCullDistances;
        public uint32_t discreteQueuePriorities;
        public fixed float pointSizeRange[2];
        public fixed float lineWidthRange[2];
        public float pointSizeGranularity;
        public float lineWidthGranularity;
        public VkBool32 strictLines;
        public VkBool32 standardSampleLocations;
        public VkDeviceSize optimalBufferCopyOffsetAlignment;
        public VkDeviceSize optimalBufferCopyRowPitchAlignment;
        public VkDeviceSize nonCoherentAtomSize;
    }

    internal unsafe struct VkExtensionProperties
    {
        public fixed byte extensionName[256];
        public uint32_t specVersion;
    }

    struct VkQueueFamilyProperties
    {
        public VkQueueFlags queueFlags;
        public uint32_t queueCount;
        public uint32_t timestampValidBits;
        public VkExtent3D minImageTransferGranularity;
    }

    struct VkExtent2D
    {
        public uint32_t width;
        public uint32_t height;
    }

    struct VkExtent3D
    {
        public uint32_t width;
        public uint32_t height;
        public uint32_t depth;
    }

    unsafe struct VkDeviceQueueCreateInfo
    {
        public VkStructureType sType;
        public IntPtr pNext;
        public VkDeviceQueueCreateFlags flags;
        public uint32_t queueFamilyIndex;
        public uint32_t queueCount;
        public float* pQueuePriorities;
    }

    unsafe struct VkDeviceCreateInfo
    {
        public VkStructureType sType;
        public IntPtr pNext;
        public uint flags;
        public uint32_t queueCreateInfoCount;
        public VkDeviceQueueCreateInfo* pQueueCreateInfos;
        public uint32_t enabledLayerCount;
        public byte** ppEnabledLayerNames;
        public uint32_t enabledExtensionCount;
        public byte** ppEnabledExtensionNames;
        public IntPtr pEnabledFeatures;
    }

    struct VkFenceCreateInfo
    {
        public VkStructureType sType;
        IntPtr pNext;
        public VkFenceCreateFlags flags;
    }

    struct VkCommandPoolCreateInfo
    {
        public VkStructureType sType;
        IntPtr pNext;
        public VkCommandPoolCreateFlags flags;
        public uint32_t queueFamilyIndex;
    }
    
    struct VkCommandBufferAllocateInfo
    {
        public VkStructureType sType;
        IntPtr pNext;
        public VkCommandPool commandPool;
        public VkCommandBufferLevel level;
        public uint32_t commandBufferCount;
    }



    struct VkCommandBufferBeginInfo
    {
        public VkStructureType sType;
        IntPtr pNext;
        public VkCommandBufferUsageFlags flags;
        IntPtr pInheritanceInfo;
    }

    struct VkSemaphoreCreateInfo
    {
        public VkStructureType sType;
        IntPtr pNext;
        uint flags;
    }

    unsafe struct VkSubmitInfo
    {
        public VkStructureType sType;
        public void* pNext;
        public uint32_t waitSemaphoreCount;
        public VkSemaphore* pWaitSemaphores;
        public VkPipelineStageFlags* pWaitDstStageMask;
        public uint32_t commandBufferCount;
        public VkCommandBuffer* pCommandBuffers;
        public uint32_t signalSemaphoreCount;
        public VkSemaphore* pSignalSemaphores;
    }


    struct VkSurfaceFormatKHR
    {
        public VkFormat format;
        public VkColorSpaceKHR colorSpace;
    }



    struct VkMemoryType
    {
        public VkMemoryPropertyFlags propertyFlags;
        public uint32_t heapIndex;
    }

    struct VkMemoryHeap
    {
        public VkDeviceSize size;
        public VkMemoryHeapFlags flags;
    }

    unsafe struct VkPhysicalDeviceMemoryProperties
    {
        public uint32_t memoryTypeCount;
        public VkMemoryTypesBuffer memoryTypes;
        public uint32_t memoryHeapCount;
        public VkMemoryHeapsBuffer memoryHeaps;

        public struct VkMemoryTypesBuffer
        {
            public VkMemoryType Element0;
            public VkMemoryType Element1;
            public VkMemoryType Element2;
            public VkMemoryType Element3;
            public VkMemoryType Element4;
            public VkMemoryType Element5;
            public VkMemoryType Element6;
            public VkMemoryType Element7;
            public VkMemoryType Element8;
            public VkMemoryType Element9;
            public VkMemoryType Element10;
            public VkMemoryType Element11;
            public VkMemoryType Element12;
            public VkMemoryType Element13;
            public VkMemoryType Element14;
            public VkMemoryType Element15;
            public VkMemoryType Element16;
            public VkMemoryType Element17;
            public VkMemoryType Element18;
            public VkMemoryType Element19;
            public VkMemoryType Element20;
            public VkMemoryType Element21;
            public VkMemoryType Element22;
            public VkMemoryType Element23;
            public VkMemoryType Element24;
            public VkMemoryType Element25;
            public VkMemoryType Element26;
            public VkMemoryType Element27;
            public VkMemoryType Element28;
            public VkMemoryType Element29;
            public VkMemoryType Element30;
            public VkMemoryType Element31;

            public ref VkMemoryType this[int index]
            {
                get
                {
                    if (index > 31 || index < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    fixed (VkMemoryType* ptr = &Element0)
                    {
                        return ref ptr[index];
                    }
                }
            }
        }

        public struct VkMemoryHeapsBuffer
        {
            public VkMemoryHeap Element0;
            public VkMemoryHeap Element1;
            public VkMemoryHeap Element2;
            public VkMemoryHeap Element3;
            public VkMemoryHeap Element4;
            public VkMemoryHeap Element5;
            public VkMemoryHeap Element6;
            public VkMemoryHeap Element7;
            public VkMemoryHeap Element8;
            public VkMemoryHeap Element9;
            public VkMemoryHeap Element10;
            public VkMemoryHeap Element11;
            public VkMemoryHeap Element12;
            public VkMemoryHeap Element13;
            public VkMemoryHeap Element14;
            public VkMemoryHeap Element15;

            public ref VkMemoryHeap this[int index]
            {
                get
                {
                    if (index > 15 || index < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    fixed (VkMemoryHeap* ptr = &Element0)
                    {
                        return ref ptr[index];
                    }
                }
            }


        }
    }

    unsafe struct VkImageCreateInfo
    {
        public VkStructureType sType;
        public void* pNext;
        public VkImageCreateFlags flags;
        public VkImageType imageType;
        public VkFormat format;
        public VkExtent3D extent;
        public uint32_t mipLevels;
        public uint32_t arrayLayers;
        public VkSampleCountFlags samples;
        public VkImageTiling tiling;
        public VkImageUsageFlags usage;
        public VkSharingMode sharingMode;
        public uint32_t queueFamilyIndexCount;
        public uint32_t* pQueueFamilyIndices;
        public VkImageLayout initialLayout;
    }

    struct VkMemoryRequirements
    {
        public VkDeviceSize size;
        public VkDeviceSize alignment;
        public uint32_t memoryTypeBits;
    }

    struct VkMemoryAllocateInfo
    {
        public VkStructureType sType;
        public IntPtr pNext;
        public VkDeviceSize allocationSize;
        public uint32_t memoryTypeIndex;
    }

    struct VkComponentMapping
    {
        public VkComponentSwizzle r;
        public VkComponentSwizzle g;
        public VkComponentSwizzle b;
        public VkComponentSwizzle a;
    }

    struct VkImageSubresourceRange
    {
        public VkImageAspectFlags aspectMask;
        public uint32_t baseMipLevel;
        public uint32_t levelCount;
        public uint32_t baseArrayLayer;
        public uint32_t layerCount;
    }

    struct VkImageViewCreateInfo
    {
        public VkStructureType sType;
        public IntPtr pNext;
        public VkImageViewCreateFlags flags;
        public VkImage image;
        public VkImageViewType viewType;
        public VkFormat format;
        public VkComponentMapping components;
        public VkImageSubresourceRange subresourceRange;
    }

    struct VkImageMemoryBarrier
    {
        public VkStructureType sType;
        IntPtr pNext;
        public VkAccessFlags srcAccessMask;
        public VkAccessFlags dstAccessMask;
        public VkImageLayout oldLayout;
        public VkImageLayout newLayout;
        public uint32_t srcQueueFamilyIndex;
        public uint32_t dstQueueFamilyIndex;
        public VkImage image;
        public VkImageSubresourceRange subresourceRange;
    }

    struct VkSurfaceCapabilitiesKHR
    {
        public uint32_t minImageCount;
        public uint32_t maxImageCount;
        public VkExtent2D currentExtent;
        public VkExtent2D minImageExtent;
        public VkExtent2D maxImageExtent;
        public uint32_t maxImageArrayLayers;
        public VkSurfaceTransformFlagsKHR supportedTransforms;
        public VkSurfaceTransformFlagsKHR currentTransform;
        public VkCompositeAlphaFlagsKHR supportedCompositeAlpha;
        public VkImageUsageFlags supportedUsageFlags;
    }

    unsafe struct VkSwapchainCreateInfoKHR
    {
        public VkStructureType sType;
        public IntPtr pNext;
        public VkSwapchainCreateFlagsKHR flags;
        public VkSurfaceKHR surface;
        public uint32_t minImageCount;
        public VkFormat imageFormat;
        public VkColorSpaceKHR imageColorSpace;
        public VkExtent2D imageExtent;
        public uint32_t imageArrayLayers;
        public VkImageUsageFlags imageUsage;
        public VkSharingMode imageSharingMode;
        public uint32_t queueFamilyIndexCount;
        public uint32_t* pQueueFamilyIndices;
        public VkSurfaceTransformFlagsKHR preTransform;
        public VkCompositeAlphaFlagsKHR compositeAlpha;
        public VkPresentModeKHR presentMode;
        public VkBool32 clipped;
        public VkSwapchainKHR oldSwapchain;
    }

    struct VkOffset3D
    {
        public int32_t x;
        public int32_t y;
        public int32_t z;
    }

    struct VkImageSubresourceLayers
    {
        public VkImageAspectFlags aspectMask;
        public uint32_t mipLevel;
        public uint32_t baseArrayLayer;
        public uint32_t layerCount;
    }

    struct VkImageBlit
    {
        public VkImageSubresourceLayers srcSubresource;
        public VkOffset3D srcOffsets1;
        public VkOffset3D srcOffsets2;
        public VkImageSubresourceLayers dstSubresource;
        public VkOffset3D dstOffsets1;
        public VkOffset3D dstOffsets2;
    }

    unsafe struct VkPresentInfoKHR
    {
        public VkStructureType sType;
        public IntPtr pNext;
        public uint32_t waitSemaphoreCount;
        public VkSemaphore* pWaitSemaphores;
        public uint32_t swapchainCount;
        public VkSwapchainKHR* pSwapchains;
        public uint32_t* pImageIndices;
        public VkResult* pResults;
    }

    unsafe struct VkImportSemaphoreFdInfoKHR
    {
        public VkStructureType sType;
        public void* pNext;
        public VkSemaphore semaphore;
        public VkSemaphoreImportFlags flags;
        public VkExternalSemaphoreHandleTypeFlags handleType;
        public int fd;
    }

    unsafe struct VkImportSemaphoreWin32HandleInfoKHR
    {
        public VkStructureType sType;
        public void* pNext;
        public VkSemaphore semaphore;
        public VkSemaphoreImportFlags flags;
        public VkExternalSemaphoreHandleTypeFlags handleType;
        public IntPtr handle;
        public IntPtr name;
    }

    unsafe struct VkImportMemoryFdInfoKHR
    {
        public VkStructureType sType;
        public void* pNext;
        public VkExternalMemoryHandleTypeFlagBits handleType;
        public int fd;
    }

    unsafe struct VkImportMemoryWin32HandleInfoKHR
    {
        public VkStructureType sType;
        public void* pNext;
        public VkExternalMemoryHandleTypeFlagBits handleType;
        public IntPtr handle;
        public IntPtr name;
    }

    unsafe struct VkMemoryWin32HandlePropertiesKHR
    {
        public VkStructureType sType;
        public void* pNext;
        public uint32_t memoryTypeBits;
    }

    unsafe struct VkWin32KeyedMutexAcquireReleaseInfoKHR
    {
        public VkStructureType sType;
        public void* pNext;
        public uint32_t acquireCount;
        public VkDeviceMemory* pAcquireSyncs;
        public ulong* pAcquireKeys;
        public uint* pAcquireTimeouts;
        public uint releaseCount;
        public VkDeviceMemory* pReleaseSyncs;
        public ulong* pReleaseKeys;
    }

    unsafe struct VkMemoryDedicatedAllocateInfo
    {
        public VkStructureType sType;
        public void* pNext;
        public VkImage image;
        public IntPtr buffer;
    }

    unsafe struct VkExternalMemoryImageCreateInfo
    {
        public VkStructureType sType;
        public void* pNext;
        public VkExternalMemoryHandleTypeFlagBits handleTypes;
    }
}
