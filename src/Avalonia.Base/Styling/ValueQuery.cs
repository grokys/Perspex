using System;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Styling
{
    public abstract class ValueQuery<T> : Query
    {
        private readonly Query? _previous;
        private T _argument;

        public ValueQuery(Query? previous, T argument)
        {
            _previous = previous;
            _argument = argument;
        }

        protected T Argument => _argument;

        internal override bool IsCombinator => false;

        public override string ToString(ContainerQuery? owner)
        {
            throw new NotImplementedException();
        }

        private protected override Query? MovePrevious() => _previous;

        private protected override Query? MovePreviousOrParent() => _previous;
    }
}
