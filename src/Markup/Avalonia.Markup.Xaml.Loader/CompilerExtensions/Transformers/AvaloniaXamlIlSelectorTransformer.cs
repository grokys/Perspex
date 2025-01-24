using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Markup.Parsers;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    internal class XamlSelectorsTransformException : XamlTransformException
    {
        public XamlSelectorsTransformException(string message, IXamlLineInfo lineInfo, Exception? innerException = null)
            : base(message, lineInfo, innerException)
        {
        }
    }

    class AvaloniaXamlIlSelectorTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is not XamlAstObjectNode on ||
                !context.GetAvaloniaTypes().Style.IsAssignableFrom(on.Type.GetClrType()))
                return node;

            var pn = on.Children.OfType<XamlAstXamlPropertyValueNode>()
                .FirstOrDefault(p => p.Property.GetClrProperty().Name == "Selector");

            // Missing selector, use the object's target type if available
            if (pn == null)
            {
                // We already went through this node
                if (context.ParentNodes().FirstOrDefault() is AvaloniaXamlIlTargetTypeMetadataNode metadataNode
                    && metadataNode.Value == on)
                {
                    return node;
                }

                if (FindStyleParentObject(on, context) is { } parentObjectNode)
                {
                    return new AvaloniaXamlIlTargetTypeMetadataNode(
                        on,
                        new XamlAstClrTypeReference(node, parentObjectNode.Type.GetClrType(), false),
                        AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.Style);
                }

                return node;
            }

            if (pn.Values.Count != 1)
                throw new XamlSelectorsTransformException("Selector property should have exactly one value",
                    node);
            
            if (pn.Values[0] is XamlIlSelectorNode)
                //Deja vu. I've just been in this place before
                return node;
            
            if (!(pn.Values[0] is XamlAstTextNode tn))
                throw new XamlSelectorsTransformException("Selector property should be a text node", node);

            var selectorType = pn.Property.GetClrProperty().Getter!.ReturnType;
            var initialNode = new XamlIlSelectorInitialNode(node, selectorType);
            var avaloniaAttachedPropertyT = context.GetAvaloniaTypes().AvaloniaAttachedPropertyT;
            XamlIlSelectorNode Create(IEnumerable<SelectorGrammar.ISyntax> syntax,
                Func<string, string, XamlAstClrTypeReference> typeResolver)
            {
                XamlIlSelectorNode result = initialNode;
                XamlIlOrSelectorNode? results = null;
                foreach (var i in syntax)
                {
                    switch (i)
                    {

                        case SelectorGrammar.OfTypeSyntax ofType:
                            result = new XamlIlTypeSelector(result, typeResolver(ofType.Xmlns, ofType.TypeName).Type, true);
                            break;
                        case SelectorGrammar.IsSyntax @is:
                            result = new XamlIlTypeSelector(result, typeResolver(@is.Xmlns, @is.TypeName).Type, false);
                            break;
                        case SelectorGrammar.ClassSyntax @class:
                            result = new XamlIlStringSelector(result, XamlIlStringSelector.SelectorType.Class, @class.Class);
                            break;
                        case SelectorGrammar.NameSyntax name:
                            result = new XamlIlStringSelector(result, XamlIlStringSelector.SelectorType.Name, name.Name);
                            break;
                        case SelectorGrammar.PropertySyntax property:
                        {
                            var type = result.TargetType;

                            if (type == null)
                                throw new XamlTransformException("Property selectors must be applied to a type.", node);

                            var targetProperty =
                                type.GetAllProperties().FirstOrDefault(p => p.Name == property.Property);

                            if (targetProperty == null)
                                throw new XamlTransformException($"Cannot find '{property.Property}' on '{type}", node);

                            if (!XamlTransformHelpers.TryGetCorrectlyTypedValue(context,
                                new XamlAstTextNode(node, property.Value, type: context.Configuration.WellKnownTypes.String),
                                targetProperty, out var typedValue))
                                throw new XamlTransformException(
                                    $"Cannot convert '{property.Value}' to '{targetProperty.PropertyType.GetFqn()}",
                                    node);

                            result = new XamlIlPropertyEqualsSelector(result, targetProperty, typedValue);
                            break;
                        }
                        case SelectorGrammar.AttachedPropertySyntax attachedProperty:
                            {
                                var targetType = result.TargetType;
                                if (targetType == null)
                                {
                                    throw new XamlTransformException("Attached Property selectors must be applied to a type.",node);
                                }
                                var attachedPropertyOwnerType = typeResolver(attachedProperty.Xmlns, attachedProperty.TypeName).Type;

                                if (attachedPropertyOwnerType is null)
                                {
                                    throw new XamlTransformException($"Cannot find '{attachedProperty.Xmlns}:{attachedProperty.TypeName}",node);
                                }

                                var attachedPropertyName = attachedProperty.Property + "Property";

                                var targetPropertyField = attachedPropertyOwnerType.GetAllFields()
                                    .FirstOrDefault(f => f.IsStatic
                                        && f.IsPublic
                                        && f.Name == attachedPropertyName
                                        && f.FieldType.GenericTypeDefinition == avaloniaAttachedPropertyT
                                        );

                                if (targetPropertyField is null)
                                {
                                    throw new XamlTransformException($"Cannot find '{attachedProperty.Property}' on '{attachedPropertyOwnerType.GetFqn()}", node);
                                }

                                var targetPropertyType = XamlIlAvaloniaPropertyHelper
                                    .GetAvaloniaPropertyType(targetPropertyField, context.GetAvaloniaTypes(), node);

                                if (!XamlTransformHelpers.TryGetCorrectlyTypedValue(context,
                                    new XamlAstTextNode(node, attachedProperty.Value, type: context.Configuration.WellKnownTypes.String),
                                    targetPropertyType, out var typedValue))
                                        throw new XamlTransformException(
                                            $"Cannot convert '{attachedProperty.Value}' to '{targetPropertyType.GetFqn()}",
                                            node);

                                result = new XamlIlAttachedPropertyEqualsSelector(result, targetPropertyField, typedValue);
                                break;
                            }
                        case SelectorGrammar.ChildSyntax child:
                            result = new XamlIlCombinatorSelector(result, XamlIlCombinatorSelector.CombinatorSelectorType.Child);
                            break;
                        case SelectorGrammar.DescendantSyntax descendant:
                            result = new XamlIlCombinatorSelector(result, XamlIlCombinatorSelector.CombinatorSelectorType.Descendant);
                            break;
                        case SelectorGrammar.TemplateSyntax template:
                            result = new XamlIlCombinatorSelector(result, XamlIlCombinatorSelector.CombinatorSelectorType.Template);
                            break;
                        case SelectorGrammar.NotSyntax not:
                            result = new XamlIlNotSelector(result, Create(not.Argument, typeResolver));
                            break;
                        case SelectorGrammar.NthChildSyntax nth:
                            result = new XamlIlNthChildSelector(result, nth.Step, nth.Offset, XamlIlNthChildSelector.SelectorType.NthChild);
                            break;
                        case SelectorGrammar.NthLastChildSyntax nth:
                            result = new XamlIlNthChildSelector(result, nth.Step, nth.Offset, XamlIlNthChildSelector.SelectorType.NthLastChild);
                            break;
                        case SelectorGrammar.CommaSyntax comma:
                            if (results == null) 
                                results = new XamlIlOrSelectorNode(node, selectorType);
                            results.Add(result);
                            result = initialNode;
                            break;
                        case SelectorGrammar.NestingSyntax:
                            var parentTargetType = context.ParentNodes().OfType<AvaloniaXamlIlTargetTypeMetadataNode>().FirstOrDefault();

                            if (parentTargetType is null)
                                throw new XamlTransformException($"Cannot find parent style for nested selector.", node);

                            result = new XamlIlNestingSelector(result, parentTargetType.TargetType.GetClrType());
                            break;
                        default:
                            throw new XamlTransformException($"Unsupported selector grammar '{i.GetType()}'.", node);
                    }
                }

                if (results != null)
                {
                    results.Add(result);
                }

                return results ?? result;
            }

            IEnumerable<SelectorGrammar.ISyntax> parsed;
            try
            {
                parsed = SelectorGrammar.Parse(tn.Text);
            }
            catch (Exception e)
            {
                throw new XamlSelectorsTransformException("Unable to parse selector: " + e.Message, node, e);
            }

            var selector = Create(parsed, (p, n) 
                => TypeReferenceResolver.ResolveType(context, $"{p}:{n}", true, node, true));
            pn.Values[0] = selector;

            var templateType = GetLastTemplateTypeFromSelector(selector);

            // Empty selector, use the object's target type if available
            if (selector == initialNode)
            {
                if (FindStyleParentObject(on, context) is { } parentObjectNode)
                {
                    return new AvaloniaXamlIlTargetTypeMetadataNode(
                        on,
                        new XamlAstClrTypeReference(node, parentObjectNode.Type.GetClrType(), false),
                        AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.Style);
                }

                return node;
            }
            
            var styleNode = new AvaloniaXamlIlTargetTypeMetadataNode(on,
                new XamlAstClrTypeReference(selector, selector.TargetType!, false),
                AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.Style);

            return templateType switch
            {
                null => styleNode,
                _ => new AvaloniaXamlIlTargetTypeMetadataNode(styleNode,
                    new XamlAstClrTypeReference(styleNode, templateType, false),
                    AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.ControlTemplate)
            };
        }

        private static XamlAstObjectNode? FindStyleParentObject(XamlAstNode styleNode, AstTransformationContext context)
        {
            var avaloniaTypes = context.GetAvaloniaTypes();

            var parentNode = context
                .ParentNodes()
                .OfType<XamlAstObjectNode>()
                .FirstOrDefault(n => !avaloniaTypes.Styles.IsAssignableFrom(n.Type.GetClrType()));

            if (parentNode is not null)
            {
                var parentType = parentNode.Type.GetClrType();

                if (avaloniaTypes.StyledElement.IsAssignableFrom(parentType))
                    return parentNode;

                if (avaloniaTypes.ControlTheme.IsAssignableFrom(parentType))
                    throw new XamlTransformException("Cannot add a Style without selector to a ControlTheme.", styleNode);
            }

            return null;
        }

        private static IXamlType? GetLastTemplateTypeFromSelector(XamlIlSelectorNode? node)
        {
            while (node is not null)
            {
                if (node is XamlIlCombinatorSelector
                    {
                        SelectorType: XamlIlCombinatorSelector.CombinatorSelectorType.Template
                    })
                {
                    return node.Previous?.TargetType;
                }
                node = node.Previous;
            }

            return null;
        }
    }

    abstract class XamlIlSelectorNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        internal XamlIlSelectorNode? Previous { get; }
        public abstract IXamlType? TargetType { get; }

        public XamlIlSelectorNode(XamlIlSelectorNode? previous,
            IXamlLineInfo? info = null,
            IXamlType? selectorType = null) : base((info ?? previous)!)
        {
            Previous = previous;
            Type = selectorType == null ? previous!.Type : new XamlAstClrTypeReference(this, selectorType, false);
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
            var selectors = context.Configuration.TypeSystem.GetType("Avalonia.Styling.Selectors");
            var found = selectors.GetMethod(m => m.IsStatic && m.Parameters.Count > 0 && method(m));
            codeGen.EmitCall(found);
        }
    }
    
    class XamlIlSelectorInitialNode : XamlIlSelectorNode
    {
        public XamlIlSelectorInitialNode(IXamlLineInfo info,
            IXamlType selectorType) : base(null, info, selectorType)
        {
        }

        public override IXamlType? TargetType => null;
        protected override void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen) => codeGen.Ldnull();
    }

    class XamlIlTypeSelector : XamlIlSelectorNode
    {
        public bool Concrete { get; }

        public XamlIlTypeSelector(XamlIlSelectorNode previous, IXamlType type, bool concrete) : base(previous)
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
    
    class XamlIlStringSelector : XamlIlSelectorNode
    {
        public string String { get; set; }
        public enum SelectorType
        {
            Class,
            Name
        }

        private SelectorType _type;

        public XamlIlStringSelector(XamlIlSelectorNode previous, SelectorType type, string s) : base(previous)
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

    class XamlIlCombinatorSelector : XamlIlSelectorNode
    {
        private readonly CombinatorSelectorType _type;

        public enum CombinatorSelectorType
        {
            Child,
            Descendant,
            Template
        }
        public XamlIlCombinatorSelector(XamlIlSelectorNode previous, CombinatorSelectorType type) : base(previous)
        {
            _type = type;
        }

        public CombinatorSelectorType SelectorType => _type;
        public override IXamlType? TargetType => null;
        protected override void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            var name = _type.ToString();
            EmitCall(context, codeGen,
                m => m.Name == name && m.Parameters.Count == 1);
        }
    }

    class XamlIlNotSelector : XamlIlSelectorNode
    {
        public XamlIlSelectorNode Argument { get; }

        public XamlIlNotSelector(XamlIlSelectorNode previous, XamlIlSelectorNode argument) : base(previous)
        {
            Argument = argument;
        }

        public override IXamlType? TargetType => Previous?.TargetType;

        protected override void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            context.Emit(Argument, codeGen, Type.GetClrType());
            EmitCall(context, codeGen,
                m => m.Name == "Not" && m.Parameters.Count == 2 && m.Parameters[1].Equals(Type.GetClrType()));
        }
    }

    class XamlIlNthChildSelector : XamlIlSelectorNode
    {
        private readonly int _step;
        private readonly int _offset;
        private readonly SelectorType _type;

        public enum SelectorType
        {
            NthChild,
            NthLastChild
        }

        public XamlIlNthChildSelector(XamlIlSelectorNode previous, int step, int offset, SelectorType type) : base(previous)
        {
            _step = step;
            _offset = offset;
            _type = type;
        }

        public override IXamlType? TargetType => Previous?.TargetType;

        protected override void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            codeGen.Ldc_I4(_step);
            codeGen.Ldc_I4(_offset);
            EmitCall(context, codeGen,
                m => m.Name == _type.ToString() && m.Parameters.Count == 3);
        }
    }

    class XamlIlPropertyEqualsSelector : XamlIlSelectorNode
    {
        public XamlIlPropertyEqualsSelector(XamlIlSelectorNode previous,
            IXamlProperty property,
            IXamlAstValueNode value)
            : base(previous)
        {
            Property = property;
            Value = value;
        }

        public IXamlProperty Property { get; set; }
        public IXamlAstValueNode Value { get; set; }
        
        public override IXamlType? TargetType => Previous?.TargetType;

        protected override void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (!XamlIlAvaloniaPropertyHelper.Emit(context, codeGen, Property))
                throw new XamlX.XamlLoadException(
                    $"{Property.Name} of {Property.DeclaringType.GetFqn()} doesn't seem to be an AvaloniaProperty",
                    this);
            context.Emit(Value, codeGen, context.Configuration.WellKnownTypes.Object);
            EmitCall(context, codeGen,
                m => m.Name == "PropertyEquals"
                     && m.Parameters.Count == 3
                     && m.Parameters[1].FullName == "Avalonia.AvaloniaProperty"
                     && m.Parameters[2].Equals(context.Configuration.WellKnownTypes.Object));
        }
    }


    class XamlIlAttachedPropertyEqualsSelector : XamlIlSelectorNode
    {
        public XamlIlAttachedPropertyEqualsSelector(XamlIlSelectorNode previous,
            IXamlField propertyFiled,
            IXamlAstValueNode value)
            : base(previous)
        {
            PropertyFiled = propertyFiled;
            Value = value;
        }

        public IXamlField PropertyFiled { get; set; }
        public IXamlAstValueNode Value { get; set; }

        public override IXamlType? TargetType => Previous?.TargetType;

        protected override void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            codeGen.Ldsfld(PropertyFiled);
            context.Emit(Value, codeGen, context.Configuration.WellKnownTypes.Object);
            EmitCall(context, codeGen,
                m => m.Name == "PropertyEquals"
                     && m.Parameters.Count == 3
                     && m.Parameters[1].FullName == "Avalonia.AvaloniaProperty"
                     && m.Parameters[2].Equals(context.Configuration.WellKnownTypes.Object));
        }
    }

    class XamlIlOrSelectorNode : XamlIlSelectorNode
    {
        List<XamlIlSelectorNode> _selectors = new List<XamlIlSelectorNode>();
        public XamlIlOrSelectorNode(IXamlLineInfo info, IXamlType selectorType) : base(null, info, selectorType)
        {
        }

        public void Add(XamlIlSelectorNode node)
        {
            _selectors.Add(node);
        }
        
        public override IXamlType? TargetType
        {
            get
            {
                IXamlType? result = null;

                foreach (var selector in _selectors)
                {
                    if (selector.TargetType is not { } targetType)
                    {
                        return null;
                    }
                    else if (result == null)
                    {
                        result = targetType;
                    }
                    else
                    {
                        while (result is not null && !result.IsAssignableFrom(selector.TargetType))
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
            if (_selectors.Count == 0)
                throw new XamlX.XamlLoadException("Invalid selector count", this);
            if (_selectors.Count == 1)
            {
                _selectors[0].Emit(context, codeGen);
                return;
            }
            var listType = context.Configuration.TypeSystem.GetType("System.Collections.Generic.List`1")
                .MakeGenericType(base.Type.GetClrType());
            var add = listType.GetMethod("Add", context.Configuration.WellKnownTypes.Void, false, Type.GetClrType());
            codeGen
                .Newobj(listType.GetConstructor());
            foreach (var s in _selectors)
            {
                codeGen.Dup();
                context.Emit(s, codeGen, Type.GetClrType());
                codeGen.EmitCall(add, true);
            }

            EmitCall(context, codeGen,
                m => m.Name == "Or" && m.Parameters.Count == 1 && m.Parameters[0].Name.StartsWith("IReadOnlyList"));
        }
    }

    class XamlIlNestingSelector : XamlIlSelectorNode
    {
        public XamlIlNestingSelector(XamlIlSelectorNode previous, IXamlType targetType)
            : base(previous)
        {
            TargetType = targetType;
        }

        public override IXamlType TargetType { get; }
        protected override void DoEmit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            EmitCall(context, codeGen,
                m => m.Name == "Nesting" && m.Parameters.Count == 1);
        }
    }
}
