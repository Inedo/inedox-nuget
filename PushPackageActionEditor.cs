using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.NuGet
{
    /// <summary>
    /// Custom editor for the publish package action.
    /// </summary>
    public sealed class PushPackageActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtPackagePath;
        private TextBox txtApiKey;
        private TextBox txtServerUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="PushPackageActionEditor"/> class.
        /// </summary>
        public PushPackageActionEditor()
        {
        }

        public override bool DisplaySourceDirectory
        {
            get { return true; }
        }

        public override void BindToForm(ActionBase extension)
        {
            EnsureChildControls();

            var action = (PushPackage)extension;
            this.txtPackagePath.Text = action.PackagePath ?? string.Empty;
            this.txtApiKey.Text = action.ApiKey ?? string.Empty;
            this.txtServerUrl.Text = action.ServerUrl ?? string.Empty;
        }
        public override ActionBase CreateFromForm()
        {
            EnsureChildControls();

            return new PushPackage
            {
                PackagePath = this.txtPackagePath.Text,
                ApiKey = this.txtApiKey.Text,
                ServerUrl = this.txtServerUrl.Text
            };
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.txtPackagePath = new ValidatingTextBox
            {
                Width = 300,
                Required = true
            };

            this.txtApiKey = new TextBox
            {
                Width = 300,
                MaxLength = 200
            };

            this.txtServerUrl = new TextBox { Width = 300 };

            CUtil.Add(this,
                new FormFieldGroup(
                    "Package",
                    "Package to publish using NuGet. If a relative path is used, it is relative to the source directory.",
                    false,
                    new StandardFormField(
                        "NuGet Package:",
                        this.txtPackagePath
                    )
                ),
                new FormFieldGroup(
                    "Server Configuration",
                    "The API Key and server URL used to publish the package.",
                    true,
                    new StandardFormField(
                        "API Key:",
                        this.txtApiKey
                    ),
                    new StandardFormField(
                        "Server URL:",
                        this.txtServerUrl
                    )
                )
            );
        }
    }
}
