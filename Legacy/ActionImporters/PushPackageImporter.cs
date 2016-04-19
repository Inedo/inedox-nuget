using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.NuGet.Operations;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.NuGet.Legacy.ActionImporters
{
    internal sealed class PushPackageImporter : IActionOperationConverter<PushPackage, PublishPackageOperation>
    {
        public ConvertedOperation<PublishPackageOperation> ConvertActionToOperation(PushPackage action, IActionConverterContext context)
        {
            return new PublishPackageOperation
            {
                PackagePath = context.ConvertLegacyExpression(PathEx.Combine(action.OverriddenSourceDirectory, action.PackagePath)),
                ServerUrl = context.ConvertLegacyExpression(action.ServerUrl),
                ApiKey = AH.NullIf(context.ConvertLegacyExpression(action.ApiKey), string.Empty)
            };
        }
    }
}
