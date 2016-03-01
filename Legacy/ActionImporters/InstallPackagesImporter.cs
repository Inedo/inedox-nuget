using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.NuGet.Operations;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.NuGet.Legacy.ActionImporters
{
    internal sealed class InstallPackagesImporter : IActionOperationConverter<InstallSolutionPackagesAction, InstallPackagesOperation>
    {
        public ConvertedOperation<InstallPackagesOperation> ConvertActionToOperation(InstallSolutionPackagesAction action, IActionConverterContext context)
        {
            return new InstallPackagesOperation
            {
                SourceDirectory = AH.NullIf(action.OverriddenSourceDirectory, string.Empty),
                PackageOutputDirectory = AH.CoalesceString(action.PackageOutputDirectory, "packages")
            };
        }
    }
}
