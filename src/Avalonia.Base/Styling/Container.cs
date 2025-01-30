using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Layout;
using Avalonia.Platform;

namespace Avalonia.Styling
{
    public class Container
    {
        /// <summary>
        /// Defines the Name attached property.
        /// </summary>
        public static readonly AttachedProperty<string?> NameProperty =
            AvaloniaProperty.RegisterAttached<Container, Layoutable, string?>("Name");

        /// <summary>
        /// Defines the Sizing attached property.
        /// </summary>
        public static readonly AttachedProperty<ContainerSizing> SizingProperty =
            AvaloniaProperty.RegisterAttached<Container, Layoutable, ContainerSizing>("Sizing", coerce:UpdateQueryProvider);

        private static ContainerSizing UpdateQueryProvider(AvaloniaObject obj, ContainerSizing sizing)
        {
            if (obj is Layoutable layoutable)
            {
                if (sizing != ContainerSizing.Normal)
                {
                    if (GetQueryProvider(layoutable) == null)
                        layoutable.SetValue(QueryProviderProperty, new VisualQueryProvider(layoutable));
                }
                else
                {
                    layoutable.SetValue(QueryProviderProperty, null);
                }
            }

            return sizing;
        }

        internal static readonly AttachedProperty<VisualQueryProvider?> QueryProviderProperty =
            AvaloniaProperty.RegisterAttached<Container, Layoutable, VisualQueryProvider?>("QueryProvider");

        /// <summary>
        /// Gets the value of the Container.Name attached property.
        /// </summary>
        /// <param name="layoutable">The layoutable to read the value from.</param>
        /// <returns>The container name of the layoutable</returns>
        public static string? GetName(Layoutable layoutable)
        {
            return layoutable.GetValue(NameProperty);
        }

        /// <summary>
        /// Sets the value of the Container.Name attached property.
        /// </summary>
        /// <param name="layoutable">The layoutable to set the value on.</param>
        /// <param name="name">The container name.</param>
        public static void SetName(Layoutable layoutable, string? name)
        {
            layoutable.SetValue(NameProperty, name);
        }

        /// <summary>
        /// Gets the value of the Container.Sizing attached property.
        /// </summary>
        /// <param name="layoutable">The layoutable to read the value from.</param>
        /// <returns>The container sizing mode of the layoutable</returns>
        public static ContainerSizing GetSizing(Layoutable layoutable)
        {
            return layoutable.GetValue(SizingProperty);
        }

        /// <summary>
        /// Sets the value of the Container.Name attached property.
        /// </summary>
        /// <param name="layoutable">The layoutable to set the value on.</param>
        /// <param name="sizing">The container sizing mode.</param>
        public static void SetSizing(Layoutable layoutable, ContainerSizing sizing)
        {
            layoutable.SetValue(SizingProperty, sizing);
        }

        internal static VisualQueryProvider? GetQueryProvider(Layoutable layoutable)
        {
            return layoutable.GetValue(QueryProviderProperty);
        }
    }
}
