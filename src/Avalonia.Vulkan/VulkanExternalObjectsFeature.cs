using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan;

internal unsafe class VulkanExternalObjectsFeature : IVulkanContextExternalObjectsFeature
{
    public static string[] RequiredInstanceExtensions = {
        "VK_KHR_get_physical_device_properties2",
        "VK_KHR_external_memory_capabilities",
        "VK_KHR_external_semaphore_capabilities"
    };


    private static string[] s_requiredCommonDeviceExtensions =
    {
        "VK_KHR_external_memory",
        "VK_KHR_external_semaphore",
        "VK_KHR_dedicated_allocation",
    };

    private static string[] s_requiredLinuxDeviceExtensions =
        s_requiredCommonDeviceExtensions.Concat(new[]
        {
            "VK_KHR_external_semaphore_fd",
            "VK_KHR_external_memory_fd"
        }).ToArray();

    private static string[] s_requiredWin32DeviceExtensions = s_requiredCommonDeviceExtensions.Concat(new[]
    {
        "VK_KHR_external_semaphore_win32",
        "VK_KHR_external_memory_win32"
    }).ToArray();

    public static string[] RequiredDeviceExtensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? s_requiredWin32DeviceExtensions
        : s_requiredLinuxDeviceExtensions;

    private readonly VulkanContext _context;
    private readonly VulkanCommandBufferPool _pool;

    public VulkanExternalObjectsFeature(VulkanContext context)
    {
        _context = context;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            //TODO: keyed muted
            SupportedImageHandleTypes = new[]
            {
                KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaqueNtHandle,
                KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaqueKmtHandle,
                KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureNtHandle,
            };
            SupportedSemaphoreTypes = new[]
            {
                KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaqueNtHandle,
                KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaqueKmtHandle
            };
        }
        else
        {
            SupportedImageHandleTypes = new[]
            {
                KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor
            };
            SupportedSemaphoreTypes = new[]
            {
                KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor
            };
        }

