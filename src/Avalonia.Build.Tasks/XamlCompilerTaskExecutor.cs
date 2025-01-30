using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;
using Avalonia.Platform;
using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using XamlX;
using XamlX.Ast;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.TypeSystem;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;
using XamlX.IL;
using XamlX.IL.Emitters;

namespace Avalonia.Build.Tasks
{
    public static partial class XamlCompilerTaskExecutor
    {
        private const string CompiledAvaloniaXamlNamespace = "CompiledAvaloniaXaml";

        static bool CheckXamlName(IResource r)
        {
            var name = r.Name;
            return name.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".xaml",StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".paml", StringComparison.OrdinalIgnoreCase);
        }

        public class CompileResult
        {
            public bool Success { get; set; }
            public bool WrittenFile { get; }

            public CompileResult(bool success, bool writtenFile = false)
            {
                Success = success;
                WrittenFile = writtenFile;
            }
        }

        internal static CompileResult Compile(IBuildEngine engine,
            string input, string output,
            string refInput, string refOutput,
            string[] references, string projectDirectory,
            bool verifyIl, bool defaultCompileBindings, MessageImportance logImportance,
            XamlCompilerDiagnosticsFilter diagnosticsFilter, string strongNameKey,
            bool skipXamlCompilation, bool debuggerLaunch, bool verboseExceptions)
        {
            try
            {
                references = references.Where(r => !r.ToLowerInvariant().EndsWith("avalonia.build.tasks.dll")).ToArray();
                using var typeSystem = new CecilTypeSystem(references, input);
                using var refTypeSystem = !string.IsNullOrWhiteSpace(refInput) && File.Exists(refInput) ? new CecilTypeSystem(references, refInput) : null;

                var asm = typeSystem.TargetAssemblyDefinition;
                var refAsm = refTypeSystem?.TargetAssemblyDefinition;
                if (!skipXamlCompilation)
                {
	                var compileRes = CompileCore(
                        engine, typeSystem, projectDirectory, verifyIl,
                        defaultCompileBindings, logImportance, diagnosticsFilter,
                        debuggerLaunch, verboseExceptions);
	                if (compileRes == null)
	                    return new CompileResult(true);
	                if (compileRes == false)
	                    return new CompileResult(false);

	                if (refTypeSystem is not null)
	                {
	                    var refCompileRes = CompileCoreForRefAssembly(engine, typeSystem, refTypeSystem);
	                    if (refCompileRes == false)
	                        return new CompileResult(false);
	                }
                }

                var writerParameters = new WriterParameters { WriteSymbols = asm.MainModule.HasSymbols };
                if (!string.IsNullOrWhiteSpace(strongNameKey))
	                writerParameters.StrongNameKeyBlob = File.ReadAllBytes(strongNameKey);

                asm.Write(output, writerParameters);

                var refWriterParameters = new WriterParameters { WriteSymbols = false };
                if (!string.IsNullOrWhiteSpace(strongNameKey))
	                writerParameters.StrongNameKeyBlob = File.ReadAllBytes(strongNameKey);
                refAsm?.Write(refOutput, refWriterParameters);

                return new CompileResult(true, true);
            }
            catch (Exception ex)
            {
                engine.LogError(AvaloniaXamlDiagnosticCodes.Unknown, "", ex);
                return new CompileResult(false);
            }
        }

