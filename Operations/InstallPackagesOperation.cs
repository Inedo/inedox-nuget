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
    [DisplayName("Install NuGet Packages")]
    [Description("Installs all packages required for projects in a solution to build.")]
    public sealed class InstallPackagesOperation : NuGetOperationBase
    {
        [ScriptAlias("OutputDirectory")]
        [DisplayName("Output directory")]
        [Description("The directory into which packages will be installed.")]
        [DefaultValue("packages")]
        public string PackageOutputDirectory { get; set; }
        [ScriptAlias("SourceDirectory")]
        [DisplayName("Source directory")]
        [Description("The working directory to use when installing packages.")]
        [PlaceholderText("$WorkingDirectory")]
        public string SourceDirectory { get; set; }
        [ScriptAlias("Source")]
        [DisplayName("Source URL")]
        [PlaceholderText("Use default URL specified in nuget.config")]
        public string ServerUrl { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var fileOps = await context.Agent.GetServiceAsync<IFileOperationsExecuter>().ConfigureAwait(false);
            var nugetExe = await this.GetNuGetExePathAsync(context).ConfigureAwait(false);
            if (string.IsNullOrEmpty(nugetExe))
            {
                this.LogError("nuget.exe path was empty.");
                return;
            }

            var sourceDirectory = context.ResolvePath(this.SourceDirectory);
            var outputDirectory = context.ResolvePath(PathEx.Combine(this.SourceDirectory, this.PackageOutputDirectory));

            this.LogInformation($"Installing packages for projects in {sourceDirectory} to {outputDirectory}...");

            if (!await fileOps.DirectoryExistsAsync(sourceDirectory).ConfigureAwait(false))
            {
                this.LogWarning(sourceDirectory + " does not exist.");
                return;
            }

            await fileOps.CreateDirectoryAsync(outputDirectory).ConfigureAwait(false);

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
                await this.ExecuteNuGetAsync(context, nugetExe, configFile.FullName, outputDirectory).ConfigureAwait(false);
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

        private Task ExecuteNuGetAsync(IOperationExecutionContext context, string nugetExe, string packagesConfig, string outputDirectory)
        {
            var args = $"install \"{packagesConfig}\" -OutputDirectory \"{outputDirectory}\"";
            if (!string.IsNullOrWhiteSpace(this.ServerUrl))
                args += " -Source \"" + this.ServerUrl + "\"";

            return this.ExecuteNuGetAsync(context, nugetExe, args);
        }
    }
}
