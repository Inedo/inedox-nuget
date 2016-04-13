using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.NuGet.Operations;

namespace Inedo.BuildMasterExtensions.NuGet.Legacy.ActionImporters
{
    internal sealed class InstallPackagesImporter : IActionOperationConverter<InstallSolutionPackagesAction, InstallPackagesOperation>
    {
        public ConvertedOperation<InstallPackagesOperation> ConvertActionToOperation(InstallSolutionPackagesAction action, IActionConverterContext context)
        {
            return new InstallPackagesOperation
            {
                SourceDirectory = context.ConvertLegacyExpression(AH.NullIf(action.OverriddenSourceDirectory, string.Empty)),
                PackageOutputDirectory = context.ConvertLegacyExpression(AH.CoalesceString(action.PackageOutputDirectory, "packages"))
            };
        }
    }
}
