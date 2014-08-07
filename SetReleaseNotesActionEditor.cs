using System.Linq;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.NuGet
{
    internal sealed class SetReleaseNotesActionEditor : ActionEditorBase
    {
        private DropDownList ddlReleaseNotesSource;
        private ValidatingTextBox txtNuspecFileName;

        public SetReleaseNotesActionEditor()
        {
            this.ValidateBeforeSave += this.SetReleaseNotesActionEditor_ValidateBeforeSave;
        }

        public override void BindToForm(ActionBase extension)
        {
            var action = (SetReleaseNotes)extension;
            if (action.IncludeReleaseNotes && !action.IncludeIssues)
                this.ddlReleaseNotesSource.SelectedValue = "RELEASE_NOTES";
            else if (!action.IncludeReleaseNotes && action.IncludeIssues)
                this.ddlReleaseNotesSource.SelectedValue = "ISSUES";
            else if (action.IncludeReleaseNotes && action.IncludeIssues)
                this.ddlReleaseNotesSource.SelectedValue = "ALL";

            this.txtNuspecFileName.Text = action.NuspecFileName ?? string.Empty;
        }
        public override ActionBase CreateFromForm()
        {
            return new SetReleaseNotes
            {
                IncludeReleaseNotes = this.ddlReleaseNotesSource.SelectedValue == "RELEASE_NOTES" || this.ddlReleaseNotesSource.SelectedValue == "ALL",
                IncludeIssues = this.ddlReleaseNotesSource.SelectedValue == "ISSUES" || this.ddlReleaseNotesSource.SelectedValue == "ALL",
                NuspecFileName = this.txtNuspecFileName.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.ddlReleaseNotesSource = new DropDownList();
            this.ddlReleaseNotesSource.Items.Add(new ListItem("Application Release Notes", "RELEASE_NOTES"));
            this.ddlReleaseNotesSource.Items.Add(new ListItem("Resolved Issue Tracker Items", "ISSUES"));
            this.ddlReleaseNotesSource.Items.Add(new ListItem("Both", "ALL"));
            this.ddlReleaseNotesSource.SelectedValue = "RELEASE_NOTES";

            this.txtNuspecFileName = new ValidatingTextBox { Required = true };

            this.Controls.Add(
                new SlimFormField("From:", this.ddlReleaseNotesSource),
                new SlimFormField("Nuspec file:", this.txtNuspecFileName)
            );
        }

        private void SetReleaseNotesActionEditor_ValidateBeforeSave(object sender, ValidationEventArgs<ActionBase> e)
        {
            if (this.ddlReleaseNotesSource.SelectedValue == "ISSUES" || this.ddlReleaseNotesSource.SelectedValue == "ALL")
            {
                var application = StoredProcs.Applications_GetApplication(this.ApplicationId)
                    .Execute()
                    .Applications_Extended
                    .First();

                if (application.IssueTracking_Provider_Id == null)
                {
                    e.ValidLevel = ValidationLevel.Warning;
                    e.Message = "The application does not have an issue tracker configured. To ignore this warning and continue, click save again.";
                }
            }
        }
    }
}
