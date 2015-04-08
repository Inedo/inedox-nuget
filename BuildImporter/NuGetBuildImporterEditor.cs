using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Web.Controls.Extensions.BuildImporters;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.NuGet.BuildImporter
{
    internal sealed class NuGetBuildImporterEditor : BuildImporterEditorBase<NuGetBuildImporterTemplate>
    {
        private ValidatingTextBox txtPackageVersion;
        private CheckBox chkIncludePrerelease;

        public override BuildImporterBase CreateFromForm()
        {
            return new NuGetBuildImporter
            {
                PackageId = this.Template.PackageId,
                PackageVersion = this.txtPackageVersion.Text,
                IncludePrerelease = this.chkIncludePrerelease.Checked,
                PackageSource = this.Template.PackageSource,
                AdditionalArguments = this.Template.AdditionalArguments
            };
        }

        protected override void CreateChildControls()
        {
            this.txtPackageVersion = new ValidatingTextBox
            {
                DefaultText = "latest",
                Text = this.Template.PackageVersion,
                Enabled = !this.Template.VersionLocked
            };

            this.chkIncludePrerelease = new CheckBox
            {
                Text = "Include prerelease versions",
                Checked = this.Template.IncludePrerelease,
                Enabled = !this.Template.VersionLocked
            };

            this.Controls.Add(
                new SlimFormField("Package ID:", this.Template.PackageId),
                new SlimFormField("Package version:", new Div(this.txtPackageVersion), new Div(this.chkIncludePrerelease))
            );
        }
    }
}
