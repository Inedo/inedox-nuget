using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Documentation;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.NuGet.Operations
{
    [ScriptAlias("Install-Packages")]
    [ScriptNamespace("NuGet")]
    [DisplayName("Install NuGet Packages")]
    [Description("Installs all packages required for projects in a solution to build.")]
    public sealed class InstallPackagesOperation : ExecuteOperation
    {
        [ScriptAlias("OutputDirectory")]
        [DefaultValue("packages")]
        public string PackageOutputDirectory { get; set; }
        [ScriptAlias("SourceDirectory")]
        public string SourceDirectory { get; set; }
        [ScriptAlias("NuGetExePath")]
        [DefaultValue("$NuGetExePath")]
        public string NuGetExePath { get; set; }
        [ScriptAlias("Source")]
        public string ServerUrl { get; set; }
        [ScriptAlias("Arguments")]
        public string AdditionalArguments { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var fileOps = context.Agent.GetService<IFileOperationsExecuter>();
            var nugetExe = this.GetNuGetExePath(fileOps);
            if (string.IsNullOrEmpty(nugetExe))
                return;

            var sourceDirectory = context.ResolvePath(this.SourceDirectory);
            var outputDirectory = context.ResolvePath(PathEx.Combine(this.SourceDirectory, this.PackageOutputDirectory));

            this.LogInformation($"Installing packages for projects in {sourceDirectory} to {outputDirectory}...");

            if (!fileOps.DirectoryExists(sourceDirectory))
            {
                this.LogWarning(sourceDirectory + " does not exist.");
                return;
            }

            fileOps.CreateDirectory(outputDirectory);

            this.LogInformation($"Finding packages.config files in {sourceDirectory}...");

            var configFiles = (from e in fileOps.GetFileSystemInfos(sourceDirectory, new MaskingContext(new[] { "**packages.config" }, Enumerable.Empty<string>()))
                               where string.Equals(e.Name, "packages.config", System.StringComparison.OrdinalIgnoreCase)
                               let f = e as SlimFileInfo
                               where f != null
                               select f).ToArray();

            if (configFiles.Length == 0)
            {
                this.LogWarning("No packages.config files found.");
                return;
            }

            foreach (var configFile in configFiles)
            {
                this.LogInformation($"Installing packages for {configFile}...");
                await this.ExecuteNuGet(context, nugetExe, configFile.FullName, outputDirectory);
            }

            this.LogInformation("Done installing packages!");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Install packages to ",
                    new DirectoryHilite(config[nameof(this.SourceDirectory)], config[nameof(this.PackageOutputDirectory)])
                ),
                new RichDescription(
                    "for projects in ",
                    new DirectoryHilite(config[nameof(this.SourceDirectory)])
                )
            );
        }

        private string GetNuGetExePath(IFileOperationsExecuter fileOps)
        {
            if (!string.IsNullOrEmpty(this.NuGetExePath))
                return this.NuGetExePath;

            return PathEx.Combine(fileOps.GetBaseWorkingDirectory(), "ExtTemp", "NuGet", "nuget.exe");
        }

        private Task ExecuteNuGet(IOperationExecutionContext context, string nugetExe, string packagesConfig, string outputDirectory)
        {
            var args = $"install \"{packagesConfig}\" -OutputDirectory \"{outputDirectory}\"";
            if (!string.IsNullOrWhiteSpace(this.ServerUrl))
                args += "-Source \"" + this.ServerUrl + "\"";
            if (!string.IsNullOrWhiteSpace(this.AdditionalArguments))
                args += " " + this.AdditionalArguments;

            this.LogDebug("Executing: " + nugetExe + " " + args);

            return this.ExecuteCommandLineAsync(
                context,
                new AgentProcessStartInfo
                {
                    FileName = nugetExe,
                    Arguments = args
                }
            );
        }
    }
}
