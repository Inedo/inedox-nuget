using System.ComponentModel;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.VariableFunctions;
using Inedo.Documentation;

namespace Inedo.BuildMasterExtensions.NuGet.Functions
{
    [ScriptAlias("NuGetExePath")]
    [Description("The path to the nuget.exe client, otherwise the included nuget.exe client is used.")]
    [ExtensionConfigurationVariable(Required = false)]
    [Tag("nuget")]
    public sealed class NuGetExePathVariableFunction : ScalarVariableFunction
    {
        protected override object EvaluateScalar(IGenericBuildMasterContext context)
        {
            return string.Empty;
        }
    }
}
