using System.Xml;

using E.Standard.CMS.Core.IO;

namespace E.Standard.CMS.Core.Test;

public class ExportOptimizationTests : IDisposable
{
    private readonly string _root;

    public ExportOptimizationTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "cms-export-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    #region FileSystemDirectoryListing

    [Fact]
    public void Listing_MatchesDirectoryInfo_AndSkipsGitFolder()
    {
        Directory.CreateDirectory(Path.Combine(_root, "sub1"));
        Directory.CreateDirectory(Path.Combine(_root, "sub2"));
        Directory.CreateDirectory(Path.Combine(_root, ".git"));
        foreach (var file in new[] { "a.xml", "a.acl", "a@name.acl", "a@x.y.acl", "ab@name.acl", "a@name.acl.bak", ".itemorder.xml", "sub1.acl", "sub1@prop.acl" })
        {
            File.WriteAllText(Path.Combine(_root, file), "");
        }

        var listing = new FileSystemDirectoryListing(_root);
        var di = new DirectoryInfo(_root);

        Assert.Equal(di.GetDirectories().Where(d => d.Name != ".git").Select(d => d.Name), listing.Directories);
        Assert.Equal(di.GetFiles().Select(f => f.Name), listing.Files);

        foreach (var title in new[] { "a", "ab", "sub1", "sub2", "x" })
        {
            Assert.Equal(di.GetFiles(title + "@*.acl").Select(f => f.FullName),
                         listing.FilesMatching(title + "@", ".acl").Select(n => Path.Combine(listing.FullName, n)));
        }

        Assert.True(listing.ContainsFile("a.xml"));
        Assert.False(listing.ContainsFile("sub1"));
        Assert.True(listing.ContainsDirectory("sub1"));
        Assert.False(listing.ContainsDirectory("a.xml"));
        Assert.False(listing.ContainsDirectory(".git"));
    }

    [Theory]
    [InlineData("a.xml", true)]
    [InlineData(".linktemplate.xml", true)]
    [InlineData("a/b.xml", false)]
    [InlineData("a\\b.xml", false)]
    [InlineData("a*.xml", false)]
    [InlineData("a.", false)]
    [InlineData("a ", false)]
    [InlineData("", false)]
    public void Listing_CanAnswer(string name, bool expected)
    {
        Assert.Equal(expected, FileSystemDirectoryListing.CanAnswer(name));
    }

    #endregion

    #region Export

    private const string Schema = """
        <schema>
          <schema-root>
            <schema-node name="links">
              <schema-link name="*" deletable="true" />
            </schema-node>
            <schema-node name="restricted">
              <schema-link name="*" deletable="true" urifilterpath="../targets" />
            </schema-node>
            <schema-node name="targets">
              <schema-link name="*" deletable="true" />
            </schema-node>
            <schema-node name="ignored" deploy="false">
              <schema-link name="*" deletable="true" />
            </schema-node>
          </schema-root>
        </schema>
        """;

    private void WriteLink(string relPath, string linkUri)
    {
        var path = Path.Combine(_root, relPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, $"<config><_linkuri type=\"System.String\">{linkUri}</_linkuri></config>");
    }

    private CMSManager CreateCmsManager()
    {
        var schema = new XmlDocument();
        schema.LoadXml(Schema);

        var cms = new CMSManager(schema);
        cms.SetConnectionString(null!, _root);
        return cms;
    }

    private static IEnumerable<string> Format(IEnumerable<CMSManager.Warning> warnings)
        => warnings.Select(w => $"{w.Level}|{w.Message}|{w.Path}").OrderBy(s => s, StringComparer.Ordinal);

    [Fact]
    public async Task Export_CollectsSameLinkWarningsAsWarningsScan()
    {
        Directory.CreateDirectory(Path.Combine(_root, "targets"));
        WriteLink("targets/t1.link", "links");
        WriteLink("links/ok.link", "targets");
        WriteLink("links/dangling.link", "does/not/exist");
        WriteLink("restricted/valid.link", "targets/t1");
        WriteLink("restricted/invalid.link", "links/ok");

        var expected = Format(CreateCmsManager().Warnings());

        var warnings = new List<CMSManager.Warning>();
        var statistics = new CMSManager.ExportStatistics();
        var xml = await CreateCmsManager().Export(null!, warnings: warnings, statistics: statistics);

        Assert.NotNull(xml);
        Assert.Equal(expected, Format(warnings));
        Assert.Equal(expected, Format(warnings));
        Assert.Contains(warnings, w => w.Level == CMSManager.Warning.WaringLevel.Critical && w.Message == "Link zeigt ins Leere" && w.Path.Contains("dangling"));
        Assert.Contains(warnings, w => w.Level == CMSManager.Warning.WaringLevel.Warning && w.Message == "Link ist ungültig" && w.Path.Contains("links/ok"));
        Assert.DoesNotContain(warnings, w => w.Path.Contains("links\\ok") || w.Path.Contains("links/ok.link"));

        Assert.Equal(5, statistics.Nodes);
    }

    [Fact]
    public async Task Export_IgnoresLinkWarningsOfNotDeployedNodes()
    {
        WriteLink("ignored/dangling.link", "does/not/exist");

        Assert.Single(CreateCmsManager().Warnings());  // full scan still reports it

        var warnings = new List<CMSManager.Warning>();
        await CreateCmsManager().Export(null!, warnings: warnings);

        Assert.Empty(warnings);  // not exported => not checked
    }

    [Fact]
    public async Task Export_ReadsRootAcl()
    {
        WriteLink("links/a.link", "x");

        var xmlDefault = await CreateCmsManager().Export(null!);
        Assert.NotNull(xmlDefault.SelectSingleNode($"//acl/authnode[@value='']/user[@name='{CmsDocument.Everyone}' and @allowed='true']"));

        File.WriteAllText(Path.Combine(_root, "root.acl"), "<acl><user name=\"admin\" allowed=\"true\" /></acl>");
        var xml = await CreateCmsManager().Export(null!);

        Assert.NotNull(xml.SelectSingleNode("//acl/authnode[@value='']/user[@name='admin' and @allowed='true']"));
        Assert.Null(xml.SelectSingleNode($"//acl/authnode[@value='']/user[@name='{CmsDocument.Everyone}']"));
    }

    [Fact]
    public async Task Export_WritesLinksInItemOrder()
    {
        WriteLink("links/b.link", "x");
        WriteLink("links/a.link", "y");
        File.WriteAllText(Path.Combine(_root, "links", ".itemorder.xml"), "<items><item name=\"b.link\" /><item name=\"a.link\" /></items>");

        var xml = await CreateCmsManager().Export(null!);

        var names = xml.SelectNodes("//links/*")!.Cast<XmlNode>().Select(n => n.Name).ToArray();
        Assert.Equal(new[] { "b", "a" }, names);
    }

    #endregion
}
