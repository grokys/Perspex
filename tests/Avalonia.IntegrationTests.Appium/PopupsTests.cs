using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium;

[Collection("Default")]
public abstract class PopupsTests : TestBase
{
    private readonly bool _isOverlayPopups;

    protected PopupsTests(bool isOverlayPopups, DefaultAppFixture fixture) : base(fixture, "Popups")
    {
        _isOverlayPopups = isOverlayPopups;
    }

    [Fact]
    public void LightDismiss_Popup_Should_Open_And_Close()
    {
        // Open popup
        var button = Session.FindElementByAccessibilityId("ShowLightDismissPopup");
        button.Click();
        Thread.Sleep(500);

        // Assert - Popup is visible
        Assert.NotNull(Session.FindElementByAccessibilityId("LightDismissPopupContent"));
        
        // Act - Click outside to dismiss
        new Actions(Session)
            .MoveByOffset(0, 500) // Move far away from popup
            .Click()
            .Perform();

        Thread.Sleep(500);

        // Assert - Popup is closed
        Assert.Throws<WebDriverException>(() =>
            Session.FindElementByAccessibilityId("LightDismissPopupContent"));
    }

    [Fact]
    public void StaysOpen_Popup_Should_Stay_Open()
    {
        // Open popup
        var button = Session.FindElementByAccessibilityId("ShowStaysOpenPopup");
        button.Click();
        Thread.Sleep(500);

        try
        {
            // Assert - Popup is visible
            Assert.NotNull(Session.FindElementByAccessibilityId("StaysOpenPopupCloseButton"));

            // Act - Click outside
            new Actions(Session)
                .MoveByOffset(0, 500)
                .Click()
                .Perform();

            Thread.Sleep(500);

            // Assert - Popup is still visible
            Assert.NotNull(Session.FindElementByAccessibilityId("StaysOpenPopupCloseButton"));

        }
        finally
        {
            // Act - Close popup with button
            Session.FindElementByAccessibilityId("StaysOpenPopupCloseButton").Click();

            Thread.Sleep(400);

            // Assert - Popup is closed
            Assert.Throws<WebDriverException>(() =>
                Session.FindElementByAccessibilityId("StaysOpenPopupCloseButton"));
        }
    }

    [Fact]
    public void StaysOpen_Popup_TextBox_Should_Be_Editable()
    {
        // Open popup
        var button = Session.FindElementByAccessibilityId("ShowStaysOpenPopup");
        button.Click();
        Thread.Sleep(500);

        try
        {
            // Find and edit the TextBox
            var textBox = Session.FindElementByAccessibilityId("StaysOpenTextBox");
            textBox.Clear();
            textBox.SendKeys("New text value");
            Thread.Sleep(500);

            // Verify text was changed
            Assert.Equal("New text value", textBox.Text);
        }
        finally
        {
            // Cleanup - close popup
            Session.FindElementByAccessibilityId("StaysOpenPopupCloseButton").Click();
        }
    }

    [Fact]
    public void TopMost_Popup_Should_Stay_Above_Other_Windows()
    {
        // It's not possible to test overlay topmost with other windows.
        if (_isOverlayPopups)
        {
            return;
        }

        var staysOpenPopup = Session.FindElementByAccessibilityId("ShowTopMostPopup");
        var mainWindowHandle = Session.CurrentWindowHandle;

        // Show topmost popup.
        staysOpenPopup.Click();
        Assert.NotNull(Session.FindElementByAccessibilityId("TopMostPopupCloseButton"));

        var hasClosedPopup = false;

        try
        {
            // Open a child window.
            using var _ = Session.FindElementByAccessibilityId("OpenNewWindowButton").OpenWindowWithClick();
            Thread.Sleep(500);

            // Force window to front by maximizing child window.
            Session.FindElementByAccessibilityId("CurrentWindowState").SendClick();
            Session.FindElementByAccessibilityId("WindowStateMaximized").SendClick();

            // Switch back to the mainwindow context and verify tooltip is still accessible.
            Session.SwitchTo().Window(mainWindowHandle);
            Assert.NotNull(Session.FindElementByAccessibilityId("TopMostPopupCloseButton"));

            // Verify we can still interact with the popup by closing it via button.
            Session.FindElementByAccessibilityId("TopMostPopupCloseButton").Click();

            // Verify popup closed
            Assert.Throws<WebDriverException>(() =>
                Session.FindElementByAccessibilityId("TopMostPopupCloseButton"));
            hasClosedPopup = true;
        }
        finally
        {
            if (!hasClosedPopup)
            {
                Session.FindElementByAccessibilityId("TopMostPopupCloseButton").Click();
            }
        }
    }

    [Fact]
    public void Non_TopMost_Popup_Does_Not_Stay_Above_Other_Windows()
    {
        var topmostButton = Session.FindElementByAccessibilityId("ShowStaysOpenPopup");
        var mainWindowHandle = Session.CurrentWindowHandle;

        // Show topmost popup.
        topmostButton.Click();
        Assert.NotNull(Session.FindElementByAccessibilityId("StaysOpenPopupCloseButton"));

        try
        {
            // Open a child window.
            var newWindowButton = Session.FindElementByAccessibilityId("OpenNewWindowButton");
            using var _ = newWindowButton.OpenWindowWithClick();

            // Force window to front by maximizing child window.
            Session.FindElementByAccessibilityId("CurrentWindowState").SendClick();
            Session.FindElementByAccessibilityId("WindowStateMaximized").SendClick();

            // Switch back to the mainwindow context and verify tooltip is still accessible.
            Session.SwitchTo().Window(mainWindowHandle);
            Assert.NotNull(Session.FindElementByAccessibilityId("StaysOpenPopupCloseButton"));

            // Verify we cannot interact with the popup by attempting closing it via button.
            Session.FindElementByAccessibilityId("StaysOpenPopupCloseButton").Click();

            // Verify popup is still accessible.
            Assert.NotNull(Session.FindElementByAccessibilityId("StaysOpenPopupCloseButton"));
        }
        finally
        {
            // At this point secondary window should be already closed. And safe to close the popup.
            Session.FindElementByAccessibilityId("StaysOpenPopupCloseButton").Click();
        }
    }

    [Collection("Default")]
    public class Default : PopupsTests
    {
        public Default(DefaultAppFixture fixture) : base(false, fixture) { }
    }

    [Collection("OverlayPopups")]
    public class OverlayPopups : PopupsTests
    {
        public OverlayPopups(OverlayPopupsAppFixture fixture) : base(true, fixture) { }
    }
}
