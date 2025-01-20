using Avalonia.Metadata;

namespace Avalonia.Controls;

[PrivateApi]
public class AdornerLayerBase
{
    /// <summary>
    /// Allows for getting and setting of the adorned element.
    /// </summary>
    public static readonly AttachedProperty<Visual?> AdornedElementProperty =
        AvaloniaProperty.RegisterAttached<AdornerLayerBase, Visual, Visual?>("AdornedElement");

    public static Visual? GetAdornedElement(Visual adorner)
    {
        return adorner.GetValue(AdornedElementProperty);
    }

    public static void SetAdornedElement(Visual adorner, Visual? adorned)
    {
        adorner.SetValue(AdornedElementProperty, adorned);
    }
}
