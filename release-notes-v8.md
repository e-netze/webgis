# Release Notes - WebGIS V8

Compact, admin-oriented summary of the changes in WebGIS V8: upgrade notes, new features and
configuration options, and fixed issues. For the full technical details see
[changelog-v8.md](changelog-v8.md). Older versions: [release-notes-v7.md](release-notes-v7.md).

The version number encodes the release date: `Major.{Year-2000}.{CalendarWeek}{Build}`
(e.g. `8.26.4102` = 2026, week 41).

## Unreleased

### Upgrade notes
### New
### Fixed

## 8.26.4102

### New

- New Editing AutoValues (geometry, UTC timestamps, edit context), optionally with a target spatial reference (e.g. `shape_area:31256`).
- Structured expression language for AutoValues (`=...`) and table columns; existing `[FIELD]` templates and `$eval` expressions remain compatible.
  [Docs](https://docs.webgiscloud.com/de/webgis/annex/expressions.html)
- ArcGIS Server: workaround for incomplete spatial query results (AGS limits by bounding box). Opt-in per service: set `QueryStrategy = BoundingBoxProblem` in the CMS; tuning via new `ags-spatial-query-*` keys in `api.config` (`tool-identify`).
  [Docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#werkzeug-identify)
- Query results: paging for large result tables, item limit for result lists, a notice when the result limit is reached, a "..." row menu, and CSV export of all results. Configurable in `custom.js` (`webgis.usability.queryResultsTable.pageSize`/`pagingThreshold`, `queryResultsList.maxItems`).
  [Docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/usability.html#ergebnisliste)
  [Discussion #451](https://github.com/e-netze/webgis-community/discussions/451)
- CMS UI is now available in German and English.
- API localization texts can be overridden with Markdown files in `etc/api/l10n/{language}`.
  [Docs](https://docs.webgiscloud.com/de/webgis/extended_config/tool_texts.html)
- CMS DeployService: the `services` allow-list now also limits the warning check before a deploy.
- Logging: multiple backends at once via `Api:logging-type` (comma-separated, e.g. `files,microsoft`); new database backends `sqlserver`, `postgres`, `oracle`, `sqlite` (tables are created/migrated automatically, `logging-*-connectionstring` keys); new `logging-username-mode` (`plaintext`/`hash`/`none`); OpenTelemetry (OTLP) export via `OTEL_EXPORTER_OTLP_ENDPOINT`; optional Serilog; logging configurable via `_config/logging.json` and `_config/logging.env`. Print entries in the performance log now contain map name, center and scale.
  [Docs](docs/logging.md)
  [Issue #461](https://github.com/e-netze/webgis-community/issues/461)
- Burger menu: custom items (e.g. Login/Logout) can be added via `custom.js` (`webgis.custom.appMenuItems.add(...)`).
  [Issue #505](https://github.com/e-netze/webgis-community/issues/505)
  [Docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/appmenuitems.html)
- CMS Playground: test expressions, AutoValues, edit forms and regular expressions without publishing.
  [Docs](https://docs.webgiscloud.com/de/webgis/annex/expressions.html)

### Fixed

- Group metadata buttons are only shown if `webgis.usability.show_metadata_i_button_toc` is `true`.
  [Issue #506](https://github.com/e-netze/webgis-community/issues/506)
- Logging: username was always empty with `logging-type: microsoft`; `server` was logged as `service` in warnings/exceptions.
  [Issue #461](https://github.com/e-netze/webgis-community/issues/461)
- Edit mask: pressing `n` in a select2 combo opened the snapping dialog.
  [Issue #515](https://github.com/e-netze/webgis-community/issues/515)
- Company `default.css` is now loaded in apps.
  [Issue #508](https://github.com/e-netze/webgis-community/issues/508)
- Detail search: dependent autocomplete values are reset correctly.
  [Issue #507](https://github.com/e-netze/webgis-community/issues/507)

## 8.26.3701

### New

- Sketch info (measuring, snapping, construction) is shown as an overlay above the coordinate display, also for MapMarkup and editing. New user preference "Sketch-Info Anzeige"; admin default via `webgis.defaults["user.preferences.sketch-info-display-mode"]` (`default`/`hidden`/`minimal`).

### Fixed

- `site.overlay.css` was not loaded in portal pages behind a reverse proxy.

## 8.26.3402

### New

- CMS DeployService: optional `services` allow-list per deployment to restrict which services are exported.

### Fixed

- Tile services: non-standard ports were dropped from tile URLs.
- Coordinates tool: CSV export showed "Rechtswert" twice instead of "Rechtswert"/"Hochwert".
  [Issue #499](https://github.com/e-netze/webgis-community/issues/499)

## 8.26.3202

### Fixed

- Styling: image and background-size corrections.
  [Issue #498](https://github.com/e-netze/webgis-community/issues/498)

## 8.26.3201

### Upgrade notes

- **Breaking change:** new CSS brand variables may change existing custom viewer stylings - check your custom CSS after the upgrade.
  [Issue #473](https://github.com/e-netze/webgis-community/issues/473)
  [Docs](https://docs.webgiscloud.com/en/webgis/config/css-styling/index.html)

### New

- CSS custom properties (variables) for WebGIS branding.
  [Docs](https://docs.webgiscloud.com/en/webgis/config/css-styling/index.html)
- `custom.js`: `expandBasemapsOnAddServices` controls whether the basemap group stays expanded when services are added.
  [Docs](https://docs.webgiscloud.com/en/webgis/apps/viewer/customjs/usability.html#hintergrundkarten-basemaps)
- CMS editing commit actions: new `Success Message` property (toast or `dialog:`), supports `[FIELDNAME]` placeholders.
- `api.config`: new `datalinq:ImageRequestWhiteList` setting.
  [Docs](https://docs.webgiscloud.com/en/webgis/config/api/index.html#datalinq)

### Fixed

- MapMarkup: drawing a rectangle with the distance/direction tool.
  [Issue #497](https://github.com/e-netze/webgis-community/issues/497)

## 8.26.3101

### New

- DataLinq update 8.26.3101.
  [DataLinq Issue #49](https://github.com/e-netze/datalinq-community/issues/49)
  [DataLinq Issue #47](https://github.com/e-netze/datalinq-community/issues/47)
- WebGIS help: admin pages in English (switchable to German), user manual in the user's language.

## 8.26.2804

### New

- Esri MapService query results: dates respect the time zone.
  [Issue #484](https://github.com/e-netze/webgis-community/issues/484)
- Service/layer metadata links in the Copyright & Info section (`custom.js`: `show_presentation_metadata_in_copyright`).
- `custom.js`: new options `show_metadata_i_button_toc` and `show_link_button_in_toc`.
- Metadata: popup dialog size configurable in the CMS.
  [Issue #459](https://github.com/e-netze/webgis-community/issues/459)

### Fixed

- Tiling service bugs and improvements.
  [Issue #480](https://github.com/e-netze/webgis-community/issues/480)

## 8.26.2401

### New

- Quick search: `minLength` and `debounceDelay` configurable.
  [Issue #469](https://github.com/e-netze/webgis-community/issues/469)
- Editing: before/after commit actions (HTTP GET/POST on insert/update/delete).
- SOLR search: placeholders `{{roles}}` and `{{namespace-roles}}` (experimental, not supported).
  [Issue #481](https://github.com/e-netze/webgis-community/issues/481)

### Fixed

- Coordinates tool: remove all vertices.
  [Issue #471](https://github.com/e-netze/webgis-community/issues/471)
- Measuring tools: sketch values not updated after changing the tool.
  [Issue #465](https://github.com/e-netze/webgis-community/issues/465)
- Identify: line/polygon selection in desktop/professional layout.
  [Discussion #399](https://github.com/e-netze/webgis-community/discussions/399)
- Editing: save-and-continue-sketch in desktop/professional layout.

## 8.26.2201

### New

- Map series print: "Create one per feature" method with preview.
  [Issue #464](https://github.com/e-netze/webgis-community/issues/464)
- Endpoint security (e.g. `cache/clear`): new `security` section in `api.config` and `portal.config`.
  [Docs api.config](https://docs.webgiscloud.com/de/webgis/config/api/index.html#security)
  [Docs portal.config](https://docs.webgiscloud.com/de/webgis/config/portal/index.html#abschnitt-security)

### Fixed

- Profile tool: print PDF could not be downloaded.
  [Issue #479](https://github.com/e-netze/webgis-community/issues/479)

## 8.26.1801

### New

- TileCache rendering mode `ScaleDependentLayers` (printing cached map services).
  [Issue #458](https://github.com/e-netze/webgis-community/issues/458)
- Map series print: max. intersection iterations configurable in `api.config`.
  [Issue #464](https://github.com/e-netze/webgis-community/issues/464)

## 8.26.1702

### New

- GeoCodes (UTMRef, ...) in the coordinates tool and quick search.
  [Discussion #449](https://github.com/e-netze/webgis-community/discussions/449)
  [Docs](https://docs.webgiscloud.com/de/webgis/annex/geocodes.html)
  [Docs api.config](https://docs.webgiscloud.com/de/webgis/config/api/index.html#abschnitt-proj4-database-geocodes)
- Basic authentication for SOLR search services.
  [Issue #456](https://github.com/e-netze/webgis-community/issues/456)
- DataLinq update 8.26.1502.

### Fixed

- API crashed on start if DataLinq was not configured in `api.config`.
- Print: error `Service with url #service not found`.
  [Issue #453](https://github.com/e-netze/webgis-community/issues/453)
- MapMarkup: snapping did not work before zoom/pan.
  [Issue #455](https://github.com/e-netze/webgis-community/issues/455)
- Map series print: save/load failed for single-page series.
- AppBuilder: side-by-side app could not be opened.
  [Issue #457](https://github.com/e-netze/webgis-community/issues/457)
- Mass attribution tool: tooltip added.
  [Issue #446](https://github.com/e-netze/webgis-community/issues/446)
- GPX upload: invalid elevation values are ignored.
  [Issue #430](https://github.com/e-netze/webgis-community/issues/430)
- UI: wrapping button groups.
  [Issue #454](https://github.com/e-netze/webgis-community/issues/454)

## 8.26.1402

### New

- Result table: new sort algorithm `number_de`.
  [Discussion #440](https://github.com/e-netze/webgis-community/discussions/440)
- WMTS: support for more exotic services.
  [Discussion #447](https://github.com/e-netze/webgis-community/discussions/447)
- DataLinq: 1:n link parameters can be posted to an internal DataLinq instance (experimental, `api.config`: `use-cache-token-for-one-2-n-links`).
  [Docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#datalinq)
- Tablet layout (`w1024.html`): usability improvements.
- DataLinq update 8.26.1402.

### Fixed

- Print: wrong circumference for measure polygons.
  [Issue #452](https://github.com/e-netze/webgis-community/issues/452)

## 8.26.1301

### New

- Logging: user information messages are only written to `webgis-exceptions.log` with log level `Information`.

### Fixed

- `TokenRequiredError`.
  [Issue #442](https://github.com/e-netze/webgis-community/issues/442)
- Query results: 1:n link type `dialog` also opened a new browser tab.
  [Issue #443](https://github.com/e-netze/webgis-community/issues/443)

## 8.26.1203

### New

- Time filter UI: switch between point in time and time span.
- DataLinq update 8.26.1201.

### Fixed

- Linux/container images: localization (default culture).
  [Issue #422](https://github.com/e-netze/webgis-community/issues/422)
- Time filter was not applied to print.
  [Issue #438](https://github.com/e-netze/webgis-community/issues/438)
- Query results: "layer not visible" notification shown only once.
  [Issue #439](https://github.com/e-netze/webgis-community/issues/439)
- Map series print: German messages.
  [Issue #436](https://github.com/e-netze/webgis-community/issues/436)
- Typos.
  [Issue #441](https://github.com/e-netze/webgis-community/issues/441)

## 8.26.1201

### New

- `api.config`: antiforgery can be disabled (not recommended); X-Forwarded middleware can be added explicitly.
  [Docs security](https://docs.webgiscloud.com/de/webgis/config/api/index.html#security)
  [Docs middleware](https://docs.webgiscloud.com/de/webgis/config/api/index.html#middleware)

## 8.26.1101

### New

- `appsettings.json`: `DefaultCulture` setting and `/instance/_culture` endpoints.
  [Issue #422](https://github.com/e-netze/webgis-community/issues/422)
- MapMarkup: improved appearance of dimension lines/polygons in PDFs.
  [Issue #434](https://github.com/e-netze/webgis-community/issues/434)
- Better error message for invalid GPX/GeoJSON uploads.
  [Issue #430](https://github.com/e-netze/webgis-community/issues/430)
- CMS result table links: new type `datalinq_pdf_report`.

### Fixed

- Language could not be set by the user in containers.
  [Issue #421](https://github.com/e-netze/webgis-community/issues/421)

## 8.26.1001

### Upgrade notes

- **Breaking change:** WebGIS API and WebGIS CMS must both be updated to >= 8.26.1001 in one step, otherwise CMS upload fails (3DES replaced by AES).
  [Issue #423](https://github.com/e-netze/webgis-community/issues/423)

### New

- `custom.js`: tool visibility via `webgis.usability.toolProperties[...]`.
  [Discussion #408](https://github.com/e-netze/webgis-community/discussions/408)
- `api.config`: tool setting `allow-anoymous-access`.
  [Docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#werkzeug-konfiguration)
- `api.config`: MapMarkup setting `save-name-maxlength`.
  [Discussion #204](https://github.com/e-netze/webgis-community/discussions/204)
  [Docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#werkzeug-mapmarkup)

### Fixed

- Security: 3DES replaced by AES.
  [Issue #423](https://github.com/e-netze/webgis-community/issues/423)
- DataLinq: environment variable (test/dev/prod) was not applied to connection strings.
  [DataLinq Issue #41](https://github.com/e-netze/datalinq-community/issues/41)
- Remove-query-result button did not work for quick search results.
  [Issue #426](https://github.com/e-netze/webgis-community/issues/426)

## 8.26.901

### New

- Health checks: `/health` and `/alive` endpoints.
  [Issue #414](https://github.com/e-netze/webgis-community/issues/414)

### Fixed

- GPX download from measure/edit sketches.
- Map series print with WebMercator.
  [Discussion #401](https://github.com/e-netze/webgis-community/discussions/401)
- Undo button in mobile editing mode.
  [Discussion #403](https://github.com/e-netze/webgis-community/discussions/403)

## 8.26.802

### New

- Editing: field type `Date_DateOnly` (date picker without time).
  [Issue #409](https://github.com/e-netze/webgis-community/issues/409)

### Fixed

- Map series print: high CPU usage when creating series from features.
  [Discussion #402](https://github.com/e-netze/webgis-community/discussions/402)
- Custom authentication via database role extension: role/role parameter confusion.
  [Discussion #394](https://github.com/e-netze/webgis-community/discussions/394)

## 8.26.801

### New

- Query results: selection/highlight colors configurable in `api.config`.
  [Docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#abschnitt-query-results)
  [Discussion #353](https://github.com/e-netze/webgis-community/discussions/353)

### Fixed

- MapMarkup: polygons with holes were incorrect.
  [Issue #413](https://github.com/e-netze/webgis-community/issues/413)
- Marker info dock window collapsed.
  [Issue #412](https://github.com/e-netze/webgis-community/issues/412)
- MapMarkup: wrong unit conversion m² to a/ha.
  [Issue #407](https://github.com/e-netze/webgis-community/issues/407)

## 8.26.501

### New

- Sketch/markup upload accepts `.geojson` files.
  [Discussion #369](https://github.com/e-netze/webgis-community/discussions/369)

## 8.26.401

### Upgrade notes

- **Breaking change:** CMS upload secret must be at least 32 characters; WebGIS API and CMS must both be updated to >= 8.26.401.

### New

- Azure AD / OpenID Connect: `roleClaimType` and `roleClaimValueSeparator`.
  [Docs](https://docs.webgiscloud.com/de/webgis/config/authentication/openid.html)

## 8.26.303

### Fixed

- CMS Dockerfile: wrong `WORKDIR`.

## 8.26.302

### New

- `custom.js`: `modify_event` for custom tools.
  [Docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/customtools.html)

### Fixed

- Time filter UI not refreshed after adding/removing services.
  [Discussion #390](https://github.com/e-netze/webgis-community/discussions/390)

## 8.26.202

### Fixed

- Legend: layer name shown for AGS raster layers.
  [Discussion #376](https://github.com/e-netze/webgis-community/discussions/376)

## 8.26.201

### Fixed

- Detail search: queries with only invisible items are hidden.
- DataLinq: document changes are kept when switching tabs.

## 8.25.5101

### New

- CMS: sort algorithm for date/time fields (`default`, `date_dd_mm_yyyy`).
  [Discussion #379](https://github.com/e-netze/webgis-community/discussions/379)

### Fixed

- Quick tools bar: remove-filter and similar tools missing if no quick tools were selected in the map builder.
