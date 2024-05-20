using System;
using System.Runtime.InteropServices;
using Avalonia;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
namespace GpuInterop.VulkanDemo;

public class D3DMemoryHelper
{
    public static ID3D11Device CreateDeviceByLuid(Span<byte> luid)
    {
        DXGI.CreateDXGIFactory1<IDXGIFactory1>(out var factory).CheckError();
        var longLuid = MemoryMarshal.Cast<byte, long>(luid)[0];
        IDXGIAdapter1? adapter;
        int c = 0;
        while (factory!.EnumAdapters1(c++, out adapter).Success)
        {
            try
            {
                if (adapter.Description1.Luid != longLuid)
                    continue;

                D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, DeviceCreationFlags.None,
                    [
                        FeatureLevel.Level_12_1, FeatureLevel.Level_12_0, FeatureLevel.Level_11_1,
                        FeatureLevel.Level_11_0, FeatureLevel.Level_10_0, FeatureLevel.Level_9_3,
                        FeatureLevel.Level_9_2, FeatureLevel.Level_9_1,
                    ],
                    out var device).CheckError();
                return device!;
            }
            finally
            {
                adapter?.Dispose();
            }
        }

        throw new ArgumentException("Device with the corresponding LUID not found");
    }

    public static ID3D11Texture2D CreateMemoryHandle(ID3D11Device device, PixelSize size, Silk.NET.Vulkan.Format format)
    {
        if (format != Silk.NET.Vulkan.Format.R8G8B8A8Unorm)
            throw new ArgumentException("Not supported format");
        return device.CreateTexture2D(
            new Texture2DDescription
            {
                Format = Format.R8G8B8A8_UNorm,
                Width = size.Width,
                Height = size.Height,
                ArraySize = 1,
                MipLevels = 1,
                SampleDescription = new SampleDescription { Count = 1, Quality = 0 },
                CPUAccessFlags = default,
                MiscFlags = ResourceOptionFlags.SharedKeyedMutex | ResourceOptionFlags.SharedNTHandle,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
            });
    }
}
