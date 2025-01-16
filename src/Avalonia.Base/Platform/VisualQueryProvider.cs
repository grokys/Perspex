using System;
using System.Runtime.InteropServices;
using Avalonia.Styling;

namespace Avalonia.Platform
{
    internal class VisualQueryProvider
    {
        private readonly Visual _visual;

        public double Width { get; private set; } = double.PositiveInfinity;

        public double Height { get; private set; } = double.PositiveInfinity;

        public VisualQueryProvider(Visual visual)
        {
            _visual = visual;
        }

        public event EventHandler? WidthChanged;
        public event EventHandler? HeightChanged;

        public virtual void SetSize(double width, double height, Layout.ContainerType containerType)
        {
            var currentWidth = Width;
            var currentHeight = Height;

            Width = width;
            Height = height;

            if (currentWidth != Width && (containerType == Layout.ContainerType.Width || containerType == Layout.ContainerType.WidthAndHeight))
                WidthChanged?.Invoke(this, EventArgs.Empty);
            if (currentHeight != Height && (containerType == Layout.ContainerType.Height || containerType == Layout.ContainerType.WidthAndHeight))
                HeightChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
