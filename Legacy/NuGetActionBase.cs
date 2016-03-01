using System;
using System.IO;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;

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
                throw new ArgumentNullException("command");
            if (args == null)
                throw new ArgumentNullException("args");

            string nugetPath;
            var configurer = (NuGetConfigurer)this.GetExtensionConfigurer();
            if (string.IsNullOrEmpty(configurer.NuGetExe) || fileName.Contains("proget.exe"))
            {
                var fileOps = this.Context.Agent.GetService<IFileOperationsExecuter>();
                var baseWorkingDirectory = fileOps.GetBaseWorkingDirectory();

                nugetPath = Path.Combine(baseWorkingDirectory, @"ExtTemp\NuGet\" + fileName);
                var fileInfo = fileOps.GetFileEntry(nugetPath);
                if (fileInfo == null)
                {
                    var path = Path.Combine(
                        Path.GetDirectoryName(typeof(NuGetActionBase).Assembly.Location),
                        fileName
                    );

                    var bytes = File.ReadAllBytes(path);
                    fileOps.WriteFileBytes(nugetPath, bytes);
                }
            }
            else
            {
                nugetPath = configurer.NuGetExe;
            }

            return this.ExecuteCommandLine(nugetPath, command + " " + string.Join(" ", args), this.Context.SourceDirectory);
        }
    }
}
