using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.NuGet.Operations
{
    [Serializable]
    [ScriptAlias("Publish-Package")]
    [ScriptNamespace("NuGet")]
    [DisplayName("Publish NuGet Package")]
    [Description("Publishes a package to a NuGet feed.")]
    [DefaultProperty(nameof(PackagePath))]
    public sealed class PublishPackageOperation : RemoteExecuteOperation
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

        [ScriptAlias("ApiKey")]
        [DisplayName("API key")]
        [Description("The NuGet API key required to push packages to the feed.")]
        public string ApiKey { get; set; }
        [ScriptAlias("UserName")]
        [DisplayName("User name")]
        [Description("The user name used to connect to the NuGet feed when authorization is required.")]
        public string UserName { get; set; }
        [ScriptAlias("Password")]
        [DisplayName("Password")]
        [Description("The password used to connect to the NuGet feed when authorization is required.")]
        public string Password { get; set; }

        protected override async Task RemoteExecuteAsync(IRemoteOperationExecutionContext context)
        {
            var packagePath = context.ResolvePath(this.PackagePath);

            this.LogInformation($"Pushing {packagePath} to {this.ServerUrl}...");

            if (!FileEx.Exists(packagePath))
            {
                this.LogError(packagePath + " does not exist.");
                return;
            }

            using (var packageStream = FileEx.Open(packagePath, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan | FileOptions.Asynchronous))
            {
                var boundary = GenerateBoundary();
                var header = GetMultipartHeader(boundary);
                var footer = GetMultipartFooter(boundary);

                var request = WebRequest.CreateHttp(this.ServerUrl);
                request.Method = "PUT";
                request.UserAgent = $"BuildMaster/{typeof(Operation).Assembly.GetName().Version} NuGet-Extension/{typeof(PublishPackageOperation).Assembly.GetName().Version} ({Environment.OSVersion})";
                request.ContentType = "multipart/form-data; boundary=" + boundary;
                request.ContentLength = header.Length + packageStream.Length + footer.Length;
                if (!string.IsNullOrWhiteSpace(this.ApiKey))
                {
                    this.LogDebug("API key is specified; adding X-NUGET-APIKEY request header.");
                    request.Headers.Add("X-NUGET-APIKEY", this.ApiKey);
                }

                if (!string.IsNullOrWhiteSpace(this.UserName) && !string.IsNullOrEmpty(this.Password))
                {
                    this.LogDebug($"Sending basic auth credentials (user={this.UserName}).");
                    request.Headers.Add(HttpRequestHeader.Authorization, "Basic " + Convert.ToBase64String(InedoLib.UTF8Encoding.GetBytes(this.UserName + ":" + this.Password)));
                }
                else
                {
                    this.LogDebug("No credentials specified; sending default credentials.");
                    request.PreAuthenticate = true;
                    request.UseDefaultCredentials = true;
                }

                using (var requestStream = await request.GetRequestStreamAsync())
                {
                    await requestStream.WriteAsync(header, 0, header.Length);
                    await packageStream.CopyToAsync(requestStream);
                    await requestStream.WriteAsync(footer, 0, footer.Length);
                }

                try
                {
                    using (var response = (HttpWebResponse)await request.GetResponseAsync())
                    {
                        this.LogDebug($"Server responded with {(int)response.StatusCode}: {response.StatusDescription}");
                        this.LogInformation("Package pushed!");
                    }
                }
                catch (WebException ex)
                {
                    var response = (HttpWebResponse)ex.Response;
                    this.LogError($"Server responded with {(int)response.StatusCode}: {response.StatusDescription}");
                    using (var reader = new StreamReader(response.GetResponseStream(), InedoLib.UTF8Encoding))
                    {
                        this.LogError(await reader.ReadToEndAsync());
                    }
                }
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

        private static string GenerateBoundary() => new string('-', 10) + Guid.NewGuid().ToString("n");
        private static byte[] GetMultipartHeader(string boundary)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream, InedoLib.UTF8Encoding);
                writer.WriteLine("--" + boundary);
                writer.WriteLine("Content-Disposition: form-data; name=\"package\"; filename=\"package\"");
                writer.WriteLine("Content-Type: application/octet-stream");
                writer.WriteLine();

                writer.Flush();
                return stream.ToArray();
            }
        }
        private static byte[] GetMultipartFooter(string boundary)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream, InedoLib.UTF8Encoding);
                writer.WriteLine();
                writer.Write("--" + boundary + "--");

                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}
