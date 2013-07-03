using System;
using System.Linq;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.NuGet
{
    internal sealed class GenerateNuspecActionEditor : ActionEditorBase
    {
        private SourceControlFileFolderPicker txtFileName;
        private ValidatingTextBox txtId;
        private ValidatingTextBox txtVersion;
        private ValidatingTextBox txtAuthors;
        private ValidatingTextBox txtDescription;

        private TextBox txtTitle;
        private TextBox txtSummary;
        private TextBox txtLanguage;
        private TextBox txtProjectUrl;
        private TextBox txtTags;
        private TextBox txtIconUrl;
        private TextBox txtLicenseUrl;
        private TextBox txtCopyright;
        private CheckBox chkRequireLicenseAcceptance;

        private TextBox txtDependencies;
        private TextBox txtFrameworkAssemblies;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateNuspecActionEditor"/> class.
        /// </summary>
        public GenerateNuspecActionEditor()
        {
        }

        public override void BindToForm(ActionBase extension)
        {
            this.EnsureChildControls();

            var action = (GenerateNuspecAction)extension;
            this.txtFileName.Text = action.OutputFileName;
            this.txtId.Text = action.Id;
            this.txtVersion.Text = action.Version;
            this.txtAuthors.Text = string.Join(", ", action.Authors ?? new string[0]);
            this.txtDescription.Text = action.Description;
            this.txtTitle.Text = action.Title;
            this.txtSummary.Text = action.Summary;
            this.txtLanguage.Text = action.Language;
            this.txtProjectUrl.Text = action.ProjectUrl;
            this.txtTags.Text = action.Tags;
            this.txtIconUrl.Text = action.IconUrl;
            this.txtLicenseUrl.Text = action.LicenseUrl;
            this.txtCopyright.Text = action.Copyright;
            this.chkRequireLicenseAcceptance.Checked = action.RequireLicenseAcceptance;
            this.txtDependencies.Text = string.Join(Environment.NewLine, action.Dependencies ?? new string[0]);
            this.txtFrameworkAssemblies.Text = string.Join(Environment.NewLine, action.FrameworkDependencies ?? new string[0]);
        }
        public override ActionBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new GenerateNuspecAction
            {
                OutputFileName = this.txtFileName.Text,
                Id = this.txtId.Text,
                Version = this.txtVersion.Text,
                Authors = this.txtAuthors.Text.Split(',').Select(s => s.Trim()).ToArray(),
                Description = this.txtDescription.Text,
                Title = this.txtTitle.Text,
                Summary = this.txtSummary.Text,
                Language = this.txtLanguage.Text,
                ProjectUrl = this.txtProjectUrl.Text,
                Tags = this.txtTags.Text,
                IconUrl = this.txtIconUrl.Text,
                LicenseUrl = this.txtLicenseUrl.Text,
                Copyright = this.txtCopyright.Text,
                RequireLicenseAcceptance = this.chkRequireLicenseAcceptance.Checked,
                Dependencies = this.txtDependencies.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                FrameworkDependencies = this.txtFrameworkAssemblies.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
            };
        }

        protected override void CreateChildControls()
        {
            this.txtFileName = new SourceControlFileFolderPicker { DisplayMode = SourceControlBrowser.DisplayModes.FoldersAndFiles, ServerId = this.ServerId, Required = true };

            this.txtId = new ValidatingTextBox { Required = true, Width = 300 };
            this.txtVersion = new ValidatingTextBox { Required = true, Width = 300 };
            this.txtAuthors = new ValidatingTextBox { Required = true, Width = 300 };
            this.txtDescription = new ValidatingTextBox { Required = true, Width = 300, TextMode = TextBoxMode.MultiLine, Rows = 5 };

            this.txtTitle = new TextBox { Width = 300 };
            this.txtSummary = new TextBox { Width = 300 };
            this.txtLanguage = new TextBox { Width = 300 };
            this.txtTags = new TextBox { Width = 300 };
            this.txtProjectUrl = new TextBox { Width = 300 };
            this.txtIconUrl = new TextBox { Width = 300 };
            this.txtLicenseUrl = new TextBox { Width = 300 };
            this.txtCopyright = new TextBox { Width = 300 };
            this.chkRequireLicenseAcceptance = new CheckBox { Text = "Require License Acceptance" };

            this.txtDependencies = new TextBox { Width = 300, TextMode = TextBoxMode.MultiLine, Rows = 5 };
            this.txtFrameworkAssemblies = new TextBox { Width = 300, TextMode = TextBoxMode.MultiLine, Rows = 5 };

            this.Controls.Add(
                new FormFieldGroup(
                    ".nuspec File Name",
                    "Provide the name and path of the .nuspec file to generate. This may be relative to the default directory.",
                    false,
                    new StandardFormField("Output File Name:", this.txtFileName)
                ),
                new FormFieldGroup(
                    "Required Metadata",
                    "This information is required for all NuGet packages.",
                    false,
                    new StandardFormField("Package Id:", this.txtId),
                    new StandardFormField("Version:", this.txtVersion),
                    new StandardFormField("Authors (comma-separated):", this.txtAuthors),
                    new StandardFormField("Description:", this.txtDescription)
                ),
                new FormFieldGroup(
                    "Optional Metadata",
                    "While not required, this information provides additional details for the package when it is added to a feed.",
                    false,
                    new StandardFormField("Title:", this.txtTitle),
                    new StandardFormField("Summary:", this.txtSummary),
                    new StandardFormField("Copyright:", this.txtCopyright),
                    new StandardFormField("Language:", this.txtLanguage),
                    new StandardFormField("Tags (space-separated):", this.txtTags),
                    new StandardFormField("Project URL:", this.txtProjectUrl),
                    new StandardFormField("Icon URL:", this.txtIconUrl),
                    new StandardFormField("License URL:", this.txtLicenseUrl),
                    new StandardFormField("", this.chkRequireLicenseAcceptance)
                ),
                new FormFieldGroup(
                    "Dependencies",
                    "Provide a list of other NuGet packages that this package depends on. Dependencies should be entered one per line in the format <i>ID:version</i>.",
                    false,
                    new StandardFormField("Dependencies:", this.txtDependencies)
                ),
                new FormFieldGroup(
                    "Framework Assemblies",
                    "Provide a list of .NET Framework assemblies that this package depends on. Assembly names should be entered one per line. For example, <i>PresentationCore</i>.",
                    true,
                    new StandardFormField("Framework Assemblies:", this.txtFrameworkAssemblies)
                )
            );
        }
    }
}
