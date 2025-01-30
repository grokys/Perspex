using Avalonia.Controls.Primitives;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class AdornerLayerTests : ScopedTestBase
{
    [Fact]
    public void Adorners_Include_Adorned_Elements_In_Transform_Visual()
    {
        var button = new Button()
        {
            Margin = new Thickness(100, 100)
        };
        var root = new TestRoot()
        {
            Child = new VisualLayerManager()
            {
                Child = button
            }
        };
        var adorner = new Border();

        var adornerLayer = AdornerLayer.GetAdornerLayer(button);
        adornerLayer.Children.Add(adorner);
        AdornerLayer.SetAdornedElement(adorner, button);

        root.LayoutManager.ExecuteInitialLayoutPass();

        var translatedPoint = root.TranslatePoint(new Point(100, 100), adorner);
        Assert.Equal(new Point(0, 0), translatedPoint);
    }
}
