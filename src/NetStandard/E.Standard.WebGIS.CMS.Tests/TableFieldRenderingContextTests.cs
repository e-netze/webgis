using System.Collections.Specialized;

using E.Standard.Api.App.Data;
using E.Standard.Api.App.DTOs;
using E.Standard.WebMapping.Core;

namespace E.Standard.WebGIS.CMS.Tests;

public class TableFieldRenderingContextTests
{
    [Fact]
    public async Task ImageContext_ResolvesHeadersOnceAndReusesFeatureParameters()
    {
        var field = new TableFieldImage
        {
            ImageExpression = "{header:X-SCHEME}://{header:X-HOST}/[ID].png",
            ImageWidth = 20,
            ImageHeight = 10
        };
        var context = await CreateContextAsync(
            field,
            new NameValueCollection
            {
                ["X-SCHEME"] = "https",
                ["X-HOST"] = "images.example"
            });

        Assert.Contains(
            "src='https://images.example/1.png'",
            context.RenderField(field, CreateFeature("1")));
        Assert.Contains(
            "src='https://images.example/2.png'",
            context.RenderField(field, CreateFeature("2")));
        Assert.Contains("{header:X-HOST}", field.ImageExpression);
    }

    [Fact]
    public async Task HotlinkContexts_IsolateHeadersForSharedFieldDefinition()
    {
        var field = new TableFieldHotlink
        {
            ColumnName = "Link",
            HotlinkUrl = "{header:X-SCHEME}://{header:X-HOST}/[ID]",
            HotlinkName = "[NAME]",
            ImageExpression = "{header:X-SCHEME}://{header:X-HOST}/icon.png",
            Target = BrowserWindowTarget._blank
        };
        var firstContext = await CreateContextAsync(
            field,
            new NameValueCollection
            {
                ["X-SCHEME"] = "https",
                ["X-HOST"] = "first.example"
            });
        var secondContext = await CreateContextAsync(
            field,
            new NameValueCollection
            {
                ["X-SCHEME"] = "http",
                ["X-HOST"] = "second.example"
            });
        var feature = CreateFeature("7");

        var first = firstContext.RenderField(field, feature);
        var second = secondContext.RenderField(field, feature);

        Assert.Contains("href='https://first.example/7'", first);
        Assert.Contains("src='https://first.example/icon.png'", first);
        Assert.Contains("href='http://second.example/7'", second);
        Assert.Contains("src='http://second.example/icon.png'", second);
        Assert.Contains(">Main</a>", first);
        Assert.Contains("{header:X-HOST}", field.HotlinkUrl);
        Assert.Contains("{header:X-HOST}", field.ImageExpression);
        Assert.False(field.PreparedImageExpressionHasParameters(
            firstContext.GetFieldContext(field)));
        Assert.Equal(
            "https://first.example/icon.png",
            field.PreparedImageExpression(firstContext.GetFieldContext(field)));
    }

    [Fact]
    public async Task HotlinkContext_DoesNotTreatHeaderBracketsAsFeatureParameters()
    {
        var field = new TableFieldHotlink
        {
            ImageExpression = "https://{header:X-HOST}/icon.png"
        };
        var context = await CreateContextAsync(
            field,
            new NameValueCollection
            {
                ["X-HOST"] = "[2001:db8::1]"
            });
        var fieldContext = context.GetFieldContext(field);

        Assert.False(field.PreparedImageExpressionHasParameters(fieldContext));
        Assert.Equal(
            "https://[2001:db8::1]/icon.png",
            field.PreparedImageExpression(fieldContext));
    }

    [Fact]
    public async Task FieldWithoutInitializedContext_UsesRequestHeadersFallback()
    {
        var initializedField = new TableFieldData { FieldName = "ID" };
        var additionalField = new TableFieldImage
        {
            ImageExpression = "https://{header:X-HOST}/[ID].png"
        };
        var context = await CreateContextAsync(
            initializedField,
            new NameValueCollection
            {
                ["X-HOST"] = "fallback.example"
            });

        Assert.Equal("3", context.RenderField(initializedField, CreateFeature("3")));
        Assert.Contains(
            "src='https://fallback.example/3.png'",
            context.RenderField(additionalField, CreateFeature("3")));
    }

