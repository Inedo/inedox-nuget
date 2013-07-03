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
    public abstract class NuGetActionBase : CommandLineActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetActionBase"/> class.
        /// </summary>
        protected NuGetActionBase()
        {
        }

        protected int NuGet(string command, params string[] args)
        {
            using (var agent = (IRemoteProcessExecuter)Util.Agents.CreateAgentFromId(this.ServerId))
            {
                return this.NuGet(agent, command, args);
            }
        }
        protected int NuGet(IRemoteProcessExecuter agent, string command, params string[] args)
        {
            return this.NuGetInternal(agent, "nuget.exe", command, args);
        }
        protected int ProGet(string command, params string[] args)
        {
            using (var agent = (IRemoteProcessExecuter)Util.Agents.CreateAgentFromId(this.ServerId))
            {
                return this.ProGet(agent, command, args);
            }
        }
        protected int ProGet(IRemoteProcessExecuter agent, string command, params string[] args)
        {
            return this.NuGetInternal(agent, "proget.exe", command, args);
        }

        private int NuGetInternal(IRemoteProcessExecuter agent, string fileName, string command, string[] args)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");
            if (args == null)
                throw new ArgumentNullException("args");

            var fileOps = (IFileOperationsExecuter)agent;
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
                fileOps.WriteFile(nugetPath, null, null, bytes, true);
            }

            return this.ExecuteCommandLine(agent, nugetPath, command + " " + string.Join(" ", args), this.RemoteConfiguration.SourceDirectory);
        }
    }
}
