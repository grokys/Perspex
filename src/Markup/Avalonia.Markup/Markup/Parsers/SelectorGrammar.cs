using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Data.Core;
using Avalonia.Utilities;

// Don't need to override GetHashCode as the ISyntax objects will not be stored in a hash; the
// only reason they have overridden Equals methods is for unit testing.
#pragma warning disable 659

namespace Avalonia.Markup.Parsers
{
    internal static class SelectorGrammar
    {
        private enum State
        {
            Start,
            Middle,
            Colon,
            Class,
            Name,
            CanHaveType,
            Traversal,
            TypeName,
            Property,
            AttachedProperty,
            Template,
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
                    case State.CanHaveType:
                        state = ParseCanHaveType(ref r);
                        break;
                    case State.Colon:
                        (state, syntax) = ParseColon(ref r);
                        break;
                    case State.Class:
                        (state, syntax) = ParseClass(ref r);
                        break;
                    case State.Traversal:
                        (state, syntax) = ParseTraversal(ref r);
                        break;
                    case State.TypeName:
                        (state, syntax) = ParseTypeName(ref r);
                        break;
                    case State.Property:
                        (state, syntax) = ParseProperty(ref r);
                        break;
                    case State.Template:
                        (state, syntax) = ParseTemplate(ref r);
                        break;
                    case State.Name:
                        (state, syntax) = ParseName(ref r);
                        break;
                    case State.AttachedProperty:
                        (state, syntax) = ParseAttachedProperty(ref r);
                        break;
                }
                if (syntax != null)
                {
                    selector.Add(syntax);
                }
            }

            if (state != State.Start && state != State.Middle && state != State.End && state != State.CanHaveType)
            {
                throw new ExpressionParseException(r.Position, "Unexpected end of selector");
            }

