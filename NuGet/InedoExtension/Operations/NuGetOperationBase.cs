using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.IO;

namespace Inedo.Extensions.NuGet.Operations
{
    public abstract class NuGetOperationBase : ExecuteOperation
    {
        protected NuGetOperationBase()
        {
        }

        [Category("Advanced")]
        [ScriptAlias("NuGetExePath")]
        [DefaultValue("$NuGetExePath")]
        [DisplayName("NuGet.exe path")]
        [Description("Full path to NuGet.exe on the target server. When not set, the bundled NuGet.exe will be used.")]
        public string NuGetExePath { get; set; }
        [Category("Advanced")]
        [ScriptAlias("Arguments")]
        [DisplayName("Additional arguments")]
        [Description("When specified, these arguments will be passed to NuGet.exe verbatim.")]
        public string AdditionalArguments { get; set; }

        protected async Task<string> GetNuGetExePathAsync(IOperationExecutionContext context)
        {
            if (!string.IsNullOrEmpty(this.NuGetExePath))
                return context.ResolvePath(this.NuGetExePath);

            var executer = await context.Agent.GetServiceAsync<IRemoteMethodExecuter>().ConfigureAwait(false);
            string assemblyDir = await executer.InvokeFuncAsync(GetNugetExeDirectory).ConfigureAwait(false);

            return PathEx.Combine(assemblyDir, "nuget.exe");
        }
        protected async Task ExecuteNuGetAsync(IOperationExecutionContext context, string nugetExe, string args)
        {
            if (!string.IsNullOrWhiteSpace(this.AdditionalArguments))
                args += " " + this.AdditionalArguments;

            this.LogDebug("Executing: " + nugetExe + " " + args);

            int exitCode = await this.ExecuteCommandLineAsync(
                context,
                new RemoteProcessStartInfo
                {
                    FileName = nugetExe,
                    Arguments = args
                }
            ).ConfigureAwait(false);

            if (exitCode != 0)
                this.LogError($"NuGet.exe exited with code {exitCode}");
        }
        protected override void LogProcessOutput(string text)
        {
            if (text.Contains("Unable to find version ") || text.StartsWith("WARNING: "))
                this.LogWarning(text);
            else
                base.LogProcessOutput(text);
        }

        private static string GetNugetExeDirectory()
        {
            return PathEx.GetDirectoryName(typeof(NuGetOperationBase).Assembly.Location);
        }

        protected static string TrimDirectorySeparator(string d)
        {
            if (string.IsNullOrEmpty(d))
                return d;
            if (d.Length == 1)
                return d;

            return d.TrimEnd('\\', '/');
        }
    }
}
