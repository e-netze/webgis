# WebGIS.UI.Tests

Browser-based characterization tests (golden master) for the WebGIS viewer JavaScript,
currently for the UI builder (`wwwroot/scripts/api/ui/webgis.ui.builder.core.js` and
`webgis.ui.builder[...]` plugins).

Purpose: lock the current behaviour **before** refactoring, so a refactoring can prove
that the generated DOM, attached events/data and calls to the map are unchanged.

Local only (not part of a CI pipeline), Chromium only.

## Setup (once)

```powershell
dotnet build src\NetCore\Tests\WebGIS.UI.Tests
powershell -ExecutionPolicy Bypass -File src\NetCore\Tests\WebGIS.UI.Tests\bin\Debug\net10.0\playwright.ps1 install chromium
```

Re-run the install after updating the `Microsoft.Playwright.MSTest.v4` package.

The page needs the libman client libraries of the Api project (`wwwroot/lib/dompurify`).
If missing, run `libman restore` in `src\NetCore\Web\Api`.

## Run

```powershell
dotnet test src\NetCore\Tests\WebGIS.UI.Tests
dotnet test src\NetCore\Tests\WebGIS.UI.Tests --filter "FullyQualifiedName~GeneralElementsTests"
```

## How it works

- No web server: every request to `https://webgis.test/**` is answered from
  `src/NetCore/Web/Api/wwwroot` via Playwright request routing. Requests to other hosts fail the test.
- The harness page is generated in `Infrastructure/UIBuilderPageTest.cs`:
  jQuery, DOMPurify, then the **source** files of `api.min.js` and `api-ui.min.js`
  in the order of `bundleconfig.json` (read at runtime), then `Harness/uibuilder-harness.js`.
  `BundleSmokeTests` loads the minified bundles instead (they may be stale, basic check only).
- `webgis.init()` is **not** called (no server). `map` is a recording fake (`uiHarness.map`, a Proxy):
  each call is logged with its path and described arguments, e.g. `map.events.fire`.
  Return values can be configured per path (`h.returns(...)`, `h.returnsFn(...)`), defaults are in
  `applyDefaults()` of the harness. `webgis.tools.onButtonClick` is replaced by a recording spy.
- Elements are serialized from the real C# UI classes (`E.Standard.WebMapping.Core.Api`) with
  `UIJson`, which mirrors the API's serialization (System.Text.Json + runtime-type converter).
  Use `BuildJsonAsync` for hand-written JSON.
- Determinism: fake clock paused at a fixed time (`FlushTimersAsync` advances it, called after each
  build), seeded `Math.random`, fixed viewport/locale/timezone, GUIDs replaced by `{guid}`.
  Page errors and `console.error` fail the test.

## Snapshots

`Snapshots/<type>/<variant>.snap.json` (committed) contains for `#host`, `#webgis-container` and
other body children:

- tag, sorted attributes, `value`/`checked`/`selected` properties, text
- jQuery data (`$.data(el)`), jQuery events (`$._data(el,'events')`, counted),
  native listeners (`addEventListener`, counted), `on*` handler properties, element expandos
- all recorded calls to the fake map and spies

| Situation | Result |
|---|---|
| snapshot missing | written, test **Inconclusive** - review the file, run again |
| snapshot differs | `<variant>.received.snap.json` written next to it, test **fails** |
| `WEBGIS_UPDATE_SNAPSHOTS=1` | all snapshots overwritten, tests Inconclusive |

During a refactoring, snapshots must not change and Inconclusive counts as failure.

## Writing a test

```csharp
[TestClass]
public class InputElementsTests : UIBuilderPageTest
{
    [TestMethod]
    public async Task InputText_PrintTextElement()
    {
        await BuildAsync(
            new UIInputText { id = "title", css = "webgis-print-textelement" },
            setup: "h => h.returns('map.getPersistentToolParameter', 'stored text')");

        await AssertSnapshotAsync("input-text", "print-textelement");
    }
}
```

Interaction tests: `TriggerAsync(selector, "change", value)` sets a value and triggers events via
jQuery (flushes timers afterwards); or use `Page` directly, then `FlushTimersAsync()`.
`WaitForAsync(jsPredicate)` waits for work independent of the fake clock (on-demand script loading
like flatpickr, ajax). `ClearCallsAsync()` drops the calls recorded so far.

Properties the C# class doesn't provide (e.g. `onchange`, `iscollapsable`) can be merged in with
`UIJson.SerializeWith(element, new { onchange = "..." })` + `BuildJsonAsync`. Extra ajax endpoints
(e.g. an autocomplete source) are faked with an additional `Page.RouteAsync(...)` in the test.

A test with several snapshots stops at the first missing one (Inconclusive), so new baselines may
need several runs.
