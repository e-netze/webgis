using System.IO.Compression;

using webgis.deploy.Models;

namespace webgis.deploy.Services;

internal class DeployVersionService
{
    public static readonly Version DeployToolVersion = new Version(8, 26, 3501);

#if INTERNAL
    private const string DefaultZipPrefix = "webgis_internal";
#else
    private const string DefaultZipPrefix = "webgis";
#endif
    private const string PortableZipPrefix = "webgis_portable";

    private readonly string ZipPrefix;

    private readonly string _versionsDirectory;
    private readonly DeployRepositoryService _deployRepositoryService;
    private readonly IOService _ioService;

    public DeployVersionService(DeployRepositoryService repositoryService,
                                IOService ioService,
                                bool portable = false)
    {
        _deployRepositoryService = repositoryService;
        _ioService = ioService;

        ZipPrefix = portable ? PortableZipPrefix : DefaultZipPrefix;

        _versionsDirectory =
            Path.Combine(_deployRepositoryService.RepositoryRootDirectoryInfo().Parent.FullName, "download");

        if (!Directory.Exists(_versionsDirectory))
        {
            Directory.CreateDirectory(_versionsDirectory);
        }
    }

    public IEnumerable<string> GetVersions()
    {
        var di = new DirectoryInfo(_versionsDirectory);

        return di
            .GetFiles($"{ZipPrefix}-{Platform.PlatformName}-*.zip")
            .Select(f => f.Name.Substring(ZipPrefix.Length + Platform.PlatformName.Length + 2, f.Name.Length - ZipPrefix.Length - Platform.PlatformName.Length - 2 - f.Extension.Length))
            .Where(n => Version.TryParse(n, out var version))
            .OrderByDescending(n => Version.Parse(n));
    }

    public void CopyFolderRecursive(string version, string relativeSourcePath, string targetPath)
    {
        if (Directory.Exists(targetPath))
        {
            throw new Exception("Target alreay exists");
        }

        string sourcePath = Path.Combine(_versionsDirectory, version, relativeSourcePath);

        _ioService.CopyFolderRecursive(sourcePath, targetPath);
    }

    public void CopyFiles(string version, string relativeSourcePath, string targetPath, string filter = "*.*")
    {
        string sourcePath = Path.Combine(_versionsDirectory, version, relativeSourcePath);

        _ioService.CopyFiles(sourcePath, targetPath, filter);
    }

