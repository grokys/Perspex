using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Markup.Parsers;
using Avalonia.Styling;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    using static Avalonia.Markup.Parsers.ContainerQueryGrammar;
    using XamlLoadException = XamlX.XamlLoadException;
    using XamlParseException = XamlX.XamlParseException;
    class AvaloniaXamlIlQueryTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is not XamlAstObjectNode on ||
                !context.GetAvaloniaTypes().Container.IsAssignableFrom(on.Type.GetClrType()))
                return node;

            var pn = on.Children.OfType<XamlAstXamlPropertyValueNode>()
                .FirstOrDefault(p => p.Property.GetClrProperty().Name == "Query");

            if (pn == null || pn.Property.GetClrProperty().Getter is not { } getter)
                return node;

            if (pn.Values.Count != 1)
                throw new XamlParseException("Query property should should have exactly one value", node);
            
            if (pn.Values[0] is XamlIlQueryNode)
                //Deja vu. I've just been in this place before
                return node;
            
            if (!(pn.Values[0] is XamlAstTextNode tn))
                throw new XamlParseException("Query property should be a text node", node);

            var queryType = getter.ReturnType;
            var initialNode = new XamlIlQueryInitialNode(node, queryType);
            var avaloniaAttachedPropertyT = context.GetAvaloniaTypes().AvaloniaAttachedPropertyT;
            XamlIlQueryNode Create(IEnumerable<ISyntax> syntax)
            {
                XamlIlQueryNode result = initialNode;
                XamlIlOrQueryNode? results = null;
                XamlIlAndQueryNode? andNode = null;
                foreach (var i in syntax)
                {
                    switch (i)
                    {
                        case ContainerQueryGrammar.WidthSyntax width:
                            result = new XamlIlWidthQuery(result, width);
                            break;
                        case ContainerQueryGrammar.HeightSyntax height:
                            result = new XamlIlHeightQuery(result, height);
                            break;
                        case ContainerQueryGrammar.OrSyntax or:
                            if (results == null)
                                results = new XamlIlOrQueryNode(node, queryType);
                            if (andNode != null && result == initialNode)
                                throw new XamlParseException($"Previously opened And node is not closed.", node);
                            results.Add(andNode ?? result);
                            result = initialNode;
                            andNode = null;
                            break;
                        case ContainerQueryGrammar.AndSyntax and:
                            if (andNode == null)
                                andNode = new XamlIlAndQueryNode(node, queryType);
                            andNode.Add(result);
                            result = initialNode;
                            break;
                        default:
                            throw new XamlParseException($"Unsupported query grammar '{i.GetType()}'.", node);
                    }

                    if (andNode != null && result != initialNode)
                    {
                        andNode.Add(result);
                    }
                }

                if (results != null && result != null)
                {
                    results.Add(result);
                }

                return results ?? result ?? initialNode;
            }

            IEnumerable<ISyntax> parsed;
            try
            {
                parsed = ContainerQueryGrammar.Parse(tn.Text);
            }
            catch (Exception e)
            {
                throw new XamlParseException("Unable to parse query: " + e.Message, node);
            }

            var query = Create(parsed);
            pn.Values[0] = query;

            return new AvaloniaXamlIlTargetTypeMetadataNode(on,
                new XamlAstClrTypeReference(query, query.TargetType!, false),
                AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.Container);
        }
    }

    abstract class XamlIlQueryNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        internal XamlIlQueryNode? Previous { get; }
        public abstract IXamlType? TargetType { get; }

        public XamlIlQueryNode(XamlIlQueryNode? previous,
            IXamlLineInfo? info = null,
            IXamlType? queryType = null) : base((info ?? previous)!)
        {
            Previous = previous;
            Type = queryType == null ? previous!.Type : new XamlAstClrTypeReference(this, queryType, false);
        }

        public IXamlAstTypeReference Type { get; }

        public virtual XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (Previous != null)
                context.Emit(Previous, codeGen, Type.GetClrType());
            DoEmit(context, codeGen);
            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }
        
        protected abstract void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen);

        protected void EmitCall(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen, Func<IXamlMethod, bool> method)
        {
            var queries = context.Configuration.TypeSystem.GetType("Avalonia.Styling.StyleQueries");
            var found = queries.FindMethod(m => m.IsStatic && m.Parameters.Count > 0 && method(m));
            if(found == null)
                throw new XamlTypeSystemException(
                                $"Unable to find {TargetType} in Avalonia.Styling.StyleQueries");
            codeGen.EmitCall(found);
        }
    }
    
    class XamlIlQueryInitialNode : XamlIlQueryNode
    {
        public XamlIlQueryInitialNode(IXamlLineInfo info,
            IXamlType queryType) : base(null, info, queryType)
        {
        }

        public override IXamlType? TargetType => null;
        protected override void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen) => codeGen.Ldnull();
    }

    class XamlIlTypeQuery : XamlIlQueryNode
    {
        public bool Concrete { get; }

        public XamlIlTypeQuery(XamlIlQueryNode previous, IXamlType type, bool concrete) : base(previous)
        {
            TargetType = type;
            Concrete = concrete;
        }

        public override IXamlType TargetType { get; }
        protected override void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            var name = Concrete ? "OfType" : "Is";
            codeGen.Ldtype(TargetType);
            EmitCall(context, codeGen,
                m => m.Name == name && m.Parameters.Count == 2 && m.Parameters[1].FullName == "System.Type");
        }
    }
    
    class XamlIlStringQuery : XamlIlQueryNode
    {
        public string String { get; set; }
        public enum QueryType
        {
            Class,
            Name
        }

        private QueryType _type;

        public XamlIlStringQuery(XamlIlQueryNode previous, QueryType type, string s) : base(previous)
        {
            _type = type;
            String = s;
        }

        public override IXamlType? TargetType => Previous?.TargetType;
        protected override void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            codeGen.Ldstr(String);
            var name = _type.ToString();
            EmitCall(context, codeGen,
                m => m.Name == name && m.Parameters.Count == 2 && m.Parameters[1].FullName == "System.String");
        }
    }

    class XamlIlCombinatorQuery : XamlIlQueryNode
    {
        private readonly CombinatorQueryType _type;

        public enum CombinatorQueryType
        {
            Child,
            Descendant,
            Template
        }
        public XamlIlCombinatorQuery(XamlIlQueryNode previous, CombinatorQueryType type) : base(previous)
        {
            _type = type;
        }

        public CombinatorQueryType QueryType => _type;
        public override IXamlType? TargetType => null;
        protected override void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            var name = _type.ToString();
            EmitCall(context, codeGen,
                m => m.Name == name && m.Parameters.Count == 1);
        }
    }
    
    class XamlIlWidthQuery : XamlIlQueryNode
    {
        private ContainerQueryGrammar.WidthSyntax _argument;
        
        public XamlIlWidthQuery(XamlIlQueryNode previous, ContainerQueryGrammar.WidthSyntax argument) : base(previous)
        {
            _argument = argument;
        }

        public override IXamlType? TargetType => Previous?.TargetType;
        protected override void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            codeGen.Ldc_I4((int)_argument.Operator);
            codeGen.Ldc_R8(_argument.Value);
            EmitCall(context, codeGen,
                m => m.Name == "Width" && m.Parameters.Count == 3);
        }
    }
    
    class XamlIlHeightQuery : XamlIlQueryNode
    {
        private ContainerQueryGrammar.HeightSyntax _argument;
        
        public XamlIlHeightQuery(XamlIlQueryNode previous, ContainerQueryGrammar.HeightSyntax argument) : base(previous)
        {
            _argument = argument;
        }

        public override IXamlType? TargetType => Previous?.TargetType;
        protected override void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            codeGen.Ldc_I4((int)_argument.Operator);
            codeGen.Ldc_R8(_argument.Value);
            EmitCall(context, codeGen,
                m => m.Name == "Height" && m.Parameters.Count == 3);
        }
    }

    class XamlIlOrQueryNode : XamlIlQueryNode
    {
        List<XamlIlQueryNode> _queries = new List<XamlIlQueryNode>();
        public XamlIlOrQueryNode(IXamlLineInfo info, IXamlType queryType) : base(null, info, queryType)
        {
        }

        public void Add(XamlIlQueryNode node)
        {
            _queries.Add(node);
        }
        
        public override IXamlType? TargetType
        {
            get
            {
                IXamlType? result = null;

                foreach (var query in _queries)
                {
                    if (query.TargetType == null)
                    {
                        return null;
                    }
                    else if (result == null)
                    {
                        result = query.TargetType;
                    }
                    else
                    {
                        while (result?.IsAssignableFrom(query.TargetType) == false)
                        {
                            result = result.BaseType;
                        }
                    }
                }

                return result;
            }
        }

        protected override void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (_queries.Count == 0)
                throw new XamlLoadException("Invalid query count", this);
            if (_queries.Count == 1)
            {
                _queries[0].Emit(context, codeGen);
                return;
            }

            if (context.Configuration.TypeSystem.FindType("System.Collections.Generic.List`1") is not { } type)
                return;

            IXamlType listType = type.MakeGenericType(base.Type.GetClrType());
            var add = listType.FindMethod("Add", context.Configuration.WellKnownTypes.Void, false, Type.GetClrType());
            if (add == null)
                return;

            var ctor = listType.FindConstructor();
            if (ctor == null)
            {
                return;
            }
            codeGen
                .Newobj(ctor);
            foreach (var s in _queries)
            {
                codeGen.Dup();
                context.Emit(s, codeGen, Type.GetClrType());
                codeGen.EmitCall(add, true);
            }

            EmitCall(context, codeGen,
                m => m.Name == "Or" && m.Parameters.Count == 1 && m.Parameters[0].Name.StartsWith("IReadOnlyList"));
        }
    }

    class XamlIlAndQueryNode : XamlIlQueryNode
    {
        List<XamlIlQueryNode> _queries = new List<XamlIlQueryNode>();
        public XamlIlAndQueryNode(IXamlLineInfo info, IXamlType queryType) : base(null, info, queryType)
        {
        }

        public void Add(XamlIlQueryNode node)
        {
            _queries.Add(node);
        }

        public override IXamlType? TargetType
        {
            get
            {
                IXamlType? result = null;

                foreach (var query in _queries)
                {
                    if (query.TargetType == null)
                    {
                        return null;
                    }
                    else if (result == null)
                    {
                        result = query.TargetType;
                    }
                    else
                    {
                        while (result?.IsAssignableFrom(query.TargetType) == false)
                        {
                            result = result.BaseType;
                        }
                    }
                }

                return result;
            }
        }

        protected override void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (_queries.Count == 0)
                throw new XamlLoadException("Invalid query count", this);
            if (_queries.Count == 1)
            {
                _queries[0].Emit(context, codeGen);
                return;
            }

            if (context.Configuration.TypeSystem.FindType("System.Collections.Generic.List`1") is not { } type)
                return;

            IXamlType listType = type.MakeGenericType(base.Type.GetClrType());
            var add = listType.FindMethod("Add", context.Configuration.WellKnownTypes.Void, false, Type.GetClrType());
            if (add == null)
                return;

            var ctor = listType.FindConstructor();
            if (ctor == null)
            {
                return;
            }
            codeGen
                .Newobj(ctor);
            foreach (var s in _queries)
            {
                codeGen.Dup();
                context.Emit(s, codeGen, Type.GetClrType());
                codeGen.EmitCall(add, true);
            }

            EmitCall(context, codeGen,
                m => m.Name == "And" && m.Parameters.Count == 1 && m.Parameters[0].Name.StartsWith("IReadOnlyList"));
        }
    }
}
