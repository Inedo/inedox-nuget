using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;

namespace Inedo.BuildMasterExtensions.NuGet
{
    internal sealed class InstallSolutionPackagesActionEditor : ActionEditorBase
    {
        private SourceControlFileFolderPicker txtInstallPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallSolutionPackagesActionEditor"/> class.
        /// </summary>
        public InstallSolutionPackagesActionEditor()
        {
        }

        public override bool DisplaySourceDirectory
        {
            get { return true; }
        }

        public override void BindToForm(ActionBase extension)
        {
            this.EnsureChildControls();

            var action = (InstallSolutionPackagesAction)extension;
            this.txtInstallPath.Text = action.PackageOutputDirectory;
        }
        public override ActionBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new InstallSolutionPackagesAction
            {
                PackageOutputDirectory = this.txtInstallPath.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtInstallPath = new SourceControlFileFolderPicker
            {
                ServerId = this.ServerId,
                DefaultText = "default",
                DisplayMode = SourceControlBrowser.DisplayModes.Folders
            };

            this.Controls.Add(
                new FormFieldGroup(
                    "Package Install Path",
                    "By default, packages will be installed to the <i>packages</i> folder at the solution level. If you have multiple solutions in the specified path or need to override this behavior, you may set the path explicitly here.",
                    true,
                    new StandardFormField(
                        "Package Install Path:",
                        this.txtInstallPath
                    )
                )
            );
        }
    }
}
