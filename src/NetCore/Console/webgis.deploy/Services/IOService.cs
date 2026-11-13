using System.IO.Compression;

using webgis.deploy.Extensions;
using webgis.deploy.Models;

namespace webgis.deploy.Services;

internal class IOService
{
    private readonly ConsoleService _console;

    public IOService(ConsoleService console)
    {
        _console = console;
    }

    public bool CopyIfNotExists(string sourceFile, string targetFile)
    {
        var sourceFileInfo = new FileInfo(sourceFile);
        var targetFileInfo = new FileInfo(targetFile);

        if (!sourceFileInfo.Exists)
        {
            return false;
        }

        if (targetFileInfo.Exists)
        {
            return false;
        }

        if (targetFileInfo.Directory?.Exists == false)
        {
            targetFileInfo.Directory.Create();
        }

        sourceFileInfo.CopyTo(targetFileInfo.FullName);

        return true;
    }

    public bool CopyIfNotExists(ZipArchive zipArchive, string path, string targetFile)
    {
        var targetFileInfo = new FileInfo(targetFile);
        if (targetFileInfo.Exists)
        {
            return false;
        }

        var entry = zipArchive.GetEntry(path);
        if (entry == null)
        {
            return false;
        }

        if (!targetFileInfo.Directory.Exists)
        {
            targetFileInfo.Directory.Create();
        }

        entry.ExtractToFile(targetFile);

        return true;
    }

    public void CopyFolderRecursive(string sourcePath, string targetPath)
    {
        int counter = 0;
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }

        Directory.CreateDirectory(targetPath);

        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);

        foreach (string newPath in files)
        {
            counter++;

            _console.Report($"Copy {newPath}", counter, files.Length);

            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }

        _console.Log($"...succeeded {counter} items created");
    }

    public void CopyFiles(string sourcePath, string targetPath, string filter = "*.*")
    {
        int counter = 0;

        var files = new DirectoryInfo(sourcePath).GetFiles(filter);

        foreach (var fi in files)
        {
            counter++;
            _console.Report($"Copy {fi.FullName}", counter, files.Length);

            fi.CopyTo(Path.Combine(targetPath, fi.Name));
        }

        _console.Log($"...succeeded {counter} items created");
    }

    public List<string> OverrideFolderRecursive(string sourcePath,
                                        string targetPath,
                                        DeployVersionModel versionModel)
    {
        int counter = 0;
        var copiedFiles = new List<string>();
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }

        Directory.CreateDirectory(targetPath);

        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);

        foreach (string newPath in files)
        {
            counter++;

            _console.Report($"Copy {newPath}", counter, files.Length);

            FileInfo soureFileInfo = new FileInfo(newPath);

            if (soureFileInfo.Extension.ToLower() == ".config")
            {
                var targetFileInfo = new FileInfo(newPath.Replace(sourcePath, targetPath));
                if (targetFileInfo.Exists)
                {
                    targetFileInfo.Delete();
                }

                var config = versionModel.ReplaceModelProperties(File.ReadAllText(soureFileInfo.FullName));

                File.WriteAllText(targetFileInfo.FullName, config);
            }
            else
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }

            copiedFiles.Add(Path.GetRelativePath(sourcePath, newPath));
        }

        _console.Log($"...succeeded {counter} items created/overridden");

        return copiedFiles;
    }

    public void ExtractZipFolderRecursive(ZipArchive zipArchive, string relativeSourcePath, string targetPath)
    {
        if (!relativeSourcePath.EndsWith("/"))
        {
            relativeSourcePath = $"{relativeSourcePath}/";
        }

        int counter = 0;
        var rootEntry = zipArchive.GetEntry(relativeSourcePath);

        relativeSourcePath = rootEntry is not null
            ? rootEntry.FullName
            : relativeSourcePath;

        var entries = zipArchive.Entries.Where(e => e.FullName.StartsWith(relativeSourcePath));
        int entriesLength = entries.Count();

        foreach (var entry in entries)
        {
            counter++;

            if (entry.IsDirectory())
            {
                //Console.WriteLine($"Got ZipArchiveDirectory: {entry.FullName} - {entry.Name}");
                DirectoryInfo di = new DirectoryInfo($"{targetPath}/{entry.FullName.Substring(relativeSourcePath.Length)}");
                di.Create();
            }
            else
            {
                FileInfo fi = new FileInfo($"{targetPath}/{entry.FullName.Substring(relativeSourcePath.Length)}");
                if (!fi.Directory?.Exists == true)
                {
                    fi.Directory?.Create();
                }

                _console.Report($"Extract {entry.FullName}", counter, entriesLength);

                entry.ExtractToFile($"{targetPath}/{entry.FullName.Substring(relativeSourcePath.Length)}");
            }
        }

        _console.Log($"...succeeded {counter} items created");
    }

    public void ExtractFiles(ZipArchive zipArchive, string relativeSourcePath, string targetPath)
    {
        int counter = 0;

        if (!relativeSourcePath.EndsWith("/"))
        {
            relativeSourcePath = $"{relativeSourcePath}/";
        }

        var targetDirectory = new DirectoryInfo(targetPath);
        if (!targetDirectory.Exists)
        {
            targetDirectory.Create();
        }

        var entries = zipArchive.Entries
            .Where(e => e.Length > 0 && e.FullName.StartsWith($"{relativeSourcePath}"))
            .Where(e => e.FullName == $"{relativeSourcePath}{e.Name}")
            .ToArray();

        foreach (var entry in entries)
        {
            counter++;

            _console.Report($"Extract {entry.FullName}", counter, entries.Length);

            entry.ExtractToFile(Path.Combine(targetDirectory.FullName, entry.Name));
        }

        _console.Log($"...succeeded {counter} items created");
    }
}