using System.Collections.Generic;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using System.IO;
using Inedo.BuildMaster.Extensibility.Agents;
using System;
using Inedo.BuildMaster.Features;
using Inedo.BuildMaster.Data;

namespace Inedo.BuildMasterExtensions.NuGet
{
    /// <summary>
    /// Installs NuGet packages for a project.
    /// </summary>
    [ActionProperties(
        "Install packages.config Packages",
        "Installs NuGet packages specified by a packages.config file.",
        "NuGet")]
    [CustomEditor(typeof(InstallPackagesActionEditor))]
    [RequiresInterface(typeof(IRemoteProcessExecuter))]
    [FeatureLevelRequired(FeatureCodes.Deployment_Plans, FeatureLevels.HighlyExperimental)]
    public sealed class InstallPackages : NuGetActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstallPackages"/> class.
        /// </summary>
        public InstallPackages()
        {
            this.PackagesConfigPath = "packages.config";
        }

        /// <summary>
        /// Gets or sets the location of the packages.config file.
        /// </summary>
        /// <remarks>
        /// This is relative to the source directory.
        /// </remarks>
        [Persistent]
        public string PackagesConfigPath { get; set; }
        /// <summary>
        /// Gets or sets the URL of the NuGet package source.
        /// </summary>
        /// <remarks>
        /// If not specified, all sources in %AppData%\NuGet\NuGet.config are used.
        /// </remarks>
        [Persistent]
        public string PackageSource { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                "Install NuGet packages specified in {0} to {1}",
                Path.Combine(this.OverriddenSourceDirectory, this.PackagesConfigPath),
                Util.CoalesceStr(this.OverriddenTargetDirectory, "default target directory"));
        }

        protected override void Execute()
        {
            var argList = new List<string>();
            argList.Add("\"" + this.PackagesConfigPath + "\"");
            argList.Add("-OutputDirectory");
            argList.Add("\"" + this.RemoteConfiguration.TargetDirectory + "\"");
            if (!string.IsNullOrEmpty(this.PackageSource))
            {
                argList.Add("-Source");
                argList.Add("\"" + this.PackageSource + "\"");
            }

            this.NuGet("install", argList.ToArray());
        }
        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            throw new NotImplementedException();
        }
    }
}
