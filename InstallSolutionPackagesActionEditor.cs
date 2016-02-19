using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.NuGet
{
    internal sealed class InstallSolutionPackagesActionEditor : ActionEditorBase
    {
        private SourceControlFileFolderPicker txtInstallPath;

        public override bool DisplaySourceDirectory => true;
        public override string SourceDirectoryLabel => "For projects in:";

        public override void BindToForm(ActionBase extension)
        {
            var action = (InstallSolutionPackagesAction)extension;
            this.txtInstallPath.Text = action.PackageOutputDirectory;
        }
        public override ActionBase CreateFromForm()
        {
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
                DefaultText = "$CurrentDirectory\\packages",
                DisplayMode = SourceControlBrowser.DisplayModes.Folders
            };

            this.Controls.Add(
                new SlimFormField("To:", this.txtInstallPath)
                {
                    HelpText = "By default, packages will be installed to the <i>packages</i> folder at the solution level. If you have multiple solutions in the specified path or need to override this behavior, you may set the path explicitly here."
                }
            );
        }
    }
}
