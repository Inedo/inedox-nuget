using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.Credentials;
using Inedo.Extensions.SecureResources;

namespace Inedo.Extensions.NuGet.Operations
{
    [ScriptAlias("Push-Package")]
    [DisplayName("Push NuGet Package")]
    [Description("Publishes a package to a NuGet feed.")]
    [DefaultProperty(nameof(PackagePath))]
    [Tag("nuget")]
    public sealed class PushPackageOperation : NuGetOperationBase
    {
        [Required]
        [ScriptAlias("Package")]
        [DisplayName("Package file name")]
        public string PackagePath { get; set; }
        [Required]
        [ScriptAlias("Source")]
        [DisplayName("Package source")]
        public string PackageSource { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var packagePath = context.ResolvePath(this.PackagePath);
            if (string.IsNullOrEmpty(packagePath))
            {
                this.LogError("Missing required argument \"Package\".");
                return;
            }

            var fileOps = await context.Agent.GetServiceAsync<IFileOperationsExecuter>();
            if (!await fileOps.FileExistsAsync(packagePath))
            {
                this.LogError($"Package file {packagePath} not found.");
                return;
            }

            this.LogDebug($"Using package source {this.PackageSource}.");
            var packageSource = (NuGetPackageSource)SecureResource.Create(this.PackageSource, (IResourceResolutionContext)context);

            var nugetExe = await this.GetNuGetExePathAsync(context);
            if (string.IsNullOrEmpty(nugetExe))
            {
                this.LogError("nuget.exe path was empty.");
                return;
            }

            this.LogDebug($"Using credentials {packageSource.CredentialName}.");
            var credentials = packageSource.GetCredentials((ICredentialResolutionContext)context);
            if (!(credentials is TokenCredentials tokenCredentials))
            {
                this.LogError("Pushing a NuGet package requires an API key specified in Token credentials.");
                return;
            }

            this.LogInformation($"Pushing package {packagePath}...");
            await this.ExecuteNuGetAsync(
                context,
                nugetExe,
                $"push \"{packagePath}\" -ApiKey \"{AH.Unprotect(tokenCredentials.Token)}\" -Source \"{packageSource.ApiEndpointUrl}\" -NonInteractive",
                $"push \"{packagePath}\" -ApiKey XXXXX -Source \"{packageSource.ApiEndpointUrl}\" -NonInteractive"
            );
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Push NuGet package ",
                    new DirectoryHilite(config[nameof(this.PackagePath)])
                ),
                new RichDescription(
                    "to ",
                    new Hilite(config[nameof(this.PackageSource)])
                )
            );
        }
    }
}
