using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.Documentation;

namespace Inedo.BuildMasterExtensions.NuGet.Operations
{
    [ScriptAlias("Create-Package")]
    [ScriptNamespace("NuGet")]
    [DisplayName("Create NuGet Package")]
    [Description("Creates a package using NuGet.")]
    [DefaultProperty(nameof(ProjectPath))]
    public sealed class CreatePackageOperation : NuGetOperationBase
    {
        [Required]
        [ScriptAlias("SourceFile")]
        [DisplayName("Source file")]
        [Description("The .nuspec or msbuild project that will be passed to NuGet.exe.")]
        public string ProjectPath { get; set; }
        [ScriptAlias("Verbose")]
        [Description(CommonDescriptions.VerboseLogging)]
        public bool Verbose { get; set; }
        [ScriptAlias("Version")]
        [Description("The package version that will be passed ot NuGet.exe.")]
        public string Version { get; set; }
        [ScriptAlias("Symbols")]
        [Description("When true, the -Symbols argument will be passed to NuGet.exe.")]
        public bool Symbols { get; set; }
        [ScriptAlias("Build")]
        [Description("When true, the -Build argument will be passed to NuGet.exe.")]
        public bool Build { get; set; }
        [ScriptAlias("Properties")]
        [Description("When Build is true, these values will be passed to NuGet.exe as MSBuild properties in the format PROP=VALUE.")]
        public IEnumerable<string> Properties { get; set; }
        [ScriptAlias("IncludeReferencedProjects")]
        [Description("When true, the -IncludeReferencedProjects argument will be passed to NuGet.exe.")]
        public bool IncludeReferencedProjects { get; set; }
        [ScriptAlias("OutputDirectory")]
        [Description("The output directory that will be passed to NuGet.exe.")]
        public string TargetDirectory { get; set; }
        [DisplayName("Source directory")]
        [Description("The working directory to use when executing NuGet.")]
        [ScriptAlias("SourceDirectory")]
        public string SourceDirectory { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var fileOps = context.Agent.GetService<IFileOperationsExecuter>();
            var nugetExe = this.GetNuGetExePath(context);
            if (string.IsNullOrEmpty(nugetExe))
                return;

            var sourceDirectory = context.ResolvePath(this.SourceDirectory ?? string.Empty);
            var outputDirectory = context.ResolvePath(this.TargetDirectory, this.SourceDirectory);
            var fullProjectPath = context.ResolvePath(this.ProjectPath, this.SourceDirectory);

            if (!fileOps.FileExists(fullProjectPath))
            {
                this.LogError(fullProjectPath + " does not exist.");
                return;
            }

            fileOps.CreateDirectory(outputDirectory);

            this.LogInformation($"Creating NuGet package from {fullProjectPath} to {outputDirectory}...");
            await this.ExecuteNuGet(context, nugetExe, fullProjectPath, sourceDirectory, outputDirectory);
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Create NuGet package from ",
                    new DirectoryHilite(config[nameof(this.ProjectPath)])
                ),
                new RichDescription(
                    "in ",
                    new DirectoryHilite(config[nameof(this.TargetDirectory)])
                )
            );
        }

        private Task ExecuteNuGet(IOperationExecutionContext context, string nugetExe, string projectPath, string sourceDirectory, string outputDirectory)
        {
            var argList = new List<string>();

            argList.Add("\"" + projectPath + "\"");
            argList.Add("-BasePath \"" + sourceDirectory + "\"");
            argList.Add("-OutputDirectory \"" + outputDirectory + "\"");

            bool isNuspec = projectPath.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase);
            var properties = this.Properties?.ToList();

            if (this.Verbose)
                argList.Add("-Verbose");
            if (!string.IsNullOrEmpty(this.Version))
                argList.Add("-Version \"" + this.Version + "\"");
            if (this.Symbols)
                argList.Add("-Symbols");
            if (this.IncludeReferencedProjects)
                argList.Add("-IncludeReferencedProjects");
            if (this.Build && !isNuspec)
                argList.Add("-Build");
            if (properties?.Count > 0 && !isNuspec)
                argList.Add("-Properties \"" + string.Join(";", properties) + "\"");

            return this.ExecuteNuGetAsync(context, nugetExe, "pack " + string.Join(" ", argList));
        }
    }
}
