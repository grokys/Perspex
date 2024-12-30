using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.ExpressionNodes.Reflection;

namespace Avalonia.Markup.Parsers
{
    /// <summary>
    /// Creates <see cref="ExpressionNode"/>s from a <see cref="BindingExpressionGrammar"/>.
    /// </summary>
    internal static class ExpressionNodeFactory
    {
        [RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
        public static List<ExpressionNode>? CreateFromAst(
            List<BindingExpressionGrammar.INode> astNodes,
            Func<string?, string, Type>? typeResolver,
            INameScope? nameScope,
            out bool isRooted)
        {
            var negated = 0;
            List<ExpressionNode>? result = null;
            ExpressionNode? node = null;

            isRooted = false;

            foreach (var astNode in astNodes)
            {
                switch (astNode)
                {
                    case BindingExpressionGrammar.AncestorNode ancestor:
                        node = LogicalAncestorNode(typeResolver, ancestor);
                        isRooted = true;
                        break;
                    case BindingExpressionGrammar.AttachedPropertyNameNode attached:
                        node = AttachedPropertyNode(typeResolver, attached);
                        break;
                    case BindingExpressionGrammar.EmptyExpressionNode:
                        node = null;
                        break;
                    case BindingExpressionGrammar.IndexerNode indexer:
                        node = new ReflectionIndexerNode((IList)indexer.Arguments);
                        break;
                    case BindingExpressionGrammar.NameNode name:
                        node = new NamedElementNode(nameScope, name.Name);
                        isRooted = true;
                        break;
                    case BindingExpressionGrammar.NotNode:
                        ++negated;
                        break;
                    case BindingExpressionGrammar.PropertyNameNode propName:
                        node = new DynamicPluginPropertyAccessorNode(propName.PropertyName, propName.CanBeNull);
                        break;
                    case BindingExpressionGrammar.SelfNode:
                        node = null;
                        isRooted = true;
                        break;
                    case BindingExpressionGrammar.StreamNode:
                        node = new DynamicPluginStreamNode();
                        break;
                    case BindingExpressionGrammar.TypeCastNode typeCast:
                        node = new ReflectionTypeCastNode(LookupType(typeResolver, typeCast.Namespace, typeCast.TypeName));
                        break;
                    default:
                        throw new Exception($"Unexpected node type '{astNode}'.");
                }

                if (node is not null)
                {
                    result ??= new(astNodes.Count);
                    result.Add(node);
                }
            }

            if (negated != 0)
            {
                result ??= new(negated);
                for (var i = 0; i < negated; ++i)
                    result.Add(new LogicalNotNode());
            }

            return result;
        }

        public static ExpressionNode? CreateRelativeSource(RelativeSource source)
        {
            return source.Mode switch
            {
                RelativeSourceMode.DataContext => new DataContextNode(),
                RelativeSourceMode.TemplatedParent => new TemplatedParentNode(),
                RelativeSourceMode.Self => null,
                RelativeSourceMode.FindAncestor when source.Tree == TreeType.Logical =>
                    new LogicalAncestorElementNode(source.AncestorType, source.AncestorLevel - 1),
                RelativeSourceMode.FindAncestor when source.Tree == TreeType.Visual =>
                    new VisualAncestorElementNode(source.AncestorType, source.AncestorLevel - 1),
                _ => throw new NotSupportedException("Unsupported RelativeSource mode.")
            };
        }

        public static ExpressionNode CreateDataContext(AvaloniaProperty? targetProperty)
        {
            return targetProperty == StyledElement.DataContextProperty ? 
                new ParentDataContextNode() :
                new DataContextNode();
        }

        private static AvaloniaPropertyAccessorNode AttachedPropertyNode(
            Func<string?, string, Type>? typeResolver,
            BindingExpressionGrammar.AttachedPropertyNameNode attached)
        {
            var type = LookupType(typeResolver, attached.Namespace, attached.TypeName);
            var property = AvaloniaPropertyRegistry.Instance.FindRegistered(type, attached.PropertyName) ??
                throw new InvalidOperationException($"Cannot find property {type}.{attached.PropertyName}.");
            return new AvaloniaPropertyAccessorNode(property, attached.CanBeNull);
        }

        private static LogicalAncestorElementNode LogicalAncestorNode(
            Func<string?, string, Type>? typeResolver,
            BindingExpressionGrammar.AncestorNode ancestor)
        {
            Type? type = null;

            if (!string.IsNullOrEmpty(ancestor.TypeName))
            {
                type = LookupType(typeResolver, ancestor.Namespace, ancestor.TypeName);
            }

            return new LogicalAncestorElementNode(type, ancestor.Level);
        }

        private static Type LookupType(
            Func<string?, string, Type>? typeResolver,
            string? @namespace,
            string? name)
        {
            if (name is null)
                throw new InvalidOperationException($"Unable to resolve unnamed type from namespace '{@namespace}'.");
            return typeResolver?.Invoke(@namespace, name) ??
                throw new InvalidOperationException($"Unable to resolve type '{@namespace}:{name}'.");
        }
    }
}