    public void ExtractZipFolderRecursive(string version, string relativeSourcePath, string targetPath)
    {
        string path = Path.Combine(_versionsDirectory, $"{ZipFile(version)}");
        Console.WriteLine($"Extracting files from {path} to {targetPath}");

        using (var fileStream = new FileStream(path, FileMode.Open))
        using (ZipArchive zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
        {
            _ioService.ExtractZipFolderRecursive(zipArchive, $"{version}/{relativeSourcePath}", targetPath);
        }
    }

    public void ExtractFiles(string version, string relativeSourcePath, string targetPath)
    {
        using (var fileStream = new FileStream(Path.Combine(_versionsDirectory, $"{ZipFile(version)}"), FileMode.Open))
        using (ZipArchive zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
        {
            _ioService.ExtractFiles(zipArchive, $"{version}/{relativeSourcePath}", targetPath);
        }
    }

    public bool Exits(string filename)
        => File.Exists(Path.Combine(_versionsDirectory, filename));

    public Task AppendAsync(string filename, byte[] fileData)
        => File.WriteAllBytesAsync(Path.Combine(_versionsDirectory, filename), fileData);

    #region Overrides

    public void InitOverrides(string profile, string version)
    {
        using (var fileStream = new FileStream(Path.Combine(_versionsDirectory, $"{ZipFile(version)}"), FileMode.Open))
        using (ZipArchive zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
        {
            _ioService.CopyIfNotExists(
                zipArchive,
                $"{version}/webgis-api/_setup/proto/_api.config",
                Path.Combine(_deployRepositoryService.ProfileDirectory(profile), "webgis-api", "override", "_config", "api.config"));

            _ioService.CopyIfNotExists(
                zipArchive,
                $"{version}/webgis-api/_setup/proto/_application-security.config",
                Path.Combine(_deployRepositoryService.ProfileDirectory(profile), "webgis-api", "override", "_config", "application-security.config"));


            _ioService.CopyIfNotExists(
                zipArchive,
                 $"{version}/webgis-portal/_setup/proto/_portal.config",
                Path.Combine(_deployRepositoryService.ProfileDirectory(profile), "webgis-portal", "override", "_config", "portal.config"));

            _ioService.CopyIfNotExists(
                zipArchive,
                $"{version}/webgis-portal/_setup/proto/_application-security.config",
                Path.Combine(_deployRepositoryService.ProfileDirectory(profile), "webgis-portal", "override", "_config", "application-security.config"));

            _ioService.CopyIfNotExists(
                zipArchive,
                 $"{version}/webgis-cms/_setup/proto/__cms.config",
                Path.Combine(_deployRepositoryService.ProfileDirectory(profile), "webgis-cms", "override", "_config", "cms.config"));

            _ioService.CopyIfNotExists(
                    zipArchive,
                     $"{version}/webgis-cms/_setup/proto/__datalinq.config",
                    Path.Combine(_deployRepositoryService.ProfileDirectory(profile), "webgis-cms", "override", "_config", "datalinq.config"));

            _ioService.CopyIfNotExists(
                zipArchive,
                 $"{version}/webgis-cms/_setup/proto/__settings.config",
                Path.Combine(_deployRepositoryService.ProfileDirectory(profile), "webgis-cms", "override", "_config", "settings.config"));

            _ioService.CopyIfNotExists(
                zipArchive,
                 $"{version}/webgis-cms/_setup/proto/__application-security.config",
                Path.Combine(_deployRepositoryService.ProfileDirectory(profile), "webgis-cms", "override", "_config", "application-security.config"));
        }
    }

    public void CopyOverrides(string profile, string appFolder, string targetPath, DeployVersionModel versionModel)
    {
        string sourcePath = Path.Combine(_deployRepositoryService.ProfileDirectory(profile), appFolder, "override");
        targetPath = Path.Combine(targetPath, appFolder);

        _ioService.OverrideFolderRecursive(sourcePath, targetPath, versionModel);
    }

    #endregion

    #region Modify-CSS

    public void CreateDefaultModifyCss(string profile)
    {
        var defaultCssDirectory = new DirectoryInfo(Path.Combine(_deployRepositoryService.ProfileDirectory(profile), "css-modify", "default.css"));
        var portalCssDirectory = new DirectoryInfo(Path.Combine(_deployRepositoryService.ProfileDirectory(profile), "css-modify", "portal.css"));
        var siteCssDirectory = new DirectoryInfo(Path.Combine(_deployRepositoryService.ProfileDirectory(profile), "css-modify", "site.css"));

        foreach (var di in new List<DirectoryInfo>() { defaultCssDirectory, portalCssDirectory, siteCssDirectory })
        {
            if (!di.Exists)
            {
                di.Create();
                File.WriteAllText(Path.Combine(di.FullName, "modify.json"),
                    """
                    {
                      "mode": "shrink",
                      "modifiers": [
                        /*
                        {
                          "pattern": "#82C828",  // CI Color (Button Borders, etc)   (--webgis-brand-primary)
                          "replace": "#aaa"
                        },
                        {
                          "pattern": "#b5dbad",  // CI Color (--webgis-brand-primary-light)
                          "replace": "#ccc"
                        },
                        */
                      ]
                    }
                    
                    """);
                File.WriteAllText(Path.Combine(di.FullName, "append.css"), "/* append.css */");
            }
        }
    }

    #endregion

    #region Helper

    private string ZipFile(string version) => $"{ZipPrefix}-{Platform.PlatformName}-{version}.zip";

    #endregion
}
