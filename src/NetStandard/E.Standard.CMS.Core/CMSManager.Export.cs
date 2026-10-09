using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.Platform;

namespace E.Standard.CMS.Core;

public partial class CMSManager
{
    /// <summary>
    /// Timings and counters of an export (deploy). Times are accumulated per phase.
    /// </summary>
    public class ExportStatistics
    {
        internal readonly Stopwatch Total = new Stopwatch();
        internal readonly Stopwatch FileSystem = new Stopwatch();
        internal readonly Stopwatch Acl = new Stopwatch();
        internal readonly Stopwatch Read = new Stopwatch();
        internal readonly Stopwatch Build = new Stopwatch();
        internal readonly Stopwatch Links = new Stopwatch();

        public int Directories { get; internal set; }
        public int Nodes { get; internal set; }

        /// <summary>
        /// Export cache: entries served from the snapshot / read from the file system (null => no cache)
        /// </summary>
        public int? CacheHits { get; internal set; }
        public int? CacheMisses { get; internal set; }

        public TimeSpan TotalTime => Total.Elapsed;
        public TimeSpan FileSystemTime => FileSystem.Elapsed;
        public TimeSpan AclTime => Acl.Elapsed;
        public TimeSpan ReadTime => Read.Elapsed;
        public TimeSpan BuildTime => Build.Elapsed;
        public TimeSpan LinksTime => Links.Elapsed;

        public override string ToString()
            => $"{Nodes} nodes in {Directories} folders: {Ms(Total.Elapsed)} total " +
               $"(file system {Ms(FileSystem.Elapsed)}, acl {Ms(Acl.Elapsed)}, read {Ms(Read.Elapsed)}, build {Ms(Build.Elapsed)}, link check {Ms(Links.Elapsed)}, " +
               $"schema/other {Ms(Total.Elapsed - FileSystem.Elapsed - Acl.Elapsed - Read.Elapsed - Build.Elapsed - Links.Elapsed)})" +
               (CacheHits.HasValue ? $", cache: {CacheHits} entries from cache, {CacheMisses} read from file system" : String.Empty);

        private static string Ms(TimeSpan ts) => $"{Math.Max(0, (long)ts.TotalMilliseconds)}ms";
    }

    private class ExportContext
    {
        public ExportContext(List<Warning> warnings, ExportStatistics statistics)
        {
            Warnings = warnings;
            Statistics = statistics;
        }

        // null => no link checks
        public List<Warning> Warnings { get; }
        public ExportStatistics Statistics { get; }
    }

    #region Export file access (optional snapshot cache)

    // snapshot of the running export (null => file system only)
    private ExportFileCache _exportCache = null;
    private string _exportCacheRoot = null;

    private void BeginExportCache(ExportFileCache cache)
    {
        _exportCache = null;
        _exportCacheRoot = null;

        if (cache == null ||
            !DocumentFactory.IsDefaultFileSystem(_root) ||
            DocumentFactory.PathInfo(_root)?.GetType() != typeof(FileSystemPathInfo))
        {
            return;
        }

        _exportCache = cache;
        _exportCache.Hits = _exportCache.Misses = 0;
        _exportCacheRoot = NormalizeFullName(new DirectoryInfo(SystemInfo.IsLinux
            ? _root.ToPlatformPath().RemoveDoubleSlashes()
            : _root).FullName);
    }

    private void EndExportCache(ExportStatistics statistics)
    {
        if (_exportCache != null)
        {
            statistics.CacheHits = _exportCache.Hits;
            statistics.CacheMisses = _exportCache.Misses;
        }

        _exportCache = null;
        _exportCacheRoot = null;
    }

    private static string NormalizeFullName(string fullName)
        => fullName.Replace('\\', '/').TrimEnd('/');

