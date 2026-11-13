# Release Notes - WebGIS V8

Compact, admin-oriented summary of the changes in WebGIS V8: upgrade notes, new features and
configuration options, and fixed issues. For the full technical details see
[changelog-v8.md](changelog-v8.md). Older versions: [release-notes-v7.md](release-notes-v7.md).

The version number encodes the release date: `Major.{Year-2000}.{CalendarWeek}{Build}`
(e.g. `8.26.4102` = 2026, week 41).

## Unreleased

### Upgrade notes

- After the update, all subscribers have to log in again once. Passwords are re-hashed (PBKDF2) at the next login, so an older API version can no longer verify them: update all instances of a load-balanced installation together, and after a rollback affected subscribers need a new password.
- If subscribers are stored in a SQL database, run the API Setup page once to create the table for two-factor authentication; until then two-factor authentication is greyed out.
  [Docs](https://docs.webgiscloud.com/en/webgis/config/hardening/index.html#hardening-subscriber-mfa)
- If the API runs behind a reverse proxy on another server with `use-x-forwarded-headers`, add the proxy to the new `api.config` keys `middleware:x-forwarded-known-proxies` or `x-forwarded-known-networks`, otherwise the brute force protection counts failed logins per proxy IP.
  [Docs](https://docs.webgiscloud.com/en/webgis/config/api/index.html#api-config-security-subscriber-login)

### New

- Subscriber login hardened for internet use: secure auth cookie with session lifetime, captcha and temporary lockout after failed logins, single-use captchas; configurable in the `api.config` section `security` (`cookie-secure`, `session-*`, `login-*`, `captcha-lifetime-minutes`), optionally shared between instances via Redis.
  [Docs](https://docs.webgiscloud.com/en/webgis/config/api/index.html#api-config-security-subscriber-login)
- Optional two-factor authentication (authenticator app) for subscribers, recommended for admins; make it mandatory with `security:mfa-required` (`admins` or `all`).
  [Docs](https://docs.webgiscloud.com/en/webgis/config/hardening/index.html#hardening-subscriber-mfa)
- New account page where subscribers can change their own password and manage two-factor authentication.
- Password rules for new and changed subscriber passwords (min. 12 characters, blocklist, no personal data); configurable via `security:password-min-length`, `password-required-character-classes` and `password-blocklist-file`.
  [Docs](https://docs.webgiscloud.com/en/webgis/config/api/index.html#api-config-security-subscriber-login)
- The default password `webgisauthor` is blocked; subscribers logging in with it must set a new password.
  [Docs](https://docs.webgiscloud.com/en/webgis/config/hardening/index.html#hardening-default-passwords)
- Redesigned subscriber login and account pages.
- New security report page `/SecurityReport` (tile *Security-Bericht* in the subscriber admin) that rates the security-relevant `api.config` keys for internet and intranet installations and lists all subscribers with their two-factor status. It is protected by the secure-endpoint credentials (`security:secure-endpoint-url-password` or `secure-endpoint-basicauth-*`); without them it is only reachable directly on the server.
  [Docs](https://docs.webgiscloud.com/en/webgis/annex/security.html#annex-security-report)

### Fixed

- Secured ArcGIS Server services (e.g. MapServer WMTS basemaps) with stored credentials work again with ArcGIS Server 12.1.
  [Issue #533](https://github.com/e-netze/webgis-community/issues/533)

## 8.26.4501

### Upgrade notes

- **Breaking change:** Lookup/autocomplete search terms (VisFilter) are now validated against the whitelist `lookup:term-whitelist` in `api.config`; terms with other characters (e.g. `;`, `"`, `=`, `*`) or more than 120 characters return no results. Adjust the key if needed, and quote `{0}` in configured lookup SQL (`'{0}%'`) so it is bound as a parameter.
  [Docs](https://docs.webgiscloud.com/en/webgis/config/api/index.html#api-config-lookup)
- **Breaking change:** Subscriber login redirects are restricted to `portal-url` (and hosts in the new `api.config` key `subscriber-login-allowed-redirect-hosts`), and portal login tokens are audience-bound. If the portal runs under another host name, add it to the key; update API and Portal together.
  [Docs](https://docs.webgiscloud.com/en/webgis/config/api/index.html)
- **Breaking change:** The default CORS policy of the Portal (all pages except `/hmac`) only allows the portal url, the api url and origins listed in the new `portal.config` key `add-cors-origins-for-default`; other sites can no longer call the portal with credentials. Add third-party hosts that need this to the key. The `hmac` policy is unchanged.
  [Docs](https://docs.webgiscloud.com/en/webgis/annex/cors.html#annex-cors)
- **Breaking change:** Dynamic content (GeoRSS/GeoJSON/API queries) is downloaded with SSRF protection and only from signed urls: internal sources must be added to the new `api.config` key `dynamic-content:allow-hosts`, and maps with unsigned dynamic content (MapBuilder html snippets created before this version, urls set in `custom.js`) only load it if `dynamic-content:allow-unsigned` is `true`. Maps with unsigned dynamic content must be reloaded and saved again.
  [Docs](https://docs.webgiscloud.com/en/webgis/config/api/index.html#api-config-dynamic-content)
- **Breaking change:** Only users listed in the map authors of a portal page can publish, change or delete its maps and apps (previously also non-authors, e.g. the original author of a map). Check the map authors of your portal pages; API and Portal must be updated together.
- **Breaking change:** Server certificates of outbound HTTPS connections (Api, Portal, Cms) are now validated; services with self-signed or internal-CA certificates fail with a TLS error. Install the CA in the server's certificate store, or as a workaround set `security:allow-untrusted-server-certificates` to `true` in `api.config`/`portal.config` (Cms: `allow_untrusted_server_certificates` in `settings.config`); `true` is insecure.
  [Docs](https://docs.webgiscloud.com/en/webgis/config/api/index.html#api-config-security-untrusted-certificates)

  ### New

- Quick search shows the copyright info assigned to a search service in the CMS (logo, text, link) below the result list and, for the selected result, in the "Info & Copyright" tab; use it e.g. to meet the terms of use of the DORIS search service.

### Fixed

- Security: SSRF via the dynamic content proxy fixed; only public targets are downloaded and dynamic content urls are signed by the API (new `api.config` section `dynamic-content`).
  [Docs](https://docs.webgiscloud.com/en/webgis/config/api/index.html#api-config-dynamic-content)
- Security: SQL injection in lookup/autocomplete queries (VisFilter) fixed; search terms are bound as parameters or escaped.
- Security: Open redirect in the subscriber login no longer leaks portal credential tokens to arbitrary URLs.
- Security: Portal CORS no longer reflects any origin with credentials; optional keys `add-cors-origins-for-default` and `cors-default-allow-any-origin-without-credentials` in `portal.config`, startup warning if `~` is used.
  [Docs](https://docs.webgiscloud.com/en/webgis/annex/cors.html#annex-cors)
- Security: Publishing, changing and deleting portal maps/apps (including signing MapBuilder dynamic content) is now checked by the API and only allowed for map authors of the portal page.
- Security: DOM XSS via links in feature properties (query result table) fixed; only the link text and `http`/`https`/relative urls are used, additional markup, `onclick` and `javascript:`/`mailto:` links in attribute values are no longer supported.
- Security: TLS server certificates of outbound connections are no longer accepted blindly (man-in-the-middle); see upgrade notes for the opt-out key `allow-untrusted-server-certificates`.

## 8.26.4301

### New
### Fixed

- AGS Query: Suppress pagination for layer without oid-field
  [Issue #520](https://github.com/e-netze/webgis-community/issues/520)
- Identify: If a single service/layer fails, the results of the other layers are still shown;
  the failed queries are reported as a warning.
- Identify (Mobile): faster, layers are queried in parallel.

## 8.26.4104

- Detailsearch: 0 Length Autocomplete not opens
  [Issure #518](https://github.com/e-netze/webgis-community/issues/518)

## 8.26.4103

### Fixed

- ArcGIS Server: layers without pagination support (`supportsPagination: false`) no longer fail with "Pagination is not supported"; they are queried with a single request (max. `maxRecordCount` results).
  [Issue #517](https://github.com/e-netze/webgis-community/issues/517)

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
