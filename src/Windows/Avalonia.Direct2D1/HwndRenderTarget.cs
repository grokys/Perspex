using Avalonia.Platform;
using Avalonia.Win32;
using Avalonia.Win32.Interop;
using Vortice.DXGI;

namespace Avalonia.Direct2D1
{
    class HwndRenderTarget : SwapChainRenderTarget
    {
        private readonly IPlatformHandle _window;

        public HwndRenderTarget(IPlatformHandle window)
        {
            _window = window;
        }

        protected override IDXGISwapChain1 CreateSwapChain(IDXGIFactory2 dxgiFactory, SwapChainDescription1 swapChainDesc)
        {
            return dxgiFactory.CreateSwapChainForHwnd(Direct2D1Platform.DxgiDevice, _window.Handle, swapChainDesc);
        }

        protected override Vortice.Mathematics.Size GetWindowDpi()
        {
            if (UnmanagedMethods.ShCoreAvailable && Win32Platform.WindowsVersion > PlatformConstants.Windows8)
            {
                uint dpix, dpiy;

                var monitor = UnmanagedMethods.MonitorFromWindow(
                    _window.Handle,
                    UnmanagedMethods.MONITOR.MONITOR_DEFAULTTONEAREST);

                if (UnmanagedMethods.GetDpiForMonitor(
                        monitor,
                        UnmanagedMethods.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                        out dpix,
                        out dpiy) == 0)
                {
                    return new Vortice.Mathematics.Size(dpix, dpiy);
                }
            }

            return new Vortice.Mathematics.Size(96, 96);
        }

        protected override Vortice.Mathematics.SizeI GetWindowSize()
        {
            UnmanagedMethods.RECT rc;
            UnmanagedMethods.GetClientRect(_window.Handle, out rc);
            return new Vortice.Mathematics.SizeI(rc.right - rc.left, rc.bottom - rc.top);
        }
    }
}
