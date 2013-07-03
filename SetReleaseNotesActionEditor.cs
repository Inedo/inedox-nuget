using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.NuGet
{
    /// <summary>
    /// Custom editor for the Set Release Notes action.
    /// </summary>
    public sealed class SetReleaseNotesActionEditor : ActionEditorBase
    {
        private DropDownList ddlReleaseNotesSource;
        private ValidatingTextBox txtNuspecFileName;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetReleaseNotesActionEditor"/> class.
        /// </summary>
        public SetReleaseNotesActionEditor()
        {
            this.ValidateBeforeSave += (s, e) =>
            {
                if (this.ddlReleaseNotesSource.SelectedValue == "ISSUES" || this.ddlReleaseNotesSource.SelectedValue == "ALL")
                {
                    var applicationRow = StoredProcs.Applications_GetApplication(this.ApplicationId).ExecuteDataRow();
                    if (Convert.IsDBNull(applicationRow[TableDefs.Applications_Extended.IssueTracking_Provider_Id]))
                    {
                        e.ValidLevel = ValidationLevels.Warning;
                        e.Message = "The application does not currently have an issue tracker configured. To ignore this warning and continue, click Save again.";
                    }
                }
            };
        }

        public override bool DisplaySourceDirectory
        {
            get { return true; }
        }

        public override void BindToForm(ActionBase extension)
        {
            EnsureChildControls();

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
            EnsureChildControls();

            return new SetReleaseNotes
            {
                IncludeReleaseNotes = this.ddlReleaseNotesSource.SelectedValue == "RELEASE_NOTES" || this.ddlReleaseNotesSource.SelectedValue == "ALL",
                IncludeIssues = this.ddlReleaseNotesSource.SelectedValue == "ISSUES" || this.ddlReleaseNotesSource.SelectedValue == "ALL",
                NuspecFileName = this.txtNuspecFileName.Text
            };
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.ddlReleaseNotesSource = new DropDownList();
            this.ddlReleaseNotesSource.Items.Add(new ListItem("Application Release Notes", "RELEASE_NOTES"));
            this.ddlReleaseNotesSource.Items.Add(new ListItem("Resolved Issue Tracker Items", "ISSUES"));
            this.ddlReleaseNotesSource.Items.Add(new ListItem("Both", "ALL"));
            this.ddlReleaseNotesSource.SelectedValue = "RELEASE_NOTES";

            this.txtNuspecFileName = new ValidatingTextBox
            {
                Required = true,
                Width = 300
            };

            CUtil.Add(this,
                new FormFieldGroup(
                    "Release Notes Source",
                    "Select whether to generate release notes from BuildMaster's Application Release Notes, the Application's associated Issue Tracker, or both.",
                    false,
                    new StandardFormField(
                        "Generate Release Notes From:",
                        this.ddlReleaseNotesSource
                    )
                ),
                new FormFieldGroup(
                    "Nuspec File",
                    "Provide the name of the .nuspec file where the release notes will be written to. This file name is relative to the source directory.",
                    true,
                    new StandardFormField(
                        "Nuspec File Name:",
                        this.txtNuspecFileName
                    )
                )
            );
        }
    }
}
