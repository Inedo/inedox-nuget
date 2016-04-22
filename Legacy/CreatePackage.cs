using System;
using System.Collections.Generic;
using System.ComponentModel;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.NuGet.Legacy.ActionImporters;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.NuGet
{
    [Tag("nuget")]
    [DisplayName("Create NuGet Package")]
    [Description("Creates a package using NuGet.")]
    [CustomEditor(typeof(CreatePackageActionEditor))]
    [ConvertibleToOperation(typeof(CreatePackageImporter))]
    public sealed class CreatePackage : NuGetActionBase
    {
        [Persistent]
        public string ProjectPath { get; set; }
        [Persistent]
        public bool Verbose { get; set; }
        [Persistent]
        public string Version { get; set; }
        [Persistent]
        public bool Symbols { get; set; }
        [Persistent]
        public bool Build { get; set; }
        [Persistent]
        public string[] Properties { get; set; }
        [Persistent]
        public bool IncludeReferencedProjects { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Create NuGet package from ",
                    new DirectoryHilite(this.OverriddenSourceDirectory, this.ProjectPath)
                ),
                new RichDescription(
                    "in ",
                    new DirectoryHilite(this.OverriddenTargetDirectory)
                )
            );
        }

        protected override void Execute()
        {
            var argList = new List<string>();
            string projectPath;

            var agent = this.Context.Agent.GetService<IFileOperationsExecuter>();
            if (this.ProjectPath.StartsWith("~\\"))
                projectPath = agent.CombinePath(agent.GetLegacyWorkingDirectory((IGenericBuildMasterContext)this.Context, this.ProjectPath));
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
            if (this.IncludeReferencedProjects)
                argList.Add("-IncludeReferencedProjects");
            if (this.Build && !isNuspec)
                argList.Add("-Build");
            if (this.Properties != null && this.Properties.Length > 0 && !isNuspec)
                argList.Add("-Properties \"" + string.Join(";", this.Properties) + "\"");

            this.NuGet("pack", argList.ToArray());
        }
    }
}
