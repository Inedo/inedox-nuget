using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Documentation;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMaster.Web;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.NuGet
{
    [Tag("nuget")]
    [DisplayName("Set Release Notes")]
    [Description("Writes release notes to a .nuspec file using BuildMaster's release notes or the application's issue tracker.")]
    [CustomEditor(typeof(SetReleaseNotesActionEditor))]
    public sealed class SetReleaseNotes : RemoteActionBase
    {
        [Persistent]
        public bool IncludeIssues { get; set; }
        [Persistent]
        public bool IncludeReleaseNotes { get; set; }
        [Persistent]
        public string NuspecFileName { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Write NuGet release notes to ",
                    new DirectoryHilite(this.OverriddenSourceDirectory, this.NuspecFileName)
                ),
                new RichDescription(
                    "using the application's ",
                    new Hilite(this.IncludeReleaseNotes ? "release notes" : "issue tracker")
                )
            );
        }

        protected override void Execute()
        {
            var allReleaseNotes = new List<string>();

            if (this.IncludeReleaseNotes)
            {
                this.LogInformation("Retrieving application release notes...");

                var releaseNotesTable = DB.Releases_GetReleaseNotes(this.Context.ApplicationId, this.Context.ReleaseNumber, null, null);

                foreach (var releaseNoteRow in releaseNotesTable)
                    allReleaseNotes.Add("* " + releaseNoteRow.Notes_Text);

                this.LogDebug("Found {0} release notes", releaseNotesTable.Count);
            }

            if (this.IncludeIssues)
            {
                this.LogInformation("Querying issue tracker for release notes...");

                var application = DB.Applications_GetApplication(this.Context.ApplicationId)
                    .Applications_Extended
                    .First();

                if (application.IssueTracking_Provider_Id != null)
                {
                    try
                    {
                        using (var proxy = Util.Proxy.CreateProviderProxy((int)application.IssueTracking_Provider_Id))
                        {
                            var categoryFilterable = proxy.TryGetService<ICategoryFilterable>();
                            if (categoryFilterable != null)
                                categoryFilterable.CategoryIdFilter = Persistence.DeserializeToStringArray(application.IssueTracking_CategoryIdList_Text ?? string.Empty);

                            var issueTracker = proxy.TryGetService<IssueTrackingProviderBase>();
                            IssueTrackerIssue[] issues = issueTracker.GetIssues(this.Context.ReleaseNumber);

                            int resolvedIssueCount = 0;
                            foreach (var issue in issues)
                            {
                                if (issueTracker.IsIssueClosed(issue))
                                {
                                    allReleaseNotes.Add("* " + issue.IssueTitle);
                                    resolvedIssueCount++;
                                }
                            }

                            this.LogDebug("Found {0} resolved issues.", resolvedIssueCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.LogError("The issue tracking provider could not be created: " + ex.Message);
                        return;
                    }
                }
                else
                {
                    this.LogWarning("There is no issue tracking provider associated with this application.");
                }
            }

            this.ExecuteRemoteCommand("SetReleaseNotes", allReleaseNotes.ToArray());
        }
        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            if (name != "SetReleaseNotes")
                throw new ArgumentException("Invalid command.");
            if (args == null)
                throw new ArgumentNullException("args");

            var nuspecFile = Path.Combine(this.Context.SourceDirectory, this.NuspecFileName);
            if (!File.Exists(nuspecFile))
            {
                this.LogError("Nuspec file {0} does not exist.", nuspecFile);
                return string.Empty;
            }
            else
            {
                this.LogDebug("Editing nuspec file {0}...", nuspecFile);
            }

            var xdoc = XDocument.Load(nuspecFile);
            var ns = xdoc.Root.GetDefaultNamespace();

            var releaseNotesElement = xdoc.Descendants(ns + "releaseNotes").FirstOrDefault();
            if (releaseNotesElement == null)
            {
                var metadataElement = xdoc.Descendants(ns + "metadata").FirstOrDefault();
                if (metadataElement == null)
                {
                    this.LogError("Invalid format for nuspec file {0}: Could not find /package/metadata element.", nuspecFile);
                    return string.Empty;
                }

                releaseNotesElement = new XElement(ns + "releaseNotes");
                metadataElement.Add(releaseNotesElement);
            }

            var notesText = string.Join(Environment.NewLine, args);
            if (string.IsNullOrEmpty(notesText))
                this.LogInformation("No release notes to write for this release.");
            else
                this.LogInformation("Writing release notes to nuspec file.");

            releaseNotesElement.Value = notesText ?? string.Empty;

            xdoc.Save(nuspecFile);
            this.LogDebug("Release notes written to nuspec file.");

            return string.Empty;
        }
    }
}
