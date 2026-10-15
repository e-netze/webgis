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

### Fixed

- Validation messages are now shown directly below the affected input field (e.g. in edit masks) instead of at the bottom of the dialog.
- Edit themes are now filtered by their database rights in the edit theme lists (e.g. mobile insert/select tools); themes without the required right are no longer listed.
- Conditional info texts/blocks that should be hidden for a selected value are now hidden correctly.
- Removing a time filter or visibility filter control no longer breaks the other filter controls in the viewer.
- With the advanced tool behaviour, the active tool is highlighted in the toolbar again after a tool dialog was closed with "x".
