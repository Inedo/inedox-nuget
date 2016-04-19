using System.Collections.Generic;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.NuGet.Operations;

namespace Inedo.BuildMasterExtensions.NuGet.Legacy.ActionImporters
{
    internal sealed class CreatePackageImporter : IActionOperationConverter<CreatePackage, CreatePackageOperation>
    {
        public ConvertedOperation<CreatePackageOperation> ConvertActionToOperation(CreatePackage action, IActionConverterContext context)
        {
            List<string> properties = null;
            if (action.Properties?.Length > 0)
            {
                properties = new List<string>(action.Properties.Length);
                foreach (var p in action.Properties)
                    properties.Add(context.ConvertLegacyExpression(p));
            }

            return new CreatePackageOperation
            {
                SourceDirectory = AH.NullIf(context.ConvertLegacyExpression(action.OverriddenSourceDirectory), string.Empty),
                TargetDirectory = AH.NullIf(context.ConvertLegacyExpression(action.OverriddenTargetDirectory), string.Empty),
                Build = action.Build,
                IncludeReferencedProjects = action.IncludeReferencedProjects,
                ProjectPath = context.ConvertLegacyExpression(action.ProjectPath),
                Symbols = action.Symbols,
                Verbose = action.Verbose,
                Version = AH.NullIf(context.ConvertLegacyExpression(action.Version), string.Empty),
                Properties = properties
            };
        }
    }
}
