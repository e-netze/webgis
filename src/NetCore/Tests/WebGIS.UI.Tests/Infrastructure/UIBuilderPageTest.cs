using System.Net;
using System.Text;
using System.Text.Json;

using E.Standard.WebMapping.Core.Api.UI.Abstractions;

using Microsoft.Playwright;

namespace WebGIS.UI.Tests.Infrastructure;

public enum HarnessScripts
{
    /// <summary>Single source files in bundleconfig.json order (default, what gets refactored).</summary>
    Sources,
    /// <summary>The minified bundles api.min.js + api-ui.min.js (smoke test only).</summary>
    Bundles,
    /// <summary>The single bundle webgis-api.min.js, loaded without script id (smoke test only).</summary>
    SingleBundle
}

/// <summary>
/// Base class for UI builder characterization tests.
///
/// Each test gets a fresh page on https://webgis.test (served from the Api project's wwwroot via
/// request routing, no web server). The page loads jQuery, typeahead, DOMPurify and the WebGIS
/// scripts, then Harness/uibuilder-harness.js, which provides a recording fake map (uiHarness).
///
/// Determinism: fixed fake clock, seeded Math.random, fixed viewport/locale/timezone, GUIDs normalized.
/// Any page error, console error or request outside https://webgis.test fails the test.
/// </summary>
public abstract class UIBuilderPageTest : PageTest
{
    protected const string Origin = "https://webgis.test";
    private const string HarnessPagePath = "/__harness/index.html";
    private const string HarnessScriptPrefix = "/__harness/";

    private static readonly DateTimeOffset FixedNow = new(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset PausedAt = FixedNow.AddMinutes(1);

    private static readonly string[] VendorScripts =
    [
        "/scripts/jquery-3.4.1.min.js",
        "/scripts/typeahead/typeahead.jquery.min.js",
        "/lib/dompurify/purify.min.js"
    ];

    private readonly List<string> _errors = new();

    protected virtual HarnessScripts Scripts => HarnessScripts.Sources;

    protected IReadOnlyList<string> Errors => _errors;

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
        Locale = "de-DE",
        TimezoneId = "Europe/Vienna",
        ColorScheme = ColorScheme.Light,
        ReducedMotion = ReducedMotion.Reduce
    };

    [TestInitialize]
    public async Task InitializeHarnessAsync()
    {
        _errors.Clear();

        Page.PageError += (_, error) => _errors.Add($"pageerror: {error}");
        Page.Console += (_, msg) =>
        {
            // missing images/fonts referenced by css are not relevant for the builder
            if (msg.Type == "error" && !msg.Text.StartsWith("Failed to load resource", StringComparison.Ordinal))
            {
                _errors.Add($"console.error: {msg.Text}");
            }
        };

        await Page.RouteAsync("**/*", RouteAsync);

        await Page.Clock.InstallAsync(new() { TimeDate = FixedNow.UtcDateTime });
        await Page.AddInitScriptAsync(scriptPath: Path.Combine(RepoPaths.HarnessDirectory, "harness-init.js"));

        await Page.GotoAsync(Origin + HarnessPagePath, new() { WaitUntil = WaitUntilState.Load });
        // installed fake time keeps flowing in real time => pause at a fixed point for deterministic Date.now()
        await Page.Clock.PauseAtAsync(PausedAt.UtcDateTime);

        var ready = await Page.EvaluateAsync<bool>("() => window.__harnessReady === true && !!window.uiHarness");
        Assert.IsTrue(ready, "Harness page did not initialize. Errors: " + string.Join(Environment.NewLine, _errors));
        AssertNoPageErrors();
    }

    [TestCleanup]
    public void CleanupHarness()
    {
        AssertNoPageErrors();
    }

    #region Build / interact

    /// <summary>
    /// Runs $('#host').webgis_uibuilder({ map: uiHarness.map, tool, elements, title: '', closebutton: false, ...options })
    /// and flushes pending timers (webgis.delayed, setTimeout, animations).
    /// </summary>
    /// <param name="setup">optional JS executed before building, receives the harness: <c>h => h.returns('map.getActiveTool', {...})</c></param>
    /// <param name="options">additional webgis_uibuilder options (e.g. tool, append), serialized as JSON</param>
    protected Task BuildAsync(IEnumerable<IUIElement> elements, string? setup = null, object? options = null)
        => BuildJsonAsync(UIJson.Serialize(elements), setup, options);

    protected Task BuildAsync(IUIElement element, string? setup = null, object? options = null)
        => BuildAsync([element], setup, options);

    /// <summary>Build from hand-written element JSON (special cases the C# classes can't produce).</summary>
    protected async Task BuildJsonAsync(string elementsJson, string? setup = null, object? options = null)
    {
        if (!string.IsNullOrWhiteSpace(setup))
        {
            await Page.EvaluateAsync($"({setup})(window.uiHarness)");
        }

        await Page.EvaluateAsync(
            "([elements, options]) => window.uiHarness.build(JSON.parse(elements), options ? JSON.parse(options) : null)",
            new object?[] { elementsJson, options is null ? null : JsonSerializer.Serialize(options) });

        await FlushTimersAsync();
        AssertNoPageErrors();
    }

    /// <summary>Advances the fake clock so webgis.delayed/setTimeout callbacks and animations run.</summary>
    protected async Task FlushTimersAsync(int milliseconds = 2000)
    {
        await Page.Clock.RunForAsync(milliseconds);
    }

    protected Task ClearCallsAsync() => Page.EvaluateAsync("() => window.uiHarness.clearCalls()");

