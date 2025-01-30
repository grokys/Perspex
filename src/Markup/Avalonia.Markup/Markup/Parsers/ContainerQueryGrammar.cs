using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Data.Core;
using Avalonia.Styling;
using Avalonia.Utilities;

// Don't need to override GetHashCode as the ISyntax objects will not be stored in a hash; the
// only reason they have overridden Equals methods is for unit testing.
#pragma warning disable 659

namespace Avalonia.Markup.Parsers
{
    internal static class ContainerQueryGrammar
    {
        const string MinWidthKeyword = "min-width";
        const string MinHeightKeyword = "min-height";
        const string MaxWidthKeyword = "max-width";
        const string MaxHeightKeyword = "max-height";
        const string WidthKeyword = "width";
        const string HeightKeyword = "height";
        static string[] Keywords = new string[] { MinWidthKeyword, MinHeightKeyword, MaxWidthKeyword, MaxHeightKeyword, WidthKeyword, HeightKeyword };

        private enum State
        {
            Start,
            Middle,
            End,
        }

        public static IEnumerable<ISyntax> Parse(string s)
        {
            var r = new CharacterReader(s.AsSpan());
            return Parse(ref r, null);
        }

        private static IEnumerable<ISyntax> Parse(ref CharacterReader r, char? end)
        {
            var state = State.Start;
            var selector = new List<ISyntax>();
            while (!r.End && state != State.End)
            {
                ISyntax? syntax = null;
                switch (state)
                {
                    case State.Start:
                        (state, syntax) = ParseStart(ref r);
                        break;
                    case State.Middle:
                        (state, syntax) = ParseMiddle(ref r, end);
                        break;
                }
                if (syntax != null)
                {
                    selector.Add(syntax);
                }
            }

            if (state != State.Start && state != State.Middle && state != State.End)
            {
                throw new ExpressionParseException(r.Position, "Unexpected end of selector");
            }

            return selector;
        }

        private static ISyntax? ParseFeature(ref CharacterReader r)
        {
            r.SkipWhitespace();

            var identifier = r.ParseStyleClass();

            if (identifier.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, "Expected query feature name.");
            }

            var s = identifier.ToString();
            if (!Keywords.Any(x => s == x))
            {
                throw new InvalidOperationException($"Unknown feature name found: {identifier.ToString()}");
            }

            if (identifier.SequenceEqual(MinWidthKeyword.AsSpan()) || identifier.SequenceEqual(MaxWidthKeyword.AsSpan()) || identifier.SequenceEqual(WidthKeyword.AsSpan()))
            {
                if (!r.TakeIf(':'))
                    throw new ExpressionParseException(r.Position, "Expected ':' after 'orientation'.");
                double val = ParseDecimal(ref r);

                var syntax = new WidthSyntax()
                {
                    Value = val,
                    Operator = identifier.SequenceEqual(WidthKeyword.AsSpan()) ? QueryComparisonOperator.Equals 
                    : identifier.SequenceEqual(MinWidthKeyword.AsSpan()) ? QueryComparisonOperator.GreaterThanOrEquals 
                    : QueryComparisonOperator.LessThanOrEquals
                };

                return syntax;
            }

            if (identifier.SequenceEqual(MinHeightKeyword.AsSpan()) || identifier.SequenceEqual(MaxHeightKeyword.AsSpan()) || identifier.SequenceEqual(HeightKeyword.AsSpan()))
            {
                if (!r.TakeIf(':'))
                    throw new ExpressionParseException(r.Position, "Expected ':' after 'orientation'.");
                double val = ParseDecimal(ref r);

                var syntax = new HeightSyntax()
                {
                    Value = val,
                    Operator = identifier.SequenceEqual(WidthKeyword.AsSpan()) ? QueryComparisonOperator.Equals 
                    : identifier.SequenceEqual(MinHeightKeyword.AsSpan()) ? QueryComparisonOperator.GreaterThanOrEquals 
                    : QueryComparisonOperator.LessThanOrEquals
                };

                return syntax;
            }

