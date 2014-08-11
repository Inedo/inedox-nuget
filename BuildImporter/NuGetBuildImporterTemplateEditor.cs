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
        private CheckBox chkIncludePrerelease;
        private CheckBox chkVersionUnlocked;

        public override void BindToForm(BuildImporterTemplateBase extension)
        {
            var template = (NuGetBuildImporterTemplate)extension;

            this.txtPackageId.Text = template.PackageId;
            this.txtPackageVersion.Text = template.PackageVersion;
            this.txtPackageSource.Text = template.PackageSource;
            this.chkIncludePrerelease.Checked = template.IncludePrerelease;
            this.chkVersionUnlocked.Checked = !template.VersionLocked;
        }
        public override BuildImporterTemplateBase CreateFromForm()
        {
            return new NuGetBuildImporterTemplate
            {
                PackageId = this.txtPackageId.Text,
                PackageVersion = this.txtPackageVersion.Text,
                PackageSource = this.txtPackageSource.Text,
                IncludePrerelease = this.chkIncludePrerelease.Checked,
                VersionLocked = !this.chkVersionUnlocked.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtPackageId = new ValidatingTextBox { Required = true };
            this.txtPackageVersion = new ValidatingTextBox { DefaultText = "latest" };
            this.txtPackageSource = new ValidatingTextBox { DefaultText = "default" };
            this.chkIncludePrerelease = new CheckBox { Text = "Include prerelease versions" };
            this.chkVersionUnlocked = new CheckBox { Text = "Allow version selection at build time" };

            this.Controls.Add(
                new SlimFormField("Package ID:", this.txtPackageId),
                new SlimFormField(
                    "Package version:",
                    new Div(this.txtPackageVersion),
                    new Div(this.chkIncludePrerelease),
                    new Div(this.chkVersionUnlocked)
                ),
                new SlimFormField("Package source:", this.txtPackageSource)
            );
        }
    }
}
