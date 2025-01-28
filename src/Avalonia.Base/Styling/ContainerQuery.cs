using System;
using Avalonia.Controls;
using Avalonia.PropertyStore;

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines a container.
    /// </summary>
    public class ContainerQuery
        : StyleBase
    {
        private Query? _query;
        private string? _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerQuery"/> class.
        /// </summary>
        public ContainerQuery()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerQuery"/> class.
        /// </summary>
        /// <param name="query">The container selector.</param>
        /// <param name="containerName"></param>
        public ContainerQuery(Func<Query?, Query> query, string? containerName = null)
        {
            Query = query(null);
            _name = containerName;
        }

        /// <summary>
        /// Gets or sets the container's query.
        /// </summary>
        public Query? Query 
        {
            get => _query;
            set => _query = value;
        }

        /// <summary>
        /// Gets or sets the container's name.
        /// </summary>
        public string? Name
        {
            get => _name;
            set => _name = value;
        }

        /// <summary>
        /// Returns a string representation of the container.
        /// </summary>
        /// <returns>A string representation of the container.</returns>
        public override string ToString() => Query?.ToString(this) ?? "ContainerQuery";

        internal override void SetParent(StyleBase? parent)
        {
            if (parent is ControlTheme)
                base.SetParent(parent);
            else
                throw new InvalidOperationException("Container cannot be added as a nested style.");
        }

        internal SelectorMatchResult TryAttach(StyledElement target, object? host, FrameType type)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));

            var result = SelectorMatchResult.NeverThisType;

            if (HasChildren)
            {
                var match = Query?.Match(target, Parent, true, Name) ??
                    (target == host ?
                        SelectorMatch.AlwaysThisInstance :
                        SelectorMatch.NeverThisInstance);

                if (match.IsMatch)
                {
                    Attach(target, match.Activator, type, true);
                }

                result = match.Result;
            }

            return result;
        }
    }
}
