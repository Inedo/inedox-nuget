using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.NuGet.BuildImporter
{
    [CustomEditor(typeof(NuGetBuildImporterTemplateEditor))]
    internal sealed class NuGetBuildImporterTemplate : BuildImporterTemplateBase<NuGetBuildImporter>
    {
        [Persistent]
        public string PackageId { get; set; }
        [Persistent]
        public string PackageVersion { get; set; }
        [Persistent]
        public string PackageSource { get; set; }
        [Persistent]
        public bool IncludePrerelease { get; set; }
        [Persistent]
        public bool VersionLocked { get; set; }
        [Persistent]
        public string AdditionalArguments { get; set; }
        [Persistent]
        public bool CaptureIdAndVersion { get; set; }
        [Persistent]
        public string PackageArtifactRoot { get; set; }
        [Persistent]
        public bool IncludeVersionInArtifactName { get; set; }

        public override ExtensionComponentDescription GetDescription()
        {
            var description = new ExtensionComponentDescription(
                "Import NuGet package ",
                new Hilite(this.PackageId)
            );

            if (!string.IsNullOrEmpty(this.PackageVersion))
                description.AppendContent(new Hilite(this.PackageVersion));

            description.AppendContent(
                " from ",
                new Hilite(!string.IsNullOrEmpty(this.PackageSource) ? this.PackageSource : "default package source")
            );

            if (this.CaptureIdAndVersion)
                description.AppendContent(" and set $ImportedPackageId and $ImportedPackageVersion build variables");

            return description;
        }
    }
}
