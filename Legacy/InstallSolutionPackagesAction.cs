using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Inedo.Agents;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.NuGet.Legacy.ActionImporters;
using Inedo.Documentation;
using Inedo.IO;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.NuGet
{
    [Tag("nuget")]
    [DisplayName("Install NuGet Packages")]
    [Description("Installs all packages required for projects in a solution to build.")]
    [CustomEditor(typeof(InstallSolutionPackagesActionEditor))]
    [ConvertibleToOperation(typeof(InstallPackagesImporter))]
    public sealed class InstallSolutionPackagesAction : NuGetActionBase
    {
        [Persistent]
        public string PackageOutputDirectory { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Install NuGet packages to ",
                    new DirectoryHilite(this.OverriddenSourceDirectory, AH.CoalesceString(this.PackageOutputDirectory, "packages"))
                ),
                new RichDescription(
                    "for projects in ",
                    new DirectoryHilite(this.OverriddenSourceDirectory)
                )
            );
        }

        protected override void Execute()
        {
            var agent = this.Context.Agent.GetService<IFileOperationsExecuter>();
            var configurer = (NuGetConfigurer)this.GetExtensionConfigurer();

            string packageOutputDirectory = GetAppRelativePath(this.PackageOutputDirectory, this.Context);

            if (configurer.UseProGetClient)
            {
                this.LogDebug("Installing packages using proget.exe...");
                if (string.IsNullOrEmpty(configurer.PackageSource))
                {
                    this.LogError("Using ProGet.exe requires the package source to be set explicitly in the NuGet extension configuration.");
                    return;
                }

                var cmdLine = "-SolutionDirectory \"" + this.Context.SourceDirectory + "\" -Source \"" + configurer.PackageSource + "\"";
                if (!string.IsNullOrEmpty(packageOutputDirectory))
                    cmdLine += " -OutputDirectory \"" + packageOutputDirectory + "\"";

                this.ProGet("install", cmdLine);
            }
            else
            {
                if (configurer.AlwaysClearNuGetCache)
                {
                    this.LogDebug("Clearing NuGet cache...");
                    if (NuGetConfigurer.ClearCache())
                        this.LogDebug("Cache cleared!");
                    else
                        this.LogWarning("Error clearing cache; a file may be locked.");
                }

                this.LogDebug("Installing packages using nuget.exe...");

                var infos = agent.GetFileSystemInfos(this.Context.SourceDirectory, IO.MaskingContext.IncludeAll);

                var configFiles = agent.GetFileSystemInfos(this.Context.SourceDirectory, IO.MaskingContext.IncludeAll)
                    .OfType<IO.SlimFileInfo>()
                    .Where(e => string.Equals(e.Name, "packages.config", StringComparison.OrdinalIgnoreCase));

                if (!configFiles.Any())
                {
                    this.LogWarning("No packages.config files were found in {0} or any of its subdirectories.", this.Context.SourceDirectory);
                    return;
                }

                var cmdLine = "\"{0}\" -OutputDirectory \"";

                if (string.IsNullOrEmpty(packageOutputDirectory))
                {
                    string bestGuess;

                    this.LogDebug("Attempting to determine package output directory...");
                    var solutionFiles = infos
                        .OfType<IO.SlimFileInfo>()
                        .Where(e => e.Name.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (solutionFiles.Count == 0)
                    {
                        this.LogWarning("No .sln files found in {0} or any subdirectories.", this.Context.SourceDirectory);
                        bestGuess = agent.CombinePath(this.Context.SourceDirectory, "packages");
                    }
                    else if (solutionFiles.Count == 1)
                    {
                        this.LogDebug("Using {0} to determine package install path.", solutionFiles[0].FullName);
                        bestGuess = agent.CombinePath(Path.GetDirectoryName(solutionFiles[0].FullName), "packages");
                    }
                    else
                    {
                        this.LogWarning("Multiple .sln files were found in {0}. Using {1} to determine package install path.", this.Context.SourceDirectory, solutionFiles[0].FullName);
                        bestGuess = agent.CombinePath(Path.GetDirectoryName(solutionFiles[0].FullName), "packages");
                    }

                    if (solutionFiles.Count != 1)
                        this.LogWarning("Correct this warning by explicitly specifying the package install path for this action.");

                    this.LogInformation("Packages will be installed to {0}", bestGuess);
                    cmdLine += bestGuess + "\"";
                }
                else
                {
                    var outputDirectory = agent.CombinePath(this.Context.SourceDirectory, packageOutputDirectory);
                    this.LogInformation("Packages will be installed to {0}", outputDirectory);
                    cmdLine += outputDirectory + "\"";
                }

                this.LogDebug("Package source is {0}", Util.CoalesceStr(configurer.PackageSource, "(default)"));

                if (!string.IsNullOrEmpty(configurer.PackageSource))
                    cmdLine += " -Source \"" + configurer.PackageSource + "\"";

                foreach (var configFile in configFiles)
                {
                    this.LogInformation("Installing packages for {0}...", configFile.FullName);
                    this.NuGet("install", string.Format(cmdLine, configFile.FullName));
                }
            }
        }

        private static string GetAppRelativePath(string path, IAgentBasedActionExecutionContext context)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            if (path.StartsWith("~\\") || path.StartsWith("~/"))
            {
                var appDir = context.ApplicationDirectory;
                return PathEx.Combine(appDir, path.Substring(2));
            }

            return path;
        }
    }
}