    /// <summary>
    /// relative cache key of a full path inside the tree; null => not cacheable (use file system)
    /// </summary>
    private string ExportCacheKey(string fullName)
    {
        if (_exportCache == null || String.IsNullOrEmpty(fullName) || !DocumentFactory.IsDefaultFileSystem(fullName))
        {
            return null;
        }

        var normalized = NormalizeFullName(fullName);
        var comparison = SystemInfo.IsLinux ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        if (normalized.Equals(_exportCacheRoot, comparison))
        {
            return String.Empty;
        }
        if (!normalized.StartsWith(_exportCacheRoot + "/", comparison))
        {
            return null;
        }

        return ExportFileCache.NormalizeRelPath(normalized.Substring(_exportCacheRoot.Length + 1));
    }

    private FileSystemDirectoryListing ExportListing(IPathInfo pathInfo)
    {
        var key = _exportCache != null && pathInfo?.GetType() == typeof(FileSystemPathInfo)
            ? ExportCacheKey(pathInfo.FullName)
            : null;

        if (key == null)
        {
            return TryCreateListing(pathInfo);
        }

        if (_exportCache.TryGetListing(key, out var cached))
        {
            _exportCache.Hits++;
            return new FileSystemDirectoryListing(pathInfo.FullName, cached.Directories, cached.Files);
        }

        var listing = TryCreateListing(pathInfo);
        if (listing != null)
        {
            _exportCache.Misses++;
            _exportCache.SetListing(key, listing.Directories, listing.Files);
        }
        return listing;
    }

    /// <summary>
    /// raw content of a file of the tree (snapshot or file system). null => not cacheable
    /// </summary>
    private byte[] ExportReadBytes(string fullName)
    {
        var key = ExportCacheKey(fullName);
        if (key == null || key.Length == 0)
        {
            return null;
        }

        if (_exportCache.TryGetFile(key, out var content))
        {
            _exportCache.Hits++;
            return content;
        }

        content = File.ReadAllBytes(fullName);
        _exportCache.Misses++;
        _exportCache.SetFile(key, content);
        return content;
    }

    private IStreamDocument ExportOpenDocument(string fullName)
    {
        if (_exportCache != null)
        {
            byte[] content;
            try
            {
                content = ExportReadBytes(fullName);
            }
            catch (Exception ex)
            {
                // same exception as XmlFileStreamDocument.Init
                throw new FileLoadException("Datei '" + fullName + "' kann nicht gelesen werden!", fullName, ex);
            }

            if (content != null)
            {
                var document = new XmlFileStreamDocument();
                document.InitFromContent(fullName, content);
                return document;
            }
        }

        return DocumentFactory.Open(fullName);
    }

    private Func<string, Stream> ExportOpenRead()
    {
        if (_exportCache == null)
        {
            return null;
        }

        return fullName =>
        {
            var content = ExportReadBytes(fullName);
            return content != null
                ? new MemoryStream(content, false)
                : File.OpenRead(fullName);
        };
    }

    // same as File.ReadAllText (FileSystemDocumentInfo.ReadAll)
    private string ExportReadAllText(IDocumentInfo documentInfo)
    {
        if (_exportCache != null && documentInfo is FileSystemDocumentInfo)
        {
            var content = ExportReadBytes(documentInfo.FullName);
            if (content != null)
            {
                using var reader = new StreamReader(new MemoryStream(content, false), Encoding.UTF8, true);
                return reader.ReadToEnd();
            }
        }

        return documentInfo.ReadAll();
    }

    private bool ExportFileExists(IDocumentInfo documentInfo)
    {
        if (_exportCache != null && documentInfo is FileSystemDocumentInfo)
        {
            var key = ExportCacheKey(documentInfo.FullName);
            var exists = key == null || key.Length == 0 ? null : _exportCache.ContainsFile(key);
            if (exists.HasValue)
            {
                _exportCache.Hits++;
                return exists.Value;
            }
        }

        return documentInfo.Exists;
    }

    private bool ExportDirectoryExists(IPathInfo pathInfo)
    {
        if (_exportCache != null && pathInfo is FileSystemPathInfo)
        {
            var key = ExportCacheKey(pathInfo.FullName);
            var exists = key == null ? null : _exportCache.ContainsDirectory(key);
            if (exists.HasValue)
            {
                _exportCache.Hits++;
                return exists.Value;
            }
        }

        return pathInfo.Exists;
    }

    #endregion
}
