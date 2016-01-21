using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.NuGet
{
    internal sealed class PushPackageActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtPackagePath;
        private ValidatingTextBox txtApiKey;
        private ValidatingTextBox txtServerUrl;

        public override void BindToForm(ActionBase extension)
        {
            var action = (PushPackage)extension;
            this.txtPackagePath.Text = Inedo.IO.PathEx.Combine(action.OverriddenSourceDirectory, action.PackagePath);
            this.txtApiKey.Text = action.ApiKey;
            this.txtServerUrl.Text = action.ServerUrl;
        }
        public override ActionBase CreateFromForm()
        {
            return new PushPackage
            {
                OverriddenSourceDirectory = Inedo.IO.PathEx.GetDirectoryName(this.txtPackagePath.Text),
                PackagePath = Inedo.IO.PathEx.GetFileName(this.txtPackagePath.Text),
                ApiKey = this.txtApiKey.Text,
                ServerUrl = this.txtServerUrl.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtPackagePath = new ValidatingTextBox
            {
                Required = true
            };

            this.txtApiKey = new ValidatingTextBox
            {
                MaxLength = 200
            };

            this.txtServerUrl = new ValidatingTextBox
            {
                DefaultText = "default"
            };

            this.Controls.Add(
                new SlimFormField("NuGet package:", this.txtPackagePath),
                new SlimFormField("API key:", this.txtApiKey),
                new SlimFormField("Server URL:", this.txtServerUrl)
            );
        }
    }
}
