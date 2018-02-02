using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Inedo.Agents;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.NuGet
{
    [Tag("nuget")]
    [DisplayName("Set Nuspec Dependency Versions")]
    [Description("Sets versions required for specific dependencies in a .nuspec file.")]
    [Inedo.Web.CustomEditor(typeof(SetDependencyVersionsActionEditor))]
    public sealed class SetDependencyVersionsAction : AgentBasedActionBase
    {
        [Persistent]
        public string NuspecFile { get; set; }
        [Persistent]
        public string[] DependencyVersions { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Write NuGet dependencies ",
                    new ListHilite(this.DependencyVersions)
                ),
                new RichDescription(
                    "to ",
                    new DirectoryHilite(this.OverriddenSourceDirectory, this.NuspecFile)
                )
            );
        }

        protected override void Execute()
        {
            var dependencies = this.DependencyVersions
                .Select(d => d.Split(new[] { '=' }, 2, StringSplitOptions.None))
                .Select(d => new { Id = d[0], Version = d[1] });

            var agent = this.Context.Agent.GetService<IFileOperationsExecuter>();
            var nuspecFilePath = agent.CombinePath(this.Context.SourceDirectory, this.NuspecFile);

            XDocument xdoc;
            this.LogInformation("Reading {0}...", nuspecFilePath);
            using (var stream = agent.OpenFile(nuspecFilePath, FileMode.Open, FileAccess.Read))
            {
                xdoc = XDocument.Load(stream);
            }

            var ns = xdoc.Root.GetDefaultNamespace();

            var docDependencies = xdoc
                .Descendants(ns + "dependency")
                .GroupBy(d => (string)d.Attribute("id"))
                .ToDictionary(d => d.Key, d => (string)d.First().Attribute("version"));

            foreach (var dependency in dependencies)
                docDependencies[dependency.Id] = dependency.Version;

            var dependenciesElement = xdoc.Descendants(ns + "dependencies").First();
            dependenciesElement.RemoveNodes();
            foreach (var dependency in docDependencies)
            {
                dependenciesElement.Add(
                    new XElement(ns + "dependency", new XAttribute("id", dependency.Key), new XAttribute("version", dependency.Value))
                );
            }

            this.LogInformation("Writing updated .nuspec file to {0}...", nuspecFilePath);
            using (var stream = agent.OpenFile(nuspecFilePath, FileMode.Create, FileAccess.Write))
            {
                xdoc.Save(stream);
            }
        }
    }
}
