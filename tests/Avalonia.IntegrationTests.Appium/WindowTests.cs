using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Xunit.Sdk;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class WindowTests : TestBase
    {
        public WindowTests(DefaultAppFixture fixture)
            : base(fixture, "Window")
        {
        }

        [Theory]
        [MemberData(nameof(StartupLocationData))]
        public void StartupLocation(Size? size, ShowWindowMode mode, WindowStartupLocation location, bool canResize)
        {
            using var window = OpenWindow(size, mode, location, canResize: canResize);
            var info = GetWindowInfo();

            if (size.HasValue)
                Assert.Equal(size.Value, info.ClientSize);

            Assert.True(info.FrameSize.Width >= info.ClientSize.Width, "Expected frame width >= client width.");
            Assert.True(info.FrameSize.Height > info.ClientSize.Height, "Expected frame height > client height.");

            var frameRect = new PixelRect(info.Position, PixelSize.FromSize(info.FrameSize, info.Scaling));

            switch (location)
            {
                case WindowStartupLocation.CenterScreen:
                {
                    var expected = info.ScreenRect.CenterRect(frameRect);
                    AssertCloseEnough(expected.Position, frameRect.Position);
                    break;
                }
                case WindowStartupLocation.CenterOwner:
                {
                    Assert.NotNull(info.OwnerRect);
                    var expected = info.OwnerRect!.Value.CenterRect(frameRect);
                    AssertCloseEnough(expected.Position, frameRect.Position);
                    break;
                }
            }
        }

        [Theory]
        [MemberData(nameof(WindowStateData))]
        public void WindowState(Size? size, ShowWindowMode mode, WindowState state, bool canResize)
        {
            using var window = OpenWindow(size, mode, state: state, canResize: canResize);

            try
            {
                var info = GetWindowInfo();

                Assert.Equal(state, info.WindowState);

                switch (state)
                {
                    case Controls.WindowState.Normal:
                        Assert.True(info.FrameSize.Width * info.Scaling < info.ScreenRect.Size.Width);
                        Assert.True(info.FrameSize.Height * info.Scaling < info.ScreenRect.Size.Height);
                        break;
                    case Controls.WindowState.Maximized:
                    case Controls.WindowState.FullScreen:
                        Assert.True(info.FrameSize.Width * info.Scaling >= info.ScreenRect.Size.Width);
                        Assert.True(info.FrameSize.Height * info.Scaling >= info.ScreenRect.Size.Height);
                        break;
                }
            }
            finally
            {
                if (state == Controls.WindowState.FullScreen)
                {
                    try
                    {
                        Session.FindElementByAccessibilityId("CurrentWindowState").SendClick();
                        Session.FindElementByAccessibilityId("WindowStateNormal").SendClick();

                        // Wait for animations to run.
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                            Thread.Sleep(1000);
                    }
                    catch
                    {
                        /* Ignore errors in cleanup */
                    }
                }
            }
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void OnWindows_Docked_Windows_Retain_Size_Position_When_Restored()
        {
            using (OpenWindow(new Size(400, 400), ShowWindowMode.NonOwned, WindowStartupLocation.Manual))
            {
                var windowState = Session.FindElementByAccessibilityId("CurrentWindowState");

                Assert.Equal("Normal", windowState.GetComboBoxValue());
                
                
                var window = Session.FindElements(By.XPath("//Window")).First();
                
                new Actions(Session)
                    .KeyDown(Keys.Meta)
                    .SendKeys(Keys.Left)
                    .KeyUp(Keys.Meta)
                    .Perform();
                
                var original = GetWindowInfo();
                
                windowState.Click();
                Session.FindElementByName("Minimized").SendClick();
                
                new Actions(Session)
                    .KeyDown(Keys.Alt)
                    .SendKeys(Keys.Tab)
                    .KeyUp(Keys.Alt)
                    .Perform();
                
                var current = GetWindowInfo();
                
                Assert.Equal(original.Position, current.Position);
                Assert.Equal(original.FrameSize, current.FrameSize);

            }
        }
        
        [Fact]
        public void Showing_Window_With_Size_Larger_Than_Screen_Measures_Content_With_Working_Area()
        {
            using (OpenWindow(new Size(4000, 2200), ShowWindowMode.NonOwned, WindowStartupLocation.Manual))
            {
                var screenRectTextBox = Session.FindElementByAccessibilityId("CurrentClientSize");
                var measuredWithTextBlock = Session.FindElementByAccessibilityId("CurrentMeasuredWithText");
                
                var measuredWithString = measuredWithTextBlock.Text;
                var workingAreaString = screenRectTextBox.Text;

                var workingArea = Size.Parse(workingAreaString);
                var measuredWith = Size.Parse(measuredWithString);

                Assert.Equal(workingArea, measuredWith);
            }
        }

        [Theory]
        [InlineData(ShowWindowMode.NonOwned)]
        [InlineData(ShowWindowMode.Owned)]
        [InlineData(ShowWindowMode.Modal)]
        public void ShowMode(ShowWindowMode mode)
        {
            using var window = OpenWindow(null, mode, WindowStartupLocation.Manual);
            var windowState = Session.FindElementByAccessibilityId("CurrentWindowState");
            var original = GetWindowInfo();

            Assert.Equal("Normal", windowState.GetComboBoxValue());

            windowState.Click();
            Session.FindElementByAccessibilityId("WindowStateMaximized").SendClick();
            Assert.Equal("Maximized", windowState.GetComboBoxValue());

            windowState.Click();
            Session.FindElementByAccessibilityId("WindowStateNormal").SendClick();

            var current = GetWindowInfo();
            Assert.Equal(original.Position, current.Position);
            Assert.Equal(original.FrameSize, current.FrameSize);

            // On macOS, only non-owned windows can go fullscreen.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || mode == ShowWindowMode.NonOwned)
            {
                windowState.Click();
                Session.FindElementByAccessibilityId("WindowStateFullScreen").SendClick();
                Assert.Equal("FullScreen", windowState.GetComboBoxValue());

                current = GetWindowInfo();
                var clientSize = PixelSize.FromSize(current.ClientSize, current.Scaling);
                Assert.True(clientSize.Width >= current.ScreenRect.Width);
                Assert.True(clientSize.Height >= current.ScreenRect.Height);

                windowState.SendClick();
                
                Session.FindElementByAccessibilityId("WindowStateNormal").SendClick();

                current = GetWindowInfo();
                Assert.Equal(original.Position, current.Position);
                Assert.Equal(original.FrameSize, current.FrameSize);
            }
        }

        [Fact]
        public void Extended_Client_Window_Shows_With_Requested_Size()
        {
            var clientSize = new Size(400, 400);
            using var window = OpenWindow(clientSize, ShowWindowMode.NonOwned, WindowStartupLocation.CenterScreen, extendClientArea: true);
            var windowState = Session.FindElementByAccessibilityId("CurrentWindowState");
            var current = GetWindowInfo();

            Assert.Equal(current.ClientSize, clientSize);
        }

        [Fact]
        public void TransparentWindow()
        {
            var showTransparentWindow = Session.FindElementByAccessibilityId("ShowTransparentWindow");
            showTransparentWindow.Click();
            Thread.Sleep(1000);

            var window = Session.FindElementByAccessibilityId("TransparentWindow");
            var screenshot = window.GetScreenshot();

            window.Click();

            var img = SixLabors.ImageSharp.Image.Load<Rgba32>(screenshot.AsByteArray);
            var topLeftColor = img[10, 10];
            var centerColor = img[img.Width / 2, img.Height / 2];

            Assert.Equal(new Rgba32(0, 128, 0), topLeftColor);
            Assert.Equal(new Rgba32(255, 0, 0), centerColor);
        }

        [Fact]
        public void TransparentPopup()
        {
            var showTransparentWindow = Session.FindElementByAccessibilityId("ShowTransparentPopup");
            showTransparentWindow.Click();
            Thread.Sleep(1000);

            var window = Session.FindElementByAccessibilityId("TransparentPopupBackground");
            var container = window.FindElementByAccessibilityId("PopupContainer");
            var screenshot = container.GetScreenshot();

            window.Click();

            var img = SixLabors.ImageSharp.Image.Load<Rgba32>(screenshot.AsByteArray);
            var topLeftColor = img[10, 10];
            var centerColor = img[img.Width / 2, img.Height / 2];

            Assert.Equal(new Rgba32(0, 128, 0), topLeftColor);
            Assert.Equal(new Rgba32(255, 0, 0), centerColor);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Owned_Window_Should_Appear_Above_Topmost_Owner()
        {
            var showTopmostWindow = Session.FindElementByAccessibilityId("ShowTopmostWindow");
            using var window = showTopmostWindow.OpenWindowWithClick();
            Thread.Sleep(1000);
            var ownerWindow = Session.GetWindowById("OwnerWindow");
            var ownedWindow = Session.GetWindowById("OwnedWindow");

            Assert.NotNull(ownerWindow);
            Assert.NotNull(ownedWindow);

            var ownerPosition = GetPosition(ownerWindow);
            var ownedPosition = GetPosition(ownedWindow);

            // Owned Window moves 
            var moveButton = ownedWindow.FindElementByAccessibilityId("MoveButton");
            moveButton.Click();
            Thread.Sleep(1000);

            Assert.Equal(GetPosition(ownerWindow), ownerPosition);
            Assert.NotEqual(GetPosition(ownedWindow), ownedPosition);

            PixelPoint GetPosition(AppiumWebElement window)
            {
                return PixelPoint.Parse(window.FindElementByAccessibilityId("CurrentPosition").Text);
            }
        }

        [Theory]
        [InlineData(ShowWindowMode.NonOwned, true)]
        [InlineData(ShowWindowMode.Owned, true)]
        [InlineData(ShowWindowMode.Modal, true)]
        [InlineData(ShowWindowMode.NonOwned, false)]
        [InlineData(ShowWindowMode.Owned, false)]
        [InlineData(ShowWindowMode.Modal, false)]
        public void Window_Has_Disabled_Maximize_Button_When_CanResize_Is_False(ShowWindowMode mode, bool extendClientArea)
        {
            using (OpenWindow(null, mode, WindowStartupLocation.Manual, canResize: false, extendClientArea: extendClientArea))
            {
                var secondaryWindow = Session.GetWindowById("SecondaryWindow");
                AppiumWebElement? maximizeButton;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    maximizeButton = extendClientArea ?
                        secondaryWindow.FindElementByXPath("//Button[@Name='Maximize']") :
                        secondaryWindow.FindElementByXPath("//TitleBar/Button[2]");
                }
                else
                {
                    maximizeButton = mode == ShowWindowMode.NonOwned ?
                        secondaryWindow.FindElementByAccessibilityId("_XCUI:FullScreenWindow") :
                        secondaryWindow.FindElementByAccessibilityId("_XCUI:ZoomWindow");
                }

                Assert.False(maximizeButton.Enabled);
            }
        }

        [Fact]
        public void Changing_SystemDecorations_Should_Not_Change_Frame_Size_And_Position()
        {
            using (OpenWindow(null, ShowWindowMode.NonOwned, WindowStartupLocation.Manual))
            {
                var info = GetWindowInfo();

                Session.FindElementByAccessibilityId("CurrentSystemDecorations").Click();
                Session.FindElementByAccessibilityId("SystemDecorationsNone").SendClick();
                var updatedInfo = GetWindowInfo();
                Assert.Equal(info.FrameSize, updatedInfo.FrameSize);
                Assert.Equal(info.Position, updatedInfo.Position);

                Session.FindElementByAccessibilityId("CurrentSystemDecorations").Click();
                Session.FindElementByAccessibilityId("SystemDecorationsFull").SendClick();
                updatedInfo = GetWindowInfo();
                Assert.Equal(info.FrameSize, updatedInfo.FrameSize);
                Assert.Equal(info.Position, updatedInfo.Position);
            }
        }

        [Fact]
        public void Changing_WindowState_Should_Not_Change_Frame_Size_And_Position()
        {
            using (OpenWindow())
            {
                var info = GetWindowInfo();

                Session.FindElementByAccessibilityId("CurrentWindowState").SendClick();
                Session.FindElementByAccessibilityId("WindowStateMaximized").SendClick();
                Session.FindElementByAccessibilityId("CurrentWindowState").SendClick();
                Session.FindElementByAccessibilityId("WindowStateNormal").SendClick();
                var updatedInfo = GetWindowInfo();
                Assert.Equal(info.FrameSize, updatedInfo.FrameSize);
                Assert.Equal(info.Position, updatedInfo.Position);
            }
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Changing_Size_Should_Not_Change_Position()
        {
            using (OpenWindow())
            {
                var info = GetWindowInfo();

                Session.FindElementByAccessibilityId("AddToWidth").SendClick();
                Session.FindElementByAccessibilityId("AddToHeight").SendClick();

                var updatedInfo = GetWindowInfo();
                Assert.Equal(info.Position, updatedInfo.Position);
                Assert.Equal(info.FrameSize
                    .WithWidth(info.FrameSize.Width + 10)
                    .WithHeight(info.FrameSize.Height + 10), updatedInfo.FrameSize);
            }
        }

        public static TheoryData<Size?, ShowWindowMode, WindowStartupLocation, bool> StartupLocationData()
        {
            var sizes = new Size?[] { null, new Size(400, 300) };
            var data = new TheoryData<Size?, ShowWindowMode, WindowStartupLocation, bool>();

            foreach (var size in sizes)
            {
                foreach (var mode in Enum.GetValues<ShowWindowMode>())
                {
                    foreach (var location in Enum.GetValues<WindowStartupLocation>())
                    {
                        if (!(location == WindowStartupLocation.CenterOwner && mode == ShowWindowMode.NonOwned))
                        {
                            data.Add(size, mode, location, true);
                            data.Add(size, mode, location, false);
                        }
                    }
                }
            }

            return data;
        }

        public static TheoryData<Size?, ShowWindowMode, WindowState, bool> WindowStateData()
        {
            var sizes = new Size?[] { null, new Size(400, 300) };
            var data = new TheoryData<Size?, ShowWindowMode, WindowState, bool>();

            foreach (var size in sizes)
            {
                foreach (var mode in Enum.GetValues<ShowWindowMode>())
                {
                    foreach (var state in Enum.GetValues<WindowState>())
                    {
                        // Not sure how to handle testing minimized windows currently.
                        if (state == Controls.WindowState.Minimized)
                            continue;

                        // Child/Modal windows cannot be fullscreen on macOS.
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                            state == Controls.WindowState.FullScreen &&
                            mode != ShowWindowMode.NonOwned)
                            continue;

                        data.Add(size, mode, state, true);
                        data.Add(size, mode, state, false);
                    }
                }
            }

            return data;
        }

        private static void AssertCloseEnough(PixelPoint expected, PixelPoint actual)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On win32, accurate frame information cannot be obtained until a window is shown but
                // WindowStartupLocation needs to be calculated before the window is shown, meaning that
                // the position of a centered window can be off by a bit. From initial testing, looks
                // like this shouldn't be more than 10 pixels.
                if (Math.Abs(expected.X - actual.X) > 10)
                    throw EqualException.ForMismatchedValues(expected, actual);
                if (Math.Abs(expected.Y - actual.Y) > 10)
                    throw EqualException.ForMismatchedValues(expected, actual);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (Math.Abs(expected.X - actual.X) > 15)
                    throw EqualException.ForMismatchedValues(expected, actual);
                if (Math.Abs(expected.Y - actual.Y) > 15)
                    throw EqualException.ForMismatchedValues(expected, actual);
            }
            else
            {
                Assert.Equal(expected, actual);
            }
        }

        private IDisposable OpenWindow(
            Size? size = null,
            ShowWindowMode mode = ShowWindowMode.NonOwned,
            WindowStartupLocation location = WindowStartupLocation.Manual,
            WindowState state = Controls.WindowState.Normal,
            bool canResize = true,
            bool extendClientArea = false)
        {
            var sizeTextBox = Session.FindElementByAccessibilityId("ShowWindowSize");
            var modeComboBox = Session.FindElementByAccessibilityId("ShowWindowMode");
            var locationComboBox = Session.FindElementByAccessibilityId("ShowWindowLocation");
            var stateComboBox = Session.FindElementByAccessibilityId("ShowWindowState");
            var canResizeCheckBox = Session.FindElementByAccessibilityId("ShowWindowCanResize");
            var showButton = Session.FindElementByAccessibilityId("ShowWindow");
            var extendClientAreaCheckBox = Session.FindElementByAccessibilityId("ShowWindowExtendClientAreaToDecorationsHint");

            if (size.HasValue)
                sizeTextBox.SendKeys($"{size.Value.Width}, {size.Value.Height}");

            if (modeComboBox.GetComboBoxValue() != mode.ToString())
            {
                modeComboBox.Click();
                Session.FindElementByName(mode.ToString()).SendClick();
            }

            if (locationComboBox.GetComboBoxValue() != location.ToString())
            {
                locationComboBox.Click();
                Session.FindElementByName(location.ToString()).SendClick();
            }

            if (stateComboBox.GetComboBoxValue() != state.ToString())
            {
                stateComboBox.Click();
                Session.FindElementByAccessibilityId($"ShowWindowState{state}").SendClick();
            }

            if (canResizeCheckBox.GetIsChecked() != canResize)
                canResizeCheckBox.Click();

            if (extendClientAreaCheckBox.GetIsChecked() != extendClientArea)
                extendClientAreaCheckBox.Click();

            return showButton.OpenWindowWithClick();
        }

        private WindowInfo GetWindowInfo()
        {
            PixelRect? ReadOwnerRect()
            {
                var text = Session.FindElementByAccessibilityId("CurrentOwnerRect").Text;
                return !string.IsNullOrWhiteSpace(text) ? PixelRect.Parse(text) : null;
            }

            var retry = 0;

            for (;;)
            {
                try
                {
                    return new(
                        Size.Parse(Session.FindElementByAccessibilityId("CurrentClientSize").Text),
                        Size.Parse(Session.FindElementByAccessibilityId("CurrentFrameSize").Text),
                        PixelPoint.Parse(Session.FindElementByAccessibilityId("CurrentPosition").Text),
                        ReadOwnerRect(),
                        PixelRect.Parse(Session.FindElementByAccessibilityId("CurrentScreenRect").Text),
                        double.Parse(Session.FindElementByAccessibilityId("CurrentScaling").Text),
                        Enum.Parse<WindowState>(Session.FindElementByAccessibilityId("CurrentWindowState").Text));
                }
                catch (OpenQA.Selenium.NoSuchElementException) when (retry++ < 3)
                {
                    // MacOS sometimes seems to need a bit of time to get itself back in order after switching out
                    // of fullscreen.
                    Thread.Sleep(1000);
                }
            }
        }

        public enum ShowWindowMode
        {
            NonOwned,
            Owned,
            Modal
        }

        private record WindowInfo(
            Size ClientSize,
            Size FrameSize,
            PixelPoint Position,
            PixelRect? OwnerRect,
            PixelRect ScreenRect,
            double Scaling,
            WindowState WindowState);
    }
}
