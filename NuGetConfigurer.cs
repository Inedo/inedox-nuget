using System;
using System.IO;
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
        /// Gets or sets the path to the NuGet.exe to use.
        /// </summary>
        [Persistent]
        public string NuGetExe { get; set; }
        /// <summary>
        /// Attempts to clear everything from the local NuGet cache before installing packages.
        /// </summary>
        [Persistent]
        public bool AlwaysClearNuGetCache { get; set; }

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

        internal static bool ClearCache()
        {
            try
            {
                var cachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.None),
                    "NuGet",
                    "Cache"
                );

                bool errors = false;

                if (Directory.Exists(cachePath))
                {
                    foreach (var fileName in Directory.EnumerateFiles(cachePath, "*.nupkg", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            File.Delete(fileName);
                        }
                        catch
                        {
                            errors = true;
                        }
                    }
                }

                return !errors;
            }
            catch
            {
                return false;
            }
        }
    }
}
