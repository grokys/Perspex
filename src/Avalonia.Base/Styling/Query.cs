using System;
using Avalonia.Styling.Activators;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// A query in a <see cref="ContainerQuery"/>.
    /// </summary>
    public abstract class Query
    {
        /// <summary>
        /// Gets a value indicating whether this query is a combinator.
        /// </summary>
        /// <remarks>
        /// A combinator is a query such as Child or Descendent which links simple querys.
        /// </remarks>
        internal abstract bool IsCombinator { get; }

        /// <summary>
        /// Tries to match the query with a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="parent">
        /// The parent container, if the container containing the query is a nested container.
        /// </param>
        /// <param name="subscribe">
        /// Whether the match should subscribe to changes in order to track the match over time,
        /// or simply return an imcontainerte result.
        /// </param>
        /// <param name="containerName">
        /// The name of container to query on.
        /// </param>
        /// <returns>A <see cref="SelectorMatch"/>.</returns>
        internal virtual SelectorMatch Match(StyledElement control, IStyle? parent = null, bool subscribe = true, string? containerName = null)
        {
            // First match the query until a combinator is found. Selectors are stored from 
            // right-to-left, so MatchUntilCombinator reverses this order because the type query
            // will be on the left.
            var match = MatchUntilCombinator(control, this, parent, subscribe, out var combinator, containerName);
            
            // If the pre-combinator query matches, we can now match the combinator, if any.
            if (match.IsMatch && combinator is object)
            {
                match = match.And(combinator.Match(control, parent, subscribe, containerName));

                // If we have a combinator then we can never say that we always match a control of
                // this type, because by definition the combinator matches on things outside of the
                // control.
                match = match.Result switch
                {
                    SelectorMatchResult.AlwaysThisType => SelectorMatch.AlwaysThisInstance,
                    SelectorMatchResult.NeverThisType => SelectorMatch.NeverThisInstance,
                    _ => match
                };
            }

            return match;
        }

        public override string ToString() => ToString(null);

        /// <summary>
        /// Gets a string representing the query, with the nesting separator (`^`) replaced with
        /// the parent query.
        /// </summary>
        /// <param name="owner">The owner container.</param>
        public abstract string ToString(ContainerQuery? owner);

        /// <summary>
        /// Evaluates the query for a match.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="parent">
        /// The parent container, if the container containing the query is a nested container.
        /// </param>
        /// <param name="subscribe">
        /// Whether the match should subscribe to changes in order to track the match over time,
        /// or simply return an imcontainerte result.
        /// </param>
        /// <param name="containerName">
        /// The name of the container to evaluate.
        /// </param>
        /// <returns>A <see cref="SelectorMatch"/>.</returns>
        internal abstract SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe, string? containerName);

        /// <summary>
        /// Moves to the previous query.
        /// </summary>
        private protected abstract Query? MovePrevious();

        /// <summary>
        /// Moves to the previous query or the parent query.
        /// </summary>
        private protected abstract Query? MovePreviousOrParent();

        private static SelectorMatch MatchUntilCombinator(
            StyledElement control,
            Query start,
            IStyle? parent,
            bool subscribe,
            out Query? combinator,
            string? containerName = null)
        {
            combinator = null;

            var activators = new AndActivatorBuilder();
            var result = Match(control, start, parent, subscribe, ref activators, ref combinator, containerName);

            return result == SelectorMatchResult.Sometimes ?
                new SelectorMatch(activators.Get()) :
                new SelectorMatch(result);
        }

        private static SelectorMatchResult Match(
            StyledElement control,
            Query query,
            IStyle? parent,
            bool subscribe,
            ref AndActivatorBuilder activators,
            ref Query? combinator, 
            string? containerName)
        {
            var previous = query.MovePrevious();

            // Selectors are stored from right-to-left, so we recurse into the query in order to
            // reverse this order, because the type query will be on the left and is our best
            // opportunity to exit early.
            if (previous != null && !previous.IsCombinator)
            {
                var previousMatch = Match(control, previous, parent, subscribe, ref activators, ref combinator, containerName);

                if (previousMatch < SelectorMatchResult.Sometimes)
                {
                    return previousMatch;
                }
            }

            // Match this query.
            var match = query.Evaluate(control, parent, subscribe, containerName);

            if (!match.IsMatch)
            {
                combinator = null;
                return match.Result;
            }
            else if (match.Activator is object)
            {
                activators.Add(match.Activator!);
            }

            if (previous?.IsCombinator == true)
            {
                combinator = previous;
            }

            return match.Result;
        }
    }
}

