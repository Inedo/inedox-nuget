using System;
using System.Collections.Generic;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.NuGet
{
    /// <summary>
    /// Publishes a NuGet package.
    /// </summary>
    [ActionProperties(
        "Publish NuGet Package",
        "Publishes a package using NuGet.",
        "NuGet")]
    [CustomEditor(typeof(PushPackageActionEditor))]
    public sealed class PushPackage : NuGetActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PushPackage"/> class.
        /// </summary>
        public PushPackage()
        {
        }

        /// <summary>
        /// Gets or sets the path of the package to publish.
        /// </summary>
        /// <remarks>
        /// This is relative to the source path.
        /// </remarks>
        [Persistent]
        public string PackagePath { get; set; }
        /// <summary>
        /// Gets or sets the API key for the NuGet server.
        /// </summary>
        /// <remarks>
        /// If not specified, the default is used.
        /// </remarks>
        [Persistent]
        public string ApiKey { get; set; }
        /// <summary>
        /// Gets or sets the server URL.
        /// </summary>
        /// <remarks>
        /// If not specified, the default is used.
        /// </remarks>
        [Persistent]
        public string ServerUrl { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Publish {0} using NuGet", this.PackagePath);
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
