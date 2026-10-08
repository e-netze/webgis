using System;
using System.Collections.Generic;
using System.IO;

using E.Standard.Platform;

namespace E.Standard.CMS.Core.IO;

/// <summary>
/// One-time snapshot of a file system directory (sub directories + files, in OS enumeration order).
/// Used by the export to avoid a stat/enumeration per item. Name lookups follow the
/// platform file system semantics (case-insensitive on Windows, ordinal on Linux).
/// </summary>
internal class FileSystemDirectoryListing
{
    private static readonly StringComparer NameComparer = SystemInfo.IsLinux
        ? StringComparer.Ordinal
        : StringComparer.OrdinalIgnoreCase;
    private static readonly StringComparison NameComparison = SystemInfo.IsLinux
        ? StringComparison.Ordinal
        : StringComparison.OrdinalIgnoreCase;

    private readonly HashSet<string> _directorySet = new HashSet<string>(NameComparer);
    private readonly HashSet<string> _fileSet = new HashSet<string>(NameComparer);

    public FileSystemDirectoryListing(string path)
    {
        var di = new DirectoryInfo(SystemInfo.IsLinux
            ? path.ToPlatformPath().RemoveDoubleSlashes()
            : path);

        FullName = di.FullName;

        // same enumeration options as DirectoryInfo.GetDirectories()/GetFiles()
        foreach (var info in di.EnumerateFileSystemInfos())
        {
            if (info is DirectoryInfo)
            {
                if (FileSystemPathInfo.IsGitFolder(info.Name))
                {
                    continue;
                }

                Directories.Add(info.Name);
                _directorySet.Add(info.Name);
            }
            else
            {
                Files.Add(info.Name);
                _fileSet.Add(info.Name);
            }
        }
    }

    public string FullName { get; }

    public List<string> Directories { get; } = new List<string>();
    public List<string> Files { get; } = new List<string>();

    public bool ContainsFile(string name) => _fileSet.Contains(name);

    public bool ContainsDirectory(string name) => _directorySet.Contains(name);

    /// <summary>
    /// Equivalent to GetFiles("{prefix}*{suffix}") (without wildcards in prefix/suffix)
    /// </summary>
    public IEnumerable<string> FilesMatching(string prefix, string suffix)
    {
        foreach (var file in Files)
        {
            if (file.Length >= prefix.Length + suffix.Length &&
                file.StartsWith(prefix, NameComparison) &&
                file.EndsWith(suffix, NameComparison))
            {
                yield return file;
            }
        }
    }

    /// <summary>
    /// Names that the file system would normalize (separators, wildcards, trailing dots/blanks)
    /// can't be answered by the listing => caller has to use the file system directly.
    /// </summary>
    public static bool CanAnswer(string name)
        => !String.IsNullOrEmpty(name) &&
           name.IndexOfAny(new[] { '/', '\\', '*', '?' }) < 0 &&
           !name.EndsWith(".") &&
           !name.EndsWith(" ");
}
