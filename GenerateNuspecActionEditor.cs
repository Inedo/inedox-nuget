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

        private ValidatingTextBox txtTitle;
        private ValidatingTextBox txtSummary;
        private ValidatingTextBox txtLanguage;
        private ValidatingTextBox txtProjectUrl;
        private ValidatingTextBox txtTags;
        private ValidatingTextBox txtIconUrl;
        private ValidatingTextBox txtLicenseUrl;
        private ValidatingTextBox txtCopyright;
        private CheckBox chkRequireLicenseAcceptance;

        private ValidatingTextBox txtDependencies;
        private ValidatingTextBox txtFrameworkAssemblies;

        public override void BindToForm(ActionBase extension)
        {
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

            this.txtId = new ValidatingTextBox { Required = true };
            this.txtVersion = new ValidatingTextBox { Required = true };
            this.txtAuthors = new ValidatingTextBox { Required = true };
            this.txtDescription = new ValidatingTextBox { Required = true, TextMode = TextBoxMode.MultiLine, Rows = 5 };

            this.txtTitle = new ValidatingTextBox();
            this.txtSummary = new ValidatingTextBox();
            this.txtLanguage = new ValidatingTextBox();
            this.txtTags = new ValidatingTextBox();
            this.txtProjectUrl = new ValidatingTextBox();
            this.txtIconUrl = new ValidatingTextBox();
            this.txtLicenseUrl = new ValidatingTextBox();
            this.txtCopyright = new ValidatingTextBox();
            this.chkRequireLicenseAcceptance = new CheckBox { Text = "Require license acceptance" };

            this.txtDependencies = new ValidatingTextBox { TextMode = TextBoxMode.MultiLine, Rows = 5 };
            this.txtFrameworkAssemblies = new ValidatingTextBox { TextMode = TextBoxMode.MultiLine, Rows = 5 };

            this.Controls.Add(
                new SlimFormField(".nuspec file:", this.txtFileName),
                new SlimFormField("Package ID:", this.txtId),
                new SlimFormField("Version:", this.txtVersion),
                new SlimFormField("Authors (comma-separated):", this.txtAuthors),
                new SlimFormField("Description:", this.txtDescription),
                new SlimFormField("Title:", this.txtTitle),
                new SlimFormField("Summary:", this.txtSummary),
                new SlimFormField("Copyright:", this.txtCopyright),
                new SlimFormField("Language:", this.txtLanguage),
                new SlimFormField("Tags (space-separated):", this.txtTags),
                new SlimFormField("Project URL:", this.txtProjectUrl),
                new SlimFormField("Icon URL:", this.txtIconUrl),
                new SlimFormField("License URL:", this.txtLicenseUrl),
                new SlimFormField("", this.chkRequireLicenseAcceptance),
                new SlimFormField("Dependencies:", this.txtDependencies)
                {
                    HelpText = HelpText.FromHtml("Provide a list of other NuGet packages that this package depends on. Dependencies should be entered one per line in the format <i>ID:version</i>.")
                },
                new SlimFormField("Framework assemblies:", this.txtFrameworkAssemblies)
                {
                    HelpText = HelpText.FromHtml("Provide a list of .NET Framework assemblies that this package depends on. Assembly names should be entered one per line. For example, <i>PresentationCore</i>.")
                }
            );
        }
    }
}
