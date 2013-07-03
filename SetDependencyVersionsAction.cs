using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.NuGet
{
    /// <summary>
    /// Writes dependency version information to a .nuspec file.
    /// </summary>
    [ActionProperties(
        "Set Nuspec Dependency Versions",
        "Sets versions required for specific dependencies in a .nuspec file.",
        "NuGet")]
    [CustomEditor(typeof(SetDependencyVersionsActionEditor))]
    public sealed class SetDependencyVersionsAction : AgentBasedActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetDependencyVersionsAction"/> class.
        /// </summary>
        public SetDependencyVersionsAction()
        {
        }

        /// <summary>
        /// Gets or sets the name of the nuspec file.
        /// </summary>
        [Persistent]
        public string NuspecFile { get; set; }
        /// <summary>
        /// Gets or sets the dependency versions to set.
        /// </summary>
        [Persistent]
        public string[] DependencyVersions { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <remarks>
        /// This should return a user-friendly string describing what the Action does
        /// and the state of its important persistent properties.
        /// </remarks>
        public override string ToString()
        {
            return string.Format(
                "Write package dependencies {0} to {1}",
                string.Join(", ", this.DependencyVersions ?? new string[0]),
                string.IsNullOrEmpty(this.OverriddenSourceDirectory) ? this.NuspecFile : Util.Path2.Combine(this.OverriddenSourceDirectory, this.NuspecFile)
            );
        }

        /// <summary>
        /// This method is called to execute the Action.
        /// </summary>
        /// <remarks>
        /// This method is always invoked on the BuildMaster server.
        /// </remarks>
        protected override void Execute()
        {
            var dependencies = this.DependencyVersions
                .Select(d => d.Split(new[] { '=' }, 2, StringSplitOptions.None))
                .Select(d => new { Id = d[0], Version = d[1] });

            int dependenciesWritten = 0;

            var agent = this.Context.Agent.GetService<IFileOperationsExecuter>();
            var nuspecFilePath = agent.CombinePath(this.Context.SourceDirectory, this.NuspecFile);

            this.LogInformation("Reading {0}...", nuspecFilePath);
            var nuspecStream = new MemoryStream(agent.ReadFileBytes(nuspecFilePath), true);
            var xdoc = new XmlDocument();
            xdoc.Load(nuspecStream);

            var ns = xdoc.DocumentElement.NamespaceURI;
            var nsManager = new XmlNamespaceManager(xdoc.NameTable);
            nsManager.AddNamespace("n", ns);

            var dependencyNodes = xdoc
                .SelectNodes("//n:dependency", nsManager)
                .Cast<XmlElement>()
                .GroupBy(e => e.GetAttribute("id"))
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var nodesToAdd = new List<XmlElement>();

            foreach (var dependency in dependencies)
            {
                List<XmlElement> elements;
                if (dependencyNodes.TryGetValue(dependency.Id, out elements))
                {
                    this.LogDebug("Updating dependency {0}...", dependency.Id, dependency.Version);
                    foreach (var element in elements)
                        element.SetAttribute("version", dependency.Version);
                }
                else
                {
                    this.LogDebug("Adding dependency {0}...", dependency.Id, dependency.Version);
                    var element = xdoc.CreateElement("dependency", ns);
                    element.SetAttribute("id", dependency.Id);
                    element.SetAttribute("version", dependency.Version);
                    nodesToAdd.Add(element);
                }

                dependenciesWritten++;
            }

            var targetElement = (XmlElement)xdoc.SelectSingleNode("//n:dependencies", nsManager);
            foreach (var node in nodesToAdd)
                targetElement.AppendChild(node);

            nuspecStream = new MemoryStream();
            xdoc.Save(nuspecStream);

            var fileAttributes = agent.GetFileEntry(nuspecFilePath);
            if ((fileAttributes.Attributes & FileAttributes.ReadOnly) != 0)
                agent.SetAttributes(nuspecFilePath, null, fileAttributes.Attributes & ~FileAttributes.ReadOnly);

            this.LogInformation("Writing updated .nuspec file to {0}...", nuspecFilePath);
            agent.WriteFileBytes(nuspecFilePath, nuspecStream.ToArray());

            this.LogInformation("Wrote {0} dependencies", dependenciesWritten);
        }
    }
}
