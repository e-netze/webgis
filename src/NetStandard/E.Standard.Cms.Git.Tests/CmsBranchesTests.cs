using E.Standard.CMS.Core.Branches;

namespace E.Standard.Cms.Git.Tests;

public class CmsBranchesTests : IDisposable
{
    private readonly string _root;
    private readonly string _mainFile;

    public CmsBranchesTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "cms-branches-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _mainFile = Path.Combine(_root, "cms.xml");
        File.WriteAllText(_mainFile, "<main/>");
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    [Theory]
    [InlineData("author/zweig1", "author_2fzweig1")]
    [InlineData("feature-x", "feature-x")]
    [InlineData("My_Branch", "_4dy_5f_42ranch")]
    public void Encode_ReturnsExpected(string branch, string expected)
    {
        Assert.Equal(expected, CmsBranches.Encode(branch));
    }

    [Theory]
    [InlineData("author/zweig1")]
    [InlineData("Ünterschied/äöü-ß")]
    [InlineData("UPPER_lower.dot$dollar")]
    [InlineData("a b+c")]
    public void Encode_Decode_RoundTrip(string branch)
    {
        var encoded = CmsBranches.Encode(branch);

        Assert.True(CmsBranches.IsValidEncoded(encoded));
        Assert.DoesNotContain('$', encoded);
        Assert.Equal(encoded, encoded.ToLowerInvariant());
        Assert.Equal(branch, CmsBranches.Decode(encoded));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Upper")]
    [InlineData("a/b")]
    [InlineData("a_2")]
    [InlineData("a_zz")]
    [InlineData("a_2F")]
    [InlineData("..")]
    public void IsValidEncoded_RejectsInvalid(string encoded)
    {
        Assert.False(CmsBranches.IsValidEncoded(encoded));
    }

    [Fact]
    public void CmsName_SplitAndJoin()
    {
        Assert.Equal("default$abc", CmsBranches.ToCmsName("default", "abc"));
        Assert.Equal("default", CmsBranches.ToCmsName("default", ""));
        Assert.Equal(("default", "abc"), CmsBranches.SplitCmsName("default$abc"));
        Assert.Equal(("default", ""), CmsBranches.SplitCmsName("default"));
        Assert.True(CmsBranches.IsBranchCmsName("x$y"));
        Assert.False(CmsBranches.IsBranchCmsName("x"));
    }

    [Fact]
    public void WriteFindReadDelete_Branch()
    {
        var encoded = CmsBranches.Encode("author/zweig1");
        CmsBranches.WriteBranch(_mainFile, new CmsBranchDeployInfo()
        {
            Branch = "author/zweig1",
            EncodedBranch = encoded,
            User = "author",
            Commit = "abc123",
            Date = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc)
        }, path => File.WriteAllText(path, "<branch/>"));

        // invalid / incomplete folders are ignored
        Directory.CreateDirectory(Path.Combine(_root, "branches", "Invalid"));
        Directory.CreateDirectory(Path.Combine(_root, "branches", "noxml"));

        Assert.Equal(new[] { encoded }, CmsBranches.FindBranches(_mainFile));
        Assert.Equal("<branch/>", File.ReadAllText(CmsBranches.BranchFilePath(_mainFile, encoded)));

        var info = Assert.Single(CmsBranches.ReadDeployInfos(_mainFile));
        Assert.Equal("author/zweig1", info.Branch);
        Assert.Equal(encoded, info.EncodedBranch);
        Assert.Equal("author", info.User);
        Assert.Equal("abc123", info.Commit);

        Assert.True(CmsBranches.DeleteBranch(_mainFile, encoded));
        Assert.Empty(CmsBranches.FindBranches(_mainFile));
        Assert.False(CmsBranches.DeleteBranch(_mainFile, encoded));
    }

    [Fact]
    public void SameBranch_DifferentCmsXmlInSameFolder_AreIndependent()
    {
        var otherFile = Path.Combine(_root, "other.xml");
        File.WriteAllText(otherFile, "<other/>");
        var encoded = CmsBranches.Encode("branch1");

        CmsBranches.WriteBranch(_mainFile, new CmsBranchDeployInfo() { Branch = "branch1", EncodedBranch = encoded, User = "a", Commit = "c1" },
            path => File.WriteAllText(path, "<main-branch/>"));
        CmsBranches.WriteBranch(otherFile, new CmsBranchDeployInfo() { Branch = "branch1", EncodedBranch = encoded, User = "b", Commit = "c2" },
            path => File.WriteAllText(path, "<other-branch/>"));

        Assert.Equal("a", CmsBranches.ReadDeployInfo(_mainFile, encoded).User);
        Assert.Equal("b", CmsBranches.ReadDeployInfo(otherFile, encoded).User);

        Assert.True(CmsBranches.DeleteBranch(_mainFile, encoded));
        Assert.Empty(CmsBranches.FindBranches(_mainFile));
        Assert.Equal(new[] { encoded }, CmsBranches.FindBranches(otherFile));
        Assert.Equal("b", CmsBranches.ReadDeployInfo(otherFile, encoded).User);

        Assert.True(CmsBranches.DeleteBranch(otherFile, encoded));
        Assert.False(Directory.Exists(Path.Combine(_root, "branches", encoded)));
    }

    [Fact]
    public void ReadDeployInfo_WithoutDeployJson_FallsBackToDecodedName()
    {
        var encoded = CmsBranches.Encode("feature/a");
        var dir = Path.Combine(_root, "branches", encoded);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "cms.xml"), "<branch/>");

        var info = CmsBranches.ReadDeployInfo(_mainFile, encoded);

        Assert.Equal("feature/a", info.Branch);
        Assert.NotNull(info.Date);
    }

    [Fact]
    public void BranchDirectory_RejectsPathTraversal()
    {
        Assert.Throws<ArgumentException>(() => CmsBranches.BranchDirectory(_mainFile, ".."));
        Assert.Throws<ArgumentException>(() => CmsBranches.DeleteBranch(_mainFile, "../x"));
    }
}
