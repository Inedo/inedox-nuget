using System;
using System.IO;
using System.Linq;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.NuGet
{
    /// <summary>
    /// Installs NuGet packages for a solution.
    /// </summary>
    [ActionProperties(
        "Install NuGet Packages",
        "Installs all packages required for projects in a solution to build.",
        "NuGet")]
    [CustomEditor(typeof(InstallSolutionPackagesActionEditor))]
    [RequiresInterface(typeof(IRemoteProcessExecuter))]
    [RequiresInterface(typeof(IFileOperationsExecuter))]
    public sealed class InstallSolutionPackagesAction : NuGetActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstallSolutionPackagesAction"/> class.
        /// </summary>
        public InstallSolutionPackagesAction()
        {
        }

        /// <summary>
        /// Gets or sets the directory to install packages to.
        /// </summary>
        /// <remarks>
        /// This may be relative to the source directory.
        /// </remarks>
        [Persistent]
        public string PackageOutputDirectory { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                "Install NuGet packages to {0} for projects in {1}",
                Util.CoalesceStr(this.PackageOutputDirectory, "(default)"),
                Util.CoalesceStr(this.OverriddenSourceDirectory, "(default source directory)")
            );
        }

        /// <summary>
        /// This method is called to execute the Action.
        /// </summary>
        protected override void Execute()
        {
            var agent = this.Context.Agent.GetService<IFileOperationsExecuter>();
            var configurer = (NuGetConfigurer)this.GetExtensionConfigurer();

            if (configurer.UseProGetClient)
            {
                this.LogDebug("Installing packages using proget.exe...");
                if (string.IsNullOrEmpty(configurer.PackageSource))
                {
                    this.LogError("Using ProGet.exe requires the package source to be set explicitly in the NuGet extension configuration.");
                    return;
                }

                var cmdLine = "-SolutionDirectory \"" + this.Context.SourceDirectory + "\" -Source \"" + configurer.PackageSource + "\"";
                if (!string.IsNullOrEmpty(this.PackageOutputDirectory))
                    cmdLine += " -OutputDirectory \"" + this.PackageOutputDirectory + "\"";

                this.ProGet("install", cmdLine);
            }
            else
            {
                this.LogDebug("Installing packages using nuget.exe...");

                var entry = agent.GetDirectoryEntry(
                    new GetDirectoryEntryCommand
                    {
                        Path = this.Context.SourceDirectory,
                        IncludeRootPath = true,
                        Recurse = true
                    }
                ).Entry;

                var configFiles = entry
                    .Flatten()
                    .SelectMany(e => e.Files ?? Enumerable.Empty<FileEntryInfo>())
                    .Where(e => string.Equals(e.Name, "packages.config", StringComparison.OrdinalIgnoreCase));

                if (!configFiles.Any())
                {
                    this.LogWarning("No packages.config files were found in {0} or any of its subdirectories.", this.Context.SourceDirectory);
                    return;
                }

                var cmdLine = "\"{0}\" -OutputDirectory \"";

                if (string.IsNullOrEmpty(this.PackageOutputDirectory))
                {
                    string bestGuess;

                    this.LogDebug("Attempting to determine package output directory...");
                    var solutionFiles = entry
                        .Flatten()
                        .SelectMany(e => e.Files ?? Enumerable.Empty<FileEntryInfo>())
                        .Where(e => e.Name.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (solutionFiles.Count == 0)
                    {
                        this.LogWarning("No .sln files found in {0} or any subdirectories.", this.Context.SourceDirectory);
                        bestGuess = agent.CombinePath(this.Context.SourceDirectory, "packages");
                    }
                    else if (solutionFiles.Count == 1)
                    {
                        this.LogDebug("Using {0} to determine package install path.", solutionFiles[0].Path);
                        bestGuess = agent.CombinePath(Path.GetDirectoryName(solutionFiles[0].Path), "packages");
                    }
                    else
                    {
                        this.LogWarning("Multiple .sln files were found in {0}. Using {1} to determine package install path.", this.Context.SourceDirectory, solutionFiles[0].Path);
                        bestGuess = agent.CombinePath(Path.GetDirectoryName(solutionFiles[0].Path), "packages");
                    }

                    if (solutionFiles.Count != 1)
                        this.LogWarning("Correct this warning by explicitly specifying the package install path for this action.");

                    this.LogInformation("Packages will be installed to {0}", bestGuess);
                    cmdLine += bestGuess + "\"";
                }
                else
                {
                    var outputDirectory = agent.GetWorkingDirectory(this.Context.ApplicationId, this.Context.DeployableId ?? 0, this.PackageOutputDirectory);
                    this.LogInformation("Packages will be installed to {0}", outputDirectory);
                    cmdLine += outputDirectory + "\"";
                }

                this.LogDebug("Package source is {0}", Util.CoalesceStr(configurer.PackageSource, "(default)"));

                if (!string.IsNullOrEmpty(configurer.PackageSource))
                    cmdLine += " -Source \"" + configurer.PackageSource + "\"";

                foreach (var configFile in configFiles)
                {
                    this.LogInformation("Installing packages for {0}...", configFile.Path);
                    this.NuGet("install", string.Format(cmdLine, configFile.Path));
                }
            }
        }
    }
}
