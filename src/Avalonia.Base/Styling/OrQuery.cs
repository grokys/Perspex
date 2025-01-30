using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Styling.Activators;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// The OR style query.
    /// </summary>
    internal sealed class OrQuery : Query
    {
        private readonly IReadOnlyList<Query> _queries;
        private string? _queryString;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrQuery"/> class.
        /// </summary>
        /// <param name="queries">The querys to OR.</param>
        public OrQuery(IReadOnlyList<Query> queries)
        {
            if (queries is null)
            {
                throw new ArgumentNullException(nameof(queries));
            }

            if (queries.Count <= 1)
            {
                throw new ArgumentException("Need more than one query to OR.");
            }

            _queries = queries;
        }

        /// <inheritdoc/>
        internal override bool IsCombinator => false;

        /// <inheritdoc/>
        public override string ToString(ContainerQuery? owner)
        {
            if (_queryString == null)
            {
                _queryString = string.Join(", ", _queries.Select(x => x.ToString(owner)));
            }

            return _queryString;
        }

        internal override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe, string? containerName = null)
        {
            if (control is not Visual visual)
            {
                return SelectorMatch.NeverThisType;
            }

            var activators = new OrQueryActivatorBuilder(visual);
            var neverThisInstance = false;

            var count = _queries.Count;

            for (var i = 0; i < count; i++)
            {
                var match = _queries[i].Match(control, parent, subscribe, containerName);

                switch (match.Result)
                {
                    case SelectorMatchResult.AlwaysThisType:
                    case SelectorMatchResult.AlwaysThisInstance:
                        return match;
                    case SelectorMatchResult.NeverThisInstance:
                        neverThisInstance = true;
                        break;
                    case SelectorMatchResult.Sometimes:
                        activators.Add(match.Activator!);
                        break;
                }
            }

            if (activators.Count > 0)
            {
                return new SelectorMatch(activators.Get());
            }
            else if (neverThisInstance)
            {
                return SelectorMatch.NeverThisInstance;
            }
            else
            {
                return SelectorMatch.NeverThisType;
            }
        }

        private protected override Query? MovePrevious() => null;
        private protected override Query? MovePreviousOrParent() => null;
    }
}

