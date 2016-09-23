using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.VariableFunctions;

namespace Inedo.BuildMasterExtensions.NuGet.Functions
{
    [ScriptAlias("NuGetExePath")]
    public sealed class NuGetExePathVariableFunction : ScalarVariableFunction
    {
        protected override object EvaluateScalar(IGenericBuildMasterContext context) => string.Empty;
    }
}
