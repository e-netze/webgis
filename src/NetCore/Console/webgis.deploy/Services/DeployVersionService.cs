using System.IO.Compression;
using webgis.deploy.Models;

namespace webgis.deploy.Services;

internal class DeployVersionService
{
    public static readonly Version DeployToolVersion = new Version(7, 25, 1701);

    private const string zipPrefix = "webgis";

    private readonly string _versionsDirectory;
    private readonly DeployRepositoryService _deployRepositoryService;
    private readonly IOService _ioService;

    public DeployVersionService(DeployRepositoryService repositoryService,
                                IOService ioService)
    {
        _deployRepositoryService = repositoryService;
        _ioService = ioService;

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
            .GetFiles($"{zipPrefix}-{Platform.PlatformName}-*.zip")
            .Select(f => f.Name.Substring(zipPrefix.Length + Platform.PlatformName.Length + 2, f.Name.Length - zipPrefix.Length - Platform.PlatformName.Length - 2 - f.Extension.Length))
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
        using (var fileStream = new FileStream(Path.Combine(_versionsDirectory, $"{ZipFile(version)}"), FileMode.Open))
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

        foreach (var di in new List<DirectoryInfo>() { defaultCssDirectory, portalCssDirectory })
        {
            if (!di.Exists)
            {
                di.Create();
                File.WriteAllText(Path.Combine(di.FullName, "modify.json"),
                    """
                    {
                      "mode": "shrink",
                      "modifiers": [
                        /*{
                          "pattern": "#b5dbad",  // CI Color 
                          "replace": "#ccc"
                        },
                        {
                          "pattern": "#82C828",  // CI Color (Button Borders, etc) 
                          "replace": "#aaa"
                        }*/
                      ]
                    }
                    
                    """);
                File.WriteAllText(Path.Combine(di.FullName, "append.css"), "/* append.css * /");
            }
        }
    }

    #endregion

    #region Helper

    private string ZipFile(string version) => $"{zipPrefix}-{Platform.PlatformName}-{version}.zip";

    #endregion
}
