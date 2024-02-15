using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.AstNodes;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using Avalonia.Media;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    class AvaloniaXamlIlLanguageParseIntrinsics
    {
        public static bool TryConvert(AstTransformationContext context, IXamlAstValueNode node, string text, IXamlType type, AvaloniaXamlIlWellKnownTypes types, out IXamlAstValueNode result)
        {
            bool ReturnOnParseError(string title, out IXamlAstValueNode result)
            {
                context.ReportDiagnostic(new XamlDiagnostic(
                    AvaloniaXamlDiagnosticCodes.AvaloniaIntrinsicsError,
                    XamlDiagnosticSeverity.Error,
                    title,
                    node)
                {
                    // Only one instance when we can lower Error to a Warning
                    MinSeverity = XamlDiagnosticSeverity.Warning
                });
                result = null;
                return false;
            }

            if (type.FullName == "System.TimeSpan")
            {
                var tsText = text.Trim();

                if (!TimeSpan.TryParse(tsText, CultureInfo.InvariantCulture, out var timeSpan))
                {
                    // // shorthand seconds format (ie. "0.25")
                    if (!tsText.Contains(":") && double.TryParse(tsText,
                            NumberStyles.Float | NumberStyles.AllowThousands,
                            CultureInfo.InvariantCulture, out var seconds))
                        timeSpan = TimeSpan.FromSeconds(seconds);
                    else
                    {
                        return ReturnOnParseError($"Unable to parse {text} as a time span", out result);
                    }
                }

                result = new XamlStaticOrTargetedReturnMethodCallNode(node,
                    type.FindMethod("FromTicks", type, false, types.Long),
                    new[] { new XamlConstantNode(node, types.Long, timeSpan.Ticks) });
                return true;
            }

            if (type.Equals(types.FontFamily))
            {
                result = new AvaloniaXamlIlFontFamilyAstNode(types, text, node);
                return true;
            }

            if (type.Equals(types.Thickness))
            {
                try
                {
                    var thickness = Thickness.Parse(text);

                    result = new AvaloniaXamlIlVectorLikeConstantAstNode(node, types, types.Thickness, types.ThicknessFullConstructor,
                        new[] { thickness.Left, thickness.Top, thickness.Right, thickness.Bottom });

                    return true;
                }
                catch
                {
                    return ReturnOnParseError($"Unable to parse \"{text}\" as a thickness", out result);
                }
            }

            if (type.Equals(types.Point))
            {
                try
                {
                    var point = Point.Parse(text);

                    result = new AvaloniaXamlIlVectorLikeConstantAstNode(node, types, types.Point, types.PointFullConstructor,
                        new[] { point.X, point.Y });

                    return true;
                }
                catch
                {
                    return ReturnOnParseError($"Unable to parse \"{text}\" as a point", out result);
                }
            }

            if (type.Equals(types.Vector))
            {
                try
                {
                    var vector = Vector.Parse(text);

                    result = new AvaloniaXamlIlVectorLikeConstantAstNode(node, types, types.Vector, types.VectorFullConstructor,
                        new[] { vector.X, vector.Y });

                    return true;
                }
                catch
                {
                    return ReturnOnParseError($"Unable to parse \"{text}\" as a vector", out result);
                }
            }

            if (type.Equals(types.Size))
            {
                try
                {
                    var size = Size.Parse(text);

                    result = new AvaloniaXamlIlVectorLikeConstantAstNode(node, types, types.Size, types.SizeFullConstructor,
                        new[] { size.Width, size.Height });

                    return true;
                }
                catch
                {
                    return ReturnOnParseError($"Unable to parse \"{text}\" as a size", out result);
                }
            }

            if (type.Equals(types.Matrix))
            {
                try
                {
                    var matrix = Matrix.Parse(text);

                    result = new AvaloniaXamlIlVectorLikeConstantAstNode(node, types, types.Matrix, types.MatrixFullConstructor,
                        new[] { matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.M31, matrix.M32 });

                    return true;
                }
                catch
                {
                    return ReturnOnParseError($"Unable to parse \"{text}\" as a matrix", out result);
                }
            }

            if (type.Equals(types.CornerRadius))
            {
                try
                {
                    var cornerRadius = CornerRadius.Parse(text);

                    result = new AvaloniaXamlIlVectorLikeConstantAstNode(node, types, types.CornerRadius, types.CornerRadiusFullConstructor,
                        new[] { cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomRight, cornerRadius.BottomLeft });

                    return true;
                }
                catch
                {
                    return ReturnOnParseError($"Unable to parse \"{text}\" as a corner radius", out result);
                }
            }

            if (type.Equals(types.Color))
            {
                if (!Color.TryParse(text, out Color color))
                {
                    return ReturnOnParseError($"Unable to parse \"{text}\" as a color", out result);
                }

                result = new XamlStaticOrTargetedReturnMethodCallNode(node,
                    type.GetMethod(
                        new FindMethodMethodSignature("FromUInt32", type, types.UInt) { IsStatic = true }),
                    new[] { new XamlConstantNode(node, types.UInt, color.ToUInt32()) });

                return true;
            }

            if (type.Equals(types.RelativePoint))
            {
                try
                {
                    var relativePoint = RelativePoint.Parse(text);

                    var relativePointTypeRef = new XamlAstClrTypeReference(node, types.RelativePoint, false);

                    result = new XamlAstNewClrObjectNode(node, relativePointTypeRef, types.RelativePointFullConstructor, new List<IXamlAstValueNode>
                    {
                        new XamlConstantNode(node, types.XamlIlTypes.Double, relativePoint.Point.X),
                        new XamlConstantNode(node, types.XamlIlTypes.Double, relativePoint.Point.Y),
                        new XamlConstantNode(node, types.RelativeUnit, (int) relativePoint.Unit),
                    });

                    return true;
                }
                catch
                {
                    return ReturnOnParseError($"Unable to parse \"{text}\" as a relative point", out result);
                }
            }

            if (type.Equals(types.GridLength))
            {
                try
                {
                    var gridLength = GridLength.Parse(text);

                    result = new AvaloniaXamlIlGridLengthAstNode(node, types, gridLength);

                    return true;
                }
                catch
                {
                    return ReturnOnParseError($"Unable to parse \"{text}\" as a grid length", out result);
                }
            }
            
            if (type.Equals(types.ColumnDefinition) || type.Equals(types.RowDefinition))
            {
                try
                {
                    var gridLength = GridLength.Parse(text);

                    result = new AvaloniaXamlIlGridLengthAstNode(node, types, gridLength);

                    var definitionConstructorGridLength = type.GetConstructor(new List<IXamlType> {types.GridLength});
                    var lengthNode = new AvaloniaXamlIlGridLengthAstNode(node, types, gridLength);
                    var definitionTypeRef = new XamlAstClrTypeReference(node, type, false);

                    result = new XamlAstNewClrObjectNode(node, definitionTypeRef,
                        definitionConstructorGridLength, new List<IXamlAstValueNode> {lengthNode});
    
                    return true;
                }
                catch
                {
                    return ReturnOnParseError($"Unable to parse \"{text}\" as a grid length", out result);
                }
            }

            if (type.Equals(types.Cursor))
            {
                if (TypeSystemHelpers.TryGetEnumValueNode(types.StandardCursorType, text, node, false, out var enumConstantNode))
                {
                    var cursorTypeRef = new XamlAstClrTypeReference(node, types.Cursor, false);

                    result = new XamlAstNewClrObjectNode(node, cursorTypeRef, types.CursorTypeConstructor, new List<IXamlAstValueNode> { enumConstantNode });

                    return true;
                }
            }

            if (types.IBrush.IsAssignableFrom(type))
            {
                if (Color.TryParse(text, out Color color))
                {
                    var brushTypeRef = new XamlAstClrTypeReference(node, types.ImmutableSolidColorBrush, false);

                    result = new XamlAstNewClrObjectNode(node, brushTypeRef,
                        types.ImmutableSolidColorBrushConstructorColor,
                        new List<IXamlAstValueNode> { new XamlConstantNode(node, types.UInt, color.ToUInt32()) });

                    return true;
                }
            }

            if (type.Equals(types.TextTrimming))
            {
                foreach (var property in types.TextTrimming.Properties)
                {
                    if (property.PropertyType == types.TextTrimming && property.Name.Equals(text, StringComparison.OrdinalIgnoreCase))
                    {
                        result = new XamlStaticOrTargetedReturnMethodCallNode(node, property.Getter, Enumerable.Empty<IXamlAstValueNode>());

                        return true;
                    }
                }
            }

            if (type.Equals(types.TextDecorationCollection))
            {
                foreach (var property in types.TextDecorations.Properties)
                {
                    if (property.PropertyType == types.TextDecorationCollection && property.Name.Equals(text, StringComparison.OrdinalIgnoreCase))
                    {
                        result = new XamlStaticOrTargetedReturnMethodCallNode(node, property.Getter, Enumerable.Empty<IXamlAstValueNode>());

                        return true;
                    }
                }
            }

            if (type.Equals(types.WindowTransparencyLevel))
            {
                foreach (var property in types.WindowTransparencyLevel.Properties)
                {
                    if (property.PropertyType == types.WindowTransparencyLevel && property.Name.Equals(text, StringComparison.OrdinalIgnoreCase))
                    {
                        result = new XamlStaticOrTargetedReturnMethodCallNode(node, property.Getter, Enumerable.Empty<IXamlAstValueNode>());

                        return true;
                    }
                }
            }

            if (type.Equals(types.Uri))
            {
                var uriText = text.Trim();

                var kind = ((!uriText?.StartsWith("/") == true) ? UriKind.RelativeOrAbsolute : UriKind.Relative);

                if (string.IsNullOrWhiteSpace(uriText) || !Uri.TryCreate(uriText, kind, out var _))
                {
                    return ReturnOnParseError($"Unable to parse text \"{uriText}\" as a {kind} uri", out result);
                }
                result = new XamlAstNewClrObjectNode(node
                    , new(node, types.Uri, false)
                    , types.UriConstructor
                    , new List<IXamlAstValueNode>()
                    {
                        new XamlConstantNode(node, context.Configuration.WellKnownTypes.String, uriText),
                        new XamlConstantNode(node, types.UriKind, (int)kind),
                    });
                return true;
            }

            if (type.Equals(types.ThemeVariant))
            {
                var variantText = text.Trim();
                var foundConstProperty = types.ThemeVariant.Properties.FirstOrDefault(p =>
                    p.Name == variantText && p.PropertyType == types.ThemeVariant);
                var themeVariantTypeRef = new XamlAstClrTypeReference(node, types.ThemeVariant, false);
                if (foundConstProperty is not null)
                {
                    result = new XamlStaticExtensionNode(new XamlAstObjectNode(node, node.Type), themeVariantTypeRef, foundConstProperty.Name);
                    return true;
                }
            }

            // Keep it in the end, so more specific parsers can be applied.
            var elementType = GetElementType(type, context.Configuration.WellKnownTypes);
            if (elementType is not null)
            {
                string[] items;
                // Normalize special case of Points collection. 
                if (elementType == types.Point)
                {
                    var pointParts = text.Split(new[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (pointParts.Length % 2 == 0)
                    {
                        items = new string[pointParts.Length / 2];
                        for (int i = 0; i < pointParts.Length; i += 2)
                        { 
                            items[i / 2] = string.Format(CultureInfo.InvariantCulture, "{0} {1}", pointParts[i],
                                pointParts[i + 1]);
                        }
                    }
                    else
                    {
                        return ReturnOnParseError($"Unable to parse text \"{text}\" as a Points list", out result);
                    }
                }
                else
                {
                    const StringSplitOptions trimOption = (StringSplitOptions)2; // StringSplitOptions.TrimEntries
                    var separators = new[] { "," };
                    var splitOptions = StringSplitOptions.RemoveEmptyEntries | trimOption;

                    var attribute = type.GetAllCustomAttributes().FirstOrDefault(a => a.Type == types.AvaloniaListAttribute);
                    if (attribute is not null)
                    {
                        if (attribute.Properties.TryGetValue("Separators", out var separatorsArray))
                        {
                            separators = ((Array)separatorsArray)?.OfType<string>().ToArray();
                        }

                        if (attribute.Properties.TryGetValue("SplitOptions", out var splitOptionsObj))
                        {
                            splitOptions = (StringSplitOptions)splitOptionsObj;
                        }
                    }

                    items = text.Split(separators, splitOptions & ~trimOption);
                    // Compiler targets netstandard, so we need to emulate StringSplitOptions.TrimEntries, if it was requested.
                    if (splitOptions.HasFlag(trimOption))
                    {
                        items = items.Select(i => i.Trim()).ToArray();
                    }
                }

                var nodes = new IXamlAstValueNode[items.Length];
                for (var index = 0; index < items.Length; index++)
                {
                    var success = XamlTransformHelpers.TryGetCorrectlyTypedValue(
                        context,
                        new XamlAstTextNode(node, items[index], true, context.Configuration.WellKnownTypes.String),
                        elementType, out var itemNode);
                    if (!success)
                    {
                        result = null;
                        return false;
                    }

                    nodes[index] = itemNode;
                }

                foreach (var element in nodes)
                {
                    if (!elementType.IsAssignableFrom(element.Type.GetClrType()))
                    {
                        return ReturnOnParseError($"x:Array element {element.Type.GetClrType().Name} is not assignable to the array element type {elementType.Name}", out result);
                    }
                }
                
                if (types.AvaloniaList.MakeGenericType(elementType).IsAssignableFrom(type))
                {
                    result = new AvaloniaXamlIlAvaloniaListConstantAstNode(node, types, type, elementType, nodes);
                    return true;
                }
                else if (type.IsArray)
                {
                    result = new AvaloniaXamlIlArrayConstantAstNode(node, elementType.MakeArrayType(1), elementType, nodes);
                    return true;
                }
                else if (type == context.Configuration.WellKnownTypes.IListOfT.MakeGenericType(elementType) ||
                    type == types.IReadOnlyListOfT.MakeGenericType(elementType))
                {
                    var listType = context.Configuration.WellKnownTypes.IListOfT.MakeGenericType(elementType);
                    result = new AvaloniaXamlIlArrayConstantAstNode(node, listType, elementType, nodes);
                    return true;
                }

                result = null;
                return false;
            }
            
            result = null;
            return false;
        }

        private static IXamlType GetElementType(IXamlType type, XamlTypeWellKnownTypes types)
        {
            if (type.IsArray)
            {
                return type.ArrayElementType;
            }

            return type.GetAllInterfaces().FirstOrDefault(i =>
                    i.FullName.StartsWith(types.IEnumerableT.FullName))?
                .GenericArguments[0];
        }
    }
}
