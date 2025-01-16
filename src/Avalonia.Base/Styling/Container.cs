using System;
using Avalonia.Controls;
using Avalonia.PropertyStore;

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines a container.
    /// </summary>
    public class Container : StyleBase
    {
        private StyleQuery? _query;
        private string? _containerName;

        /// <summary>
        /// Initializes a new instance of the <see cref="Container"/> class.
        /// </summary>
        public Container()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Container"/> class.
        /// </summary>
        /// <param name="query">The container selector.</param>
        /// <param name="containerName"></param>
        public Container(Func<StyleQuery?, StyleQuery> query, string? containerName = null)
        {
            Query = query(null);
            _containerName = containerName;
        }

        /// <summary>
        /// Gets or sets the container's query.
        /// </summary>
        public StyleQuery? Query 
        {
            get => _query;
            set => _query = value;
        }

        /// <summary>
        /// Gets or sets the container's name.
        /// </summary>
        public string? ContainerName
        {
            get => _containerName;
            set => _containerName = value;
        }

        /// <summary>
        /// Returns a string representation of the container.
        /// </summary>
        /// <returns>A string representation of the container.</returns>
        public override string ToString() => Query?.ToString(this) ?? "Container";

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
                var match = Query?.Match(target, Parent, true, ContainerName) ??
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