        static bool? CompileCore(IBuildEngine engine, CecilTypeSystem typeSystem,
            string projectDirectory, bool verifyIl,
            bool defaultCompileBindings,
            MessageImportance logImportance,
            XamlCompilerDiagnosticsFilter diagnosticsFilter,
            bool debuggerLaunch,
            bool verboseExceptions)
        {
            if (debuggerLaunch)
            {
                Avalonia.Utilities.DebuggerHelper.Launch(m => engine.LogMessage(m, MessageImportance.High));
            }

            // Some transformers might need to parse "avares://" Uri.
            AssetLoader.RegisterResUriParsers();

            var asm = typeSystem.TargetAssemblyDefinition;
            var avares = new AvaloniaResources(asm, projectDirectory);
            if (avares.Resources.Count(CheckXamlName) == 0)
                // Nothing to do
                return null;

            if (typeSystem.FindType("System.Reflection.AssemblyMetadataAttribute") is {} asmMetadata)
            {
                var ctor = asm.MainModule.ImportReference(typeSystem.GetTypeReference(asmMetadata).Resolve()
                    .GetConstructors().First(c => c.Parameters.Count == 2).Resolve());
                var strType = asm.MainModule.TypeSystem.String;
                var arg1 = new CustomAttributeArgument(strType, "AvaloniaUseCompiledBindingsByDefault");
                var arg2 = new CustomAttributeArgument(strType, defaultCompileBindings.ToString());
                asm.CustomAttributes.Add(new CustomAttribute(ctor) { ConstructorArguments = { arg1, arg2 } });
            }

            TypeDefinition AddClass(string name, TypeAttributes extraAttributes = 0)
            {
                var typeDef = new TypeDefinition(
                    CompiledAvaloniaXamlNamespace,
                    name,
                    TypeAttributes.Class | extraAttributes,
                    asm.MainModule.TypeSystem.Object);
                asm.MainModule.Types.Add(typeDef);
                return typeDef;
            }

            var clrPropertiesDef = AddClass("XamlIlHelpers");
            var indexerAccessorClosure = AddClass("!IndexerAccessorFactoryClosure");
            var trampolineBuilder = AddClass("XamlIlTrampolines");
            var dynamicSettersBuilder = typeSystem.CreateTypeBuilder(AddClass("XamlDynamicSetters"));

            var (xamlLanguage , emitConfig) = AvaloniaXamlIlLanguage.Configure(typeSystem);
            var diagnostics = new List<XamlDiagnostic>();
            var diagnosticsHandler = new XamlDiagnosticsHandler()
            {
                HandleDiagnostic = diagnostic =>
                {
                    var newSeverity = diagnosticsFilter.Handle(diagnostic);
                    diagnostic = diagnostic with { Severity = newSeverity };
                    diagnostics.Add(diagnostic);
                    return newSeverity;
                },
                CodeMappings = AvaloniaXamlDiagnosticCodes.XamlXDiagnosticCodeToAvalonia,
                ExceptionFormatter = ex => ex.FormatException(verboseExceptions)
            };

            var compilerConfig = new AvaloniaXamlIlCompilerConfiguration(typeSystem,
                typeSystem.TargetAssembly,
                xamlLanguage,
                XamlXmlnsMappings.Resolve(typeSystem, xamlLanguage),
                AvaloniaXamlIlLanguage.CustomValueConverter,
                new XamlIlClrPropertyInfoEmitter(typeSystem.CreateTypeBuilder(clrPropertiesDef)),
                new XamlIlPropertyInfoAccessorFactoryEmitter(typeSystem.CreateTypeBuilder(indexerAccessorClosure)),
                new XamlIlTrampolineBuilder(typeSystem.CreateTypeBuilder(trampolineBuilder)),
                new DeterministicIdGenerator(),
                diagnosticsHandler);


            var contextDef = AddClass("XamlIlContext");

            var contextClass = XamlILContextDefinition.GenerateContextClass(typeSystem.CreateTypeBuilder(contextDef), typeSystem,
                xamlLanguage, emitConfig);

            var compiler = new AvaloniaXamlIlCompiler(compilerConfig, emitConfig, contextClass)
            {
                EnableIlVerification = verifyIl,
                DefaultCompileBindings = defaultCompileBindings,
                DynamicSetterContainerProvider = new DefaultXamlDynamicSetterContainerProvider(dynamicSettersBuilder)
            };

            var editorBrowsableAttribute = typeSystem
                .GetTypeReference(typeSystem.FindType("System.ComponentModel.EditorBrowsableAttribute"))
                .Resolve();
            var editorBrowsableCtor =
                asm.MainModule.ImportReference(editorBrowsableAttribute.GetConstructors()
                    .First(c => c.Parameters.Count == 1));

            var runtimeHelpers = typeSystem.GetType("Avalonia.Markup.Xaml.XamlIl.Runtime.XamlIlRuntimeHelpers");
            var createRootServiceProviderMethod = asm.MainModule.ImportReference(
                typeSystem.GetTypeReference(runtimeHelpers).Resolve().Methods
                    .First(x => x.Name == "CreateRootServiceProviderV3"));
            var serviceProviderType = createRootServiceProviderMethod.ReturnType;

            var loaderDispatcherDef = AddClass("!XamlLoader", TypeAttributes.Public);

            loaderDispatcherDef.CustomAttributes.Add(new CustomAttribute(editorBrowsableCtor)
            {
                ConstructorArguments = {new CustomAttributeArgument(editorBrowsableCtor.Parameters[0].ParameterType, 1)}
            });


            var loaderDispatcherMethod = new MethodDefinition("TryLoad",
                MethodAttributes.Static | MethodAttributes.Public,
                asm.MainModule.TypeSystem.Object)
            {
                Parameters =
                {
                    new ParameterDefinition(serviceProviderType),
                    new ParameterDefinition(asm.MainModule.TypeSystem.String)
                },
            };
            var loaderDispatcherMethodOld = new MethodDefinition("TryLoad",
                MethodAttributes.Static | MethodAttributes.Public,
                asm.MainModule.TypeSystem.Object)
            {
                Parameters =
                {
                    new ParameterDefinition(asm.MainModule.TypeSystem.String)
                },
                Body =
                {
                    Instructions =
                    {
                        Instruction.Create(OpCodes.Ldnull),
                        Instruction.Create(OpCodes.Ldarg_0),
                        Instruction.Create(OpCodes.Call, loaderDispatcherMethod),
                        Instruction.Create(OpCodes.Ret)
                    }
                }
            };
            loaderDispatcherDef.Methods.Add(loaderDispatcherMethod);
            loaderDispatcherDef.Methods.Add(loaderDispatcherMethodOld);


            var stringEquals = asm.MainModule.ImportReference(asm.MainModule.TypeSystem.String.Resolve().Methods.First(
                m =>
                    m.IsStatic && m.Name == "Equals" && m.Parameters.Count == 3 &&
                    m.ReturnType.FullName == "System.Boolean"
                    && m.Parameters[0].ParameterType.FullName == "System.String"
                    && m.Parameters[1].ParameterType.FullName == "System.String"
                    && m.Parameters[2].ParameterType.FullName == "System.StringComparison"));
            
            bool CompileGroup(IResourceGroup group)
            {
                var typeDef = AddClass("!" + group.Name, TypeAttributes.Public);
                var transformFailed = false;

                typeDef.CustomAttributes.Add(new CustomAttribute(editorBrowsableCtor)
                {
                    ConstructorArguments = {new CustomAttributeArgument(editorBrowsableCtor.Parameters[0].ParameterType, 1)}
                });
                var builder = typeSystem.CreateTypeBuilder(typeDef);

                IReadOnlyCollection<XamlDocumentResource> parsedXamlDocuments = new List<XamlDocumentResource>();
                foreach (var res in group.Resources.Where(CheckXamlName).OrderBy(x => x.FilePath.ToLowerInvariant()))
                {
                    engine.LogMessage($"XAMLIL: {res.Name} -> {res.Uri}", logImportance);

                    try
                    {
                        // StreamReader is needed here to handle BOM
                        var xaml = new StreamReader(new MemoryStream(res.FileContents)).ReadToEnd();
                        var parsed = XDocumentXamlParser.Parse(xaml);
                        parsed.Document = res.FilePath;

                        var initialRoot = (XamlAstObjectNode)parsed.Root;
                        
                        
                        var precompileDirective = initialRoot.Children.OfType<XamlAstXmlDirective>()
                            .FirstOrDefault(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == "Precompile");
                        if (precompileDirective != null)
                        {
                            var precompileText = (precompileDirective.Values[0] as XamlAstTextNode)?.Text.Trim()
                                .ToLowerInvariant();
                            if (precompileText == "false")
                                continue;
                            if (precompileText != "true")
                                throw new XamlParseException("Invalid value for x:Precompile", precompileDirective);
                        }

                        var classModifierDirective = initialRoot.Children.OfType<XamlAstXmlDirective>()
                            .FirstOrDefault(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == "ClassModifier");
                        bool? classModifierPublic = null;
                        if (classModifierDirective != null)
                        {
                            var classModifierText = (classModifierDirective.Values[0] as XamlAstTextNode)?.Text.Trim()
                                .ToLowerInvariant();
                            if ("Public".Equals(classModifierText, StringComparison.OrdinalIgnoreCase))
                                classModifierPublic = true;
                            // XAML spec uses "Public" and "NotPublic" values,
                            // When WPF documentation uses "public" and "internal".
                            else if ("NotPublic".Equals(classModifierText, StringComparison.OrdinalIgnoreCase)
                                     || "Internal".Equals(classModifierText, StringComparison.OrdinalIgnoreCase))
                                classModifierPublic = false;
                            else
                                throw new XamlParseException("Invalid value for x:ClassModifier. Expected value are: Public, NotPublic (internal).", classModifierDirective);
                        }

                        var classDirective = initialRoot.Children.OfType<XamlAstXmlDirective>()
                            .FirstOrDefault(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == "Class");
                        IXamlType classType = null;
                        if (classDirective != null)
                        {
                            if (classDirective.Values.Count != 1 || !(classDirective.Values[0] is XamlAstTextNode tn))
                                throw new XamlParseException("x:Class should have a string value", classDirective);
                            classType = typeSystem.TargetAssembly.FindType(tn.Text);

                            if (classType == null)
                                throw new XamlParseException($"Unable to find type `{tn.Text}`", classDirective);

                            var isClassPublic = typeSystem.GetTypeReference(classType).Resolve().IsPublic;
                            classModifierPublic ??= isClassPublic;

                            // We do not really need x:ClassModifier support for x:Class, but we can at least use it for validation here.
                            if (classModifierPublic != isClassPublic)
                            {
                                throw new XamlParseException(
                                    "XAML file x:ClassModifier doesn't match the x:Class type modifiers.",
                                    precompileDirective);
                            }

                            compiler.OverrideRootType(parsed,
                                new XamlAstClrTypeReference(classDirective, classType, false));
                            initialRoot.Children.Remove(classDirective);
                        }

                        compiler.Transform(parsed);

                        var populateName = classType == null ? "Populate:" + res.Name : "!XamlIlPopulate";
                        var buildName = classType == null ? "Build:" + res.Name : null;
                        var classTypeDefinition =
                            classType == null ? null : typeSystem.GetTypeReference(classType).Resolve();

                        // All XAML files are public by default.
                        classModifierPublic ??= true;

                        var populateBuilder = classTypeDefinition == null ?
                            builder :
                            typeSystem.CreateTypeBuilder(classTypeDefinition);

                        ((List<XamlDocumentResource>)parsedXamlDocuments).Add(new XamlDocumentResource(
                            parsed, res.Uri, res, classType,
                            classModifierPublic.Value,
                            () => new XamlDocumentTypeBuilderProvider(
                                populateBuilder,
                                compiler.DefinePopulateMethod(
                                    populateBuilder,
                                    parsed,
                                    populateName,
                                    classTypeDefinition == null && classModifierPublic.Value
                                        ? XamlVisibility.Public
                                        : XamlVisibility.Private),
                                buildName == null ? null : builder,
                                buildName == null ?
                                    null :
                                    compiler.DefineBuildMethod(
                                        builder,
                                        parsed,
                                        buildName,
                                        classModifierPublic.Value ? XamlVisibility.Public : XamlVisibility.Assembly))));
                    }
                    catch (Exception e)
                    {
                        transformFailed = true;
                        engine.LogError(AvaloniaXamlDiagnosticCodes.TransformError, res.FilePath, e);
                    }
                }

                try
                {
                    if (!transformFailed)
                    {
                        compiler.TransformGroup(parsedXamlDocuments);
                    }
                }
                catch (Exception e)
                {
                    transformFailed = true;
                    engine.LogError(AvaloniaXamlDiagnosticCodes.TransformError, "", e);
                }

                var hasAnyError = ReportDiagnostics(engine, diagnostics) || transformFailed;
                if (hasAnyError)
                {
                    return false;
                }

                foreach (var document in parsedXamlDocuments)
                {
                    var res = (IResource)document.FileSource!;

                    if (document.Usage == XamlDocumentUsage.Merged && !document.IsPublic)
                    {
                        res.Remove();
                        continue;
                    }

                    var parsed = document.XamlDocument;
                    var classType = document.ClassType;
                    var populateBuilder = document.TypeBuilderProvider.PopulateDeclaringType;

                    try
                    {
                        var classTypeDefinition =
                            classType == null ? null : typeSystem.GetTypeReference(classType).Resolve();

                        compiler.Compile(parsed,
                            contextClass,
                            document.TypeBuilderProvider.PopulateMethod,
                            populateBuilder,
                            document.TypeBuilderProvider.BuildMethod,
                            builder,
                            builder.DefineSubType(compilerConfig.WellKnownTypes.Object, "NamespaceInfo:" + res.Name, XamlVisibility.Assembly),
                            res.Uri, res
                        );

                        if (classTypeDefinition != null)
                        {
                            var compiledPopulateMethod = typeSystem.GetTypeReference(populateBuilder).Resolve()
                                .Methods.First(m => m.Name == document.TypeBuilderProvider.PopulateMethod.Name);

                            var designLoaderFieldType = typeSystem
                                .GetType("System.Action`1")
                                .MakeGenericType(typeSystem.GetType("System.Object"));

                            var designLoaderFieldTypeReference = (GenericInstanceType)typeSystem.GetTypeReference(designLoaderFieldType);
                            designLoaderFieldTypeReference.GenericArguments[0] =
                                asm.MainModule.ImportReference(designLoaderFieldTypeReference.GenericArguments[0]);
                            designLoaderFieldTypeReference = (GenericInstanceType)
                                asm.MainModule.ImportReference(designLoaderFieldTypeReference);
                            
                            var designLoaderLoad =
                                typeSystem.GetMethodReference(
                                    designLoaderFieldType.Methods.First(m => m.Name == "Invoke"));
                            designLoaderLoad =
                                asm.MainModule.ImportReference(designLoaderLoad);
                            designLoaderLoad.DeclaringType = designLoaderFieldTypeReference;

                            var designLoaderField = new FieldDefinition("!XamlIlPopulateOverride",
                                FieldAttributes.Static | FieldAttributes.Private, designLoaderFieldTypeReference);
                            classTypeDefinition.Fields.Add(designLoaderField);

                            const string TrampolineName = "!XamlIlPopulateTrampoline";
                            var trampolineMethodWithoutSP = new Lazy<MethodDefinition>(() => CreateTrampolineMethod(false));
                            var trampolineMethodWithSP = new Lazy<MethodDefinition>(() => CreateTrampolineMethod(true));
                            MethodDefinition CreateTrampolineMethod(bool hasSystemProviderArg)
                            {
                                var trampoline = new MethodDefinition(TrampolineName,
                                    MethodAttributes.Static | MethodAttributes.Private, asm.MainModule.TypeSystem.Void);
                                if (hasSystemProviderArg)
                                {
                                    trampoline.Parameters.Add(new ParameterDefinition(serviceProviderType));
                                }
                                trampoline.Parameters.Add(new ParameterDefinition(classTypeDefinition));

                                classTypeDefinition.Methods.Add(trampoline);

                                var regularStart = Instruction.Create(OpCodes.Nop);
                            
                                trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, designLoaderField));
                                trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Brfalse, regularStart));
                                trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, designLoaderField));
                                trampoline.Body.Instructions.Add(Instruction.Create(hasSystemProviderArg ? OpCodes.Ldarg_1 : OpCodes.Ldarg_0));
                                trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Call, designLoaderLoad));
                                trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                            
                                trampoline.Body.Instructions.Add(regularStart);
                                trampoline.Body.Instructions.Add(Instruction.Create(hasSystemProviderArg ? OpCodes.Ldarg_0 : OpCodes.Ldnull));
                                trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Call, createRootServiceProviderMethod));
                                trampoline.Body.Instructions.Add(Instruction.Create(hasSystemProviderArg ? OpCodes.Ldarg_1 : OpCodes.Ldarg_0));
                                trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Call, compiledPopulateMethod));
                                trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                                CopyDebugDocument(trampoline, compiledPopulateMethod);
                                return trampoline;
                            }

                            var foundXamlLoader = false;
                            // Find AvaloniaXamlLoader.Load(this) or AvaloniaXamlLoader.Load(sp, this) and replace it with !XamlIlPopulateTrampoline(this)
                            foreach (var method in classTypeDefinition.Methods.Where(m => m.Body is not null).ToArray())
                            {
                                var i = method.Body.Instructions;
                                for (var c = 1; c < i.Count; c++)
                                {
                                    if (i[c].OpCode == OpCodes.Call)
                                    {
                                        var op = i[c].Operand as MethodReference;
                                        if (op != null
                                            && op.Name == TrampolineName)
                                        {
                                            throw new InvalidProgramException("Same XAML file was loaded twice." +
                                                "Make sure there is no x:Class duplicates no files were added to the AvaloniaResource msbuild items group twice.");
                                        }
                                        if (op != null
                                            && op.Name == "Load"
                                            && op.Parameters.Count == 1
                                            && op.Parameters[0].ParameterType.FullName == "System.Object"
                                            && op.DeclaringType.FullName == "Avalonia.Markup.Xaml.AvaloniaXamlLoader")
                                        {
                                            if (MatchThisCall(i, c - 1))
                                            {
                                                i[c].Operand = trampolineMethodWithoutSP.Value;
                                                foundXamlLoader = true;
                                            }
                                        }
                                        if (op != null
                                            && op.Name == "Load"
                                            && op.Parameters.Count == 2
                                            && op.Parameters[0].ParameterType.FullName == "System.IServiceProvider"
                                            && op.Parameters[1].ParameterType.FullName == "System.Object"
                                            && op.DeclaringType.FullName == "Avalonia.Markup.Xaml.AvaloniaXamlLoader")
                                        {
                                            if (MatchThisCall(i, c - 1))
                                            {
                                                i[c].Operand = trampolineMethodWithSP.Value;
                                                foundXamlLoader = true;
                                            }
                                        }
                                    }
                                }
                            }

                            if (!foundXamlLoader)
                            {
                                var ctors = classTypeDefinition.GetConstructors()
                                    .Where(c => !c.IsStatic).ToList();
                                // We can inject xaml loader into default constructor
                                if (ctors.Count == 1 && ctors[0].Body.Instructions.Count(o=>o.OpCode != OpCodes.Nop) == 3)
                                {
                                    var i = ctors[0].Body.Instructions;
                                    var retIdx = i.IndexOf(i.Last(x => x.OpCode == OpCodes.Ret));
                                    i.Insert(retIdx, Instruction.Create(OpCodes.Call, trampolineMethodWithoutSP.Value));
                                    i.Insert(retIdx, Instruction.Create(OpCodes.Ldarg_0));
                                }
                                else
                                {
                                    throw new InvalidProgramException(
                                        $"No call to AvaloniaXamlLoader.Load(this) call found anywhere in the type {classType.FullName} and type seems to have custom constructors.");
                                }
                            }

                        }

                        if (document.IsPublic
                            && (document.TypeBuilderProvider.BuildMethod != null || classTypeDefinition != null))
                        {
                            var compiledBuildMethod = document.TypeBuilderProvider.BuildMethod is not { } buildMethod ?
                                null :
                                typeSystem.GetTypeReference(builder).Resolve()
                                    .Methods.First(m => m.Name == buildMethod.Name);
                            var parameterlessConstructor = compiledBuildMethod != null ?
                                null :
                                classTypeDefinition.GetConstructors().FirstOrDefault(c =>
                                    c.IsPublic && !c.IsStatic && !c.HasParameters);
                            var constructorWithSp = compiledBuildMethod != null ?
                                null :
                                classTypeDefinition.GetConstructors().FirstOrDefault(c =>
                                    c.IsPublic && !c.IsStatic && c.Parameters.Count == 1 && c.Parameters[0].ParameterType.FullName == serviceProviderType.FullName);

                            if (compiledBuildMethod != null || parameterlessConstructor != null || constructorWithSp != null)
                            {
                                var i = loaderDispatcherMethod.Body.Instructions;
                                var nop = Instruction.Create(OpCodes.Nop);
                                i.Add(Instruction.Create(OpCodes.Ldarg_1));
                                i.Add(Instruction.Create(OpCodes.Ldstr, res.Uri));
                                i.Add(Instruction.Create(OpCodes.Ldc_I4, (int)StringComparison.OrdinalIgnoreCase));
                                i.Add(Instruction.Create(OpCodes.Call, stringEquals));
                                i.Add(Instruction.Create(OpCodes.Brfalse, nop));
                                if (parameterlessConstructor != null)
                                {
                                    i.Add(Instruction.Create(OpCodes.Newobj, parameterlessConstructor));
                                }
                                else if (constructorWithSp != null)
                                {
                                    i.Add(Instruction.Create(OpCodes.Ldarg_0));
                                    i.Add(Instruction.Create(OpCodes.Call, createRootServiceProviderMethod));
                                    i.Add(Instruction.Create(OpCodes.Newobj, constructorWithSp));
                                }
                                else
                                {
                                    i.Add(Instruction.Create(OpCodes.Ldarg_0));
                                    i.Add(Instruction.Create(OpCodes.Call, createRootServiceProviderMethod));
                                    i.Add(Instruction.Create(OpCodes.Call, compiledBuildMethod));
                                }

                                i.Add(Instruction.Create(OpCodes.Ret));
                                i.Add(nop);
                            }
                            else
                            {
                                var diagnostic = new XamlDiagnostic(
                                    AvaloniaXamlDiagnosticCodes.XamlLoaderUnreachable,
                                    diagnosticsFilter.Handle(
                                        XamlDiagnosticSeverity.Warning,
                                        AvaloniaXamlDiagnosticCodes.XamlLoaderUnreachable),
                                    $"XAML resource \"{res.Uri}\" won't be reachable via runtime loader, as no public constructor was found")
                                {
                                    Document = document.FileSource?.FilePath
                                };
                                engine.LogDiagnostic(diagnostic);
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        engine.LogError(AvaloniaXamlDiagnosticCodes.EmitError, res.FilePath, e);
                        return false;
                    }
                    res.Remove();
                }

                // Technically that's a hack, but it fixes corert incompatibility caused by deterministic builds
                int dupeCounter = 1;
                foreach (var grp in typeDef.NestedTypes.GroupBy(x => x.Name))
                {
                    if (grp.Count() > 1)
                    {
                        foreach (var dupe in grp)
                            dupe.Name += "_dup" + dupeCounter++;
                    }
                }

                return true;
            }

            if (avares.Resources.Count(CheckXamlName) != 0)
            {
                if (!CompileGroup(avares))
                    return false;
                avares.Save();
            }
            
            loaderDispatcherMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            loaderDispatcherMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return true;
        }

        static bool ReportDiagnostics(IBuildEngine engine, IReadOnlyCollection<XamlDiagnostic> diagnostics)
        {
            var hasAnyError = diagnostics.Any(d => d.Severity >= XamlDiagnosticSeverity.Error);

            const int maxErrorsPerDocument = 100;
            foreach (var diagnostic in diagnostics
                         .GroupBy(d => d.Document)
                         .SelectMany(d => d.Take(maxErrorsPerDocument)))
            {
                engine.LogDiagnostic(diagnostic);
            }

            return hasAnyError;
        }

        static bool? CompileCoreForRefAssembly(
            IBuildEngine engine, CecilTypeSystem sourceTypeSystem, CecilTypeSystem refTypeSystem)
        {
            var asm = refTypeSystem.TargetAssemblyDefinition;

            var compiledTypes = sourceTypeSystem.TargetAssemblyDefinition.MainModule.Types
                .Where(t => t.Namespace.StartsWith(CompiledAvaloniaXamlNamespace) && t.IsPublic).ToArray();
            if (compiledTypes.Length == 0)
            {
                return null;
            }

            try
            {
                foreach (var ogType in compiledTypes)
                {
                    var wrappedOgType = sourceTypeSystem.TargetAssembly.FindType(ogType.FullName);

                    var clrPropertiesDef = new TypeDefinition(ogType.Namespace, ogType.Name,
                        TypeAttributes.Class | TypeAttributes.Public, asm.MainModule.TypeSystem.Object);
                    asm.MainModule.Types.Add(clrPropertiesDef);
                    foreach (var attribute in ogType.CustomAttributes)
                    {
                        var method = asm.MainModule.ImportReference(attribute.Constructor);
                        clrPropertiesDef.CustomAttributes.Add(new CustomAttribute(method, attribute.GetBlob()));
                    }

                    var typeBuilder = refTypeSystem.CreateTypeBuilder(clrPropertiesDef);
                    foreach (var ogMethod in wrappedOgType.Methods.Where(m => m.IsPublic && m.IsStatic))
                    {
                        var method = typeBuilder.DefineMethod(ogMethod.ReturnType, ogMethod.Parameters, ogMethod.Name,
                            XamlVisibility.Public, ogMethod.IsStatic, false);
                        method.Generator.Ldnull();
                        method.Generator.Throw();
                    }

                    typeBuilder.CreateType();
                }
            }
            catch (Exception e)
            {
                engine.LogError(AvaloniaXamlDiagnosticCodes.Unknown, e.Message, e);
                return false;
            }

            return true;
        }
    }
}
