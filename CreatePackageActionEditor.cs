using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.NuGet
{
    /// <summary>
    /// Custom editor for the Create Package action.
    /// </summary>
    public sealed class CreatePackageActionEditor : ActionEditorBase
    {
        private DropDownList ddlSourceType;
        private SourceControlFileFolderPicker txtProjectPath;
        private TextBox txtVersion;
        private CheckBox chkSymbols;
        private TextBox txtProperties;
        private SourceControlFileFolderPicker txtNuspecPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatePackageActionEditor"/> class.
        /// </summary>
        public CreatePackageActionEditor()
        {
            this.ValidateBeforeSave += this.CreatePackageActionEditor_ValidateBeforeSave;
        }

        public override bool DisplaySourceDirectory
        {
            get { return true; }
        }
        public override bool DisplayTargetDirectory
        {
            get { return true; }
        }

        public override void BindToForm(ActionBase extension)
        {
            this.EnsureChildControls();

            var action = (CreatePackage)extension;
            if (action.ProjectPath == null || action.ProjectPath.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
            {
                this.txtNuspecPath.Text = action.ProjectPath;
                this.ddlSourceType.SelectedValue = "nuspec";
            }
            else
            {
                this.txtProjectPath.Text = action.ProjectPath;
                this.ddlSourceType.SelectedValue = "msbuild";
            }

            this.txtVersion.Text = action.Version;
            this.chkSymbols.Checked = action.Symbols;
            if (action.Properties != null)
                this.txtProperties.Text = string.Join(Environment.NewLine, action.Properties);
        }
        public override ActionBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new CreatePackage
            {
                ProjectPath = this.ddlSourceType.SelectedValue == "nuspec" ? this.txtNuspecPath.Text : this.txtProjectPath.Text,
                Version = this.txtVersion.Text,
                Symbols = this.chkSymbols.Checked,
                Build = true,
                Properties = this.txtProperties.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
            };
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

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

            this.txtVersion = new ValidatingTextBox { Width = 300, DefaultText = "default" };

            this.txtProperties = new TextBox
            {
                Width = 300,
                Rows = 5,
                TextMode = TextBoxMode.MultiLine
            };

            this.chkSymbols = new CheckBox { Text = "Create Symbol Package" };

            var ctlNuspecFileField = new StandardFormField(".nuspec File:", this.txtNuspecPath) { ID = "ctlNuspecFileField" };
            var ctlProjectFileField = new StandardFormField("MSBuild Project:", this.txtProjectPath) { ID = "ctlProjectFileField" };

            var ffgProperties = new FormFieldGroup(
                "Properties",
                "Provide additional properties to pass to NuGet. Use the format Property=Value (one per line). For example:<br/><i>Configuration=Release</i>",
                false,
                new StandardFormField(
                    "Properties:",
                    this.txtProperties
                )
            ) { ID = "ffgProperties" };

            this.Controls.Add(
                new FormFieldGroup(
                    "Source Files",
                    "Provide the file which will be used to create the NuGet package.",
                    false,
                    new StandardFormField(
                        "Source:",
                        this.ddlSourceType
                    ),
                    ctlNuspecFileField,
                    ctlProjectFileField
                ),
                new FormFieldGroup(
                    "Version",
                    "Specify a version for the NuGet package. If not specified, the version in the nuspec file will be used.",
                    false,
                    new StandardFormField(
                        "Package Version:",
                        this.txtVersion
                    )
                ),
                ffgProperties,
                new FormFieldGroup(
                    "Options",
                    "Additional options for controlling package creation.",
                    true,
                    new StandardFormField(
                        string.Empty,
                        this.chkSymbols
                    )
                ),
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
                    e.ValidLevel = ValidationLevels.Error;
                    e.Message = "Properties must be in the form PROPERTY=VALUE.";
                    return;
                }
            }

            if (ddlSourceType.SelectedValue == "nuspec")
            {
                if (string.IsNullOrEmpty(this.txtNuspecPath.Text))
                {
                    e.ValidLevel = ValidationLevels.Error;
                    e.Message = ".nuspec file path is required.";
                    return;
                }
            }
            else if (ddlSourceType.SelectedValue == "msbuild")
            {
                if (string.IsNullOrEmpty(this.txtProjectPath.Text))
                {
                    e.ValidLevel = ValidationLevels.Error;
                    e.Message = "MSBuild project file path is required.";
                    return;
                }
            }
        }
    }
}
