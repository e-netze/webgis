using E.Standard.CMS.Core.Branches;
using E.Standard.Security.Cryptography.Services;
using Microsoft.Extensions.Options;
using System;
using Xunit;

namespace E.Standard.Cms.Git.Tests;

public class CmsBranchTokensTests
{
    private readonly CryptoService _crypto = new CryptoService(Options.Create(new CryptoServiceOptions()));

    [Fact]
    public void Create_TryRead_RoundTrip_WithoutExpiration()
    {
        var encoded = CmsBranches.Encode("Feature/Zweig ä");
        var token = CmsBranchTokens.Create(_crypto, encoded);

        Assert.StartsWith(CmsBranchTokens.Prefix, token);
        Assert.Matches("^enc:[0-9a-f]+$", token);
        Assert.True(CmsBranchTokens.TryRead(_crypto, token, out var branch, out var expires));
        Assert.Equal(encoded, branch);
        Assert.Null(expires);
        Assert.Equal(encoded, CmsBranchTokens.ResolveEncodedBranch(_crypto, token));
    }

    [Fact]
    public void Expired_Token_Is_Accepted_By_Api_Within_Tolerance_Only()
    {
        var recent = CmsBranchTokens.Create(_crypto, "zweig1", DateTime.UtcNow.AddHours(-1));
        Assert.True(CmsBranchTokens.TryRead(_crypto, recent, out _, out var expires));
        Assert.True(CmsBranchTokens.IsExpired(expires));
        Assert.Equal("zweig1", CmsBranchTokens.ResolveEncodedBranch(_crypto, recent));

        var old = CmsBranchTokens.Create(_crypto, "zweig1", DateTime.UtcNow.AddHours(-13));
        Assert.Null(CmsBranchTokens.ResolveEncodedBranch(_crypto, old));

        var valid = CmsBranchTokens.Create(_crypto, "zweig1", DateTime.UtcNow.AddHours(1));
        Assert.True(CmsBranchTokens.TryRead(_crypto, valid, out _, out expires));
        Assert.False(CmsBranchTokens.IsExpired(expires));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("zweig1")]
    [InlineData("enc:")]
    [InlineData("enc:zweig1")]
    [InlineData("enc:abcd")]
    [InlineData("enc:0x1234")]
    public void Invalid_Tokens_Are_Rejected(string token)
    {
        Assert.False(CmsBranchTokens.TryRead(_crypto, token, out _, out _));
        Assert.Null(CmsBranchTokens.ResolveEncodedBranch(_crypto, token));
    }

    [Fact]
    public void Tampered_Token_Is_Rejected()
    {
        var token = CmsBranchTokens.Create(_crypto, "zweig1");
        var last = token[^1];
        var tampered = token.Substring(0, token.Length - 1) + (last == '0' ? '1' : '0');

        Assert.Null(CmsBranchTokens.ResolveEncodedBranch(_crypto, tampered));
    }

    [Fact]
    public void Create_Rejects_Invalid_Branch()
    {
        Assert.Throws<ArgumentException>(() => CmsBranchTokens.Create(_crypto, "Feature/A"));
    }
}