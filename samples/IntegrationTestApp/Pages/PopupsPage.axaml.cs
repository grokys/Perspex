using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace IntegrationTestApp.Pages;

public partial class PopupsPage : UserControl
{
    public PopupsPage()
    {
        InitializeComponent();
    }

    private void ButtonLightDismiss_OnClick(object sender, RoutedEventArgs e)
    {
        LightDismissPopup.Open();
    }

    private void ButtonPopupStaysOpen_OnClick(object sender, RoutedEventArgs e)
    {
        StaysOpenPopup.Open();
    }
    private void StaysOpenPopupCloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        StaysOpenPopup.Close();
    }

    private void ButtonTopMostPopupStaysOpen(object sender, RoutedEventArgs e)
    {
        TopMostPopup.Open();
    }
    private void TopMostPopupCloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TopMostPopup.Close();
    }

    private void OpenRegularNewWindow_Click(object? sender, RoutedEventArgs e)
    {
        var newWindow = new ShowWindowTest();
        newWindow.Show((Window)TopLevel.GetTopLevel(this)!);
    }
}
