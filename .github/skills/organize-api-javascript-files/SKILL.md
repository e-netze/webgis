---
name: organize-api-javascript-files
description: Conventions for where viewer/API JavaScript files live in webgis-api (wwwroot/scripts), how they are named (webgis.*, webgis.ui.builder.*, webgis.ui.plugins.*, webgis_*, leaflet webgis-<topic>.*) and how they are registered (bundleconfig.json, Portal views). Use when adding, splitting, moving or renaming a JS file of the API/viewer.
---

# Skill: Organize API JavaScript Files

All viewer JavaScript lives in the Api project (`src/NetCore/Web/Api/wwwroot/scripts/`) and is
bundled with BuildBundlerMinifier (`src/NetCore/Web/Api/bundleconfig.json`, runs on build of
`webgis-api.csproj`). Pick the file name by **what the file contains** - the prefix tells the
reader (and Visual Studio file nesting) the kind of code inside.

## 1. Choose folder and name

| Content | Folder | File name | Example |
|---------|--------|-----------|---------|
| Core API / viewer modules (`webgis.<area>` objects) | `scripts/api/` | `webgis.<area>[.<sub>].js` | `webgis.map.js`, `webgis.sketch.construct.js`, `webgis.position.debug_geolocation.js` |
| UI element builders (`webgis.ui.builder['<type>'] = {...}`) and builder-only helpers | `scripts/api/ui/` | `webgis.ui.builder.<topic>.js` | `webgis.ui.builder.inputs.js` |
| UI plugins in the **current** format (`webgis.ui.definePlugin('webgis_x', {...})`) | `scripts/api/ui/` | `webgis.ui.plugins.<topic>.js` | `webgis.ui.plugins.timefilter.js` |
| **Legacy** jQuery plugins (`$.fn.webgis_x = function (method) {...}`) | `scripts/api/ui/` | `webgis_<pluginName>.js` (one plugin, or a few closely related ones) | `webgis_hourglass.js`, `webgis_serviceControls.js` |
| Leaflet classes (`L.Xxx` + `L.xxx` factory) | `scripts/leaflet/plugins/` | `webgis-<topic>.<class_snake_case>.js` | `webgis-graphics.dim_line.js`, `webgis-servicelayer.webgis_service.js` |

Rules:

- **One class / one plugin per file.** Exceptions: a Leaflet class and its lowercase factory
  (`L.DimLine` + `L.dimLine`) stay together; a few tightly related small plugins of one topic may
  share a file.
- **Never mix kinds in one file.** A file named `webgis.ui.plugins.*` contains only
  `webgis.ui.definePlugin(...)` calls (plus tiny `webgis.ui.*` helpers that use them, e.g.
  `webgis.ui.addTaskProgress`). Builders go to `webgis.ui.builder.*`, legacy `$.fn` plugins to
  `webgis_*.js`. When a legacy plugin is converted to `definePlugin` (see the
  `create-webgis-ui-plugin` skill), move it from `webgis_<name>.js` into the matching
  `webgis.ui.plugins.<topic>.js` and delete the old file.
- **Grouping files for Visual Studio nesting:** a family `<prefix>.<topic>.js` is nested under
  `<prefix>.js`. If that file has no code, it still exists and contains only a comment explaining
  the family and listing its files, e.g. `scripts/leaflet/plugins/webgis-graphics.js`,
  `webgis-servicelayer.js`. `webgis.ui.builder.js` additionally defines the builder registry,
  `webgis.ui.plugins.js` defines `webgis.ui.definePlugin`.
- File header (first lines): what the file contains and where the counterpart lives, e.g.
  `// WebGIS UI Plugins (timefilter) / plugin names / Element builders: webgis.ui.builder.timefilter.js`.
- Encoding: JS files are **UTF-8 with BOM, CRLF**. `bundleconfig.json` and the Portal `.cshtml`
  views are **UTF-8 with BOM, LF** - don't introduce CRLF there.

## 2. Register the file

### `scripts/api/**` (viewer API)

1. `bundleconfig.json`: add it to `api.min.js` (core `webgis.*`) or `api-ui.min.js` (everything in
   `scripts/api/ui/`). Order matters:
   - `webgis.ui.plugins.js` before any `webgis.ui.plugins.<topic>.js` (needs `definePlugin`),
   - `webgis.ui.builder.js` before any `webgis.ui.builder.<topic>.js` (needs the registry),
   - plugin files before builder files (current layout), legacy `webgis_*.js` near the related ones.
2. Development script lists (unbundled files, `<environment include="Development">`) in **both**
   `src/NetCore/Web/Portal/Views/Map/Index.cshtml` and
   `src/NetCore/Web/Portal/Views/MapBuilder/Index.cshtml` - same order as in `bundleconfig.json`.
3. The UI tests (`src/NetCore/Tests/WebGIS.UI.Tests`) load the single source files in
   `bundleconfig.json` order, so a file that is not registered there is missing in the tests too.

### `scripts/leaflet/plugins/**`

- Only bundled, never loaded unbundled: `webgis.js` loads `leaflet-plugins.min.js` at runtime.
  Add the file to **both** `leaflet-plugins.min.js` and `leaflet-plugins-gesture-handling.min.js`
  in `bundleconfig.json`, base classes first (e.g. `webgis-graphics.layer_collection.js` before
  the graphics deriving from it). Update the file list in the family's grouping file comment.
- No Portal view changes needed - but changes are only visible after rebuilding the bundle.

## 3. Moving / renaming

- Use `git mv` for tracked files (keeps history), plain move for untracked ones.
- Search the repo for the old file name (`bundleconfig.json`, both Portal views, comments, docs in
  `docs/architecture/`, tests) and update every hit - except *History* rows in architecture docs,
  which are append-only.
- Pure moves: move whole IIFEs/definitions unchanged, don't refactor in the same step.

## 4. Verify

- `node --check` every new/touched JS file.
- Rebuild the bundle and check the output contains the moved code:
  touch one input file, then
  `dotnet build src\NetCore\Web\Api\webgis-api.csproj -p:BaseOutputPath="$env:TEMP\webgis-api-bundlecheck\"`
  (alternate output path avoids file locks from a running dev server) and search the generated
  `*.min.js` for a plugin/class name. Make sure nothing is bundled twice.
- For `scripts/api/ui/` changes run the UI tests (`dotnet test` in
  `src/NetCore/Tests/WebGIS.UI.Tests`) - all green, snapshots unchanged.
- Smoke test in the viewer (Development environment uses the single files, Production the bundle).
