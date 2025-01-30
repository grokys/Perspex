using Avalonia.Layout;

namespace Avalonia.Styling.Activators
{
    internal sealed class WidthActivator : ContainerQueryActivatorBase
    {
        private readonly (QueryComparisonOperator @operator, double value) _argument;

        public WidthActivator(Visual visual, (QueryComparisonOperator @operator, double value) argument, string? containerName = null) : base(visual, containerName)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => (CurrentContainer is Layoutable layoutable
            && Container.GetSizing(layoutable) is { } sizing
            && Container.GetQueryProvider(layoutable) is { } queryProvider
            && (sizing is Layout.ContainerSizing.Width or Layout.ContainerSizing.WidthAndHeight))
            && WidthQuery.Evaluate(queryProvider, _argument).IsMatch;
    }

    internal sealed class HeightActivator : ContainerQueryActivatorBase
    {
        private readonly (QueryComparisonOperator @operator, double value) _argument;

        public HeightActivator(Visual visual, (QueryComparisonOperator @operator, double value) argument, string? containerName = null) : base(visual, containerName)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => (CurrentContainer is Layoutable layoutable
            && Container.GetSizing(layoutable) is { } sizing
            && Container.GetQueryProvider(layoutable) is { } queryProvider
            && (sizing is Layout.ContainerSizing.Height or Layout.ContainerSizing.WidthAndHeight))
            && HeightQuery.Evaluate(queryProvider, _argument).IsMatch;
    }
}
