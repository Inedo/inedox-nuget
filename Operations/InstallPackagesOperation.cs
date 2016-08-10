using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.NuGet.Operations
{
    [ScriptAlias("Install-Packages")]
    [ScriptNamespace("NuGet")]
    [DisplayName("Install NuGet Packages")]
    [Description("Installs all packages required for projects in a solution to build.")]
    public sealed class InstallPackagesOperation : NuGetOperationBase
    {
        [DisplayName("Output directory")]
        [Description("The directory into which packages will be installed.")]
        [ScriptAlias("OutputDirectory")]
        [DefaultValue("packages")]
        public string PackageOutputDirectory { get; set; }
        [DisplayName("Source directory")]
        [Description("The working directory to use when installing packages.")]
        [ScriptAlias("SourceDirectory")]
        public string SourceDirectory { get; set; }
        [DisplayName("Source URL")]
        [Description("The NuGet package source URL. If not specified, the default source will be used for the current server.")]
        [ScriptAlias("Source")]
        public string ServerUrl { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var fileOps = context.Agent.GetService<IFileOperationsExecuter>();
            var nugetExe = this.GetNuGetExePath(context);
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
                this.LogInformation($"Installing packages for {configFile.FullName}...");
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

        private Task ExecuteNuGet(IOperationExecutionContext context, string nugetExe, string packagesConfig, string outputDirectory)
        {
            var args = $"install \"{packagesConfig}\" -OutputDirectory \"{outputDirectory}\"";
            if (!string.IsNullOrWhiteSpace(this.ServerUrl))
                args += " -Source \"" + this.ServerUrl + "\"";

            return this.ExecuteNuGetAsync(context, nugetExe, args);
        }
    }
}