    [Theory]
    [InlineData(20, 10, "<img style='width:20px;;height:10px;' src='https://img/1.png' />")]
    [InlineData(20, 0, "<img style='width:20px;' src='https://img/1.png' />")]
    [InlineData(0, 10, "<img style=';height:10px;' src='https://img/1.png' />")]
    [InlineData(0, 0, "<img style='' src='https://img/1.png' />")]
    public async Task ImageField_RendersExactHtml(int width, int height, string expected)
    {
        var field = new TableFieldImage
        {
            ImageExpression = "https://img/[ID].png",
            ImageWidth = width,
            ImageHeight = height
        };
        var context = await CreateContextAsync(field, new NameValueCollection());

        Assert.Equal(expected, context.RenderField(field, CreateFeature("1")));
    }

    [Fact]
    public async Task ImageField_RendersEmpty_WhenExpressionResolvesToWhitespace()
    {
        var field = new TableFieldImage { ImageExpression = "[EMPTY]" };
        var context = await CreateContextAsync(field, new NameValueCollection());

        Assert.Equal(String.Empty, context.RenderField(field, CreateFeature("1")));
    }

    [Fact]
    public async Task HotlinkField_RendersExactHtml_WithImage()
    {
        var field = new TableFieldHotlink
        {
            HotlinkUrl = "https://host/[ID]",
            HotlinkName = "[NAME]",
            ImageExpression = "https://host/icon.png",
            ImageWidth = 16,
            ImageHeight = 12,
            Target = BrowserWindowTarget._blank
        };
        var context = await CreateContextAsync(field, new NameValueCollection());

        Assert.Equal(
            "<a target='_blank' href='https://host/5'>" +
            "<img style='width:16px;;height:12px;margin-right:8px;vertical-align:sub' src='https://host/icon.png' />" +
            "Main</a>",
            context.RenderField(field, CreateFeature("5")));
    }

    [Fact]
    public async Task HotlinkField_RendersExactHtml_WithoutImage_AndColumnNameFallback()
    {
        var field = new TableFieldHotlink
        {
            ColumnName = "Open",
            HotlinkUrl = "https://host/[ID]",
            Target = BrowserWindowTarget._self
        };
        var context = await CreateContextAsync(field, new NameValueCollection());

        Assert.Equal(
            "<a target='_self' href='https://host/5'>Open</a>",
            context.RenderField(field, CreateFeature("5")));
    }

    [Fact]
    public async Task HotlinkField_RendersEmpty_WhenUrlIsEmpty()
    {
        var field = new TableFieldHotlink { HotlinkUrl = "[EMPTY]", HotlinkName = "x" };
        var context = await CreateContextAsync(field, new NameValueCollection());

        Assert.Equal(String.Empty, context.RenderField(field, CreateFeature("5")));
    }

    [Theory]
    [InlineData(ColumnType.EmailAddress, "a@b.at", "<a href='mailto:a@b.at'>a@b.at</a>")]
    [InlineData(ColumnType.PhoneNumber, "+43 1", "<a href='tel:+43 1'>+43 1</a>")]
    public async Task DataField_RendersLinks(ColumnType columnType, string value, string expected)
    {
        var field = new TableFieldData { FieldName = "ID", ColType = columnType };
        var context = await CreateContextAsync(field, new NameValueCollection());

        Assert.Equal(expected, context.RenderField(field, CreateFeature(value)));
    }

    [Fact]
    public async Task DataField_AppliesSimpleDomains_AndEscapesHtml()
    {
        var field = new TableFieldData { FieldName = "ID", SimpleDomains = "1,2=<b>one</b>,two" };
        var rawField = new TableFieldData { FieldName = "ID", SimpleDomains = "1=<b>one</b>", RawHtml = true };
        var context = await CreateContextAsync(field, new NameValueCollection());
        var rawContext = await CreateContextAsync(rawField, new NameValueCollection());

        Assert.Equal("&lt;b&gt;one&lt;/b&gt;", context.RenderField(field, CreateFeature("1")));
        Assert.Equal("two", context.RenderField(field, CreateFeature("2")));
        Assert.Equal("3", context.RenderField(field, CreateFeature("3")));
        Assert.Equal("<b>one</b>", rawContext.RenderField(rawField, CreateFeature("1")));
    }

    private static Task<QueryFieldRenderingContext> CreateContextAsync(
        TableField field,
        NameValueCollection headers)
        => new QueryDTO
        {
            Fields = [field]
        }.InitFieldRendering(httpService: null!, headers);

    private static Feature CreateFeature(string id)
        => new(
        [
            new WebMapping.Core.Attribute("ID", id),
            new WebMapping.Core.Attribute("NAME", "Main")
        ]);
}