        var physicalDeviceIDProperties = new VkPhysicalDeviceIDProperties()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_ID_PROPERTIES
        };

        var physicalDeviceProperties2 = new VkPhysicalDeviceProperties2()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_PROPERTIES_2,
            pNext = &physicalDeviceIDProperties
        };
        _context.InstanceApi.GetPhysicalDeviceProperties2(_context.PhysicalDeviceHandle, &physicalDeviceProperties2);

        var luid = new Span<byte>(physicalDeviceIDProperties.deviceLUID, 8).ToArray();
        if (luid.Any(b => b != 0))
            DeviceLuid = luid;
        var uuid = new Span<byte>(physicalDeviceIDProperties.deviceUUID, 16).ToArray();
        if (uuid.Any(b => b != 0))
            DeviceUuid = uuid;
        _pool = new VulkanCommandBufferPool(_context, true);
    }

    public IReadOnlyList<string> SupportedImageHandleTypes { get; }
    public IReadOnlyList<string> SupportedSemaphoreTypes { get; }
    public byte[]? DeviceUuid { get; }
    public byte[]? DeviceLuid { get; }


    public CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType)
    {
        if (!SupportedImageHandleTypes.Contains(imageHandleType))
            throw new ArgumentException();
        
        return CompositionGpuImportedImageSynchronizationCapabilities.Semaphores | CompositionGpuImportedImageSynchronizationCapabilities.KeyedMutex;
    }

    public IVulkanExternalImage ImportImage(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties)
    {
        if (!SupportedImageHandleTypes.Contains(handle.HandleDescriptor))
            throw new NotSupportedException();

        return new ImportedImage(_context, _pool, handle, properties);
    }

    public IVulkanExternalSemaphore ImportSemaphore(IPlatformHandle handle)
    {
        if (!SupportedSemaphoreTypes.Contains(handle.HandleDescriptor))
            throw new NotSupportedException();

        var typeBit = handle.HandleDescriptor switch
        {
            KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor =>
                VkExternalSemaphoreHandleTypeFlags.VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_FD_BIT,
            KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaqueKmtHandle =>
                VkExternalSemaphoreHandleTypeFlags.VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_WIN32_KMT_BIT,
            KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaqueNtHandle =>
                VkExternalSemaphoreHandleTypeFlags.VK_EXTERNAL_SEMAPHORE_HANDLE_TYPE_OPAQUE_WIN32_BIT,
            _ => throw new NotSupportedException()
        };

        var semaphore = new VulkanSemaphore(_context);
        if (handle.HandleDescriptor ==
            KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor)
        {
            var info = new VkImportSemaphoreFdInfoKHR
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMPORT_SEMAPHORE_FD_INFO_KHR,
                fd = handle.Handle.ToInt32(),
                handleType = typeBit,
                semaphore = semaphore.Handle
            };
            var addr = _context.Instance.GetDeviceProcAddress(_context.Device.Handle, "vkImportSemaphoreFdKHR");
            if (addr == IntPtr.Zero)
                addr = _context.Instance.GetInstanceProcAddress(_context.Instance.Handle, "vkImportSemaphoreFdKHR");
            _context.DeviceApi.ImportSemaphoreFdKHR(_context.DeviceHandle, &info)
                .ThrowOnError("vkImportSemaphoreFdKHR");
            return new ImportedSemaphore(_context, _pool, semaphore);
        }
        else
        {
            var info = new VkImportSemaphoreWin32HandleInfoKHR()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMPORT_SEMAPHORE_WIN32_HANDLE_INFO_KHR,
                handle = handle.Handle,
                handleType = typeBit,
                semaphore = semaphore.Handle
            };
            _context.DeviceApi.ImportSemaphoreWin32HandleKHR(_context.DeviceHandle, &info)
                .ThrowOnError("vkImportSemaphoreWin32HandleKHR");
            return new ImportedSemaphore(_context, _pool, semaphore);
        }
    }

    class ImportedSemaphore : IVulkanExternalSemaphore
    {
        private readonly VulkanContext _context;
        private readonly VulkanCommandBufferPool _pool;
        private VulkanSemaphore? _sem;
        private VulkanSemaphore Sem => _sem ?? throw new ObjectDisposedException(nameof(ImportedSemaphore));

        public ImportedSemaphore(VulkanContext context, VulkanCommandBufferPool pool, VulkanSemaphore sem)
        {
            _context = context;
            _pool = pool;
            _sem = sem;
        }

        public void Dispose()
        {
            _sem?.Dispose();
            _sem = null;
        }

        public ulong Handle => Sem.Handle.Handle;

        void SubmitSemaphore(VulkanSemaphore? wait, VulkanSemaphore? signal)
        {
            var buf = _pool.CreateCommandBuffer();
            buf.BeginRecording();
            _context.DeviceApi.CmdPipelineBarrier(
                buf.Handle,
                VkPipelineStageFlags.VK_PIPELINE_STAGE_ALL_COMMANDS_BIT,
                VkPipelineStageFlags.VK_PIPELINE_STAGE_ALL_COMMANDS_BIT,
                0,
                0,
                IntPtr.Zero,
                0,
                IntPtr.Zero,
                0,
                null);

            buf.EndRecording();
            buf.Submit(wait != null ? new[] { wait }.AsSpan() : default,
                new[] { VkPipelineStageFlags.VK_PIPELINE_STAGE_ALL_GRAPHICS_BIT },
                signal != null ? new[] { signal }.AsSpan() : default);
        }
        public void SubmitWaitSemaphore()
        {
            SubmitSemaphore(_sem, null);
        }

        public void SubmitSignalSemaphore()
        {
            SubmitSemaphore(null, _sem);
        }
    }

    class ImportedImage : VulkanImageBase, IVulkanExternalImage
    {
        private readonly IVulkanPlatformGraphicsContext _context;
        private readonly IPlatformHandle _importHandle;
        private readonly PlatformGraphicsExternalImageProperties _properties;
        private readonly VkExternalMemoryHandleTypeFlagBits _typeBit;
        public VulkanImageInfo Info => ImageInfo;

        public ImportedImage(IVulkanPlatformGraphicsContext context, VulkanCommandBufferPool commandBufferPool,
            IPlatformHandle importHandle,
            PlatformGraphicsExternalImageProperties properties) : base(context, commandBufferPool,
            properties.Format switch
            {
                PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm => VkFormat.VK_FORMAT_B8G8R8A8_UNORM,
                PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm => VkFormat.VK_FORMAT_R8G8B8A8_UNORM,
                _ => throw new ArgumentException($"Format {properties.Format} is not supported")
            }, new PixelSize(properties.Width, properties.Height), 1)
        {
            _context = context;
            _importHandle = importHandle;
            _properties = properties;
            _typeBit = importHandle.HandleDescriptor switch
            {
                KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor =>
                    VkExternalMemoryHandleTypeFlagBits.VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_FD_BIT,
                KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaqueKmtHandle =>
                    VkExternalMemoryHandleTypeFlagBits.VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_WIN32_KMT_BIT,
                KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaqueNtHandle =>
                    VkExternalMemoryHandleTypeFlagBits.VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_WIN32_BIT,
                KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureNtHandle =>
                VkExternalMemoryHandleTypeFlagBits.VK_EXTERNAL_MEMORY_HANDLE_TYPE_D3D11_TEXTURE_BIT,
                _ => throw new NotSupportedException()
            };
            var externalAlloc = new VkExternalMemoryImageCreateInfo
            {
                handleTypes = _typeBit,
                sType = VkStructureType.VK_STRUCTURE_TYPE_EXTERNAL_MEMORY_IMAGE_CREATE_INFO
            };
            Initialize(&externalAlloc);
            TransitionLayout(VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, VkAccessFlags.VK_ACCESS_TRANSFER_READ_BIT);
            this.CurrentLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;
        }

        protected override VkDeviceMemory CreateMemory(VkImage image, ulong size, uint memoryTypeBits)
        {
            var handle = _importHandle;

            if (_typeBit == VkExternalMemoryHandleTypeFlagBits.VK_EXTERNAL_MEMORY_HANDLE_TYPE_D3D11_TEXTURE_BIT)
            {
                // Bind the Vulkan Memory Allocation to the exported D3D11 Image Resource Handle.
                VkMemoryWin32HandlePropertiesKHR vkImportedHandleProperties = new()
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_WIN32_HANDLE_PROPERTIES_KHR
                };

                _context.DeviceApi.GetMemoryWin32HandlePropertiesKHR(
                    _context.DeviceHandle, 
                    VkExternalMemoryHandleTypeFlagBits.VK_EXTERNAL_MEMORY_HANDLE_TYPE_D3D11_TEXTURE_BIT,
                    handle.Handle, 
                    &vkImportedHandleProperties).ThrowOnError("vkGetMemoryWin32HandlePropertiesKHR");

                memoryTypeBits = vkImportedHandleProperties.memoryTypeBits;
            }
            else
            {
                if (_properties.MemoryOffset != 0 || _properties.MemorySize != size)
                    throw new Exception("Invalid memory size");
            }

            var dedicated = new VkMemoryDedicatedAllocateInfo()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_DEDICATED_ALLOCATE_INFO,
                image = image,
            };

            var isPosixHandle = handle.HandleDescriptor ==
                                KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor;
            var win32Info = new VkImportMemoryWin32HandleInfoKHR
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMPORT_MEMORY_WIN32_HANDLE_INFO_KHR,
                handleType = _typeBit,
                handle = handle.Handle,
                pNext = &dedicated
            };
            var posixInfo = new VkImportMemoryFdInfoKHR()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_IMPORT_MEMORY_FD_INFO_KHR,
                handleType = _typeBit,
                fd = isPosixHandle ? handle.Handle.ToInt32() : 0,
                pNext = &dedicated
            };

            var memoryAllocateInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
                pNext = new IntPtr(isPosixHandle ? &posixInfo : &win32Info),
                allocationSize = size,
                memoryTypeIndex = (uint)VulkanMemoryHelper.FindSuitableMemoryTypeIndex(_context,
                    memoryTypeBits,
                    VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT)
            };

            _context.DeviceApi.AllocateMemory(_context.DeviceHandle, ref memoryAllocateInfo, IntPtr.Zero,
                out var imageMemory).ThrowOnError("vkAllocateMemory");
            return imageMemory;
        }
    }
}
