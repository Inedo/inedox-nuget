using System.Collections.Generic;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.NuGet
{
    [Tag("nuget")]
    [ActionProperties(
        "Publish NuGet Package",
        "Publishes a package using NuGet.")]
    [CustomEditor(typeof(PushPackageActionEditor))]
    public sealed class PushPackage : NuGetActionBase
    {
        [Persistent]
        public string PackagePath { get; set; }
        [Persistent]
        public string ApiKey { get; set; }
        [Persistent]
        public string ServerUrl { get; set; }

        public override ActionDescription GetActionDescription()
        {
            return new ActionDescription(
                new ShortActionDescription(
                    "Publish ",
                    new DirectoryHilite(this.OverriddenSourceDirectory, this.PackagePath),
                    " to NuGet"
                )
            );
        }

        protected override void Execute()
        {
            var argList = new List<string>();
            argList.Add("\"" + this.PackagePath + "\"");

            if (!string.IsNullOrEmpty(this.ApiKey))
                argList.Add("\"" + this.ApiKey + "\"");
            if (!string.IsNullOrEmpty(this.ServerUrl))
                argList.Add("-s \"" + this.ServerUrl + "\"");

            this.NuGet("push", argList.ToArray());
        }
    }
}