            return null;
        }

        private static (State, ISyntax?) ParseStart(ref CharacterReader r)
        {
            r.SkipWhitespace();
            if (r.End)
            {
                return (State.End, null);
            }

            if(char.IsLetter(r.Peek))
            {
                return (State.Middle, ParseFeature(ref r));
            }

            throw new InvalidOperationException("Invalid syntax found");
        }

        private static (State, ISyntax?) ParseMiddle(ref CharacterReader r, char? end)
        {
            r.SkipWhitespace();

            if (r.TakeIf(','))
            {
                return (State.Start, new OrSyntax());
            }
            else if (end.HasValue && !r.End && r.Peek == end.Value)
            {
                return (State.End, null);
            }
            else
            {
                var identifier = r.TakeWhile(c => !char.IsWhiteSpace(c));

                if (identifier.SequenceEqual("and".AsSpan()))
                {
                    return (State.Start, new AndSyntax());
                }
            }
            throw new InvalidOperationException("Invalid syntax found");
        }
        
        private static double ParseDecimal(ref CharacterReader r)
        {
            r.SkipWhitespace();
            var number = r.ParseNumber();
            if (number.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, $"Expected a number after.");
            }

            return double.Parse(number.ToString());
        }
        
        private static QueryComparisonOperator ParseOperator(ref CharacterReader r)
        {
            r.SkipWhitespace();
            var queryOperator = r.TakeWhile(x => !char.IsWhiteSpace(x));

            return queryOperator.ToString() switch
            {
                "=" => QueryComparisonOperator.Equals,
                "<" => QueryComparisonOperator.LessThan,
                ">" => QueryComparisonOperator.GreaterThan,
                "<=" => QueryComparisonOperator.LessThanOrEquals,
                ">=" => QueryComparisonOperator.GreaterThanOrEquals,
                "" => QueryComparisonOperator.None,
                _ => throw new ExpressionParseException(r.Position, $"Expected a comparison operator after.")
            };
        }
        
        private static T ParseEnum<T>(ref CharacterReader r) where T: struct
        {
            var identifier = r.ParseIdentifier();

            if (Enum.TryParse<T>(identifier.ToString(), true, out T value))
                return value;

            throw new ExpressionParseException(r.Position, $"Expected a {typeof(T)} after.");
        }
        
        private static string ParseString(ref CharacterReader r) 
        {
            return r.ParseIdentifier().ToString();
        }

        private static void Expect(ref CharacterReader r, char c)
        {
            if (r.End)
            {
                throw new ExpressionParseException(r.Position, $"Expected '{c}', got end of selector.");
            }
            else if (!r.TakeIf(')'))
            {
                throw new ExpressionParseException(r.Position, $"Expected '{c}', got '{r.Peek}'.");
            }
        }

        public class OrSyntax : ISyntax
        {
            public override bool Equals(object? obj)
            {
                return obj is OrSyntax;
            }
        }

        public class AndSyntax : ISyntax
        {
            public override bool Equals(object? obj)
            {
                return obj is AndSyntax;
            }
        }

        public abstract class QuerySyntax<T> : ISyntax
        {
            public T? Value { get; set; }
            public QueryComparisonOperator Operator { get; set; }
        }

        public abstract class RangeSyntax : QuerySyntax<double>
        {

        }

        public class WidthSyntax : RangeSyntax
        {
            public override bool Equals(object? obj)
            {
                return (obj is WidthSyntax width) && width.Value == Value
                    && width.Operator == Operator;
            }
        }

        public class HeightSyntax : RangeSyntax
        {
            public override bool Equals(object? obj)
            {
                return (obj is HeightSyntax height) && height.Value == Value
                    && height.Operator == Operator;
            }
        }

        public interface ISyntax
        {

        }
    }
}
