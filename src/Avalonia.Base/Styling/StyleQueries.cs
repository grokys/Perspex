using Avalonia.Platform;
using System.Collections.Generic;

namespace Avalonia.Styling
{
    /// <summary>
    /// Extension methods for <see cref="Query"/>.
    /// </summary>
    public static class StyleQueries
    {
        /// <summary>
        /// Returns a query which matches the device width with a value.
        /// </summary>
        /// <param name="previous">The previous query.</param>
        /// <param name="operator">The operator to match the device width</param>
        /// <param name="value">The width to match</param>
        /// <returns>The query.</returns>
        public static Query Width(this Query? previous, QueryComparisonOperator @operator, double value)
        {
            return new WidthQuery(previous, @operator, value);
        }



        /// <summary>
        /// Returns a query which matches the device height with a value.
        /// </summary>
        /// <param name="previous">The previous query.</param>
        /// <param name="operator">The operator to match the device height</param>
        /// <param name="value">The height to match</param>
        /// <returns>The query.</returns>
        public static Query Height(this Query? previous, QueryComparisonOperator @operator, double value)
        {
            return new HeightQuery(previous, @operator, value);
        }

        /// <summary>
        /// Returns a query which ORs queries.
        /// </summary>
        /// <param name="queries">The queries to be OR'd.</param>
        /// <returns>The query.</returns>
        public static Query Or(params Query[] queries)
        {
            return new OrQuery(queries);
        }

        /// <summary>
        /// Returns a query which ORs queries.
        /// </summary>
        /// <param name="query">The queries to be OR'd.</param>
        /// <returns>The query.</returns>
        public static Query Or(IReadOnlyList<Query> query)
        {
            return new OrQuery(query);
        }

        /// <summary>
        /// Returns a query which ANDs queries.
        /// </summary>
        /// <param name="queries">The queries to be AND'd.</param>
        /// <returns>The query.</returns>
        public static Query And(params Query[] queries)
        {
            return new AndQuery(queries);
        }

        /// <summary>
        /// Returns a query which ANDs queries.
        /// </summary>
        /// <param name="query">The queries to be AND'd.</param>
        /// <returns>The query.</returns>
        public static Query And(IReadOnlyList<Query> query)
        {
            return new AndQuery(query);
        }
    }
}
