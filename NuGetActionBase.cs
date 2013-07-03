using System;
using System.IO;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;

namespace Inedo.BuildMasterExtensions.NuGet
{
    /// <summary>
    /// Provides common NuGet action functionality.
    /// </summary>
    public abstract class NuGetActionBase : AgentBasedActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetActionBase"/> class.
        /// </summary>
        protected NuGetActionBase()
        {
        }

        protected int NuGet(string command, params string[] args)
        {
            return this.NuGetInternal("nuget.exe", command, args);
        }
        protected int ProGet(string command, params string[] args)
        {
            return this.NuGetInternal("proget.exe", command, args);
        }

        private int NuGetInternal(string fileName, string command, string[] args)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");
            if (args == null)
                throw new ArgumentNullException("args");

            var fileOps = this.Context.Agent.GetService<IFileOperationsExecuter>();
            var baseWorkingDirectory = fileOps.GetBaseWorkingDirectory();

            var nugetPath = Path.Combine(baseWorkingDirectory, @"ExtTemp\NuGet\" + fileName);
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

            return this.ExecuteCommandLine(nugetPath, command + " " + string.Join(" ", args), this.Context.SourceDirectory);
        }
    }
}
