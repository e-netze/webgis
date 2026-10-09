# Release Notes - WebGIS V7

Compact, admin-oriented summary of the changes in WebGIS V7: upgrade notes, new features and
configuration options, and fixed issues. For the full technical details see
[changelog-v7.md](changelog-v7.md). Newer versions: [release-notes-v8.md](release-notes-v8.md).

The version number encodes the release date: `Major.{Year-2000}.{CalendarWeek}{Build}`
(e.g. `7.25.4805` = 2025, week 48).

## 7.25.4805

### New

- CMS export/import: lowercase folder names (Linux), clean/import must be confirmed with the CMS id.

## 7.25.4601

### New

- Editing: geometries are validated before saving (lines >= 2 points, polygons >= 3 points).

### Fixed

- `all-queries`/`all-editthemes`: scale is interpreted like in the TOC, so "Identify visible layers" works correctly near min/max scale.

## 7.25.4001

### New

- DataLinq: `GetUserClaim(claimName)` helper to read role parameters in views.
  [DataLinq Discussion #36](https://github.com/e-netze/datalinq-community/discussions/36)

### Fixed

- Print: AGS WMTS tile cache services outside the cache extent.
  [Discussion #341](https://github.com/e-netze/webgis-community/discussions/341)
- Result table: numeric sorting.
  [Discussion #348](https://github.com/e-netze/webgis-community/discussions/348)

## 7.25.3801

### New

- Quick search: select first result on Enter.
  [Discussion #328](https://github.com/e-netze/webgis-community/discussions/328)
  [Docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/usability.html#schnellsuche)
- Feature attachments: smaller, enlargeable previews; non-image attachments (e.g. PDF) downloadable.

### Fixed

- Sketch: strange behavior when moving vertices of multipart geometries.
  [Discussion #342](https://github.com/e-netze/webgis-community/discussions/342)
- Basemap tiles not removed when removing a service (also when loading a user map).
  [Discussion #346](https://github.com/e-netze/webgis-community/discussions/346)
- Result table: sorting of numeric expressions.
  [Discussion #348](https://github.com/e-netze/webgis-community/discussions/348)

## 7.25.3601

### New

- WMS: `SLD_VERSION` support.
  [Discussion #324](https://github.com/e-netze/webgis-community/discussions/324)
- Query table: note if the layer is not visible in the map (`custom.js`: `webgis.usability.showQueryLayerNotVisbleNotification`).
  [Discussion #334](https://github.com/e-netze/webgis-community/discussions/334)
- Measuring tools: section length/area.
  [Discussion #325](https://github.com/e-netze/webgis-community/discussions/325)
- MapMarkup: tool to measure polygon area/circumference.

### Fixed

- MapMarkup: GPX/Shape download.
  [Discussion #335](https://github.com/e-netze/webgis-community/discussions/335)

## 7.25.3401

### New

- Keyboard shortcuts (e.g. edit tool: Space, E, D).
  [Docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/usability.html#tastatur-shortcuts)
- Tool priority (sort order of tools in containers).
  [Discussion #279](https://github.com/e-netze/webgis-community/discussions/279)
  [Docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/usability.html#toolbox)
- Icons for 1:n hyperlinks.
  [Discussion #299](https://github.com/e-netze/webgis-community/discussions/299)
- Edit mask: `select2` for combo boxes with many items.

### Fixed

- `<strong>` tag removed from results.
  [Discussion #244](https://github.com/e-netze/webgis-community/discussions/244)
- Several MapMarkup bugs.
  [Discussion #319](https://github.com/e-netze/webgis-community/discussions/319)
- CMS upload: warnings could not be solved automatically.

## 7.25.3202

### Fixed

- Edit mask: autocomplete with database queries.
  [Discussion #316](https://github.com/e-netze/webgis-community/discussions/316)
- MapMarkup: texts/labels lost when changing the tool.
  [Discussion #319](https://github.com/e-netze/webgis-community/discussions/319)

## 7.25.2807

### Fixed

- Esri dates before 1.1.1970.
  [Discussion #313](https://github.com/e-netze/webgis-community/discussions/313)

## 7.25.2803

### Fixed

- Query via URL parameters: features are selected by default; use `&mode=noselect` to avoid this.
  [Discussion #313](https://github.com/e-netze/webgis-community/discussions/313)

## 7.25.2801

### New

- AGS results with dates: NULL shown empty, no time part for date-only values; format/culture configurable in `api.config`.
  [Docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#werkzeug-identify)
- Editing: extended Markdown links in info field blocks.
  [Docs](https://docs.webgiscloud.com/de/webgis/apps/cms/editing/fields.html)
- Header authentication: `extended-role-parameters-from-headers-prefix` and `extended-role-parameters-from-headers`.
  [Docs](https://docs.webgiscloud.com/de/webgis/config/authentication/header-auth.html)
- Snapping tolerance configurable (`custom.js`: `webgis.usability.defaultSnapPixelTolerance`) and adjustable by the user.
- Save/share map: warning if not all query result tabs can be restored.

### Fixed

- AGS legend not shown for non-string JSON values.
- Identify: all identify tools available with the default identify tool.
- Identify: selection remove button showed markers although "Show markers" was off.
- Chainage tool only shown if the map has valid chainage themes.
  [Discussion #291](https://github.com/e-netze/webgis-community/discussions/291)

## 7.25.2501

### New

- Identify: hover highlight only for features below a vertex limit (`api.config`: `max-vertices-for-hover-highlighting`).
  [Discussion #293](https://github.com/e-netze/webgis-community/discussions/293)
  [Docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#werkzeug-identify)

### Fixed

- Print: multi-page output stored images in the app pool user's default folder.
  [Discussion #305](https://github.com/e-netze/webgis-community/discussions/305)
- Print: marker numbers were always 1.

## 7.25.2403

### Upgrade notes

- If a database is used as KeyValue cache: log in to the API as admin and run setup (cache table name now depends on the crypto keys).

### New

- Editing AutoValues `guid_v7` and `guid_v7_sql`; feature transfer allows empty mass attribution fields.
  [Docs](https://docs.webgiscloud.com/de/webgis/apps/cms/editing/fields_autovalues.html)

### Fixed

- Image georeferencing: PNG transparency.

## 7.25.2302

### New

- Tool properties in `custom.js`.
  [Discussion #279](https://github.com/e-netze/webgis-community/discussions/279)
  [Docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/usability.html#toolbox)
- Chainage calculation via API service.
  [Discussion #261](https://github.com/e-netze/webgis-community/discussions/261)
- Identify: visible query themes on top of the combo box (alpha, `custom.js`: `webgis.usability.listVisibleQueriesInQueryCombo`).
  [Discussion #278](https://github.com/e-netze/webgis-community/discussions/278)
- 1:n links with target `dialog` open in a dialog.
- AGS feature attachments in query results (must be allowed and authorized in the CMS).
  [Discussion #272](https://github.com/e-netze/webgis-community/discussions/272)
- `api.config`: HttpClient default timeout.
  [Docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#httpclient)
- Result table usability and user preferences (show markers, select new results).
- Metadata: improved buttons, service metadata in TOC and copyright dialog, presentation type `Info`.

### Fixed

- Editing: features could not be saved if calculation CRS differs from map CRS.
- Metadata button sometimes missing for services with dynamic TOC.

## 7.25.2001

### New

- Print: improved text block-out (labels, coordinates).
  [Discussion #277](https://github.com/e-netze/webgis-community/discussions/277)
- Bookmarks: pan to center and scale instead of zoom to extent.
  [Discussion #276](https://github.com/e-netze/webgis-community/discussions/276)

## 7.25.1904

### New

- File-based caching of DataLinq view compilations.

## 7.25.1902

### New

- Deployment zip files and Docker images are built with NUKE.

### Fixed

- AGS FeatureService editing: transferring lines/polygons failed due to `STLength()`/`STArea()` fields.
- Editing autocomplete.
- Mass attribution: shape field name could not be determined.

## 7.25.1503

### New

- Multiple overlay basemaps.
  [Discussion #264](https://github.com/e-netze/webgis-community/discussions/264)
- CMS: opacity factor to keep services always transparent.
  [Discussion #268](https://github.com/e-netze/webgis-community/discussions/268)

## 7.25.1502

### New

- Editing: field type "Locked" (read-only in the mask, but written to the database).

### Fixed

- Editing: nullable `esriFieldTypeDate` fields.
  [Discussion #254](https://github.com/e-netze/webgis-community/discussions/254)
- CMS: AGS services listed without credentials if a folder has restricted access.
- Reverse proxy: hard-coded URLs in portal pages and MapBuilder.

## 7.25.1402

### Fixed

- `webgis.defaults` was removed by the JS minifier.

## 7.25.1401

### New

- TOC containers ordered by service order (`orderPresentationTocContainsByServiceOrder`).
  [Docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/usability.html#inhaltsverzeichnis)
- `custom.js`: defaults.
  [Docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/defaults.html)
- CMS items sorted alphabetically.
  [Discussion #252](https://github.com/e-netze/webgis-community/discussions/252)
- Editing: NULL allowed for nullable number fields.
  [Discussion #254](https://github.com/e-netze/webgis-community/discussions/254)

### Fixed

- AGS image services with authentication not displayed.
- Viewer URL parameter `srs` caused a NullReferenceException.
  [Discussion #250](https://github.com/e-netze/webgis-community/discussions/250)
- Group layer order in TOC differed from AGS services.
  [Discussion #251](https://github.com/e-netze/webgis-community/discussions/251)
