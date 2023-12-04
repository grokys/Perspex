using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Avalonia.Build.Tasks
{
    public class CompileAvaloniaXamlTask: ITask
    {
        public const string AvaloniaCompileOutputMetadataName = "AvaloniaCompileOutput";

        public bool Execute()
        {
            Enum.TryParse(ReportImportance, true, out MessageImportance outputImportance);
            
            var outputPath = AssemblyFile.GetMetadata(AvaloniaCompileOutputMetadataName);
            var refOutputPath = RefAssemblyFile?.GetMetadata(AvaloniaCompileOutputMetadataName);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            if (!string.IsNullOrEmpty(refOutputPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(refOutputPath));
            }

            var msg = $"CompileAvaloniaXamlTask -> AssemblyFile:{AssemblyFile}, ProjectDirectory:{ProjectDirectory}, OutputPath:{outputPath}";
            BuildEngine.LogMessage(msg, outputImportance < MessageImportance.Low ? MessageImportance.High : outputImportance);

            var res = XamlCompilerTaskExecutor.Compile(BuildEngine,
                AssemblyFile.ItemSpec, outputPath,
                RefAssemblyFile?.ItemSpec, refOutputPath,
                References?.Select(i => i.ItemSpec).ToArray() ?? Array.Empty<string>(),
                ProjectDirectory, VerifyIl, DefaultCompileBindings, outputImportance,
                new XamlCompilerDiagnosticsFilter(AnalyzerConfigFiles),
                (SignAssembly && !DelaySign) ? AssemblyOriginatorKeyFile : null,
                SkipXamlCompilation, DebuggerLaunch, VerboseExceptions);

            if (res.Success && !res.WrittenFile)
            {
                // To simplify incremental build checks, copy the input files to the expected output locations even if the Xaml compiler didn't do anything.
                File.Copy(AssemblyFile.ItemSpec, outputPath, overwrite: true);
                File.Copy(Path.ChangeExtension(AssemblyFile.ItemSpec, ".pdb"), Path.ChangeExtension(outputPath, ".pdb"), overwrite: true);

                if (!string.IsNullOrEmpty(refOutputPath))
                {
                    File.Copy(RefAssemblyFile.ItemSpec, refOutputPath, overwrite: true);
                }
            }

            return res.Success;
        }
        
        [Required]
        public string ProjectDirectory { get; set; }
        
        [Required]
        public ITaskItem AssemblyFile { get; set; }

        public ITaskItem? RefAssemblyFile { get; set; }

        public ITaskItem[]? References { get; set; }

        public bool VerifyIl { get; set; }

        public bool DefaultCompileBindings { get; set; }
        
        public bool SkipXamlCompilation { get; set; }
        
        public string AssemblyOriginatorKeyFile { get; set; }
        public bool SignAssembly { get; set; }
        public bool DelaySign { get; set; }

        public string ReportImportance { get; set; }

        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        public bool DebuggerLaunch { get; set; }

        public bool VerboseExceptions { get; set; }
        
        public ITaskItem[] AnalyzerConfigFiles { get; set; }
    }
}
