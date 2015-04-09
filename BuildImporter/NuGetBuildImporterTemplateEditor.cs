using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Web.Controls.Extensions.BuildImporters;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.NuGet.BuildImporter
{
    internal sealed class NuGetBuildImporterTemplateEditor : BuildImporterTemplateEditorBase
    {
        private ValidatingTextBox txtPackageId;
        private ValidatingTextBox txtPackageVersion;
        private ValidatingTextBox txtPackageSource;
        private ValidatingTextBox txtAdditionalArguments;
        private ValidatingTextBox txtPackageArtifactRoot;
        private CheckBox chkIncludePrerelease;
        private CheckBox chkVersionUnlocked;
        private CheckBox chkCaptureIdAndVersion;
        private CheckBox chkIncludeVersionInArtifactName;

        public override void BindToForm(BuildImporterTemplateBase extension)
        {
            var template = (NuGetBuildImporterTemplate)extension;

            this.txtPackageId.Text = template.PackageId;
            this.txtPackageVersion.Text = template.PackageVersion;
            this.txtPackageSource.Text = template.PackageSource;
            this.txtAdditionalArguments.Text = template.AdditionalArguments;
            this.txtPackageArtifactRoot.Text = template.PackageArtifactRoot;
            this.chkIncludePrerelease.Checked = template.IncludePrerelease;
            this.chkVersionUnlocked.Checked = !template.VersionLocked;
            this.chkCaptureIdAndVersion.Checked = template.CaptureIdAndVersion;
            this.chkIncludeVersionInArtifactName.Checked = template.IncludeVersionInArtifactName;
        }
        public override BuildImporterTemplateBase CreateFromForm()
        {
            return new NuGetBuildImporterTemplate
            {
                PackageId = this.txtPackageId.Text,
                PackageVersion = this.txtPackageVersion.Text,
                PackageSource = this.txtPackageSource.Text,
                AdditionalArguments = this.txtAdditionalArguments.Text,
                PackageArtifactRoot = this.txtPackageArtifactRoot.Text,
                IncludePrerelease = this.chkIncludePrerelease.Checked,
                VersionLocked = !this.chkVersionUnlocked.Checked,
                CaptureIdAndVersion = this.chkCaptureIdAndVersion.Checked,
                IncludeVersionInArtifactName = this.chkIncludeVersionInArtifactName.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtPackageId = new ValidatingTextBox { Required = true };
            this.txtPackageVersion = new ValidatingTextBox { DefaultText = "latest" };
            this.txtPackageSource = new ValidatingTextBox { DefaultText = "default" };
            this.txtAdditionalArguments = new ValidatingTextBox { DefaultText = "none" };
            this.txtPackageArtifactRoot = new ValidatingTextBox { DefaultText = "/" };
            this.chkIncludePrerelease = new CheckBox { Text = "Include prerelease versions" };
            this.chkVersionUnlocked = new CheckBox { Text = "Allow version selection at build time" };
            this.chkCaptureIdAndVersion = new CheckBox
            {
                Text = "Capture $ImportedPackageId and $ImportedPackageVersion build variables",
                Checked = true
            };

            this.chkIncludeVersionInArtifactName = new CheckBox { Text = "Include package version in build artifact name" };

            this.Controls.Add(
                new SlimFormField("Package ID:", this.txtPackageId),
                new SlimFormField(
                    "Package version:",
                    new Div(this.txtPackageVersion),
                    new Div(this.chkIncludePrerelease),
                    new Div(this.chkVersionUnlocked)
                ),
                new SlimFormField("Package source:", this.txtPackageSource),
                new SlimFormField("Package root:", this.txtPackageArtifactRoot)
                {
                    HelpText = "Optionally specify a relative path within the package to only capture files beneath that path as part of the build artifact."
                },
                new SlimFormField("Additional arguments:", this.txtAdditionalArguments)
                {
                    HelpText = "Optionally supply any additional arguments that will be passed to NuGet.exe when the package is installed."
                },
                new SlimFormField(
                    "Options:",
                    new Div(this.chkCaptureIdAndVersion),
                    new Div(this.chkIncludeVersionInArtifactName)
                )
            );
        }
    }
}
