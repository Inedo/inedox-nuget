using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.NuGet.Operations
{
    [Serializable]
    [ScriptAlias("Publish-Package")]
    [DisplayName("Publish NuGet Package")]
    [Description("Publishes a package to a NuGet feed.")]
    [DefaultProperty(nameof(PackagePath))]
    public sealed class PublishPackageOperation : RemoteExecuteOperation, IHasCredentials<UsernamePasswordCredentials>
    {
        [Required]
        [ScriptAlias("Package")]
        [DisplayName("Package file name")]
        [Description("The path of the .nupkg file to push to the NuGet feed.")]
        public string PackagePath { get; set; }
        [Required]
        [ScriptAlias("Url")]
        [DisplayName("Source")]
        [Description("The NuGet feed source URL to push the package to.")]
        public string ServerUrl { get; set; }

        [Category("Authentication")]
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public string CredentialName { get; set; }
        [Category("Authentication")]
        [ScriptAlias("ApiKey")]
        [DisplayName("API key")]
        [Description("The NuGet API key required to push packages to the feed.")]
        public string ApiKey { get; set; }
        [Category("Authentication")]
        [ScriptAlias("UserName")]
        [DisplayName("User name")]
        [MappedCredential(nameof(UsernamePasswordCredentials.UserName))]
        [PlaceholderText("Use username from credentials")]
        public string UserName { get; set; }
        [Category("Authentication")]
        [ScriptAlias("Password")]
        [DisplayName("Password")]
        [MappedCredential(nameof(UsernamePasswordCredentials.Password))]
        [PlaceholderText("Use password from credentials")]
        public string Password { get; set; }

        protected override async Task<object> RemoteExecuteAsync(IRemoteOperationExecutionContext context)
        {
            var packagePath = context.ResolvePath(this.PackagePath);

            this.LogInformation($"Pushing {packagePath} to {this.ServerUrl}...");

            if (!FileEx.Exists(packagePath))
            {
                this.LogError(packagePath + " does not exist.");
                return null;
            }

            var handler = new HttpClientHandler { Proxy = WebRequest.DefaultWebProxy };

            if (string.IsNullOrWhiteSpace(this.UserName) || string.IsNullOrEmpty(this.Password))
            {
                this.LogDebug("No credentials specified; sending default credentials.");
                handler.PreAuthenticate = true;
                handler.UseDefaultCredentials = true;
            }

            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BuildMaster", typeof(Operation).Assembly.GetName().Version.ToString()));
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NuGet-Extension", typeof(PublishPackageOperation).Assembly.GetName().Version.ToString()));

                if (!string.IsNullOrWhiteSpace(this.ApiKey))
                {
                    this.LogDebug("API key is specified; adding X-NuGet-ApiKey request header.");
                    client.DefaultRequestHeaders.Add("X-NuGet-ApiKey", this.ApiKey);
                }

                if (!string.IsNullOrWhiteSpace(this.UserName) && !string.IsNullOrEmpty(this.Password))
                {
                    this.LogDebug($"Sending basic auth credentials (user={this.UserName}).");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(InedoLib.UTF8Encoding.GetBytes(this.UserName + ":" + this.Password)));
                }

                using (var packageStream = FileEx.Open(packagePath, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan | FileOptions.Asynchronous))
                using (var contentStream = new StreamContent(packageStream))
                using (var formData = new MultipartFormDataContent())
                {
                    contentStream.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    formData.Add(contentStream, "package", "package");
                    using (var response = await client.PutAsync(this.ServerUrl, formData, context.CancellationToken).ConfigureAwait(false))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            this.LogInformation("Package pushed!");
                        }
                        else
                        {
                            this.LogError($"Server responded with {(int)response.StatusCode}: {response.ReasonPhrase}");
                            this.LogError(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                        }
                    }
                }
            }

            return null;
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Publish NuGet package ",
                    new DirectoryHilite(config[nameof(this.PackagePath)])
                ),
                new RichDescription(
                    "to ",
                    new Hilite(config[nameof(this.ServerUrl)])
                )
            );
        }
    }
}
