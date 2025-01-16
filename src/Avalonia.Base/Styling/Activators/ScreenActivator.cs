namespace Avalonia.Styling.Activators
{
    internal sealed class WidthActivator : ContainerQueryActivatorBase
    {
        private readonly (QueryComparisonOperator @operator, double value) _argument;

        public WidthActivator(Visual visual, (QueryComparisonOperator @operator, double value) argument, string? containerName = null) : base(visual, containerName)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => (CurrentContainer?.ContainerType == Layout.ContainerType.Width || CurrentContainer?.ContainerType == Layout.ContainerType.WidthAndHeight) 
            && WidthQuery.Evaluate(CurrentContainer.QueryProvider, _argument).IsMatch;
    }

    internal sealed class HeightActivator : ContainerQueryActivatorBase
    {
        private readonly (QueryComparisonOperator @operator, double value) _argument;

        public HeightActivator(Visual visual, (QueryComparisonOperator @operator, double value) argument, string? containerName = null) : base(visual, containerName)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => (CurrentContainer?.ContainerType == Layout.ContainerType.Height || CurrentContainer?.ContainerType == Layout.ContainerType.WidthAndHeight)
            && HeightQuery.Evaluate(CurrentContainer.QueryProvider, _argument).IsMatch;
    }
}
