using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web;

[assembly: ExtensionConfigurer(typeof(Inedo.BuildMasterExtensions.NuGet.NuGetConfigurer))]

namespace Inedo.BuildMasterExtensions.NuGet
{
    /// <summary>
    /// Configurer for the NuGet extension.
    /// </summary>
    [CustomEditor(typeof(NuGetConfigurerEditor))]
    public sealed class NuGetConfigurer : ExtensionConfigurerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetConfigurer"/> class.
        /// </summary>
        public NuGetConfigurer()
        {
        }

        /// <summary>
        /// Gets or sets the URL of the NuGet package source.
        /// </summary>
        [Persistent]
        public string PackageSource { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to use proget.exe for installing packages.
        /// </summary>
        [Persistent]
        public bool UseProGetClient { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Empty;
        }
    }
}
