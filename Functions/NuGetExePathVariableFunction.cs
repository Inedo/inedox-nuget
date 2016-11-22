using System.ComponentModel;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.VariableFunctions;

namespace Inedo.BuildMasterExtensions.NuGet.Functions
{
    [ScriptAlias("NuGetExePath")]
    [Description("The path to the nuget.exe client, otherwise the included nuget.exe client is used.")]
    //[ExtensionConfigurationVariable(Required = false)]
    public sealed class NuGetExePathVariableFunction : ScalarVariableFunctionBase
    {
        protected override object EvaluateScalar(IGenericBuildMasterContext context)
        {
            return string.Empty;
        }
    }
}
