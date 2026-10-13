using E.Standard.WebMapping.Core.Api.UI.Elements;

using WebGIS.UI.Tests.Infrastructure;

namespace WebGIS.UI.Tests.UIBuilder;

/// <summary>
/// Characterization tests for the builders already moved to webgis.ui.plugins.general.js
/// (label, paragraph, title). They prove the harness and serve as template for further packages.
/// </summary>
[TestClass]
public class GeneralElementsTests : UIBuilderPageTest
{
    [TestMethod]
    public async Task Label_Default()
    {
        await BuildAsync(new UILabel { label = "Hallo Welt" });
        await AssertSnapshotAsync("label", "default");
    }

    [TestMethod]
    public async Task Label_WithIdAndCss()
    {
        await BuildAsync(new UILabel { id = "my-label", css = "custom-css another", label = "Text" });
        await AssertSnapshotAsync("label", "id-css");
    }

    [TestMethod]
    public async Task Label_MultipleParagraphs()
    {
        await BuildAsync(new UILabel { label = "Zeile 1|Zeile 2|Zeile 3" });
        await AssertSnapshotAsync("label", "multiple-paragraphs");
    }

    [TestMethod]
    public async Task Label_Markdown()
    {
        await BuildAsync(new UILabel { label = "md:**fett** und _kursiv_" });
        await AssertSnapshotAsync("label", "markdown");
    }

    [TestMethod]
    public async Task Label_UntrustedHtml()
    {
        await BuildAsync(new UILabel { label = "<b>fett</b><script>alert(1)</script>" });
        await AssertSnapshotAsync("label", "untrusted-html");
    }

    [TestMethod]
    public async Task Label_TrustedHtml()
    {
        await BuildAsync(new UILabel { label = "<b>fett</b>", IsTrusted = true });
        await AssertSnapshotAsync("label", "trusted-html");
    }

    [TestMethod]
    public async Task Paragraph_Default()
    {
        await BuildAsync(new UIParagraph("Ein Absatz"));
        await AssertSnapshotAsync("paragraph", "default");
    }

    [TestMethod]
    public async Task Paragraph_Markdown()
    {
        await BuildAsync(new UIParagraph("md:Ein **Absatz**"));
        await AssertSnapshotAsync("paragraph", "markdown");
    }

    [TestMethod]
    public async Task Title_Default()
    {
        await BuildAsync(new UITitle { label = "Überschrift" });
        await AssertSnapshotAsync("title", "default");
    }

    [TestMethod]
    public async Task MultipleElements_KeepOrder()
    {
        await BuildAsync(
        [
            new UITitle { label = "Titel" },
            new UILabel { label = "Label" },
            new UIParagraph("Absatz")
        ]);
        await AssertSnapshotAsync("general", "multiple-elements");
    }
}
