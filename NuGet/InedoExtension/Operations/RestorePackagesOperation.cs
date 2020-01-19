using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.Documentation;
using System.Linq;
using System;
using Inedo.ExecutionEngine.Executer;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.SecureResources;

namespace Inedo.Extensions.NuGet.Operations
{
    [ScriptAlias("Restore-Packages")]
    [DisplayName("Restore NuGet Packages")]
    [Description("Restores all packages in a specified solution, project, or packages.config file.")]
    [Tag("nuget")]
    public sealed class RestorePackagesOperation : NuGetOperationBase
    {
        [ScriptAlias("Target")]
        [DisplayName("Target")]
        [Description("The target solution, project, or packages.config file used to restore packages, or directory containing a solution.")]
        [PlaceholderText("$WorkingDirectory")]
        public string Target { get; set; }
        [ScriptAlias("PackagesDirectory")]
        [DisplayName("Packages directory")]
        [Description("The directory into which packages will be restored.")]
        [PlaceholderText("packages")]
        public string PackagesDirectory { get; set; }
        [ScriptAlias("Source")]
        [DisplayName("Source URL")]
        [PlaceholderText("Use default URL specified in nuget.config")]
        public string ServerUrl { get; set; }
        [ScriptAlias("SourceName")]
        [Category("Advanced")]
        [DisplayName("Package source")]
        public string PackageSource { get; set; }


        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            if (!string.IsNullOrEmpty(this.PackageSource))
            {
                if (!string.IsNullOrEmpty(this.ServerUrl))
                {
                    this.LogWarning("SourceName will be ignored because Source (url) is specified.");
                }
                else
                {
                    this.LogDebug($"Using package source {this.PackageSource}.");
                    var packageSource = (NuGetPackageSource)SecureResource.Create(this.PackageSource, (IResourceResolutionContext)context);
                    this.ServerUrl = packageSource.ApiEndpointUrl;
                }
            }

            var fileOps = await context.Agent.GetServiceAsync<IFileOperationsExecuter>().ConfigureAwait(false);
            var nugetExe = await this.GetNuGetExePathAsync(context).ConfigureAwait(false);
            if (string.IsNullOrEmpty(nugetExe))
            {
                this.LogError("nuget.exe path was empty.");
                return;
            }

            var target = context.ResolvePath(this.Target);

            this.LogInformation($"Restoring packages for {target}...");
            
            await this.ExecuteNuGetRestoreAsync(context, nugetExe, target).ConfigureAwait(false);

            this.LogInformation("Done restoring packages.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Restore NuGet packages for ",
                    new DirectoryHilite(config[nameof(this.Target)])
                )
            );
        }

        private Task ExecuteNuGetRestoreAsync(IOperationExecutionContext context, string nugetExe, string target)
        {
            var buffer = new StringBuilder();
            buffer.Append("restore");
            if (!string.IsNullOrEmpty(target))
                buffer.Append($" \"{TrimDirectorySeparator(target)}\"");

            if (!string.IsNullOrEmpty(this.PackagesDirectory))
                buffer.Append($" -PackagesDirectory \"{TrimDirectorySeparator(context.ResolvePath(this.PackagesDirectory))}\"");

            if (!string.IsNullOrWhiteSpace(this.ServerUrl))
                buffer.Append($" -Source \"{this.ServerUrl}\"");

            return this.ExecuteNuGetAsync(context, nugetExe, buffer.ToString());
        }
    }
}
