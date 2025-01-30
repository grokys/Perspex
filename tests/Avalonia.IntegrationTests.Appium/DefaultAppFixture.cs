using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Enums;

namespace Avalonia.IntegrationTests.Appium
{
    public class DefaultAppFixture : IDisposable
    {
        private const string TestAppPath = @"..\..\..\..\..\samples\IntegrationTestApp\bin\Debug\net8.0\IntegrationTestApp.exe";
        private const string TestAppBundleId = "net.avaloniaui.avalonia.integrationtestapp";

        public DefaultAppFixture()
        {
            var options = new AppiumOptions();

            if (OperatingSystem.IsWindows())
            {
                ConfigureWin32Options(options);
                Session = new WindowsDriver(
                    new Uri("http://127.0.0.1:4723"),
                    options);

                // https://github.com/microsoft/WinAppDriver/issues/1025
                SetForegroundWindow(new IntPtr(int.Parse(
                    Session.WindowHandles[0].Substring(2),
                    NumberStyles.AllowHexSpecifier)));
            }
            else if (OperatingSystem.IsMacOS())
            {
                ConfigureMacOptions(options);
                Session = new MacDriver(
                    new Uri("http://127.0.0.1:4723/wd/hub"),
                    options);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        protected virtual void ConfigureWin32Options(AppiumOptions options, string? app = null)
        {
            options.AddAdditionalCapability(MobileCapabilityType.App, app ?? Path.GetFullPath(TestAppPath));
            options.AddAdditionalCapability(MobileCapabilityType.PlatformName, MobilePlatform.Windows);
            options.AddAdditionalCapability(MobileCapabilityType.DeviceName, "WindowsPC");
        }

        protected virtual void ConfigureMacOptions(AppiumOptions options, string? app = null)
        {
            options.AddAdditionalCapability("appium:bundleId", app ?? TestAppBundleId);
            options.AddAdditionalCapability(MobileCapabilityType.PlatformName, MobilePlatform.MacOS);
            options.AddAdditionalCapability(MobileCapabilityType.AutomationName, "mac2");
            options.AddAdditionalCapability("appium:showServerLogs", true);
            options.AddAdditionalCapability("appium:newCommandTimeout", 60 * 60 * 4);
        }

        public AppiumDriver Session { get; }

        public void Dispose()
        {
            try
            {
                Session.Close();
            }
            catch
            {
                // Closing the session currently seems to crash the mac2 driver.
            }
        }

        public AppiumDriver CreateNestedSession(string appName)
        {
            var options = new AppiumOptions();
            if (OperatingSystem.IsWindows())
            {
                ConfigureWin32Options(options, appName);
            
                return new WindowsDriver(new Uri("http://127.0.0.1:4723"), options);
            }
            else if (OperatingSystem.IsMacOS())
            {
                ConfigureMacOptions(options, appName);
                return new MacDriver(new Uri("http://127.0.0.1:4723/wd/hub"), options);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
