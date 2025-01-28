using System;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling
{
    internal sealed class WidthQuery : ValueQuery<(QueryComparisonOperator @operator, double value)>
    {
        public WidthQuery(Query? previous, QueryComparisonOperator @operator, double value) : base(previous, (@operator, value))
        {
        }

        internal override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe, string? containerName = null)
        {
            if (control is not Visual visual)
            {
                return SelectorMatch.NeverThisType;
            }

            if (subscribe)
            {
                return new SelectorMatch(new WidthActivator(visual, Argument, containerName));
            }

            if (ContainerQueryActivatorBase.GetContainer(visual, containerName) is { } container
                && container is Layoutable layoutable
                && Container.GetQueryProvider(layoutable) is { } queryProvider
                && Container.GetSizing(layoutable) == Layout.ContainerSizing.WidthAndHeight)
            {
                return Evaluate(queryProvider, Argument);
            }

            return SelectorMatch.NeverThisInstance;
        }

        internal static SelectorMatch Evaluate(VisualQueryProvider queryProvider, (QueryComparisonOperator @operator, double value) argument)
        {
            var width = queryProvider.Width;
            if (double.IsNaN(width))
            {
                return SelectorMatch.NeverThisInstance;
            }

            bool IsTrue(QueryComparisonOperator comparisonOperator, double value)
            {
                switch (comparisonOperator)
                {
                    case QueryComparisonOperator.None:
                        return true;
                    case QueryComparisonOperator.Equals:
                        return width == value;
                    case QueryComparisonOperator.LessThan:
                        return width < value;
                    case QueryComparisonOperator.GreaterThan:
                        return width > value;
                    case QueryComparisonOperator.LessThanOrEquals:
                        return width <= value;
                    case QueryComparisonOperator.GreaterThanOrEquals:
                        return width >= value;
                }

                return false;
            }

            return IsTrue(argument.@operator, argument.value) ?
                new SelectorMatch(SelectorMatchResult.AlwaysThisInstance) : SelectorMatch.NeverThisInstance;
        }

        public override string ToString() => "width";

        public override string ToString(ContainerQuery? owner)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class HeightQuery : ValueQuery<(QueryComparisonOperator @operator, double value)>
    {
        public HeightQuery(Query? previous, QueryComparisonOperator @operator, double value) : base(previous, (@operator, value))
        {
        }

        internal override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe, string? containerName = null)
        {
            if (control is not Visual visual)
            {
                return SelectorMatch.NeverThisType;
            }

            if (subscribe)
            {
                return new SelectorMatch(new HeightActivator(visual, Argument));
            }

            if (ContainerQueryActivatorBase.GetContainer(visual, containerName) is { } container
                && container is Layoutable layoutable
                && Container.GetQueryProvider(layoutable) is { } queryProvider
                && Container.GetSizing(layoutable) == Layout.ContainerSizing.WidthAndHeight)
            {
                return Evaluate(queryProvider, Argument);
            }

            return SelectorMatch.NeverThisInstance;
        }

        internal static SelectorMatch Evaluate(VisualQueryProvider screenSizeProvider, (QueryComparisonOperator @operator, double value) argument)
        {
            var height = screenSizeProvider.Height;
            if (double.IsNaN(height))
            {
                return SelectorMatch.NeverThisInstance;
            }

            var isvalueTrue = IsTrue(argument.@operator, argument.value);

            bool IsTrue(QueryComparisonOperator comparisonOperator, double value)
            {
                switch (comparisonOperator)
                {
                    case QueryComparisonOperator.None:
                        return true;
                    case QueryComparisonOperator.Equals:
                        return height == value;
                    case QueryComparisonOperator.LessThan:
                        return height < value;
                    case QueryComparisonOperator.GreaterThan:
                        return height > value;
                    case QueryComparisonOperator.LessThanOrEquals:
                        return height <= value;
                    case QueryComparisonOperator.GreaterThanOrEquals:
                        return height >= value;
                }

                return false;
            }

            return IsTrue(argument.@operator, argument.value) ?
                SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
        }

        public override string ToString() => "height";

        public override string ToString(ContainerQuery? owner)
        {
            throw new NotImplementedException();
        }
    }
}
