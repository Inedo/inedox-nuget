using System;
using System.IO;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web;
using Inedo.Serialization;

[assembly: ExtensionConfigurer(typeof(Inedo.BuildMasterExtensions.NuGet.NuGetConfigurer))]

namespace Inedo.BuildMasterExtensions.NuGet
{
    [CustomEditor(typeof(NuGetConfigurerEditor))]
    public sealed class NuGetConfigurer : ExtensionConfigurerBase
    {
        [Persistent]
        public string PackageSource { get; set; }
        [Persistent]
        public bool UseProGetClient { get; set; }
        [Persistent]
        public string NuGetExe { get; set; }
        [Persistent]
        public bool AlwaysClearNuGetCache { get; set; }

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
