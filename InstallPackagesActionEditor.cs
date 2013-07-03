using System.IO;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.NuGet
{
    /// <summary>
    /// Custom editor for the Install Packages action.
    /// </summary>
    public sealed class InstallPackagesActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtPackagesConfig;
        private TextBox txtSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallPackagesActionEditor"/> class.
        /// </summary>
        public InstallPackagesActionEditor()
        {
        }

        public override bool DisplayTargetDirectory
        {
            get { return true; }
        }

        public override void BindToForm(ActionBase extension)
        {
            EnsureChildControls();

            var action = (InstallPackages)extension;
            this.txtPackagesConfig.Text = Path.Combine(action.OverriddenSourceDirectory, action.PackagesConfigPath ?? string.Empty);
            this.txtSource.Text = action.PackageSource ?? string.Empty;
        }
        public override ActionBase CreateFromForm()
        {
            EnsureChildControls();

            return new InstallPackages
            {
                PackagesConfigPath = Path.GetFileName(this.txtPackagesConfig.Text),
                OverriddenSourceDirectory = Path.GetDirectoryName(this.txtPackagesConfig.Text),
                PackageSource = this.txtSource.Text
            };
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.txtSource = new TextBox
            {
                Width = 300
            };

            this.txtPackagesConfig = new ValidatingTextBox
            {
                Required = true,
                Width = 300,
                Text = "packages.config"
            };

            CUtil.Add(this,
                new FormFieldGroup(
                    "Package Source",
                    @"Provide the URL of the package source. If not specified, all sources in %AppData%\NuGet\NuGet.config are used.",
                    false,
                    new StandardFormField(
                        "Package Source (optional):",
                        this.txtSource
                    )
                ),
                new FormFieldGroup(
                    "Packages",
                    "Provide the location of the packages.config file.",
                    true,
                    new StandardFormField(
                        "Location of packages.config:",
                        this.txtPackagesConfig
                    )
                )
            );
        }
    }
}
