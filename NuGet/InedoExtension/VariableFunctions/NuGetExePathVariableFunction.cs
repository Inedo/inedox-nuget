using System.ComponentModel;
using Inedo.Extensibility;
using Inedo.Extensibility.VariableFunctions;
using Inedo.Documentation;

namespace Inedo.Extensions.NuGet.VariableFunctions
{
    [ScriptAlias("NuGetExePath")]
    [Description("The path to the nuget.exe client, otherwise the included nuget.exe client is used.")]
    [ExtensionConfigurationVariable(Required = false)]
    [Tag("nuget")]
    public sealed class NuGetExePathVariableFunction : ScalarVariableFunction
    {
        protected override object EvaluateScalar(IVariableFunctionContext context)
        {
            return string.Empty;
        }
    }
}
