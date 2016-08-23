using System;
using Inedo.Agents;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.Documentation;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.NuGet
{
    [Tag("nuget")]
    public abstract class NuGetActionBase : AgentBasedActionBase
    {
        protected NuGetActionBase()
        {
        }

        protected int NuGet(string command, params string[] args) => this.NuGetInternal("nuget.exe", command, args);
        protected int ProGet(string command, params string[] args) => this.NuGetInternal("proget.exe", command, args);

        private int NuGetInternal(string fileName, string command, string[] args)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException(nameof(command));
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            string nugetPath;
            var configurer = (NuGetConfigurer)this.GetExtensionConfigurer();
            if (string.IsNullOrEmpty(configurer.NuGetExe) || fileName.Contains("proget.exe"))
            {
                nugetPath = GetNuGetExePath(this.Context.Agent, fileName);
            }
            else
            {
                nugetPath = configurer.NuGetExe;
            }

            return this.ExecuteCommandLine(nugetPath, command + " " + string.Join(" ", args), this.Context.SourceDirectory);
        }

        private static string GetNuGetExePath(BuildMasterAgent agent, string fileName)
        {
            var executer = agent.GetService<IRemoteMethodExecuter>();
            string assemblyDir = executer.InvokeFunc(GetNugetExeDirectory);

            return PathEx.Combine(assemblyDir, fileName);
        }

        private static string GetNugetExeDirectory()
        {
            return PathEx.GetDirectoryName(typeof(NuGetActionBase).Assembly.Location);
        }
    }
}
