using System;
using System.Collections.Generic;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.NuGet
{
    /// <summary>
    /// Creates a NuGet package.
    /// </summary>
    [ActionProperties(
        "Create NuGet Package",
        "Creates a package using NuGet.",
        "NuGet")]
    [CustomEditor(typeof(CreatePackageActionEditor))]
    [RequiresInterface(typeof(IRemoteProcessExecuter))]
    [RequiresInterface(typeof(IFileOperationsExecuter))]
    public sealed class CreatePackage : NuGetActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreatePackage"/> class.
        /// </summary>
        public CreatePackage()
        {
        }

        /// <summary>
        /// Gets or sets the project or nuspec file to package.
        /// </summary>
        /// <remarks>
        /// This is relative to the source directory.
        /// </remarks>
        [Persistent]
        public string ProjectPath { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to use verbose output.
        /// </summary>
        [Persistent]
        public bool Verbose { get; set; }
        /// <summary>
        /// Gets or sets the version to use for the NuGet package. If null or empty, the nuspec value is used.
        /// </summary>
        [Persistent]
        public string Version { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to include symbols in the package.
        /// </summary>
        [Persistent]
        public bool Symbols { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to build a project.
        /// </summary>
        [Persistent]
        public bool Build { get; set; }
        /// <summary>
        /// Gets or sets optional properties and values to pass to NuGet.
        /// </summary>
        /// <remarks>
        /// These strings should be in the form Property=Value.
        /// </remarks>
        [Persistent]
        public string[] Properties { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                "Create a NuGet package from {0} in {1}",
                this.ProjectPath,
                Util.CoalesceStr(this.OverriddenTargetDirectory, "default target directory"));
        }

        protected override void Execute()
        {
            var argList = new List<string>();
            string projectPath;

            var agent = this.Context.Agent.GetService<IFileOperationsExecuter>();
            if (this.ProjectPath.StartsWith("~\\"))
                projectPath = agent.GetWorkingDirectory(this.Context.ApplicationId, this.Context.DeployableId ?? 0, this.ProjectPath);
            else
                projectPath = agent.CombinePath(this.Context.SourceDirectory, this.ProjectPath);

            argList.Add("\"" + projectPath + "\"");
            argList.Add("-BasePath \"" + this.Context.SourceDirectory + "\"");
            argList.Add("-OutputDirectory \"" + this.Context.TargetDirectory + "\"");

            bool isNuspec = projectPath.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase);

            if (this.Verbose)
                argList.Add("-Verbose");
            if (!string.IsNullOrEmpty(this.Version))
                argList.Add("-Version \"" + this.Version + "\"");
            if (this.Symbols)
                argList.Add("-Symbols");
            if (this.Build && !isNuspec)
                argList.Add("-Build");
            if (this.Properties != null && this.Properties.Length > 0 && !isNuspec)
                argList.Add("-Properties \"" + string.Join(";", this.Properties) + "\"");

            this.NuGet("pack", argList.ToArray());
        }
    }
}
