using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Linq;
using Inedo.ExecutionEngine.Executer;
using System.Xml.Linq;
using Inedo.Extensions.SecureResources;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.Credentials;
using LegacyUsernamePasswordCredentials = Inedo.Extensibility.Credentials.UsernamePasswordCredentials;

namespace Inedo.Extensions.NuGet.Operations
{
    [Serializable]
    [ScriptAlias("Publish-Package")]
    [DisplayName("Publish NuGet Package")]
    [Description("Publishes a package to a NuGet feed.")]
    [DefaultProperty(nameof(PackagePath))]
    [Tag("nuget")]
    public sealed class PublishPackageOperation : RemoteExecuteOperation
#pragma warning disable CS0618 // Type or member is obsolete
        , IHasCredentials<LegacyUsernamePasswordCredentials>
#pragma warning restore CS0618 // Type or member is obsolete
    {
        [NonSerialized]
        private IPackageManager packageManager;

        [Required]
        [ScriptAlias("Package")]
        [DisplayName("Package file name")]
        [Description("The path of the .nupkg file to push to the NuGet feed.")]
        public string PackagePath { get; set; }
        [ScriptAlias("Url")]
        [DisplayName("Source URL")]
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
        [MappedCredential(nameof(LegacyUsernamePasswordCredentials.UserName))]
        [PlaceholderText("Use username from credentials")]
        public string UserName { get; set; }
        [Category("Authentication")]
        [ScriptAlias("Password")]
        [DisplayName("Password")]
        [MappedCredential(nameof(LegacyUsernamePasswordCredentials.Password))]
        [PlaceholderText("Use password from credentials")]
        public string Password { get; set; }

        [ScriptAlias("Source")]
        [Category("Advanced")]
        [DisplayName("Package source")]
        public string PackageSource { get; set; }
        [DefaultValue(true)]
        [ScriptAlias("AttachToBuild")]
        [Category("Advanced")]
        [DisplayName("Attach to build")]
        public bool AttachToBuild { get; set; } = true;

        protected override async Task BeforeRemoteExecuteAsync(IOperationExecutionContext context)
        {
            this.packageManager = await context.TryGetServiceAsync<IPackageManager>();

            // if username is not already specified and there is a package source, look up any attached credentials
            if (string.IsNullOrEmpty(this.UserName) && !string.IsNullOrEmpty(this.PackageSource))
            {
                var packageSource = (NuGetPackageSource)SecureResource.Create(this.PackageSource, (IResourceResolutionContext)context);

                if (string.IsNullOrEmpty(this.ServerUrl))
                    this.ServerUrl = packageSource.ApiEndpointUrl;

                if (!string.IsNullOrEmpty(packageSource.CredentialName))
                {
                    var creds = packageSource.GetCredentials((ICredentialResolutionContext)context);
                    if (creds is TokenCredentials tc)
                    {
                        this.UserName = "api";
                        this.Password = AH.Unprotect(tc.Token);
                    }
                    else if (creds is Inedo.Extensions.Credentials.UsernamePasswordCredentials upc)
                    {
                        this.UserName = upc.UserName;
                        this.Password = AH.Unprotect(upc.Password);
                    }
                    else
                        throw new InvalidOperationException();
                }
            }

            await base.BeforeRemoteExecuteAsync(context);
        }

        protected override async Task<object> RemoteExecuteAsync(IRemoteOperationExecutionContext context)
        {
            var packagePath = context.ResolvePath(this.PackagePath);

            this.LogInformation($"Pushing {packagePath} to {this.ServerUrl}...");

            if (string.IsNullOrEmpty(this.ServerUrl))
            {
                this.LogError("Missing required property \"Url\".");
                return null;
            }

            if (!FileEx.Exists(packagePath))
            {
                this.LogError(packagePath + " does not exist.");
                return null;
            }

            var packageInfo = PackageInfo.Extract(packagePath);

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

            return packageInfo;
        }

        protected override async Task AfterRemoteExecuteAsync(object result)
        {
            await base.AfterRemoteExecuteAsync(result);

            if (this.AttachToBuild && !string.IsNullOrWhiteSpace(this.PackageSource) && result is PackageInfo info)
            {
                if (this.packageManager == null)
                {
                    this.LogWarning("Package manager is not available; cannot attach to build.");
                    return;
                }

                await this.packageManager.AttachPackageToBuildAsync(new AttachedPackage(AttachedPackageType.NuGet, info.Id, info.Version, info.SHA1, this.PackageSource), default);
            }
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

        [Serializable]
        private sealed class PackageInfo
        {
            private static readonly LazyRegex NuspecRegex = new LazyRegex(@"^[^/\\]+\.nuspec$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

            private PackageInfo(string id, string version, byte[] sha1)
            {
                this.Id = id;
                this.Version = version;
                this.SHA1 = sha1;
            }

            public string Id { get; }
            public string Version { get; }
            public byte[] SHA1 { get; }

            public static PackageInfo Extract(string packagePath)
            {
                using (var fileStream = FileEx.Open(packagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] hash;
                    using (var sha1 = System.Security.Cryptography.SHA1.Create())
                    {
                        hash = sha1.ComputeHash(fileStream);
                    }

                    fileStream.Position = 0;

                    using (var zip = new ZipArchive(fileStream, ZipArchiveMode.Read, true))
                    {
                        var nuspecFile = zip.Entries.FirstOrDefault(e => NuspecRegex.IsMatch(e.FullName));
                        if (nuspecFile == null)
                            throw new ExecutionFailureException(packagePath + " is not a valid NuGet package; it is missing a .nuspec file.");

                        using (var nuspecStream = nuspecFile.Open())
                        {
                            var xdoc = XDocument.Load(nuspecStream);
                            var ns = xdoc.Root.GetDefaultNamespace();
                            var id = (string)xdoc.Root.Element(ns + "metadata")?.Element(ns + "id");
                            if (string.IsNullOrWhiteSpace(id))
                                throw new ExecutionFailureException(packagePath + " has an invalid .nuspec file; missing \"id\" element.");

                            var version = (string)xdoc.Root.Element(ns + "metadata")?.Element(ns + "version");
                            if (string.IsNullOrWhiteSpace(version))
                                throw new ExecutionFailureException(packagePath + " has an invalid .nuspec file; missing \"version\" element.");

                            return new PackageInfo(id, version, hash);
                        }
                    }
                }
            }
        }
    }
}
