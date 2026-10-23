# Release Notes - WebGIS V9

Compact, admin-oriented summary of the changes in WebGIS V9: upgrade notes, new features and
configuration options, and fixed issues. For the full technical details see
[changelog-v9.md](changelog-v9.md). Older versions: [release-notes-v8.md](release-notes-v8.md).

The version number encodes the release date: `Major.{Year-2000}.{CalendarWeek}{Build}`
(e.g. `9.26.4102` = 2026, week 41).

## Unreleased

### Upgrade notes

- **Breaking change:** Viewer script files were renamed or removed (e.g. `webgis_modal.js`, `webgis_contextMenu.js`, `webgis_uibuilder.js`, Leaflet `webgisgraphics.js`/`webgisservicelayer.js`). Custom pages that include single script files must include the bundles (`api.min.js`, `api-ui.min.js`) instead; custom code should open dialogs with `webgis.ui.showModal(...)` (`$.fn.webgis_modal` still works).
- **Breaking change:** Tool requests (`/rest/toolevent`) now send their parameters as JSON (`eventBody`) instead of a pipe-separated `eventString`; custom code using `webgis.tools.toolParameterString` or posting `eventString` directly must be adapted.
- **Breaking change:** The `api.config` section `tool-visfilter` was renamed to `toc-contextmenu`; move existing settings (e.g. `allow-toc-visfilter`) to the new section.
  [Docs](https://docs.webgiscloud.com/en/webgis/config/api/index.html#api-config-toc-contextmenu)
- **Breaking change:** The legacy tool "Labeling" (`webgis.tools.presentation.labeling`) and the CMS labeling nodes were removed; use the new user labeling from the table of contents instead (labels stored in old maps are ignored).
  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/presentation/index.html#toc-user-labeling)
- **Breaking change:** The viewer no longer uses twitter typeahead / jQuery UI autocomplete; custom CSS for autocomplete menus must use the new classes `webgis-autocomplete-*` instead of `tt-*` / `.twitter-typeahead`, and the API no longer ships `scripts/typeahead/typeahead.jquery.min.js` - remove it from custom pages (e.g. Smartmap).
  [Docs](https://docs.webgiscloud.com/en/webgis-dev/plugins/smartmap-simple.html)
- The CMS node search no longer supports Elasticsearch and always filters the loaded tree; the `cms.config` key `elasticsearch-endpoint` is obsolete and can be removed.
  [Docs](https://docs.webgiscloud.com/en/webgis/config/cms/index.html)

### New

- Optional git versioning for CMS trees: configure a `git` section per cms-item in `cms.config` (`remote-url`, `username`, `token`, `default-branch`, ...); each user then works in an own workspace with branches, commit, push, merge, history and diffs. [Docs](https://docs.webgiscloud.com/en/webgis/apps/cms/git.html)
- Branch deploys: with `allowBranchDeploy` on a deployment, users can deploy their workspace as a branch for testing. The WebGIS API needs `allow-branches` in `api.config`. [Docs](https://docs.webgiscloud.com/en/webgis/apps/cms/deploy.html#cms-deploy-branch)
- Portal and viewer can show maps with a selected CMS branch (also via the `?branch=` url parameter). [Docs](https://docs.webgiscloud.com/en/webgis/apps/portal/branches.html)
- Fast branch deploy: repeated branch deploys only re-read changed CMS files; a full read can be forced in the deploy dialog. [Docs](https://docs.webgiscloud.com/en/webgis/apps/cms/deploy.html#cms-deploy-fast)
- Faster CMS deploys: link warnings are now collected during the export instead of a separate scan.
- Modernized CMS look; the main deploy is now labeled "Published state (main)" instead of "Production".
- Custom pages can include the whole WebGIS API with one script and one stylesheet: `scripts/api/webgis-api.min.js` and `content/styles/webgis-api.min.css` (the previous files `api.min.js`, `api-ui.min.js`, `default.css` and `ui.css` are still supported, but deprecated). Full-page map apps can additionally include `content/api/ui.app.css` (no text selection, no pull-to-refresh). All WebGIS pages and templates now use the new files. [Docs](https://docs.webgiscloud.com/en/webgis-dev/API/API_JavaScript/index.html#api-javascript-single-bundle)
- Viewer: the toolbar and the query results table (desktop layout) can be detached into a separate window, e.g. for a second screen (Chromium browsers only); enable with `webgis.usability.detachableWindows = true` in `custom.js`.
  [Docs](https://docs.webgiscloud.com/en/webgis/apps/viewer/customjs/usability.html#customjs-detachable-windows)
  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/general/ui.html#ui-detachable-windows)
- Viewer: presentations and groups in the table of contents have a context menu (right mouse button or burger icon on hover) with Metadata, Query Builder per layer and a new properties dialog (layers, scale ranges, filters, legend; technical details for map authors).
  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/presentation/index.html#toc-context-menu)
- Viewer: users can define their own presentation (single symbol, unique values, classes) and labeling of layers of ArcGIS Server services with DynamicLayers from the table of contents ("Presentation" / "Labeling"). Enable per service with the CMS properties `allow_user_renderers` / `allow_user_labeling` and globally with `allow-toc-userrenderer` / `allow-toc-userlabeling` in the `api.config` section `toc-contextmenu`.
  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/presentation/index.html#toc-user-renderer)
  [Docs api.config](https://docs.webgiscloud.com/en/webgis/config/api/index.html#api-config-toc-contextmenu)
  [Docs CMS](https://docs.webgiscloud.com/en/webgis/apps/cms/add_ags_service.html#cms-ags-user-renderer-labeling)
- Viewer: presentations in the table of contents show badges for user presentation, labeling, filters and metadata; the context menu offers "Reset filter", "Reset presentation" and "Reset labeling".
  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/presentation/index.html#toc-badges)
- Viewer: when a query hits the result limit, the notification above the results table also shows the total number of hit features (ArcGIS Server services).
- Viewer: the Query Builder suggests values after a field is selected (autocomplete for texts, a list for numbers with few distinct values, a date picker for dates) and offers `<`, `>`, `<=`, `>=` for numbers and dates; suggestions are available for ArcGIS Server services.
  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/presentation/index.html#toc-querybuilder)
- Viewer: query and identify results can be narrowed down in the results table with the Query Builder (table tool "Restrict"); the original query including its query geometry is re-run with the additional conditions, and the restriction can be removed again. Available for services where the Query Builder is allowed.
  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/search_identify/operations/results_desktop/results_desktop.html#results-query-restriction)
- Viewer: the table of contents context menu has a new item "Table" that lists the features of a queryable layer in the results table, where they can be narrowed down with "Restrict"; the maximum number of features is set with `table-feature-limit` in the `api.config` section `toc-contextmenu` (default `1000`), the item can be hidden with `webgis.usability.allowTocTable = false`.
  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/presentation/index.html#toc-table)
  [Docs api.config](https://docs.webgiscloud.com/en/webgis/config/api/index.html#api-config-toc-contextmenu)
  [Docs custom.js](https://docs.webgiscloud.com/en/webgis/apps/viewer/customjs/usability.html#customjs-toc-table)
- Viewer: the Query Builder offers the operators `not like`, `not in`, `is null` and `is not null`, checks values before applying them, and the context menu entry is now called "Display filter (DefinitionQuery)".
  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/presentation/index.html#toc-querybuilder)
- Viewer: expand/collapse and fade animations use CSS transitions instead of jQuery animations; switch back with `webgis.usability.cssAnimations = false` in `custom.js` if problems occur.
  [Docs](https://docs.webgiscloud.com/en/webgis/apps/viewer/customjs/usability.html#customjs-css-animations)

### Fixed

- Validation messages are now shown directly below the affected input field (e.g. in edit masks) instead of at the bottom of the dialog.
- Edit themes are now filtered by their database rights in the edit theme lists (e.g. mobile insert/select tools); themes without the required right are no longer listed.
- Conditional info texts/blocks that should be hidden for a selected value are now hidden correctly.
- Removing a time filter or visibility filter control no longer breaks the other filter controls in the viewer.
- With the advanced tool behaviour, the active tool is highlighted in the toolbar again after a tool dialog was closed with "x".
