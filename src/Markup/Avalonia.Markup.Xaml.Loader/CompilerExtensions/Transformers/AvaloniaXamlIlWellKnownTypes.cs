using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.GroupTransformers;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{

    sealed class AvaloniaXamlIlWellKnownTypes
    {
        public IXamlType RuntimeHelpers { get; }
        public IXamlType AvaloniaObject { get; }
        public IXamlType BindingPriority { get; }
        public IXamlType AvaloniaObjectExtensions { get; }
        public IXamlType AvaloniaProperty { get; }
        public IXamlType AvaloniaPropertyT { get; }
        public IXamlType StyledPropertyT { get; }
        public IXamlMethod AvaloniaObjectSetStyledPropertyValue { get; }
        public IXamlType AvaloniaAttachedPropertyT { get; }
        public IXamlType IBinding { get; }
        public IXamlType MultiBinding { get; }
        public IXamlMethod AvaloniaObjectBindMethod { get; }
        public IXamlMethod AvaloniaObjectSetValueMethod { get; }
        public IXamlType IDisposable { get; }
        public IXamlType ICommand { get; }
        public XamlTypeWellKnownTypes XamlIlTypes { get; }
        public IXamlType Transitions { get; }
        public IXamlType AssignBindingAttribute { get; }
        public IXamlType DependsOnAttribute { get; }
        public IXamlType DataTypeAttribute { get; }
        public IXamlType InheritDataTypeFromItemsAttribute { get; }
        public IXamlType InheritDataTypeFromAttribute { get; }
        public IXamlType MarkupExtensionOptionAttribute { get; }
        public IXamlType MarkupExtensionDefaultOptionAttribute { get; }
        public IXamlType ControlTemplateScopeAttribute { get; }
        public IXamlType AvaloniaListAttribute { get; }
        public IXamlType AvaloniaList { get; }
        public IXamlType OnExtensionType { get; }
        public IXamlType UnsetValueType { get; }
        public IXamlType StyledElement { get; }
        public IXamlType NameScope { get; }
        public IXamlMethod NameScopeSetNameScope { get; }
        public IXamlType INameScope { get; }
        public IXamlMethod INameScopeRegister { get; }
        public IXamlMethod INameScopeComplete { get; }
        public IXamlType IPropertyInfo { get; }
        public IXamlType ClrPropertyInfo { get; }
        public IXamlType PropertyPath { get; }
        public IXamlType PropertyPathBuilder { get; }
        public IXamlType IPropertyAccessor { get; }
        public IXamlType PropertyInfoAccessorFactory { get; }
        public IXamlType CompiledBindingPathBuilder { get; }
        public IXamlType CompiledBindingPath { get; }
        public IXamlType CompiledBindingExtension { get; }

        public IXamlType ResolveByNameExtension { get; }

        public IXamlType DataTemplate { get; }
        public IXamlType IDataTemplate { get; }
        public IXamlType ITemplateOfControl { get; }
        public IXamlType Control { get; }
        public IXamlType ItemsControl { get; }
        public IXamlType ReflectionBindingExtension { get; }

        public IXamlType RelativeSource { get; }
        public IXamlType UInt { get; }
        public IXamlType Int { get; }
        public IXamlType Long { get; }
        public IXamlType Uri { get; }
        public IXamlType IDictionaryT { get; }
        public IXamlType FontFamily { get; }
        public IXamlConstructor FontFamilyConstructorUriName { get; }
        public IXamlType Thickness { get; }
        public IXamlConstructor ThicknessFullConstructor { get; }
        public IXamlType ThemeVariant { get; }
        public IXamlType Point { get; }
        public IXamlConstructor PointFullConstructor { get; }
        public IXamlType Vector { get; }
        public IXamlConstructor VectorFullConstructor { get; }
        public IXamlType Size { get; }
        public IXamlConstructor SizeFullConstructor { get; }
        public IXamlType Matrix { get; }
        public IXamlConstructor MatrixFullConstructor { get; }
        public IXamlType CornerRadius { get; }
        public IXamlConstructor CornerRadiusFullConstructor { get; }
        public IXamlType RelativeUnit { get; }
        public IXamlType RelativePoint { get; }
        public IXamlConstructor RelativePointFullConstructor { get; }
        public IXamlType GridLength { get; }
        public IXamlConstructor GridLengthConstructorValueType { get; }
        public IXamlType Color { get; }
        public IXamlType StandardCursorType { get; }
        public IXamlType Cursor { get; }
        public IXamlConstructor CursorTypeConstructor { get; }
        public IXamlType RowDefinition { get; }
        public IXamlType RowDefinitions { get; }
        public IXamlType ColumnDefinition { get; }
        public IXamlType ColumnDefinitions { get; }
        public IXamlType Classes { get; }
        public IXamlMethod ClassesBindMethod { get; }
        public IXamlProperty StyledElementClassesProperty { get; }
        public IXamlType IBrush { get; }
        public IXamlType ImmutableSolidColorBrush { get; }
        public IXamlConstructor ImmutableSolidColorBrushConstructorColor { get; }
        public IXamlType TypeUtilities { get; }
        public IXamlType TextDecorationCollection { get; }
        public IXamlType TextDecorations { get; }
        public IXamlType TextTrimming { get; }
        public IXamlType SetterBase { get; }
        public IXamlType Setter { get; }
        public IXamlType IStyle { get; }
        public IXamlType StyleInclude { get; }
        public IXamlType ResourceInclude { get; }
        public IXamlType MergeResourceInclude { get; }
        public IXamlType IResourceDictionary { get; }
        public IXamlType ResourceDictionary { get; }
        public IXamlMethod ResourceDictionaryDeferredAdd { get; }
        public IXamlMethod ResourceDictionaryNotSharedDeferredAdd { get; }
        public IXamlMethod ResourceDictionaryEnsureCapacity { get; }
        public IXamlMethod ResourceDictionaryGetCount { get; }
        public IXamlType IThemeVariantProvider { get; }
        public IXamlType UriKind { get; }
        public IXamlConstructor UriConstructor { get; }
        public IXamlType Style { get; }
        public IXamlType Styles { get; }
        public IXamlType ControlTheme { get; }
        public IXamlType WindowTransparencyLevel { get; }
        public IXamlType IReadOnlyListOfT { get; }
        public IXamlType ControlTemplate { get; }
        public IXamlType EventHandlerT {  get; }
        public IXamlMethod GetClassProperty { get; }

        sealed internal class InteractivityWellKnownTypes
        {
            public IXamlType Interactive { get; }
            public IXamlType RoutedEvent { get; }
            public IXamlType RoutedEventArgs { get; }
            public IXamlType RoutedEventHandler { get; }
            public IXamlMethod AddHandler { get; }
            public IXamlMethod AddHandlerT { get; }

            internal InteractivityWellKnownTypes(TransformerConfiguration cfg)
            {
                var ts = cfg.TypeSystem;
                Interactive = ts.GetType("Avalonia.Interactivity.Interactive");
                RoutedEvent = ts.GetType("Avalonia.Interactivity.RoutedEvent");
                RoutedEventArgs = ts.GetType("Avalonia.Interactivity.RoutedEventArgs");
                var eventHanlderT = ts.GetType("System.EventHandler`1");
                RoutedEventHandler = eventHanlderT.MakeGenericType(RoutedEventArgs);
                AddHandler = Interactive.GetMethod(m => m.IsPublic
                    && !m.IsStatic
                    && m.Name == "AddHandler"
                    && m.Parameters.Count == 4
                    && m.Parameters[0].Equals(RoutedEvent)
                    && m.Parameters[1].Equals(cfg.WellKnownTypes.Delegate)
                    && m.Parameters[2].IsEnum
                    && m.Parameters[3].Equals(cfg.WellKnownTypes.Boolean)
                    );
                AddHandlerT = Interactive.GetMethod(m => m.IsPublic
                    && !m.IsStatic
                    && m.Name == "AddHandler"
                    && m.Parameters.Count == 4
                    && RoutedEvent.IsAssignableFrom(m.Parameters[0])
                    && m.Parameters[0].GenericArguments.Count == 1 // This is specific this case  workaround to check is generic method
                    && (cfg.WellKnownTypes.Delegate).IsAssignableFrom(m.Parameters[1])
                    && m.Parameters[2].IsEnum
                    && m.Parameters[3].Equals(cfg.WellKnownTypes.Boolean)
                );

            }
        }

        public InteractivityWellKnownTypes Interactivity { get; }

        public AvaloniaXamlIlWellKnownTypes(TransformerConfiguration cfg)
        {
            RuntimeHelpers = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.XamlIl.Runtime.XamlIlRuntimeHelpers");

            XamlIlTypes = cfg.WellKnownTypes;
            AvaloniaObject = cfg.TypeSystem.GetType("Avalonia.AvaloniaObject");
            AvaloniaObjectExtensions = cfg.TypeSystem.GetType("Avalonia.AvaloniaObjectExtensions");
            AvaloniaProperty = cfg.TypeSystem.GetType("Avalonia.AvaloniaProperty");
            AvaloniaPropertyT = cfg.TypeSystem.GetType("Avalonia.AvaloniaProperty`1");
            StyledPropertyT = cfg.TypeSystem.GetType("Avalonia.StyledProperty`1");
            AvaloniaAttachedPropertyT = cfg.TypeSystem.GetType("Avalonia.AttachedProperty`1");
            BindingPriority = cfg.TypeSystem.GetType("Avalonia.Data.BindingPriority");
            AvaloniaObjectSetStyledPropertyValue = AvaloniaObject
                .GetMethod(m => m.IsPublic && !m.IsStatic && m.Name == "SetValue"
                                 && m.Parameters.Count == 3
                                 && m.Parameters[0].Name == "StyledProperty`1"
                                 && m.Parameters[2].Equals(BindingPriority));
            IBinding = cfg.TypeSystem.GetType("Avalonia.Data.IBinding");
            MultiBinding = cfg.TypeSystem.GetType("Avalonia.Data.MultiBinding");
            IDisposable = cfg.TypeSystem.GetType("System.IDisposable");
            ICommand = cfg.TypeSystem.GetType("System.Windows.Input.ICommand");
            Transitions = cfg.TypeSystem.GetType("Avalonia.Animation.Transitions");
            AssignBindingAttribute = cfg.TypeSystem.GetType("Avalonia.Data.AssignBindingAttribute");
            DependsOnAttribute = cfg.TypeSystem.GetType("Avalonia.Metadata.DependsOnAttribute");
            DataTypeAttribute = cfg.TypeSystem.GetType("Avalonia.Metadata.DataTypeAttribute");
            InheritDataTypeFromItemsAttribute = cfg.TypeSystem.GetType("Avalonia.Metadata.InheritDataTypeFromItemsAttribute");
            InheritDataTypeFromAttribute = cfg.TypeSystem.GetType("Avalonia.Metadata.InheritDataTypeFromAttribute");
            MarkupExtensionOptionAttribute = cfg.TypeSystem.GetType("Avalonia.Metadata.MarkupExtensionOptionAttribute");
            MarkupExtensionDefaultOptionAttribute = cfg.TypeSystem.GetType("Avalonia.Metadata.MarkupExtensionDefaultOptionAttribute");
            ControlTemplateScopeAttribute = cfg.TypeSystem.GetType("Avalonia.Metadata.ControlTemplateScopeAttribute");
            AvaloniaListAttribute = cfg.TypeSystem.GetType("Avalonia.Metadata.AvaloniaListAttribute");
            AvaloniaList = cfg.TypeSystem.GetType("Avalonia.Collections.AvaloniaList`1");
            OnExtensionType = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.On");
            AvaloniaObjectBindMethod = AvaloniaObjectExtensions.GetMethod("Bind", IDisposable, false, AvaloniaObject,
                AvaloniaProperty,
                IBinding, cfg.WellKnownTypes.Object);
            UnsetValueType = cfg.TypeSystem.GetType("Avalonia.UnsetValueType");
            StyledElement = cfg.TypeSystem.GetType("Avalonia.StyledElement");
            INameScope = cfg.TypeSystem.GetType("Avalonia.Controls.INameScope");
            INameScopeRegister = INameScope.GetMethod(
                new FindMethodMethodSignature("Register", XamlIlTypes.Void,
                     XamlIlTypes.String, XamlIlTypes.Object)
                {
                    IsStatic = false,
                    DeclaringOnly = true,
                    IsExactMatch = true
                });
            INameScopeComplete = INameScope.GetMethod(
                new FindMethodMethodSignature("Complete", XamlIlTypes.Void)
                {
                    IsStatic = false,
                    DeclaringOnly = true,
                    IsExactMatch = true
                });
            NameScope = cfg.TypeSystem.GetType("Avalonia.Controls.NameScope");
            NameScopeSetNameScope = NameScope.GetMethod(new FindMethodMethodSignature("SetNameScope",
                XamlIlTypes.Void, StyledElement, INameScope)
            { IsStatic = true });
            AvaloniaObjectSetValueMethod = AvaloniaObject.GetMethod("SetValue", IDisposable,
                false, AvaloniaProperty, XamlIlTypes.Object, BindingPriority);
            IPropertyInfo = cfg.TypeSystem.GetType("Avalonia.Data.Core.IPropertyInfo");
            ClrPropertyInfo = cfg.TypeSystem.GetType("Avalonia.Data.Core.ClrPropertyInfo");
            PropertyPath = cfg.TypeSystem.GetType("Avalonia.Data.Core.PropertyPath");
            PropertyPathBuilder = cfg.TypeSystem.GetType("Avalonia.Data.Core.PropertyPathBuilder");
            IPropertyAccessor = cfg.TypeSystem.GetType("Avalonia.Data.Core.Plugins.IPropertyAccessor");
            PropertyInfoAccessorFactory = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings.PropertyInfoAccessorFactory");
            CompiledBindingPathBuilder = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings.CompiledBindingPathBuilder");
            CompiledBindingPath = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings.CompiledBindingPath");
            CompiledBindingExtension = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindingExtension");
            ResolveByNameExtension = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.ResolveByNameExtension");
            DataTemplate = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.Templates.DataTemplate");
            IDataTemplate = cfg.TypeSystem.GetType("Avalonia.Controls.Templates.IDataTemplate");
            Control = cfg.TypeSystem.GetType("Avalonia.Controls.Control");
            ITemplateOfControl = cfg.TypeSystem.GetType("Avalonia.Controls.ITemplate`1").MakeGenericType(Control);
            ItemsControl = cfg.TypeSystem.GetType("Avalonia.Controls.ItemsControl");
            ReflectionBindingExtension = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.MarkupExtensions.ReflectionBindingExtension");
            RelativeSource = cfg.TypeSystem.GetType("Avalonia.Data.RelativeSource");
            UInt = cfg.TypeSystem.GetType("System.UInt32");
            Int = cfg.TypeSystem.GetType("System.Int32");
            Long = cfg.TypeSystem.GetType("System.Int64");
            Uri = cfg.TypeSystem.GetType("System.Uri");
            IDictionaryT = cfg.TypeSystem.GetType("System.Collections.Generic.IDictionary`2");
            FontFamily = cfg.TypeSystem.GetType("Avalonia.Media.FontFamily");
            FontFamilyConstructorUriName = FontFamily.GetConstructor(new List<IXamlType> { Uri, XamlIlTypes.String });
            ThemeVariant = cfg.TypeSystem.GetType("Avalonia.Styling.ThemeVariant");
            WindowTransparencyLevel = cfg.TypeSystem.GetType("Avalonia.Controls.WindowTransparencyLevel");

            (IXamlType, IXamlConstructor) GetNumericTypeInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string name, IXamlType componentType, int componentCount)
            {
                var type = cfg.TypeSystem.GetType(name);
                var ctor = type.GetConstructor(Enumerable.Range(0, componentCount).Select(_ => componentType).ToList());

                return (type, ctor);
            }

            (Thickness, ThicknessFullConstructor) = GetNumericTypeInfo("Avalonia.Thickness", XamlIlTypes.Double, 4);
            (Point, PointFullConstructor) = GetNumericTypeInfo("Avalonia.Point", XamlIlTypes.Double, 2);
            (Vector, VectorFullConstructor) = GetNumericTypeInfo("Avalonia.Vector", XamlIlTypes.Double, 2);
            (Size, SizeFullConstructor) = GetNumericTypeInfo("Avalonia.Size", XamlIlTypes.Double, 2);
            (Matrix, MatrixFullConstructor) = GetNumericTypeInfo("Avalonia.Matrix", XamlIlTypes.Double, 6);
            (CornerRadius, CornerRadiusFullConstructor) = GetNumericTypeInfo("Avalonia.CornerRadius", XamlIlTypes.Double, 4);

            RelativeUnit = cfg.TypeSystem.GetType("Avalonia.RelativeUnit");
            RelativePoint = cfg.TypeSystem.GetType("Avalonia.RelativePoint");
            RelativePointFullConstructor = RelativePoint.GetConstructor(new List<IXamlType> { XamlIlTypes.Double, XamlIlTypes.Double, RelativeUnit });

            GridLength = cfg.TypeSystem.GetType("Avalonia.Controls.GridLength");
            GridLengthConstructorValueType = GridLength.GetConstructor(new List<IXamlType> { XamlIlTypes.Double, cfg.TypeSystem.GetType("Avalonia.Controls.GridUnitType") });
            Color = cfg.TypeSystem.GetType("Avalonia.Media.Color");
            StandardCursorType = cfg.TypeSystem.GetType("Avalonia.Input.StandardCursorType");
            Cursor = cfg.TypeSystem.GetType("Avalonia.Input.Cursor");
            CursorTypeConstructor = Cursor.GetConstructor(new List<IXamlType> { StandardCursorType });
            ColumnDefinition = cfg.TypeSystem.GetType("Avalonia.Controls.ColumnDefinition");
            ColumnDefinitions = cfg.TypeSystem.GetType("Avalonia.Controls.ColumnDefinitions");
            RowDefinition = cfg.TypeSystem.GetType("Avalonia.Controls.RowDefinition");
            RowDefinitions = cfg.TypeSystem.GetType("Avalonia.Controls.RowDefinitions");
            Classes = cfg.TypeSystem.GetType("Avalonia.Controls.Classes");
            StyledElementClassesProperty =
                StyledElement.Properties.First(x => x.Name == "Classes" && x.PropertyType.Equals(Classes));
            ClassesBindMethod = cfg.TypeSystem.GetType("Avalonia.StyledElementExtensions")
                .GetMethod("BindClass", IDisposable, false, StyledElement,
                cfg.WellKnownTypes.String,
                IBinding, cfg.WellKnownTypes.Object);

            IBrush = cfg.TypeSystem.GetType("Avalonia.Media.IBrush");
            ImmutableSolidColorBrush = cfg.TypeSystem.GetType("Avalonia.Media.Immutable.ImmutableSolidColorBrush");
            ImmutableSolidColorBrushConstructorColor = ImmutableSolidColorBrush.GetConstructor(new List<IXamlType> { UInt });
            TypeUtilities = cfg.TypeSystem.GetType("Avalonia.Utilities.TypeUtilities");
            TextDecorationCollection = cfg.TypeSystem.GetType("Avalonia.Media.TextDecorationCollection");
            TextDecorations = cfg.TypeSystem.GetType("Avalonia.Media.TextDecorations");
            TextTrimming = cfg.TypeSystem.GetType("Avalonia.Media.TextTrimming");
            SetterBase = cfg.TypeSystem.GetType("Avalonia.Styling.SetterBase");
            Setter = cfg.TypeSystem.GetType("Avalonia.Styling.Setter");
            IStyle = cfg.TypeSystem.GetType("Avalonia.Styling.IStyle");
            StyleInclude = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.Styling.StyleInclude");
            ResourceInclude = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.Styling.ResourceInclude");
            MergeResourceInclude = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.Styling.MergeResourceInclude");
            IResourceDictionary = cfg.TypeSystem.GetType("Avalonia.Controls.IResourceDictionary");
            ResourceDictionary = cfg.TypeSystem.GetType("Avalonia.Controls.ResourceDictionary");
            ResourceDictionaryDeferredAdd = ResourceDictionary.GetMethod("AddDeferred", XamlIlTypes.Void, true, XamlIlTypes.Object,
                cfg.TypeSystem.GetType("Avalonia.Controls.IDeferredContent"));
            ResourceDictionaryNotSharedDeferredAdd = ResourceDictionary.GetMethod("AddNotSharedDeferred", XamlIlTypes.Void, true, XamlIlTypes.Object,
                cfg.TypeSystem.GetType("Avalonia.Controls.IDeferredContent"));

            ResourceDictionaryEnsureCapacity = ResourceDictionary.GetMethod("EnsureCapacity", XamlIlTypes.Void, true, XamlIlTypes.Int32);
            ResourceDictionaryGetCount = ResourceDictionary.GetMethod("get_Count", XamlIlTypes.Int32, true);
            IThemeVariantProvider = cfg.TypeSystem.GetType("Avalonia.Controls.IThemeVariantProvider");
            UriKind = cfg.TypeSystem.GetType("System.UriKind");
            UriConstructor = Uri.GetConstructor(new List<IXamlType>() { cfg.WellKnownTypes.String, UriKind });
            Style = cfg.TypeSystem.GetType("Avalonia.Styling.Style");
            Styles = cfg.TypeSystem.GetType("Avalonia.Styling.Styles");
            ControlTheme = cfg.TypeSystem.GetType("Avalonia.Styling.ControlTheme");
            ControlTemplate = cfg.TypeSystem.GetType("Avalonia.Markup.Xaml.Templates.ControlTemplate");
            IReadOnlyListOfT = cfg.TypeSystem.GetType("System.Collections.Generic.IReadOnlyList`1");
            EventHandlerT = cfg.TypeSystem.GetType("System.EventHandler`1");
            Interactivity = new InteractivityWellKnownTypes(cfg);

            GetClassProperty = cfg.TypeSystem.GetType("Avalonia.StyledElementExtensions")
                .GetMethod(name: "GetClassProperty",
                returnType: AvaloniaProperty,
                allowDowncast:false,
                cfg.WellKnownTypes.String
                );
        }
    }

    static class AvaloniaXamlIlWellKnownTypesExtensions
    {
        public static AvaloniaXamlIlWellKnownTypes GetAvaloniaTypes(this AstTransformationContext ctx)
        {
            if (ctx.TryGetItem<AvaloniaXamlIlWellKnownTypes>(out var rv))
                return rv;
            ctx.SetItem(rv = new AvaloniaXamlIlWellKnownTypes(ctx.Configuration));
            return rv;
        }

        public static AvaloniaXamlIlWellKnownTypes GetAvaloniaTypes(this XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> ctx)
        {
            if (ctx.TryGetItem<AvaloniaXamlIlWellKnownTypes>(out var rv))
                return rv;
            ctx.SetItem(rv = new AvaloniaXamlIlWellKnownTypes(ctx.Configuration));
            return rv;
        }

        public static AvaloniaXamlIlWellKnownTypes GetAvaloniaTypes(this AstGroupTransformationContext ctx)
        {
            if (ctx.TryGetItem<AvaloniaXamlIlWellKnownTypes>(out var rv))
                return rv;
            ctx.SetItem(rv = new AvaloniaXamlIlWellKnownTypes(ctx.Configuration));
            return rv;
        }
    }
}
