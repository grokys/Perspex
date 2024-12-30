// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data.Core;
using Avalonia.Utilities;
using System;
using System.Collections.Generic;

namespace Avalonia.Markup.Parsers
{
    internal enum SourceMode
    {
        Data,
        Control
    }

    internal static class BindingExpressionGrammar
    {
        private static readonly List<INode> s_pool = new();

        public static (List<INode> Nodes, SourceMode Mode) Parse(ref CharacterReader r)
        {
            var result = new List<INode>();
            var mode = Parse(ref r, result);
            return (result, mode);
        }

        public static (List<INode> Nodes, SourceMode Mode) ParseToPooledList(ref CharacterReader r)
        {
            // Most of the time the list will be passed to `ExpressionNodeFactory.CreateFromAst`
            // and then discarded so as a micro-optimization we can reuse the list.
            s_pool.Clear();
            var mode = Parse(ref r, s_pool);
            return (s_pool, mode);
        }

        private static SourceMode Parse(ref CharacterReader r, List<INode> nodes)
        {
            var state = State.Start;
            var mode = SourceMode.Data;

            while (!r.End && state != State.End)
            {
                switch (state)
                {
                    case State.Start:
                        state = ParseStart(ref r, nodes);
                        break;

                    case State.AfterMember:
                        state = ParseAfterMember(ref r, nodes);
                        break;

                    case State.BeforeMember:
                        state = ParseBeforeMember(ref r, nodes);
                        break;

                    case State.AttachedProperty:
                        state = ParseAttachedProperty(ref r, nodes);
                        break;

                    case State.Indexer:
                        state = ParseIndexer(ref r, nodes);
                        break;

                    case State.TypeCast:
                        state = ParseTypeCast(ref r, nodes);
                        break;

                    case State.ElementName:
                        state = ParseElementName(ref r, nodes);
                        mode = SourceMode.Control;
                        break;

                    case State.RelativeSource:
                        state = ParseRelativeSource(ref r, nodes);
                        mode = SourceMode.Control;
                        break;
                }
            }

            if (!r.End)
            {
                throw new ExpressionParseException(r.Position, "Expected end of expression.");
            }

            if (state == State.BeforeMember)
            {
                throw new ExpressionParseException(r.Position, "Unexpected end of expression.");
            }

            return mode;
        }

        private static State ParseStart(ref CharacterReader r, IList<INode> nodes)
        {
            if (ParseNot(ref r))
            {
                nodes.Add(new NotNode());
                return State.Start;
            }

            else if (ParseSharp(ref r))
            {
                return State.ElementName;
            }
            else if (ParseDollarSign(ref r))
            {
                return State.RelativeSource;
            }
            else if (ParseOpenBrace(ref r))
            {
                if (PeekOpenBrace(ref r))
                {
                    return State.TypeCast;
                }

                return State.AttachedProperty;
            }
            else if (PeekOpenBracket(ref r))
            {
                return State.Indexer;
            }
            else if (ParseDot(ref r))
            {
                nodes.Add(new EmptyExpressionNode());
                return State.End;
            }
            else
            {
                var identifier = r.ParseIdentifier();

                if (!identifier.IsEmpty)
                {
                    nodes.Add(new PropertyNameNode { PropertyName = identifier.ToString() });
                    return State.AfterMember;
                }
            }

            return State.End;
        }

        private static State ParseAfterMember(ref CharacterReader r, IList<INode> nodes)
        {
            if (ParseMemberAccessor(ref r))
            {
                return State.BeforeMember;
            }
            else if (ParseStreamOperator(ref r))
            {
                nodes.Add(new StreamNode());
                return State.AfterMember;
            }
            else if (PeekOpenBracket(ref r))
            {
                return State.Indexer;
            }
            else if (ParseOpenBrace(ref r))
            {
                return State.TypeCast;
            }

            return State.End;
        }

        private static State ParseBeforeMember(ref CharacterReader r, IList<INode> nodes)
        {
            if (ParseOpenBrace(ref r))
            {
                if (PeekOpenBrace(ref r))
                {
                    return State.TypeCast;
                }

                return State.AttachedProperty;
            }
            else
            {
                var identifier = r.ParseIdentifier();

                if (!identifier.IsEmpty)
                {
                    var canBeNull = ParseNullConditional(ref r);

                    nodes.Add(new PropertyNameNode 
                    {
                        CanBeNull = canBeNull,
                        PropertyName = identifier.ToString() 
                    });

                    return State.AfterMember;
                }

                return State.End;
            }
        }

