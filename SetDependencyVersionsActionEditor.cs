using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.NuGet
{
    internal sealed class SetDependencyVersionsActionEditor : ActionEditorBase
    {
        private SourceControlFileFolderPicker txtNuspecFile;
        private ValidatingTextBox txtVersions;

        public SetDependencyVersionsActionEditor()
        {
            this.ValidateBeforeSave += this.SetDependencyVersionsActionEditor_ValidateBeforeSave;
        }

        public override void BindToForm(ActionBase extension)
        {
            this.EnsureChildControls();

            var action = (SetDependencyVersionsAction)extension;
            this.txtNuspecFile.Text = string.IsNullOrEmpty(action.OverriddenSourceDirectory) ? action.NuspecFile : Util.Path2.Combine(action.OverriddenSourceDirectory, action.NuspecFile);
            this.txtVersions.Text = string.Join(Environment.NewLine, action.DependencyVersions ?? new string[0]);
        }
        public override ActionBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new SetDependencyVersionsAction
            {
                OverriddenSourceDirectory = Util.NullIf(Util.Path2.GetDirectoryName(this.txtNuspecFile.Text), string.Empty),
                NuspecFile = Util.Path2.GetFileName(this.txtNuspecFile.Text),
                DependencyVersions = TryParseDependencies(this.txtVersions.Text)
            };
        }

        protected override void CreateChildControls()
        {
            this.txtNuspecFile = new SourceControlFileFolderPicker
            {
                ServerId = this.ServerId,
                DisplayMode = SourceControlBrowser.DisplayModes.FoldersAndFiles,
                Required = true
            };

            this.txtVersions = new ValidatingTextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Required = true,
                Rows = 3,
                Width = 300
            };

            this.Controls.Add(
                new FormFieldGroup(
                    "Nuspec File",
                    "Provide the path to the .nuspec file to update. If the path is relative, the default source directory is used.",
                    false,
                    new StandardFormField(
                        "Nuspec File:",
                        this.txtNuspecFile
                    )
                ),
                new FormFieldGroup(
                    "Dependencies",
                    "Provide a list of dependency versions to write to the .nuspec file in the format <i>Id=Version</i> (one per line). For example:<br/><i>jQuery=[1.9.1]<br/>Internal.Library=[%RELNO%.%BLDNO%]</i>",
                    true,
                    new StandardFormField(
                        "Dependency Versions:",
                        this.txtVersions
                    )
                )
            );
        }

        private static string[] TryParseDependencies(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var lines = text
                .Trim()
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim());

            var list = new List<string>();

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                    return null;

                var parts = line.Split(new[] { '=' }, 2, StringSplitOptions.None);
                if (parts.Length != 2)
                    return null;

                parts[0] = parts[0].Trim();
                parts[1] = parts[1].Trim();
                if (string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
                    return null;

                list.Add(parts[0] + "=" + parts[1]);
            }

            return list.ToArray();
        }
        private void SetDependencyVersionsActionEditor_ValidateBeforeSave(object sender, ValidationEventArgs<ActionBase> e)
        {
            if (TryParseDependencies(this.txtVersions.Text) == null)
            {
                e.ValidLevel = ValidationLevel.Error;
                e.Message = "Invalid dependency version format.";
                return;
            }
        }
    }
}
