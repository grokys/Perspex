using System;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Styling
{
    public abstract class ContainerQuery<T> : StyleQuery
    {
        private readonly StyleQuery? _previous;
        private T _argument;

        public ContainerQuery(StyleQuery? previous, T argument)
        {
            _previous = previous;
            _argument = argument;
        }

        protected T Argument => _argument;

        internal override bool IsCombinator => false;

        public override string ToString(Container? owner)
        {
            throw new NotImplementedException();
        }

        private protected override StyleQuery? MovePrevious() => _previous;

        private protected override StyleQuery? MovePreviousOrParent() => _previous;
    }
}
