using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.NuGet
{
    internal sealed class CreatePackageActionEditor : ActionEditorBase
    {
        private DropDownList ddlSourceType;
        private SourceControlFileFolderPicker txtProjectPath;
        private ValidatingTextBox txtVersion;
        private CheckBox chkSymbols;
        private CheckBox chkIncludeReferencedProjects;
        private ValidatingTextBox txtProperties;
        private SourceControlFileFolderPicker txtNuspecPath;

        public CreatePackageActionEditor()
        {
            this.ValidateBeforeSave += this.CreatePackageActionEditor_ValidateBeforeSave;
        }

        public override bool DisplayTargetDirectory
        {
            get { return true; }
        }
        public override string TargetDirectoryLabel
        {
            get { return "In:"; }
        }

        public override void BindToForm(ActionBase extension)
        {
            var action = (CreatePackage)extension;
            if (action.ProjectPath == null || action.ProjectPath.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
            {
                this.txtNuspecPath.Text = Util.Path2.Combine(action.OverriddenSourceDirectory, action.ProjectPath);
                this.ddlSourceType.SelectedValue = "nuspec";
            }
            else
            {
                this.txtProjectPath.Text = Util.Path2.Combine(action.OverriddenSourceDirectory, action.ProjectPath);
                this.ddlSourceType.SelectedValue = "msbuild";
            }

            this.txtVersion.Text = action.Version;
            this.chkSymbols.Checked = action.Symbols;
            this.chkIncludeReferencedProjects.Checked = action.IncludeReferencedProjects;
            if (action.Properties != null)
                this.txtProperties.Text = string.Join(Environment.NewLine, action.Properties);
        }
        public override ActionBase CreateFromForm()
        {
            var path = this.ddlSourceType.SelectedValue == "nuspec" ? this.txtNuspecPath.Text : this.txtProjectPath.Text;

            return new CreatePackage
            {
                OverriddenSourceDirectory = Util.Path2.GetDirectoryName(path),
                ProjectPath = Util.Path2.GetFileName(path),
                Version = this.txtVersion.Text,
                Symbols = this.chkSymbols.Checked,
                IncludeReferencedProjects = this.chkIncludeReferencedProjects.Checked,
                Build = true,
                Properties = this.txtProperties.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
            };
        }

        protected override void CreateChildControls()
        {
            this.ddlSourceType = new DropDownList { ID = "ddlSourceType" };
            this.ddlSourceType.Items.Add(new ListItem(".nuspec file", "nuspec"));
            this.ddlSourceType.Items.Add(new ListItem("msbuild project", "msbuild"));

            this.txtNuspecPath = new SourceControlFileFolderPicker
            {
                DisplayMode = SourceControlBrowser.DisplayModes.FoldersAndFiles,
                ServerId = this.ServerId
            };

            this.txtProjectPath = new SourceControlFileFolderPicker
            {
                DisplayMode = SourceControlBrowser.DisplayModes.FoldersAndFiles,
                ServerId = this.ServerId
            };

            this.txtVersion = new ValidatingTextBox { DefaultText = "default (nuspec or project version)" };

            this.txtProperties = new ValidatingTextBox
            {
                Rows = 5,
                TextMode = TextBoxMode.MultiLine,
                DefaultText = "none"
            };

            this.chkSymbols = new CheckBox { Text = "Create symbol package" };
            this.chkIncludeReferencedProjects = new CheckBox { Text = "Include referenced projects" };

            var ctlNuspecFileField = new SlimFormField(".nuspec file:", this.txtNuspecPath) { ID = "ctlNuspecFileField" };
            var ctlProjectFileField = new SlimFormField("MSBuild project:", this.txtProjectPath) { ID = "ctlProjectFileField" };

            var ffgProperties = new SlimFormField("Properties:", this.txtProperties)
            {
                ID = "ffgProperties",
                HelpText = HelpText.FromHtml("Provide additional properties to pass to NuGet. Use the format Property=Value (one per line). For example:<br/><i>Configuration=Release</i>")
            };

            this.Controls.Add(
                new SlimFormField("Source:", this.ddlSourceType),
                ctlNuspecFileField,
                ctlProjectFileField,
                new SlimFormField("Package version:", this.txtVersion),
                ffgProperties,
                new SlimFormField("Options:", new Div(this.chkSymbols), new Div(this.chkIncludeReferencedProjects)),
                new RenderJQueryDocReadyDelegator(
                    w =>
                    {
                        w.WriteLine("var updateFields = function() {");
                        w.WriteLine("  if($('#{0}').val() == 'nuspec') {{", this.ddlSourceType.ClientID);
                        w.WriteLine("    $('#{0}').show();", ctlNuspecFileField.ClientID);
                        w.WriteLine("    $('#{0}').hide();", ctlProjectFileField.ClientID);
                        w.WriteLine("    $('#{0}').hide();", ffgProperties.ClientID);
                        w.WriteLine("  } else {");
                        w.WriteLine("    $('#{0}').show();", ctlProjectFileField.ClientID);
                        w.WriteLine("    $('#{0}').hide();", ctlNuspecFileField.ClientID);
                        w.WriteLine("    $('#{0}').show();", ffgProperties.ClientID);
                        w.WriteLine("  }");
                        w.WriteLine("};");
                        w.WriteLine("updateFields();");
                        w.WriteLine("$('#{0}').change(updateFields);", this.ddlSourceType.ClientID);
                    }
                )
            );
        }

        private void CreatePackageActionEditor_ValidateBeforeSave(object sender, ValidationEventArgs<ActionBase> e)
        {
            var properties = this.txtProperties.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var property in properties)
            {
                if (!property.Contains("="))
                {
                    e.ValidLevel = ValidationLevel.Error;
                    e.Message = "Properties must be in the form PROPERTY=VALUE.";
                    return;
                }
            }

            if (ddlSourceType.SelectedValue == "nuspec")
            {
                if (string.IsNullOrEmpty(this.txtNuspecPath.Text))
                {
                    e.ValidLevel = ValidationLevel.Error;
                    e.Message = ".nuspec file path is required.";
                    return;
                }
            }
            else if (ddlSourceType.SelectedValue == "msbuild")
            {
                if (string.IsNullOrEmpty(this.txtProjectPath.Text))
                {
                    e.ValidLevel = ValidationLevel.Error;
                    e.Message = "MSBuild project file path is required.";
                    return;
                }
            }
        }
    }
}
