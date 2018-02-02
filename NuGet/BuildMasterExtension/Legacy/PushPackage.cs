﻿using System.Collections.Generic;
using System.ComponentModel;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.NuGet.Legacy.ActionImporters;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.NuGet
{
    [Tag("nuget")]
    [DisplayName("Publish NuGet Package")]
    [Description("Publishes a package using NuGet.")]
    [Inedo.Web.CustomEditor(typeof(PushPackageActionEditor))]
    [ConvertibleToOperation(typeof(PushPackageImporter))]
    public sealed class PushPackage : NuGetActionBase
    {
        [Persistent]
        public string PackagePath { get; set; }
        [Persistent]
        public string ApiKey { get; set; }
        [Persistent]
        public string ServerUrl { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
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
