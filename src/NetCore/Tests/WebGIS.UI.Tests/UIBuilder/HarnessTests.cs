using System.Text.Json;

using E.Standard.WebMapping.Core.Api.UI.Elements;

using WebGIS.UI.Tests.Infrastructure;

namespace WebGIS.UI.Tests.UIBuilder;

/// <summary>
/// Self-tests of the test infrastructure (script loading, fake map, determinism).
/// </summary>
[TestClass]
public class HarnessTests : UIBuilderPageTest
{
    [TestMethod]
    public async Task SourceScripts_LoadWithoutErrors()
    {
        var hasBuilder = await Page.EvaluateAsync<bool>(
            "() => typeof jQuery.fn.webgis_uibuilder === 'function' && typeof webgis.ui.builder['label'] === 'function'");
        Assert.IsTrue(hasBuilder);

        var baseUrl = await Page.EvaluateAsync<string>("() => webgis.baseUrl");
        Assert.AreEqual(Origin, baseUrl);
    }

    [TestMethod]
    public void UIJson_SerializesRuntimeTypeProperties()
    {
        var json = UIJson.Serialize(new UILabel { label = "x", IsTrusted = true }, new UIParagraph("p"));
        using var doc = JsonDocument.Parse(json);

        Assert.AreEqual("x", doc.RootElement[0].GetProperty("label").GetString());
        Assert.IsTrue(doc.RootElement[0].GetProperty("is_trusted").GetBoolean());
        Assert.AreEqual("p", doc.RootElement[1].GetProperty("text").GetString());
    }

    [TestMethod]
    public async Task FakeMap_RecordsCallsAndConfiguredReturns()
    {
        var result = await Page.EvaluateAsync<JsonElement>("""
            () => {
                const h = window.uiHarness;
                h.returns('map.getPersistentToolParameter', 'stored');
                const r = h.map.getPersistentToolParameter('webgis.tools.print', 'LAYOUT_TEXT_x');
                h.map.events.fire('some-event', h.map, { a: 1 });
                return { r, calls: h.calls() };
            }
            """);

        Assert.AreEqual("stored", result.GetProperty("r").GetString());
        var calls = result.GetProperty("calls");
        Assert.AreEqual("map.getPersistentToolParameter", calls[0].GetProperty("call").GetString());
        Assert.AreEqual("map.events.fire", calls[1].GetProperty("call").GetString());
        Assert.AreEqual("[fake:map]", calls[1].GetProperty("args")[1].GetString());
    }

    [TestMethod]
    public async Task Environment_IsDeterministic()
    {
        const string probe = "() => ({ now: Date.now(), random: Math.random(), lang: webgis.l10n.language })";
        var first = await Page.EvaluateAsync<JsonElement>(probe);

        await Page.ReloadAsync();
        var second = await Page.EvaluateAsync<JsonElement>(probe);

        Assert.AreEqual(PausedAt.ToUnixTimeMilliseconds(), first.GetProperty("now").GetInt64());
        Assert.AreEqual(first.GetProperty("random").GetDouble(), second.GetProperty("random").GetDouble());
        Assert.AreEqual("de", first.GetProperty("lang").GetString());
    }
}

/// <summary>
/// Smoke test against the single bundle webgis-api.min.js (api.min.js + api-ui.min.js).
/// Loaded without script id, so webgis.baseUrl must be derived via document.currentScript.
/// </summary>
[TestClass]
public class SingleBundleSmokeTests : UIBuilderPageTest
{
    protected override HarnessScripts Scripts => HarnessScripts.SingleBundle;

    [TestMethod]
    public async Task SingleBundle_LoadsAndBuildsLabel()
    {
        var baseUrl = await Page.EvaluateAsync<string>("() => webgis.baseUrl");
        Assert.AreEqual(Origin, baseUrl);

        await BuildAsync(new UILabel { label = "Hallo" });

        var text = await Page.Locator("#host .webgis-label").InnerTextAsync();
        Assert.AreEqual("Hallo", text);
    }
}

/// <summary>
/// Ensures webgis-api.min.js is (and stays) exactly api.min.js + api-ui.min.js.
/// </summary>
[TestClass]
public class SingleBundleConfigTests
{
    [TestMethod]
    public void SingleBundle_IsApiPlusApiUi_NotReminified_AndDefinedAfterBoth()
    {
        var bundle = BundleConfig.GetBundle(BundleConfig.WebGISApiBundle);

        CollectionAssert.AreEqual(
            new[] { BundleConfig.ToWebPath(BundleConfig.ApiBundle), BundleConfig.ToWebPath(BundleConfig.ApiUiBundle) },
            BundleConfig.GetInputFiles(BundleConfig.WebGISApiBundle).ToArray());

        Assert.IsFalse(bundle.GetProperty("minify").GetProperty("enabled").GetBoolean());

        var outputs = BundleConfig.GetOutputFileNames().ToList();
        var index = outputs.IndexOf(BundleConfig.WebGISApiBundle);
        Assert.IsTrue(index > outputs.IndexOf(BundleConfig.ApiBundle));
        Assert.IsTrue(index > outputs.IndexOf(BundleConfig.ApiUiBundle));
    }

    [TestMethod]
    public void SingleBundle_ContainsApiAndApiUiInOrder()
    {
        string Read(string bundle) =>
            File.ReadAllText(Path.Combine(RepoPaths.WwwRoot, BundleConfig.ToWebPath(bundle).TrimStart('/').Replace('/', Path.DirectorySeparatorChar)))
                .Trim('\uFEFF', '\r', '\n', ' ');

        var single = Path.Combine(RepoPaths.WwwRoot, "scripts", "api", "webgis-api.min.js");
        if (!File.Exists(single))
        {
            Assert.Inconclusive("webgis-api.min.js not built yet (build the Api project).");
        }

        var content = Read(BundleConfig.WebGISApiBundle);
        var api = Read(BundleConfig.ApiBundle);
        var apiUi = Read(BundleConfig.ApiUiBundle);

        var apiPos = content.IndexOf(api, StringComparison.Ordinal);
        var apiUiPos = content.IndexOf(apiUi, StringComparison.Ordinal);
        Assert.AreEqual(0, apiPos, "webgis-api.min.js must start with api.min.js");
        Assert.IsTrue(apiUiPos >= api.Length, "webgis-api.min.js must contain api-ui.min.js after api.min.js");
    }
}

/// <summary>
/// Smoke test against the minified bundles (api.min.js, api-ui.min.js).
/// The bundles are produced by the bundler and may be stale, so only basic behaviour is checked.
/// </summary>
[TestClass]
public class BundleSmokeTests : UIBuilderPageTest
{
    protected override HarnessScripts Scripts => HarnessScripts.Bundles;

    [TestMethod]
    public async Task Bundles_LoadAndBuildLabel()
    {
        await BuildAsync(new UILabel { label = "Hallo" });

        var text = await Page.Locator("#host .webgis-label").InnerTextAsync();
        Assert.AreEqual("Hallo", text);
    }
}
