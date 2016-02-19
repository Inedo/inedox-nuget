using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.VariableFunctions;

namespace Inedo.BuildMasterExtensions.NuGet.Functions
{
    [ScriptAlias("NuGetExePath")]
    public sealed class NuGetExePathVariableFunction : ScalarVariableFunctionBase
    {
        protected override object EvaluateScalar(IGenericBuildMasterContext context)
        {
            return string.Empty;
        }
    }
}
