using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Styling;
using static Avalonia.Markup.Parsers.ContainerQueryGrammar;

namespace Avalonia.Markup.Parsers
{
    /// <summary>
    /// Parses a <see cref="Selector"/> from text.
    /// </summary>
    internal class ContainerQueryParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerQueryParser"/> class.
        /// </summary>
        public ContainerQueryParser()
        {
        }

        /// <summary>
        /// Parses a <see cref="Selector"/> from a string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The parsed selector.</returns>
        [RequiresUnreferencedCode(TrimmingMessages.SelectorsParseRequiresUnreferencedCodeMessage)]
        public StyleQuery? Parse(string s)
        {
            var syntax = ContainerQueryGrammar.Parse(s);
            return Create(syntax);
        }

        [RequiresUnreferencedCode(TrimmingMessages.SelectorsParseRequiresUnreferencedCodeMessage)]
        private StyleQuery? Create(IEnumerable<ISyntax> syntax)
        {
            var result = default(StyleQuery);
            var results = default(List<StyleQuery>);

            foreach (var i in syntax)
            {
                switch (i)
                {
                    case ContainerQueryGrammar.WidthSyntax width:
                        result = result.Width(width.Operator, width.Value);
                        break;
                    case ContainerQueryGrammar.HeightSyntax height:
                        result = result.Height(height.Operator, height.Value);
                        break;
                    case ContainerQueryGrammar.OrSyntax or:
                    case ContainerQueryGrammar.AndSyntax and:
                        if (results == null)
                        {
                            results = new List<StyleQuery>();
                        }

                        results.Add(result ?? throw new NotSupportedException("Invalid query!"));
                        result = null;
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported selector grammar '{i.GetType()}'.");
                }
            }

            if (results != null)
            {
                if (result != null)
                {
                    results.Add(result);
                }

                result = results.Count > 1 ? StyleQueries.Or(results) : results[0];
            }

            return result;
        }
    }
}
