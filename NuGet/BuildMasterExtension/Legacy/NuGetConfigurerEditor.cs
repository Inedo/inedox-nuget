using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.NuGet
{
    internal sealed class NuGetConfigurerEditor : ExtensionConfigurerEditorBase
    {
        private ValidatingTextBox txtPackageSource;
        private ValidatingTextBox txtNuGetExe;
        private CheckBox chkUseProGet;
        private CheckBox chkClearCache;

        public override void InitializeDefaultValues()
        {
            this.BindToForm(new NuGetConfigurer());
        }
        public override void BindToForm(ExtensionConfigurerBase extension)
        {
            var configurer = (NuGetConfigurer)extension;
            this.txtPackageSource.Text = configurer.PackageSource;
            this.txtNuGetExe.Text = configurer.NuGetExe;
            this.chkUseProGet.Checked = configurer.UseProGetClient;
            this.chkClearCache.Checked = configurer.AlwaysClearNuGetCache;
        }
        public override ExtensionConfigurerBase CreateFromForm()
        {
            return new NuGetConfigurer
            {
                PackageSource = this.txtPackageSource.Text,
                NuGetExe = this.txtNuGetExe.Text,
                UseProGetClient = this.chkUseProGet.Checked,
                AlwaysClearNuGetCache = this.chkClearCache.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtPackageSource = new ValidatingTextBox
            {
                DefaultText = "nuget.org"
            };

            this.txtNuGetExe = new ValidatingTextBox
            {
                DefaultText = "use embedded nuget.exe"
            };

            this.chkUseProGet = new CheckBox
            {
                Text = "Use proget.exe instead of nuget.exe for installing packages"
            };

            this.chkClearCache = new CheckBox
            {
                Text = "Attempt to clear the NuGet cache before packages are installed"
            };

            this.Controls.Add(
                new SlimFormField("Package source:", this.txtPackageSource),
                new SlimFormField("NuGet client:", this.txtNuGetExe),
                new SlimFormField("Options:", new Div(this.chkUseProGet), new Div(this.chkClearCache))
            );
        }
    }
}
