global using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Logging;
using Avalonia.Vulkan;

namespace GpuInterop
{
    public class Program
    {
        static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new Win32PlatformOptions
                {
                    RenderingMode = new []
                    {
                        Win32RenderingMode.AngleEgl
                    }
                })
                .With(new X11PlatformOptions(){RenderingMode =new[] { X11RenderingMode.Vulkan } })
                .With(new VulkanOptions()
                {
                    VulkanInstanceCreationOptions = new VulkanInstanceCreationOptions()
                    {
                        UseDebug = true
                    }
                })
                .LogToTrace(LogEventLevel.Debug, "Vulkan");
    }
}
