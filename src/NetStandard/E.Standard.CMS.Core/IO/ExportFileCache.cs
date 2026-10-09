using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using E.Standard.Platform;

namespace E.Standard.CMS.Core.IO;

/// <summary>
/// Snapshot of the raw files (directory listings + file contents) of a file system tree, used by the
/// export ("fast deploy") to avoid file system access for unchanged parts of the tree.
/// Entries of changed paths are removed with <see cref="Invalidate"/>; missing entries are read from the
/// file system by the export and added to the snapshot. The export result is the same as without a cache.
/// </summary>
public class ExportFileCache
{
    public const int FormatVersion = 1;
    private const string Magic = "WGXC";

    private static readonly StringComparer NameComparer = SystemInfo.IsLinux
        ? StringComparer.Ordinal
        : StringComparer.OrdinalIgnoreCase;

    private readonly Dictionary<string, CachedListing> _listings = new Dictionary<string, CachedListing>(NameComparer);
    private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>(NameComparer);

    public string Commit { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Files (relative paths) with uncommitted changes when the snapshot was taken.
    /// They have to be invalidated on the next use, even if they are not changed anymore (reverted).
    /// </summary>
    public IReadOnlyCollection<string> UncommittedFiles { get; set; } = Array.Empty<string>();

    public int FileCount => _files.Count;
    public int FolderCount => _listings.Count;

    #region Export access

    internal int Hits { get; set; }
    internal int Misses { get; set; }

    internal bool TryGetListing(string relPath, out CachedListing listing)
        => _listings.TryGetValue(relPath, out listing);

    internal void SetListing(string relPath, IEnumerable<string> directories, IEnumerable<string> files)
        => _listings[relPath] = new CachedListing(directories.ToArray(), files.ToArray());

    internal bool TryGetFile(string relPath, out byte[] content)
        => _files.TryGetValue(relPath, out content);

    internal void SetFile(string relPath, byte[] content)
        => _files[relPath] = content;

    /// <summary>
    /// true/false, if the (valid) listing of the parent folder can answer the question, null otherwise
    /// </summary>
    internal bool? ContainsFile(string relPath)
    {
        if (_files.ContainsKey(relPath))
        {
            return true;
        }

        SplitParent(relPath, out var parent, out var name);
        return _listings.TryGetValue(parent, out var listing)
            ? listing.ContainsFile(name)
            : null;
    }

    internal bool? ContainsDirectory(string relPath)
    {
        if (relPath.Length == 0)
        {
            return null;
        }

        SplitParent(relPath, out var parent, out var name);
        return _listings.TryGetValue(parent, out var listing)
            ? listing.ContainsDirectory(name)
            : null;
    }

    #endregion

    #region Invalidate

    /// <summary>
    /// Removes the entries of changed files (relative paths, '/' separated) from the snapshot.
    /// A directory listing is removed, if an entry was added to or removed from the folder (compared with the file system).
    /// </summary>
    /// <returns>number of removed entries</returns>
    public int Invalidate(string rootPath, IEnumerable<string> changedPaths)
    {
        int removed = 0;

        foreach (var changedPath in changedPaths ?? Enumerable.Empty<string>())
        {
            var relPath = NormalizeRelPath(changedPath);
            if (relPath == null)
            {
                // can't be mapped => no reliable snapshot
                removed += _files.Count + _listings.Count;
                _files.Clear();
                _listings.Clear();
                return removed;
            }
            if (relPath.Length == 0)
            {
                continue;
            }

            if (_files.Remove(relPath))
            {
                removed++;
            }
            if (_listings.Remove(relPath))  // file <-> folder type change
            {
                removed++;
            }

            // walk up: invalidate listings, that don't match the file system anymore
            bool isFile = true;
            var current = relPath;
            while (current.Length > 0)
            {
                SplitParent(current, out var parent, out var name);
                var fullName = Path.Combine(rootPath, current.Replace('/', Path.DirectorySeparatorChar));

                bool existsNow = isFile ? File.Exists(fullName) : Directory.Exists(fullName);

                if (_listings.TryGetValue(parent, out var listing))
                {
                    bool inListing = isFile ? listing.ContainsFile(name) : listing.ContainsDirectory(name);
                    if (inListing == existsNow)
                    {
                        break;  // folder structure unchanged from here
                    }

                    _listings.Remove(parent);
                    removed++;
                }

                current = parent;
                isFile = false;
            }
        }

        return removed;
    }

    /// <summary>
    /// '/' separated path relative to the tree root (no leading/trailing separator). null => not representable.
    /// </summary>
    internal static string NormalizeRelPath(string path)
    {
        if (path == null)
        {
            return null;
        }

        var parts = path.Replace('\\', '/').Split('/').Where(p => p.Length > 0).ToArray();
        if (parts.Any(p => p == "." || p == ".." || !FileSystemDirectoryListing.CanAnswer(p)))
        {
            return null;
        }

        return String.Join("/", parts);
    }

    private static void SplitParent(string relPath, out string parent, out string name)
    {
        int pos = relPath.LastIndexOf('/');
        parent = pos < 0 ? String.Empty : relPath.Substring(0, pos);
        name = pos < 0 ? relPath : relPath.Substring(pos + 1);
    }

    #endregion

    #region Persistence

    public void Save(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!String.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";

        using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using (var header = new BinaryWriter(fileStream, Encoding.UTF8, true))
            {
                WriteHeader(header);
            }

            using (var gzip = new GZipStream(fileStream, CompressionLevel.Fastest, true))
            using (var writer = new BinaryWriter(gzip, Encoding.UTF8, true))
            {
                var uncommitted = UncommittedFiles ?? Array.Empty<string>();
                writer.Write(uncommitted.Count);
                foreach (var file in uncommitted)
                {
                    writer.Write(file ?? String.Empty);
                }

                writer.Write(_listings.Count);
                foreach (var entry in _listings)
                {
                    writer.Write(entry.Key);
                    WriteNames(writer, entry.Value.Directories);
                    WriteNames(writer, entry.Value.Files);
                }

                writer.Write(_files.Count);
                foreach (var entry in _files)
                {
                    writer.Write(entry.Key);
                    writer.Write(entry.Value.Length);
                    writer.Write(entry.Value);
                }
            }
        }

        File.Move(tempPath, path, true);
    }

