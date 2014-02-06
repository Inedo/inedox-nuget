using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.NuGet
{
    internal sealed class NuGetConfigurerEditor : ExtensionConfigurerEditorBase
    {
        private ValidatingTextBox txtPackageSource;
        private ValidatingTextBox txtNuGetExe;
        private CheckBox chkUseProGet;

        public override void InitializeDefaultValues()
        {
            this.BindToForm(new NuGetConfigurer());
        }
        public override void BindToForm(ExtensionConfigurerBase extension)
        {
            this.EnsureChildControls();

            var configurer = (NuGetConfigurer)extension;
            this.txtPackageSource.Text = configurer.PackageSource;
            this.txtNuGetExe.Text = configurer.NuGetExe;
            this.chkUseProGet.Checked = configurer.UseProGetClient;
        }
        public override ExtensionConfigurerBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new NuGetConfigurer
            {
                PackageSource = this.txtPackageSource.Text,
                NuGetExe = this.txtNuGetExe.Text,
                UseProGetClient = this.chkUseProGet.Checked
            };
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.txtPackageSource = new ValidatingTextBox
            {
                Required = false,
                DefaultText = "default",
                Width = 300
            };

            this.txtNuGetExe = new ValidatingTextBox
            {
                Required = false,
                DefaultText = "use embedded nuget.exe",
                Width = 300
            };

            this.chkUseProGet = new CheckBox
            {
                Text = "Use proget.exe instead of nuget.exe for installing packages"
            };

            this.Controls.Add(
                new FormFieldGroup(
                    "Package Source",
                    "Provide the URL of the package source.",
                    false,
                    new StandardFormField(
                        "Package Source:",
                        this.txtPackageSource
                    )
                ),
                new FormFieldGroup(
                    "NuGet Client",
                    "Specify the full path to a nuget.exe to use instead of the embedded NuGet client. If this is blank, the NuGet client included with the extension is used instead.",
                    false,
                    new StandardFormField(
                        "Path to NuGet.exe:",
                        this.txtNuGetExe
                    )
                ),
                new FormFieldGroup(
                    "Additional Options",
                    "These options control the behavior of certain NuGet actions.",
                    true,
                    new StandardFormField(
                        string.Empty,
                        this.chkUseProGet
                    )
                )
            );
        }
    }
}
