using System;
using System.IO;
using System.Reflection;

namespace E.Standard.Configuration;

/// <summary>
/// Resolves file paths inside the WebGIS "_config" directory (next to the entry assembly). This
/// is the directory administrators are expected to edit/mount in production (e.g. a single
/// Kubernetes ConfigMap volume, as opposed to individual environment variables), so any new,
/// optional configuration file that should follow the same convention (see
/// <see cref="Extensions.DependencyInjection.ConfiguraitonBuilderExtensions.AddConfigDirectoryJsonFile"/>,
/// <see cref="EnvFileLoader"/>) resolves its path through this class.
///
/// This intentionally mirrors the (non-shared) lookup already used by <see cref="AppConfiguration"/>
/// for the classic *.config files.
/// </summary>
static public class ConfigDirectory
{
    public const string DirectoryName = "_config";

    static public string ResolveFilePath(string fileName)
    {
        var rootPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);

        return Path.Combine(rootPath ?? String.Empty, DirectoryName, fileName);
    }
}
