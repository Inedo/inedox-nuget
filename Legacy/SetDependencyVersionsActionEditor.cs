using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.IO;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.NuGet
{
    internal sealed class SetDependencyVersionsActionEditor : ActionEditorBase
    {
        private FileBrowserTextBox txtNuspecFile;
        private ValidatingTextBox txtVersions;

        public SetDependencyVersionsActionEditor()
        {
            this.ValidateBeforeSave += this.SetDependencyVersionsActionEditor_ValidateBeforeSave;
        }

        public override void BindToForm(ActionBase extension)
        {
            var action = (SetDependencyVersionsAction)extension;
            this.txtNuspecFile.Text = string.IsNullOrEmpty(action.OverriddenSourceDirectory) ? action.NuspecFile : PathEx.Combine(action.OverriddenSourceDirectory, action.NuspecFile);
            this.txtVersions.Text = string.Join(Environment.NewLine, action.DependencyVersions ?? new string[0]);
        }
        public override ActionBase CreateFromForm()
        {
            return new SetDependencyVersionsAction
            {
                OverriddenSourceDirectory = Util.NullIf(PathEx.GetDirectoryName(this.txtNuspecFile.Text), string.Empty),
                NuspecFile = PathEx.GetFileName(this.txtNuspecFile.Text),
                DependencyVersions = TryParseDependencies(this.txtVersions.Text)
            };
        }

        protected override void CreateChildControls()
        {
            this.txtNuspecFile = new FileBrowserTextBox
            {
                ServerId = this.ServerId,
                IncludeFiles = true,
                Required = true
            };

            this.txtVersions = new ValidatingTextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Required = true,
                Rows = 3
            };

            this.Controls.Add(
                new SlimFormField("Nuspec file:", this.txtNuspecFile),
                new SlimFormField("Dependencies:", this.txtVersions)
                {
                    HelpText = "Provide a list of dependency versions to write to the .nuspec file in the format <i>Id=Version</i> (one per line). For example:<br/><i>jQuery=[1.9.1]<br/>Internal.Library=[$ReleaseName]</i>"
                }
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