    /// <summary>
    /// Loads a snapshot. Throws, if the file is missing, corrupt or has an other format version.
    /// </summary>
    public static ExportFileCache Load(string path)
    {
        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var header = new BinaryReader(fileStream, Encoding.UTF8, true);

        var cache = new ExportFileCache();
        var info = ReadHeader(header);
        cache.Commit = info.Commit;
        cache.CreatedUtc = info.CreatedUtc;

        using var gzip = new GZipStream(fileStream, CompressionMode.Decompress, true);
        using var reader = new BinaryReader(gzip, Encoding.UTF8, true);

        var uncommitted = new string[reader.ReadInt32()];
        for (int i = 0; i < uncommitted.Length; i++)
        {
            uncommitted[i] = reader.ReadString();
        }
        cache.UncommittedFiles = uncommitted;

        int listingCount = reader.ReadInt32();
        for (int i = 0; i < listingCount; i++)
        {
            var key = reader.ReadString();
            cache._listings[key] = new CachedListing(ReadNames(reader), ReadNames(reader));
        }

        int fileCount = reader.ReadInt32();
        for (int i = 0; i < fileCount; i++)
        {
            var key = reader.ReadString();
            int length = reader.ReadInt32();
            var content = reader.ReadBytes(length);
            if (content.Length != length)
            {
                throw new EndOfStreamException("Export cache is truncated");
            }
            cache._files[key] = content;
        }

        if (cache.FileCount != info.Files || cache.FolderCount != info.Folders)
        {
            throw new InvalidDataException("Export cache is corrupt");
        }

        return cache;
    }

    /// <summary>
    /// Reads only the header of a snapshot. null, if missing or invalid.
    /// </summary>
    public static ExportFileCacheInfo ReadInfo(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(fileStream, Encoding.UTF8, true);

            return ReadHeader(reader);
        }
        catch
        {
            return null;
        }
    }

    private void WriteHeader(BinaryWriter writer)
    {
        writer.Write(Encoding.ASCII.GetBytes(Magic));
        writer.Write(FormatVersion);
        writer.Write(Commit ?? String.Empty);
        writer.Write(CreatedUtc.ToUniversalTime().Ticks);
        writer.Write(_files.Count);
        writer.Write(_listings.Count);
    }

    private static ExportFileCacheInfo ReadHeader(BinaryReader reader)
    {
        var magic = Encoding.ASCII.GetString(reader.ReadBytes(Magic.Length));
        if (magic != Magic)
        {
            throw new InvalidDataException("Not an export cache file");
        }

        int version = reader.ReadInt32();
        if (version != FormatVersion)
        {
            throw new InvalidDataException($"Unsupported export cache version {version}");
        }

        var commit = reader.ReadString();

        return new ExportFileCacheInfo()
        {
            Commit = String.IsNullOrEmpty(commit) ? null : commit,
            CreatedUtc = new DateTime(reader.ReadInt64(), DateTimeKind.Utc),
            Files = reader.ReadInt32(),
            Folders = reader.ReadInt32()
        };
    }

    private static void WriteNames(BinaryWriter writer, string[] names)
    {
        writer.Write(names.Length);
        foreach (var name in names)
        {
            writer.Write(name);
        }
    }

    private static string[] ReadNames(BinaryReader reader)
    {
        var names = new string[reader.ReadInt32()];
        for (int i = 0; i < names.Length; i++)
        {
            names[i] = reader.ReadString();
        }
        return names;
    }

    #endregion

    internal class CachedListing
    {
        private readonly HashSet<string> _directorySet;
        private readonly HashSet<string> _fileSet;

        public CachedListing(string[] directories, string[] files)
        {
            Directories = directories;
            Files = files;
            _directorySet = new HashSet<string>(directories, NameComparer);
            _fileSet = new HashSet<string>(files, NameComparer);
        }

        // enumeration order of the file system
        public string[] Directories { get; }
        public string[] Files { get; }

        public bool ContainsFile(string name) => _fileSet.Contains(name);
        public bool ContainsDirectory(string name) => _directorySet.Contains(name);
    }
}

public class ExportFileCacheInfo
{
    public string Commit { get; set; }
    public DateTime CreatedUtc { get; set; }
    public int Files { get; set; }
    public int Folders { get; set; }
}
