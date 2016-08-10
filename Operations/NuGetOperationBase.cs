using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.NuGet.Operations
{
    public abstract class NuGetOperationBase : ExecuteOperation
    {
        protected NuGetOperationBase()
        {
        }

        [ScriptAlias("NuGetExePath")]
        [DefaultValue("$NuGetExePath")]
        [DisplayName("NuGet.exe path")]
        [Description("Full path to NuGet.exe on the target server. When not set, the bundled NuGet.exe will be used.")]
        public string NuGetExePath { get; set; }
        [ScriptAlias("Arguments")]
        [DisplayName("Additional arguments")]
        [Description("When specified, these arguments will be passed to NuGet.exe verbatim.")]
        public string AdditionalArguments { get; set; }

        protected string GetNuGetExePath(IOperationExecutionContext context)
        {
            if (!string.IsNullOrEmpty(this.NuGetExePath))
                return context.ResolvePath(this.NuGetExePath);

            var executer = context.Agent.GetService<IRemoteMethodExecuter>();
            string assemblyDir = executer.InvokeFunc(GetNugetExeDirectory);

            return PathEx.Combine(assemblyDir, "nuget.exe");
        }
        protected Task ExecuteNuGetAsync(IOperationExecutionContext context, string nugetExe, string args)
        {
            if (!string.IsNullOrWhiteSpace(this.AdditionalArguments))
                args += " " + this.AdditionalArguments;

            this.LogDebug("Executing: " + nugetExe + " " + args);

            return this.ExecuteCommandLineAsync(
                context,
                new RemoteProcessStartInfo
                {
                    FileName = nugetExe,
                    Arguments = args
                }
            );
        }

        private static string GetNugetExeDirectory()
        {
            return PathEx.GetDirectoryName(typeof(NuGetOperationBase).Assembly.Location);
        }
    }
}