        private static State ParseAttachedProperty(
#if NET7SDK
            scoped
#endif
            ref CharacterReader r, List<INode> nodes)
        {
            var (ns, owner) = ParseTypeName(ref r);

            if(!r.End && r.TakeIf(')'))
            {
                nodes.Add(new TypeCastNode() { Namespace = ns, TypeName = owner });
                return State.AfterMember;
            }

            if (r.End || !r.TakeIf('.'))
            {
                throw new ExpressionParseException(r.Position, "Invalid attached property name.");
            }

            var name = r.ParseIdentifier();

            if (name.Length == 0)
            {
                throw new ExpressionParseException(r.Position, "Attached Property name expected after '.'.");
            }

            if (r.End || !r.TakeIf(')'))
            {
                throw new ExpressionParseException(r.Position, "Expected ')'.");
            }

            var canBeNull = ParseNullConditional(ref r);

            nodes.Add(new AttachedPropertyNameNode
            {
                CanBeNull = canBeNull,
                Namespace = ns,
                TypeName = owner,
                PropertyName = name.ToString()
            });
            return State.AfterMember;
        }

        private static State ParseIndexer(ref CharacterReader r, List<INode> nodes)
        {
            var args = r.ParseArguments('[', ']');

            if (args.Count == 0)
            {
                throw new ExpressionParseException(r.Position, "Indexer may not be empty.");
            }

            nodes.Add(new IndexerNode { Arguments = args });
            return State.AfterMember;
        }

        private static State ParseTypeCast(ref CharacterReader r, List<INode> nodes)
        {
            bool parseMemberBeforeAddCast = ParseOpenBrace(ref r);

            var (ns, typeName) = ParseTypeName(ref r);

            var result = State.AfterMember;

            if (parseMemberBeforeAddCast)
            {
                if (!ParseCloseBrace(ref r))
                {
                    throw new ExpressionParseException(r.Position, "Expected ')'.");
                }

                result = ParseBeforeMember(ref r, nodes);
                if (result == State.AttachedProperty)
                    result = ParseAttachedProperty(ref r, nodes);

                if (r.Peek == '[')
                {
                    result = ParseIndexer(ref r, nodes);
                }
            }

            nodes.Add(new TypeCastNode { Namespace = ns, TypeName = typeName });

            if (r.End || !r.TakeIf(')'))
            {
                throw new ExpressionParseException(r.Position, "Expected ')'.");
            }

            return result;
        }

        private static State ParseElementName(ref CharacterReader r, List<INode> nodes)
        {
            var name = r.ParseIdentifier();

            if (name.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, "Element name expected after '#'.");
            }

            nodes.Add(new NameNode { Name = name.ToString() });
            return State.AfterMember;
        }

        private static State ParseRelativeSource(ref CharacterReader r, List<INode> nodes)
        {
            var mode = r.ParseIdentifier();

            if (mode.SequenceEqual("self".AsSpan()))
            {
                nodes.Add(new SelfNode());
            }
            else if (mode.SequenceEqual("parent".AsSpan()))
            {
                string? ancestorNamespace = null;
                string? ancestorType = null;
                var ancestorLevel = 0;
                if (PeekOpenBracket(ref r))
                {
                    var args = r.ParseArguments('[', ']', ';');
                    if (args.Count > 2 || args.Count == 0)
                    {
                        throw new ExpressionParseException(r.Position, "Too many arguments in RelativeSource syntax sugar");
                    }
                    else if (args.Count == 1)
                    {
                        if (int.TryParse(args[0], out int level))
                        {
                            ancestorType = null;
                            ancestorLevel = level;
                        }
                        else
                        {
                            var reader = new CharacterReader(args[0].AsSpan());
                            (ancestorNamespace, ancestorType) = ParseTypeName(ref reader);
                        }
                    }
                    else
                    {
                        var reader = new CharacterReader(args[0].AsSpan());
                        (ancestorNamespace, ancestorType) = ParseTypeName(ref reader);
                        ancestorLevel = int.Parse(args[1]);
                    }
                }
                nodes.Add(new AncestorNode
                {
                    Namespace = ancestorNamespace,
                    TypeName = ancestorType,
                    Level = ancestorLevel
                });
            }
            else
            {
                throw new ExpressionParseException(r.Position, "Unknown RelativeSource mode.");
            }

            return State.AfterMember;
        }

