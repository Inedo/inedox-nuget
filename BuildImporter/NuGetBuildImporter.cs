using System;
using System.IO;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Artifacts;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;
using Inedo.NuGet.Packages;

namespace Inedo.BuildMasterExtensions.NuGet.BuildImporter
{
    [Tag("nuget")]
    [BuildImporterProperties(
        "NuGet",
        "Imports a NuGet package as a BuildMaster artifact.",
        typeof(NuGetBuildImporterTemplate))]
    [CustomEditor(typeof(NuGetBuildImporterEditor))]
    public sealed class NuGetBuildImporter : BuildImporterBase
    {
        [Persistent]
        public string PackageId { get; set; }
        [Persistent]
        public string PackageVersion { get; set; }
        [Persistent]
        public string PackageSource { get; set; }
        [Persistent]
        public bool IncludePrerelease { get; set; }
        [Persistent]
        public string AdditionalArguments { get; set; }
        [Persistent]
        public bool CaptureIdAndVersion { get; set; }
        [Persistent]
        public string PackageArtifactRoot { get; set; }
        [Persistent]
        public bool IncludeVersionInArtifactName { get; set; }

        public override void Import(IBuildImporterContext context)
        {
            var configurer = (NuGetConfigurer)this.GetExtensionConfigurer();
            var nugetExe = configurer != null ? configurer.NuGetExe : null;
            if (string.IsNullOrEmpty(nugetExe))
                nugetExe = Path.Combine(Path.GetDirectoryName(typeof(NuGetBuildImporter).Assembly.Location), "nuget.exe");

            var packageSource = Util.CoalesceStr(this.PackageSource, configurer != null ? configurer.PackageSource : null);

            var args = "install \"" + this.PackageId + "\" -ExcludeVersion -NoCache";
            if (!string.IsNullOrEmpty(this.PackageVersion))
                args += " -Version \"" + this.PackageVersion + "\"";
            if (this.IncludePrerelease)
                args += " -Prerelease";
            if (!string.IsNullOrEmpty(packageSource))
                args += " -Source \"" + packageSource + "\"";

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                Directory.CreateDirectory(tempPath);

                args += " -OutputDirectory \"" + tempPath + "\"";

                if (!string.IsNullOrWhiteSpace(this.AdditionalArguments))
                    args += " " + this.AdditionalArguments;

                this.LogDebug("Executing {0} {1}", nugetExe, args);
                this.LogInformation("Executing NuGet...");

                using (var process = new LocalTextDataProcess(new AgentProcessStartInfo { FileName = nugetExe, Arguments = args }))
                {
                    process.OutputDataReceived += this.Process_OutputDataReceived;
                    process.ErrorDataReceived += this.Process_ErrorDataReceived;
                    process.Start();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        this.LogError("NuGet failed with exit code {0}.", process.ExitCode);
                        return;
                    }
                }

                this.LogInformation("NuGet indicated successful package install.");

                var packageRootPath = Path.Combine(tempPath, this.PackageId);
                var artifactName = this.PackageId;

                if (this.CaptureIdAndVersion || this.IncludeVersionInArtifactName)
                {
                    try
                    {
                        var nupkgPath = Path.Combine(packageRootPath, this.PackageId + ".nupkg");
                        this.LogDebug("Attempting to gather metadata from {0}...", nupkgPath);
                        var nuspec = NuGetPackage.ReadFromNupkgFile(nupkgPath);

                        var packageId = nuspec.Id;
                        var packageVersion = nuspec.Version.OriginalString;

                        if (this.CaptureIdAndVersion)
                        {
                            this.LogDebug("Setting $ImportedPackageId = " + packageId);
                            SetBuildVariable(context, "ImportedPackageId", packageId);

                            this.LogDebug("Setting $ImportedPackageVersion = " + packageVersion);
                            SetBuildVariable(context, "ImportedPackageVersion", packageVersion);
                        }

                        if (this.IncludeVersionInArtifactName)
                            artifactName = packageId + "." + packageVersion;
                    }
                    catch (Exception ex)
                    {
                        this.LogError("Could not read package metadata: " + ex.ToString());
                        return;
                    }
                }

                var rootCapturePath = Path.Combine(packageRootPath, (this.PackageArtifactRoot ?? string.Empty).TrimStart('/', '\\'));
                this.LogDebug("Capturing files in {0}...", rootCapturePath);

                var rootEntry = Util.Files.GetDirectoryEntry(
                    new GetDirectoryEntryCommand
                    {
                        Path = rootCapturePath,
                        IncludeRootPath = true,
                        Recurse = true
                    }
                ).Entry;

                using (var agent = Util.Agents.CreateLocalAgent())
                {
                    var fileOps = agent.GetService<IFileOperationsExecuter>();
                    var matches = Util.Files.Comparison.GetMatches(packageRootPath, rootEntry, new[] { "!\\" + this.PackageId + ".nupkg", "*" });
                    var artifactId = new ArtifactIdentifier(context.ApplicationId, context.ReleaseNumber, context.BuildNumber, context.DeployableId, artifactName);

                    this.LogInformation("Creating artifact {0}...", artifactName);
                    using (var artifact = new ArtifactBuilder(artifactId))
                    {
                        artifact.RootPath = packageRootPath;

                        foreach (var match in matches)
                            artifact.Add(match, fileOps);

                        artifact.Commit();
                    }

                    this.LogInformation("Artifact created.");
                }
            }
            finally
            {
                try
                {
                    Util.Files.DeleteFolder(tempPath);
                }
                catch
                {
                }
            }
        }

        private static void SetBuildVariable(IBuildImporterContext context, string variableName, string variableValue)
        {
            StoredProcs.Variables_CreateOrUpdateVariableDefinition(
                Variable_Name: variableName,
                Environment_Id: null,
                Server_Id: null,
                ApplicationGroup_Id: null,
                Application_Id: context.ApplicationId,
                Deployable_Id: context.DeployableId,
                Release_Number: context.ReleaseNumber,
                Build_Number: context.BuildNumber,
                Execution_Id: null,
                Value_Text: variableValue,
                Sensitive_Indicator: Domains.YN.No
            ).Execute();
        }

        private void Process_OutputDataReceived(object sender, ProcessDataReceivedEventArgs<string> e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                this.LogDebug(e.Data);
        }
        private void Process_ErrorDataReceived(object sender, ProcessDataReceivedEventArgs<string> e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                this.LogError(e.Data);
        }
    }
}
