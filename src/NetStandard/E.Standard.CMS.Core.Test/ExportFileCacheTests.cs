using System.Xml;

using E.Standard.CMS.Core.IO;

namespace E.Standard.CMS.Core.Test;

/// <summary>
/// Fast deploy: an export with a (invalidated) snapshot cache must give exactly the same result as a full export
/// </summary>
public class ExportFileCacheTests : IDisposable
{
    private readonly string _root;
    private readonly string _cacheFile;

    public ExportFileCacheTests()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "cms-export-cache-tests-" + Guid.NewGuid().ToString("N"));
        _root = Path.Combine(baseDir, "tree");
        _cacheFile = Path.Combine(baseDir, "cache", "user.snapshot");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(Path.GetDirectoryName(_root)!, true); } catch { }
    }

    private const string Schema = """
        <schema>
          <schema-root>
            <schema-node name="links">
              <schema-link name="*" deletable="true" />
            </schema-node>
            <schema-node name="targets">
              <schema-link name="*" deletable="true" />
            </schema-node>
            <schema-node name="services">
              <schema-node name="*">
                <schema-node name="queries">
                  <schema-node name="*">
                    <schema-link name="*" deletable="true" />
                  </schema-node>
                </schema-node>
                <schema-link name="*" deletable="true" />
              </schema-node>
            </schema-node>
          </schema-root>
        </schema>
        """;

    #region Helper

    private string Full(string relPath) => Path.Combine(_root, relPath.Replace('/', Path.DirectorySeparatorChar));

    private void Write(string relPath, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Full(relPath))!);
        File.WriteAllText(Full(relPath), content);
    }

    private void WriteLink(string relPath, string linkUri)
        => Write(relPath, $"<config><_linkuri type=\"System.String\">{linkUri}</_linkuri></config>");

    private void WriteAcl(string relPath, string user)
        => Write(relPath, $"<acl><user name=\"{user}\" allowed=\"true\" /></acl>");

    private void WriteItemOrder(string folder, params string[] items)
        => Write(folder + "/.itemorder.xml", "<items>" + String.Concat(items.Select(i => $"<item name=\"{i}\" />")) + "</items>");

    private CMSManager CreateCmsManager()
    {
        var schema = new XmlDocument();
        schema.LoadXml(Schema);

        var cms = new CMSManager(schema);
        cms.SetConnectionString(null!, _root);
        return cms;
    }

    private async Task<(string xml, string[] warnings, CMSManager.ExportStatistics statistics)> Export(ExportFileCache? cache = null)
    {
        var warnings = new List<CMSManager.Warning>();
        var statistics = new CMSManager.ExportStatistics();
        var xml = await CreateCmsManager().Export(null!, warnings: warnings, statistics: statistics, fileCache: cache);

        return (xml.OuterXml,
                warnings.Select(w => $"{w.Level}|{w.Message}|{w.Path}").ToArray(),
                statistics);
    }

    private void CreateInitialTree()
    {
        WriteLink("links/a.link", "targets/t1");
        WriteLink("links/b.link", "targets/t2");
        WriteLink("links/c.link", "targets/t1");
        WriteItemOrder("links", "c.link", "a.link", "b.link");
        WriteLink("targets/t1.link", "links");
        WriteLink("targets/t2.link", "links");
        WriteAcl("links/b.acl", "admin");

        foreach (var service in new[] { "s1", "s2", "s3" })
        {
            WriteLink($"services/{service}/theme1.link", "targets/t1");
            WriteLink($"services/{service}/queries/q1/field1.link", "targets/t2");
            WriteLink($"services/{service}/queries/q2/field1.link", "targets/t2");
        }
    }

    /// <summary>
    /// builds a snapshot (full export), applies the change, invalidates the given paths and
    /// compares the fast export with a full export
    /// </summary>
    private async Task<(string xml, string[] warnings, CMSManager.ExportStatistics statistics)> AssertFastEqualsFull(Action change, params string[] changedPaths)
    {
        CreateInitialTree();

        var recording = new ExportFileCache() { Commit = "c1" };
        var initial = await Export(recording);
        Assert.Equal((await Export()).xml, initial.xml);
        recording.Save(_cacheFile);

        change();

        var cache = ExportFileCache.Load(_cacheFile);
        cache.Invalidate(_root, changedPaths);

        var fast = await Export(cache);
        var full = await Export();

        Assert.Equal(full.xml, fast.xml);
        Assert.Equal(full.warnings, fast.warnings);
        Assert.True(fast.statistics.CacheHits > 0);

        // the cache is complete again => same result without file system access
        var again = await Export(cache);
        Assert.Equal(full.xml, again.xml);
        Assert.Equal(0, again.statistics.CacheMisses);

        return fast;
    }

    #endregion

    [Fact]
    public async Task Cache_IsUsed_StaleWithoutInvalidation()
    {
        CreateInitialTree();

        var cache = new ExportFileCache();
        var first = await Export(cache);
        Assert.True(first.statistics.CacheMisses > 0);

        WriteLink("links/a.link", "targets/t2");

        var stale = await Export(cache);
        Assert.Equal(0, stale.statistics.CacheMisses);
        Assert.Equal(first.xml, stale.xml);  // not invalidated => old content

        cache.Invalidate(_root, new[] { "links/a.link" });
        var fast = await Export(cache);
        Assert.Equal((await Export()).xml, fast.xml);
        Assert.NotEqual(first.xml, fast.xml);
        Assert.Equal(1, fast.statistics.CacheMisses);  // only the changed file is read
    }

    [Fact]
    public async Task ModifiedFile_ReadsOnlyTheFile()
    {
        var fast = await AssertFastEqualsFull(() => WriteLink("services/s2/theme1.link", "targets/t2"),
                                              "services/s2/theme1.link");

        Assert.Equal(1, fast.statistics.CacheMisses);
    }

    [Fact]
    public async Task NewServiceFolder()
        => await AssertFastEqualsFull(() =>
        {
            WriteLink("services/s4/theme1.link", "targets/t1");
            WriteLink("services/s4/queries/q1/field1.link", "targets/t2");
        },
        "services/s4/theme1.link", "services/s4/queries/q1/field1.link");

    [Fact]
    public async Task NewQuery_WithItemOrder()
        => await AssertFastEqualsFull(() =>
        {
            WriteItemOrder("services/s1/queries", "q2", "q1", "q3");
            WriteLink("services/s1/queries/q3/field1.link", "targets/t1");
        },
        "services/s1/queries/.itemorder.xml", "services/s1/queries/q3/field1.link");

    [Fact]
    public async Task DeletedFile()
        => await AssertFastEqualsFull(() => File.Delete(Full("links/c.link")),
                                      "links/c.link");

    [Fact]
    public async Task DeletedFolder()
        => await AssertFastEqualsFull(() => Directory.Delete(Full("services/s2"), true),
                                      "services/s2/theme1.link", "services/s2/queries/q1/field1.link", "services/s2/queries/q2/field1.link");

    [Fact]
    public async Task RenamedFolder()
        => await AssertFastEqualsFull(() => Directory.Move(Full("services/s3/queries/q2"), Full("services/s3/queries/q9")),
                                      "services/s3/queries/q2/field1.link", "services/s3/queries/q9/field1.link");

    [Fact]
    public async Task AclChanges()
        => await AssertFastEqualsFull(() =>
        {
            WriteAcl("links/b.acl", "editor");
            WriteAcl("links/a.acl", "admin");
            WriteAcl("services/s1@name.acl", "admin");
            WriteAcl("services/s2.acl", "admin");
        },
        "links/b.acl", "links/a.acl", "services/s1@name.acl", "services/s2.acl");

    [Fact]
    public async Task RootAcl()
        => await AssertFastEqualsFull(() => WriteAcl("root.acl", "admin"),
                                      "root.acl");

    [Fact]
    public async Task DeletedLinkTarget_Warning()
    {
        var fast = await AssertFastEqualsFull(() => File.Delete(Full("targets/t2.link")),
                                              "targets/t2.link");

        Assert.Contains(fast.warnings, w => w.Contains("Link zeigt ins Leere") && w.Contains("links"));
    }

    [Fact]
    public async Task UnchangedTree_NoFileSystemAccess()
    {
        var fast = await AssertFastEqualsFull(() => { });

        Assert.Equal(0, fast.statistics.CacheMisses);
    }

    [Fact]
    public async Task ModifiedFile_KeepsFolderListing()
    {
        CreateInitialTree();
        var cache = new ExportFileCache();
        await Export(cache);
        int folders = cache.FolderCount;

        WriteLink("links/a.link", "targets/t2");
        Assert.Equal(1, cache.Invalidate(_root, new[] { "links/a.link" }));
        Assert.Equal(folders, cache.FolderCount);

        File.Delete(Full("links/b.link"));
        Assert.Equal(2, cache.Invalidate(_root, new[] { "links/b.link" }));  // file + listing of "links"
        Assert.Equal(folders - 1, cache.FolderCount);
    }

    [Fact]
    public void SaveLoad_Header()
    {
        var cache = new ExportFileCache() { Commit = "abc", UncommittedFiles = new[] { "x/y.xml" } };
        cache.Save(_cacheFile);

        var info = ExportFileCache.ReadInfo(_cacheFile);
        Assert.NotNull(info);
        Assert.Equal("abc", info!.Commit);

        var loaded = ExportFileCache.Load(_cacheFile);
        Assert.Equal(new[] { "x/y.xml" }, loaded.UncommittedFiles);

        File.WriteAllText(_cacheFile, "corrupt");
        Assert.Null(ExportFileCache.ReadInfo(_cacheFile));
        Assert.ThrowsAny<Exception>(() => ExportFileCache.Load(_cacheFile));
    }
}