        private static TypeName ParseTypeName(
#if NET7SDK
            scoped
#endif
            ref CharacterReader r)
        {
            ReadOnlySpan<char> ns, typeName;
            ns = ReadOnlySpan<char>.Empty;
            var typeNameOrNamespace = r.ParseIdentifier();

            if (!r.End && r.TakeIf(':'))
            {
                ns = typeNameOrNamespace;
                typeName = r.ParseIdentifier();
            }
            else
            {
                typeName = typeNameOrNamespace;
            }

            return new TypeName(ns, typeName);
        }

        private static bool ParseNot(ref CharacterReader r)
        {
            return !r.End && r.TakeIf('!');
        }

        private static bool ParseMemberAccessor(ref CharacterReader r)
        {
            return !r.End && r.TakeIf('.');
        }

        private static bool ParseOpenBrace(ref CharacterReader r)
        {
            return !r.End && r.TakeIf('(');
        }

        private static bool ParseCloseBrace(ref CharacterReader r)
        {
            return !r.End && r.TakeIf(')');
        }

        private static bool PeekOpenBracket(ref CharacterReader r)
        {
            return !r.End && r.Peek == '[';
        }

        private static bool PeekOpenBrace(ref CharacterReader r)
        {
            return !r.End && r.Peek == '(';
        }

        private static bool ParseStreamOperator(ref CharacterReader r)
        {
            return !r.End && r.TakeIf('^');
        }

        private static bool ParseDollarSign(ref CharacterReader r)
        {
            return !r.End && r.TakeIf('$');
        }

        private static bool ParseSharp(ref CharacterReader r)
        {
            return !r.End && r.TakeIf('#');
        }

        private static bool ParseDot(ref CharacterReader r)
        {
            return !r.End && r.TakeIf('.');
        }

        private static bool ParseNullConditional(ref CharacterReader r)
        {
            if (r.TryPeek(2) is { } s && s.Length == 2 && s[0] == '?' && s[1] == '.')
            {
                // We need to skip the '?' but leave the '.' intact.
                r.Take();
                return true;
            }

            return false;
        }

        private enum State
        {
            Start,
            RelativeSource,
            ElementName,
            AfterMember,
            BeforeMember,
            AttachedProperty,
            Indexer,
            TypeCast,
            End,
        }

        private readonly ref struct TypeName
        {
            public TypeName(ReadOnlySpan<char> ns, ReadOnlySpan<char> typeName)
            {
                Namespace = ns;
                Type = typeName;
            }

            public readonly ReadOnlySpan<char> Namespace;
            public readonly ReadOnlySpan<char> Type;

            public void Deconstruct(out string ns, out string typeName)
            {
                ns = Namespace.ToString();
                typeName = Type.ToString();
            }
        }

        public interface INode { }

        public interface ITransformNode { }

        public class EmptyExpressionNode : INode { }

        public class PropertyNameNode : INode
        {
            public bool CanBeNull { get; set; }
            public string PropertyName { get; set; } = string.Empty;
        }

        public class AttachedPropertyNameNode : INode
        {
            public bool CanBeNull { get; set; }
            public string Namespace { get; set; } = string.Empty;
            public string TypeName { get; set; } = string.Empty;
            public string PropertyName { get; set; } = string.Empty;
        }

        public class IndexerNode : INode
        {
            public IList<string> Arguments { get; set; } = Array.Empty<string>();
        }

        public class NotNode : INode, ITransformNode { }

        public class StreamNode : INode { }

        public class SelfNode : INode { }

        public class NameNode : INode
        {
            public string Name { get; set; } = string.Empty;
        }

        public class AncestorNode : INode
        {
            public string? Namespace { get; set; }
            public string? TypeName { get; set; }
            public int Level { get; set; }
        }

        public class TypeCastNode : INode
        {
            public string Namespace { get; set; } = string.Empty;
            public string TypeName { get; set; } = string.Empty;
        }
    }
}