            return selector;
        }

        private static (State, ISyntax?) ParseStart(ref CharacterReader r)
        {
            r.SkipWhitespace();
            if (r.End)
            {
                return (State.End, null);
            }

            if (r.TakeIf(':'))
            {
                return (State.Colon, null);
            }
            else if (r.TakeIf('.'))
            {
                return (State.Class, null);
            }
            else if (r.TakeIf('#'))
            {
                return (State.Name, null);
            }
            else if (r.TakeIf('^'))
            {
                return (State.CanHaveType, new NestingSyntax());
            }
            return (State.TypeName, null);
        }

        private static (State, ISyntax?) ParseMiddle(ref CharacterReader r, char? end)
        {
            if (r.TakeIf(':'))
            {
                return (State.Colon, null);
            }
            else if (r.TakeIf('.'))
            {
                return (State.Class, null);
            }
            else if (r.TakeIf(char.IsWhiteSpace) || r.Peek == '>')
            {
                return (State.Traversal, null);
            }
            else if (r.TakeIf('/'))
            {
                return (State.Template, null);
            }
            else if (r.TakeIf('#'))
            {
                return (State.Name, null);
            }
            else if (r.TakeIf(','))
            {
                return (State.Start, new CommaSyntax());
            }
            else if (r.TakeIf('^'))
            {
                return (State.CanHaveType, new NestingSyntax());
            }
            else if (end.HasValue && !r.End && r.Peek == end.Value)
            {
                return (State.End, null);
            }
            return (State.TypeName, null);
        }

        private static State ParseCanHaveType(ref CharacterReader r)
        {
            if (r.TakeIf('['))
            {
                return State.Property;
            }
            return State.Middle;
        }

        private static (State, ISyntax) ParseColon(ref CharacterReader r)
        {
            var identifier = r.ParseStyleClass();

            if (identifier.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, "Expected class name, is, nth-child or nth-last-child selector after ':'.");
            }

            const string IsKeyword = "is";
            const string NotKeyword = "not";
            const string NthChildKeyword = "nth-child";
            const string NthLastChildKeyword = "nth-last-child";

            if (identifier.SequenceEqual(IsKeyword.AsSpan()) && r.TakeIf('('))
            {
                var syntax = ParseType(ref r, new IsSyntax());
                Expect(ref r, ')');

                return (State.CanHaveType, syntax);
            }
            if (identifier.SequenceEqual(NotKeyword.AsSpan()) && r.TakeIf('('))
            {
                var argument = Parse(ref r, ')');
                Expect(ref r, ')');

                var syntax = new NotSyntax { Argument = argument };
                return (State.Middle, syntax);
            }
            if (identifier.SequenceEqual(NthChildKeyword.AsSpan()) && r.TakeIf('('))
            {
                var (step, offset) = ParseNthChildArguments(ref r);

                var syntax = new NthChildSyntax { Step = step, Offset = offset };
                return (State.Middle, syntax);
            }
            if (identifier.SequenceEqual(NthLastChildKeyword.AsSpan()) && r.TakeIf('('))
            {
                var (step, offset) = ParseNthChildArguments(ref r);

                var syntax = new NthLastChildSyntax { Step = step, Offset = offset };
                return (State.Middle, syntax);
            }
            else
            {
                return (
                    State.CanHaveType,
                    new ClassSyntax
                    {
                        Class = ":" + identifier.ToString()
                    });
            }
        }
        private static (State, ISyntax?) ParseTraversal(ref CharacterReader r)
        {
            r.SkipWhitespace();
            if (r.TakeIf('>'))
            {
                r.SkipWhitespace();
                return (State.Middle, new ChildSyntax());
            }
            else if (r.TakeIf('/'))
            {
                return (State.Template, null);
            }
            else if (!r.End)
            {
                return (State.Middle, new DescendantSyntax());
            }
            else
            {
                return (State.End, null);
            }
        }

        private static (State, ISyntax) ParseClass(ref CharacterReader r)
        {
            var @class = r.ParseStyleClass();
            if (@class.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, $"Expected a class name after '.'.");
            }

            return (State.CanHaveType, new ClassSyntax { Class = @class.ToString() });
        }

        private static (State, ISyntax) ParseTemplate(ref CharacterReader r)
        {
            var template = r.ParseIdentifier();
            const string TemplateKeyword = "template";
            if (!template.SequenceEqual(TemplateKeyword.AsSpan()))
            {
                throw new ExpressionParseException(r.Position, $"Expected 'template', got '{template.ToString()}'");
            }
            else if (!r.TakeIf('/'))
            {
                throw new ExpressionParseException(r.Position, "Expected '/'");
            }
            return (State.Start, new TemplateSyntax());
        }

        private static (State, ISyntax) ParseName(ref CharacterReader r)
        {
            var name = r.ParseIdentifier();
            if (name.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, $"Expected a name after '#'.");
            }
            return (State.CanHaveType, new NameSyntax { Name = name.ToString() });
        }

        private static double ParseDecimal(ref CharacterReader r)
        {
            var number = r.ParseNumber();
            if (number.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, $"Expected a number after.");
            }

            return double.Parse(number.ToString());
        }

        private static T ParseEnum<T>(ref CharacterReader r) where T : struct
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

        private static (State, ISyntax) ParseTypeName(ref CharacterReader r)
        {
            return (State.CanHaveType, ParseType(ref r, new OfTypeSyntax()));
        }

        private static (State, ISyntax?) ParseProperty(ref CharacterReader r)
        {
            var property = r.ParseIdentifier();

            if (r.TakeIf('('))
            {
                return (State.AttachedProperty, default);
            }
            else if (!r.TakeIf('='))
            {
                throw new ExpressionParseException(r.Position, $"Expected '=', got '{r.Peek}'");
            }

            var value = r.TakeUntil(']');

            r.Take();

            return (State.CanHaveType, new PropertySyntax { Property = property.ToString(), Value = value.ToString() });
        }

        private static (State, ISyntax) ParseAttachedProperty(ref CharacterReader r)
        {
            var syntax = ParseType(ref r, new AttachedPropertySyntax());
            if (!r.TakeIf('.'))
            {
                throw new ExpressionParseException(r.Position, $"Expected '.', got '{r.Peek}'");
            }
            var property = r.ParseIdentifier();
            if (property.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, $"Expected Attached Property Name, got '{r.Peek}'");
            }
            syntax.Property = property.ToString();

            if (!r.TakeIf(')'))
            {
                throw new ExpressionParseException(r.Position, $"Expected ')', got '{r.Peek}'");
            }

            if (!r.TakeIf('='))
            {
                throw new ExpressionParseException(r.Position, $"Expected '=', got '{r.Peek}'");
            }

            var value = r.TakeUntil(']');

            syntax.Value = value.ToString();

            r.Take();

            var state = r.End
                ? State.End
                : State.Middle;
            return (state, syntax);
        }

        private static TSyntax ParseType<TSyntax>(ref CharacterReader r, TSyntax syntax)
            where TSyntax : ITypeSyntax
        {
            ReadOnlySpan<char> ns = default;
            ReadOnlySpan<char> type;
            var namespaceOrTypeName = r.ParseIdentifier();

            if (namespaceOrTypeName.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, $"Expected an identifier, got '{r.Peek}");
            }

            if (!r.End && r.TakeIf('|'))
            {
                ns = namespaceOrTypeName;
                if (r.End)
                {
                    throw new ExpressionParseException(r.Position, $"Unexpected end of selector.");
                }
                type = r.ParseIdentifier();
            }
            else
            {
                type = namespaceOrTypeName;
            }

            syntax.Xmlns = ns.ToString();
            syntax.TypeName = type.ToString();
            return syntax;
        }

        private static (int step, int offset) ParseNthChildArguments(ref CharacterReader r)
        {
            int step = 0;
            int offset = 0;

            if (r.Peek == 'o')
            {
                var constArg = r.TakeUntil(')').Trim();
                if (constArg.SequenceEqual("odd".AsSpan()))
                {
                    step = 2;
                    offset = 1;
                }
                else
                {
                    throw new ExpressionParseException(r.Position, $"Expected nth-child(odd). Actual '{constArg.ToString()}'.");
                }
            }
            else if (r.Peek == 'e')
            {
                var constArg = r.TakeUntil(')').Trim();
                if (constArg.SequenceEqual("even".AsSpan()))
                {
                    step = 2;
                    offset = 0;
                }
                else
                {
                    throw new ExpressionParseException(r.Position, $"Expected nth-child(even). Actual '{constArg.ToString()}'.");
                }
            }
            else
            {
                r.SkipWhitespace();

                var stepOrOffset = 0;
                var stepOrOffsetStr = r.TakeWhile(c => char.IsDigit(c) || c == '-' || c == '+');
                if (stepOrOffsetStr.Length == 0
                    || (stepOrOffsetStr.Length == 1
                    && stepOrOffsetStr[0] == '+'))
                {
                    stepOrOffset = 1;
                }
                else if (stepOrOffsetStr.Length == 1
                    && stepOrOffsetStr[0] == '-')
                {
                    stepOrOffset = -1;
                }
                else if (!stepOrOffsetStr.TryParseInt(out stepOrOffset))
                {
                    throw new ExpressionParseException(r.Position, "Couldn't parse nth-child step or offset value. Integer was expected.");
                }

                r.SkipWhitespace();

                if (r.Peek == ')')
                {
                    step = 0;
                    offset = stepOrOffset;
                }
                else
                {
                    step = stepOrOffset;

                    if (r.Peek != 'n')
                    {
                        throw new ExpressionParseException(r.Position, "Couldn't parse nth-child step value, \"xn+y\" pattern was expected.");
                    }

                    r.Skip(1); // skip 'n'

                    r.SkipWhitespace();

                    if (r.Peek != ')')
                    {
                        int sign;
                        var nextChar = r.Take();
                        if (nextChar == '+')
                        {
                            sign = 1;
                        }
                        else if (nextChar == '-')
                        {
                            sign = -1;
                        }
                        else
                        {
                            throw new ExpressionParseException(r.Position, "Couldn't parse nth-child sign. '+' or '-' was expected.");
                        }

                        r.SkipWhitespace();

                        if (sign != 0
                            && !r.TakeUntil(')').TryParseInt(out offset))
                        {
                            throw new ExpressionParseException(r.Position, "Couldn't parse nth-child offset value. Integer was expected.");
                        }

                        offset *= sign;
                    }
                }
            }

            Expect(ref r, ')');

            return (step, offset);
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

        public interface ISyntax
        {
        }

        public interface ITypeSyntax
        {
            string TypeName { get; set; }

            string Xmlns { get; set; }
        }

        public class OfTypeSyntax : ISyntax, ITypeSyntax
        {
            public string TypeName { get; set; } = string.Empty;

            public string Xmlns { get; set; } = string.Empty;

            public override bool Equals(object? obj)
            {
                var other = obj as OfTypeSyntax;
                return other != null && other.TypeName == TypeName && other.Xmlns == Xmlns;
            }
        }

        public class AttachedPropertySyntax : ISyntax, ITypeSyntax
        {
            public string Xmlns { get; set; } = string.Empty;

            public string TypeName { get; set; } = string.Empty;

            public string Property { get; set; } = string.Empty;

            public string Value { get; set; } = string.Empty;

            public override bool Equals(object? obj)
            {
                return obj is AttachedPropertySyntax syntax
                    && syntax.Xmlns == Xmlns
                    && syntax.TypeName == TypeName
                    && syntax.Property == Property
                    && syntax.Value == Value;
            }
        }

        public class IsSyntax : ISyntax, ITypeSyntax
        {
            public string TypeName { get; set; } = string.Empty;

            public string Xmlns { get; set; } = string.Empty;

            public override bool Equals(object? obj)
            {
                var other = obj as IsSyntax;
                return other != null && other.TypeName == TypeName && other.Xmlns == Xmlns;
            }
        }

        public class ClassSyntax : ISyntax
        {
            public string Class { get; set; } = string.Empty;

            public override bool Equals(object? obj)
            {
                return obj is ClassSyntax syntax &&
                       syntax.Class == Class;
            }
        }

        public class NameSyntax : ISyntax
        {
            public string Name { get; set; } = string.Empty;

            public override bool Equals(object? obj)
            {
                return obj is NameSyntax syntax &&
                       syntax.Name == Name;
            }
        }

        public class PropertySyntax : ISyntax
        {
            public string Property { get; set; } = string.Empty;

            public string Value { get; set; } = string.Empty;

            public override bool Equals(object? obj)
            {
                return obj is PropertySyntax syntax &&
                       syntax.Property == Property &&
                       syntax.Value == Value;
            }
        }

        public class ChildSyntax : ISyntax
        {
            public override bool Equals(object? obj)
            {
                return obj is ChildSyntax;
            }
        }

        public class DescendantSyntax : ISyntax
        {
            public override bool Equals(object? obj)
            {
                return obj is DescendantSyntax;
            }
        }

        public class TemplateSyntax : ISyntax
        {
            public override bool Equals(object? obj)
            {
                return obj is TemplateSyntax;
            }
        }

        public class NotSyntax : ISyntax
        {
            public IEnumerable<ISyntax> Argument { get; set; } = Enumerable.Empty<ISyntax>();

            public override bool Equals(object? obj)
            {
                return (obj is NotSyntax not) && Argument.SequenceEqual(not.Argument);
            }
        }

        public class NthChildSyntax : ISyntax
        {
            public int Offset { get; set; }
            public int Step { get; set; }

            public override bool Equals(object? obj)
            {
                return (obj is NthChildSyntax nth) && nth.Offset == Offset && nth.Step == Step;
            }
        }

        public class NthLastChildSyntax : ISyntax
        {
            public int Offset { get; set; }
            public int Step { get; set; }

            public override bool Equals(object? obj)
            {
                return (obj is NthLastChildSyntax nth) && nth.Offset == Offset && nth.Step == Step;
            }
        }

        public class CommaSyntax : ISyntax
        {
            public override bool Equals(object? obj)
            {
                return obj is CommaSyntax;
            }
        }

        public class NestingSyntax : ISyntax
        {
            public override bool Equals(object? obj)
            {
                return obj is NestingSyntax;
            }
        }

        public class DecimalSyntax : ISyntax
        {
            public decimal Number { get; set; }

            public override bool Equals(object? obj)
            {
                return obj is DecimalSyntax dec && dec.Number == Number;
            }
        }
    }
}
