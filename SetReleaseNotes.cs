using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.NuGet
{
    /// <summary>
    /// An action for writing release notes to a NuGet nuspec file.
    /// </summary>
    [ActionProperties(
        "Set Release Notes",
        "Writes release notes to a .nuspec file using BuildMaster's release notes or the Application's issue tracker.",
        "NuGet")]
    [CustomEditor(typeof(SetReleaseNotesActionEditor))]
    public sealed class SetReleaseNotes : RemoteActionBase
    {
        /// <summary>
        /// Namespace URI for nuspec files.
        /// </summary>
        private const string NamespaceUri = "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd";

        /// <summary>
        /// Initializes a new instance of the <see cref="SetReleaseNotes"/> class.
        /// </summary>
        public SetReleaseNotes()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether to include resolved issues in the release notes.
        /// </summary>
        [Persistent]
        public bool IncludeIssues { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to include application release notes in the release notes.
        /// </summary>
        [Persistent]
        public bool IncludeReleaseNotes { get; set; }
        /// <summary>
        /// Gets or sets the name of the .nuspec file to write the release notes to.
        /// </summary>
        [Persistent]
        public string NuspecFileName { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var source = "";

            if (this.IncludeReleaseNotes && !this.IncludeIssues)
                source = " from the application's release notes";
            else if (!this.IncludeReleaseNotes && this.IncludeIssues)
                source = " from the application's issue tracker";
            else if (this.IncludeReleaseNotes && this.IncludeIssues)
                source = " from the application's release notes and issue tracker";

            return string.Format(
                "Write release notes to {0} in {1}{2}",
                this.NuspecFileName,
                Util.CoalesceStr(this.OverriddenSourceDirectory, "(default source directory)"),
                source
            );
        }

        /// <summary>
        /// This method is called to execute the Action.
        /// </summary>
        protected override void Execute()
        {
            var allReleaseNotes = new List<string>();

            if (this.IncludeReleaseNotes)
            {
                LogInformation("Retrieving application release notes...");

                var releaseNotesTable = StoredProcs
                    .Releases_GetReleaseNotes(this.Context.ApplicationId, this.Context.ReleaseNumber, null, null)
                    .ExecuteDataTable();

                foreach (DataRow releaseNoteRow in releaseNotesTable.Rows)
                    allReleaseNotes.Add("* " + releaseNoteRow[TableDefs.ReleaseNotes_Extended.Notes_Text].ToString());

                LogDebug("Found {0} release note(s)", releaseNotesTable.Rows.Count);
            }

            if (this.IncludeIssues)
            {
                LogInformation("Querying issue tracker for release notes...");

                var applicationRow = StoredProcs.Applications_GetApplication(this.Context.ApplicationId).ExecuteDataRow();
                IssueTrackerIssue[] issues = null;
                IssueTrackingProviderBase issueProvider = null;

                if (!Convert.IsDBNull((applicationRow[TableDefs.Applications_Extended.IssueTracking_Provider_Id])))
                {
                    #region Create Provider
                    try
                    {
                        issueProvider = Util.Providers.CreateProviderFromId<IssueTrackingProviderBase>(
                                (int)applicationRow[TableDefs.Applications_Extended.IssueTracking_Provider_Id]);
                        if (issueProvider is ICategoryFilterable)
                            ((ICategoryFilterable)issueProvider).CategoryIdFilter =
                                Util.Persistence.DeserializeToStringArray(
                                    applicationRow[TableDefs.Applications_Extended.IssueTracking_CategoryIdList_Text]
                                    as string);
                        issues = issueProvider.GetIssues(this.Context.ReleaseNumber);
                        if (issues == null) throw new Exception("GetIssues returned null");
                    }
                    catch (Exception ex)
                    {
                        LogError("The Issue Tracking Provider could not be created: " + ex.Message);
                        return;
                    }
                    #endregion

                    int resolvedIssueCount = 0;
                    foreach (var issue in issues)
                    {
                        if (issueProvider.IsIssueClosed(issue))
                        {
                            allReleaseNotes.Add("* " + issue.IssueTitle);
                            resolvedIssueCount++;
                        }
                    }

                    LogDebug("Found {0} resolved issue(s)", resolvedIssueCount);
                }
                else
                {
                    LogWarning("No Issue Tracking Provider is associated with this Application.");
                }
            }

            ExecuteRemoteCommand("SetReleaseNotes", string.Join(Environment.NewLine, allReleaseNotes.ToArray()));
        }
        /// <summary>
        /// When implemented in a derived class, processes an arbitrary command
        /// on the appropriate server.
        /// </summary>
        /// <param name="name">Name of command to process.</param>
        /// <param name="args">Optional command arguments.</param>
        /// <returns>
        /// Result of the command.
        /// </returns>
        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            if (name != "SetReleaseNotes")
                throw new ArgumentException("Invalid command.");
            if (args == null || args.Length < 1)
                throw new ArgumentNullException("args");

            var nuspecFile = Path.Combine(this.Context.SourceDirectory, this.NuspecFileName);
            if (!File.Exists(nuspecFile))
            {
                LogError("Nuspec file '{0}' does not exist", nuspecFile);
                return string.Empty;
            }
            else
                LogDebug("Editing nuspec file '{0}'...", nuspecFile);

            var nuspecDoc = new XmlDocument();
            var nsManager = new XmlNamespaceManager(nuspecDoc.NameTable);
            nsManager.AddNamespace("d", NamespaceUri);
            nuspecDoc.Load(nuspecFile);

            var releaseNotesElement = nuspecDoc.SelectSingleNode("/d:package/d:metadata/d:releaseNotes", nsManager);
            if (releaseNotesElement == null)
            {
                var metadataElement = nuspecDoc.SelectSingleNode("/d:package/d:metadata", nsManager);
                if (metadataElement == null)
                {
                    LogError("Invalid format for nuspec file '{0}': Could not find /package/metadata element.", nuspecFile);
                    return string.Empty;
                }

                releaseNotesElement = nuspecDoc.CreateElement("releaseNotes", NamespaceUri);
                metadataElement.AppendChild(releaseNotesElement);
            }

            var notesText = args[0];
            if (string.IsNullOrEmpty(notesText))
                LogInformation("No release notes to write for this release");
            else
                LogInformation("Writing release notes to nuspec file");

            releaseNotesElement.InnerText = notesText ?? string.Empty;

            nuspecDoc.Save(nuspecFile);
            LogDebug("Release notes written to nuspec file");

            return string.Empty;
        }
    }
}