    /// <summary>
    /// Optionally sets the value (jQuery .val) and triggers the given event(s) on the elements matching the selector,
    /// then flushes timers. Uses jQuery, so handlers bound via jQuery and native listeners both run.
    /// </summary>
    protected async Task TriggerAsync(string selector, string events, string? value = null)
    {
        await Page.EvaluateAsync(
            "([selector, events, value]) => { const $e = $(selector); if (!$e.length) throw new Error('no element: ' + selector); if (value !== null) $e.val(value); events.split(' ').forEach(ev => $e.trigger(ev)); }",
            new object?[] { selector, events, value });
        await FlushTimersAsync();
        AssertNoPageErrors();
    }

    /// <summary>Waits for async work that doesn't depend on the fake clock (script loading, ajax).</summary>
    protected async Task WaitForAsync(string jsPredicate, int timeoutMs = 10000)
    {
        await Page.WaitForFunctionAsync(jsPredicate, null, new() { PollingInterval = 50, Timeout = timeoutMs });
        await FlushTimersAsync();
        AssertNoPageErrors();
    }

    #endregion

    #region Assertions

    /// <summary>Compares the current harness state (DOM + recorded calls) with Snapshots/&lt;type&gt;/&lt;variant&gt;.snap.json</summary>
    protected async Task AssertSnapshotAsync(string type, string variant)
    {
        AssertNoPageErrors();
        // JSON string instead of JsonElement: Playwright's deserializer would add "$id" reference markers
        var json = await Page.EvaluateAsync<string>("() => JSON.stringify(window.uiHarness.snapshot())");
        using var snapshot = JsonDocument.Parse(json);
        Snapshot.AssertMatches(type, variant, snapshot.RootElement);
    }

    protected void AssertNoPageErrors()
    {
        if (_errors.Count > 0)
        {
            var message = "Page errors:" + Environment.NewLine + string.Join(Environment.NewLine, _errors);
            _errors.Clear();
            Assert.Fail(message);
        }
    }

    #endregion

    #region Page / routing

    private async Task RouteAsync(IRoute route)
    {
        var uri = new Uri(route.Request.Url);
        if (!string.Equals(uri.GetLeftPart(UriPartial.Authority), Origin, StringComparison.OrdinalIgnoreCase))
        {
            _errors.Add($"unexpected request outside the harness: {route.Request.Method} {route.Request.Url}");
            await route.AbortAsync();
            return;
        }

        var path = Uri.UnescapeDataString(uri.AbsolutePath);

        if (path == HarnessPagePath)
        {
            await route.FulfillAsync(new() { Status = 200, ContentType = "text/html; charset=utf-8", Body = BuildHarnessHtml() });
            return;
        }

        var file = path.StartsWith(HarnessScriptPrefix, StringComparison.Ordinal)
            ? ResolveFile(RepoPaths.HarnessDirectory, path[HarnessScriptPrefix.Length..])
            : ResolveFile(RepoPaths.WwwRoot, path.TrimStart('/'));

        if (file is null)
        {
            await route.FulfillAsync(new() { Status = (int)HttpStatusCode.NotFound, Body = "" });
            return;
        }

        await route.FulfillAsync(new() { Status = 200, Path = file, ContentType = ContentType(file) });
    }

    private static string? ResolveFile(string root, string relativePath)
    {
        var fullRoot = Path.GetFullPath(root);
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath)
            ? fullPath
            : null;
    }

    private static string ContentType(string file) => Path.GetExtension(file).ToLowerInvariant() switch
    {
        ".js" => "application/javascript; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".html" => "text/html; charset=utf-8",
        ".json" => "application/json; charset=utf-8",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".svg" => "image/svg+xml",
        ".woff" => "font/woff",
        ".woff2" => "font/woff2",
        _ => "application/octet-stream"
    };

    private string BuildHarnessHtml()
    {
        var webgisScripts = Scripts switch
        {
            HarnessScripts.Bundles => new List<string> { BundleConfig.ToWebPath(BundleConfig.ApiBundle), BundleConfig.ToWebPath(BundleConfig.ApiUiBundle) },
            HarnessScripts.SingleBundle => new List<string> { BundleConfig.ToWebPath(BundleConfig.WebGISApiBundle) },
            _ => BundleConfig.GetInputFiles(BundleConfig.ApiBundle)
                .Concat(BundleConfig.GetInputFiles(BundleConfig.ApiUiBundle))
                .ToList()
        };

        foreach (var script in VendorScripts.Concat(webgisScripts))
        {
            if (ResolveFile(RepoPaths.WwwRoot, script.TrimStart('/')) is null)
            {
                throw new FileNotFoundException(
                    $"Harness script '{script}' not found in {RepoPaths.WwwRoot}." +
                    (script.StartsWith("/lib/", StringComparison.Ordinal)
                        ? " Restore client libraries (libman restore) in the Api project."
                        : ""));
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"de\"><head><meta charset=\"utf-8\"><title>webgis uibuilder harness</title>");
        sb.AppendLine("<link rel=\"stylesheet\" href=\"/content/api/ui.css\">");

        foreach (var script in VendorScripts)
        {
            sb.AppendLine($"<script src=\"{script}\"></script>");
        }

        for (int i = 0; i < webgisScripts.Count; i++)
        {
            // webgis.js derives webgis.baseUrl from the script with this id
            // (the single bundle is loaded without id to cover the document.currentScript fallback)
            var id = i == 0 && Scripts != HarnessScripts.SingleBundle ? " id=\"webgis-api-script\"" : "";
            sb.AppendLine($"<script src=\"{webgisScripts[i]}\"{id}></script>");
        }

        sb.AppendLine($"<script src=\"{HarnessScriptPrefix}uibuilder-harness.js\"></script>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<div id=\"webgis-container\"></div>");
        sb.AppendLine("<div id=\"host\"></div>");
        sb.AppendLine("<script>window.__harnessReady = true;</script>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    #endregion
}
