using Avalonia.Platform;
using System.Collections.Generic;

namespace Avalonia.Styling
{
    /// <summary>
    /// Extension methods for <see cref="StyleQuery"/>.
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
        public static StyleQuery Width(this StyleQuery? previous, QueryComparisonOperator @operator, double value)
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
        public static StyleQuery Height(this StyleQuery? previous, QueryComparisonOperator @operator, double value)
        {
            return new HeightQuery(previous, @operator, value);
        }

        /// <summary>
        /// Returns a query which ORs queries.
        /// </summary>
        /// <param name="queries">The queries to be OR'd.</param>
        /// <returns>The query.</returns>
        public static StyleQuery Or(params StyleQuery[] queries)
        {
            return new OrQuery(queries);
        }

        /// <summary>
        /// Returns a query which ORs queries.
        /// </summary>
        /// <param name="query">The queries to be OR'd.</param>
        /// <returns>The query.</returns>
        public static StyleQuery Or(IReadOnlyList<StyleQuery> query)
        {
            return new OrQuery(query);
        }

        /// <summary>
        /// Returns a query which ANDs queries.
        /// </summary>
        /// <param name="queries">The queries to be AND'd.</param>
        /// <returns>The query.</returns>
        public static StyleQuery And(params StyleQuery[] queries)
        {
            return new AndQuery(queries);
        }

        /// <summary>
        /// Returns a query which ANDs queries.
        /// </summary>
        /// <param name="query">The queries to be AND'd.</param>
        /// <returns>The query.</returns>
        public static StyleQuery And(IReadOnlyList<StyleQuery> query)
        {
            return new AndQuery(query);
        }
    }
}
