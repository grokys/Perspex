using System;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Styling.Activators
{
    internal abstract class ContainerQueryActivatorBase : StyleActivatorBase, IStyleActivatorSink
    {
        private readonly Visual _visual;
        private readonly string? _containerName;
        private IContainer? _currentScreenSizeProvider;

        public ContainerQueryActivatorBase(
            Visual visual, string? containerName = null)
        {
            _visual = visual;
            _containerName = containerName;

            _visual.AttachedToVisualTree += Visual_AttachedToVisualTree;
        }

        private void Visual_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            InitializeScreenSizeProvider();
        }

        protected IContainer? CurrentContainer => _currentScreenSizeProvider;

        void IStyleActivatorSink.OnNext(bool value) => ReevaluateIsActive();

        protected override void Initialize()
        {
            InitializeScreenSizeProvider();
        }

        protected override void Deinitialize()
        {
            _visual.AttachedToVisualTree -= Visual_AttachedToVisualTree;

            if (_currentScreenSizeProvider is { })
            {
                _currentScreenSizeProvider.QueryProvider.WidthChanged -= WidthChanged;
                _currentScreenSizeProvider.QueryProvider.HeightChanged -= HeightChanged;
                _currentScreenSizeProvider = null;
            }
        }

        private void InitializeScreenSizeProvider()
        {
            if (GetContainer(_visual, _containerName) is { } container)
            {
                _currentScreenSizeProvider = container;

                _currentScreenSizeProvider.QueryProvider.WidthChanged += WidthChanged;
                _currentScreenSizeProvider.QueryProvider.HeightChanged += HeightChanged;
            }

            ReevaluateIsActive();
        }

        internal static IContainer? GetContainer(Visual visual, string? containerName)
        {
            return visual.GetVisualAncestors().Where(x => x != visual && x is IContainer container && (containerName == null || container.ContainerName == containerName)).FirstOrDefault() as IContainer;
        }

        private void HeightChanged(object? sender, EventArgs e)
        {
            ReevaluateIsActive();
        }

        private void WidthChanged(object? sender, EventArgs e)
        {
            ReevaluateIsActive();
        }

        private void OrientationChanged(object? sender, EventArgs e)
        {
            ReevaluateIsActive();
        }
    }
}
