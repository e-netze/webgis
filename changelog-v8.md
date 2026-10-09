# Change Log - WebGIS V8

All notable changes to WebGIS V8 are documented in this file.
A compact, admin-oriented summary is available in [release-notes-v8.md](release-notes-v8.md).

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## Unreleased

### Added
### Fixed

## 8.26.4102

### Added

- New Editing AutoValues: AutoValue calculation has been extracted from ``EditEnvironment`` into
  the dedicated ``EditAutoValueService`` and extended with new values:
  - geometry: ``shape_perimeter``, ``shape_centroid_x``, ``shape_centroid_y``,
    ``shape_vertex_count``, ``shape_part_count``, ``shape_type``, ``shape_srefid``;
  - UTC timestamps: ``create_datetime_utc``, ``change_datetime_utc``;
  - editing context: ``edit_operation``, ``map_srefid``, ``edit_service_id``,
    ``edit_layer_id``, ``edit_theme_id``.
  * Coordinate, length, area, perimeter and centroid values (``shape_len``, ``shape_area``,
    ``shape_minx``, ``shape_centroid_x``, ...) can optionally specify a target spatial reference,
    e.g. ``shape_area:31256`` or ``shape_minx:4326``. Calculations use a transformed copy and
    leave the edited geometry unchanged.
  * Existing AutoValues keep their behavior.

- Structured expressions for AutoValues (``=...``) and table columns (TableFieldExpression):
  a typed expression language with field references (``[FIELD]``), arithmetic, comparison and
  logical operators, ``if(...)``, string/number/date functions, null handling and geometry
  functions with optional target spatial reference (``shape_area()``, ``shape_len()``,
  ``shape_centroid_x(4326)``, ...), e.g. ``concat("Area: ", round([AREA], 2), " m2")``.
  * The syntax is detected automatically; existing ``[FIELD]`` text templates and legacy
    ``$eval``/``$round``/``$n`` expressions remain fully compatible.
  * Field values are never interpreted as expression code (no expression injection).
  * Performance: parser selection, syntax trees, feature placeholders and request-header
    placeholders of table expressions, image and hotlink columns are prepared once per request
    instead of for every result row; per-row HTML rendering allocates less.
  * Shape transformations now consistently set the target spatial reference (``SrsId``) on the
    resulting shape.

- ArcGIS Server (AGS) spatial query workaround: ArcGIS Server internally queries its underlying
  database using only the bounding box of a spatial query geometry (not the actual shape) and
  applies the requested result limit already at that stage, so the final (correctly clipped)
  result can end up with far fewer features than actually match - in the worst case 0, even
  though matching features exist. ESRI confirms this behavior but considers it "as designed".

  Since not every AGS instance/database is actually affected, this is opt-in per service via a
  new `QueryStrategy` property on the ArcGIS Server service definition (CMS): `Default` (regular
  query, unchanged behavior) or `BoundingBoxProblem` (enables the workaround below).

  When `BoundingBoxProblem` is selected, the actual strategy used per query is still decided
  dynamically, always preferring the cheaper `Default` behavior whenever it is safe:
  - queries without a spatial filter, or with an `Envelope`/`Point` query geometry, can never
    trigger the bug (bounding box == geometry) and always use `Default`;
  - otherwise, a cheap upfront `returnCountOnly` request against the bounding box of the query
    geometry checks whether the number of candidates ArcGIS Server would clip against even
    reaches the service's result limit; if not, `Default` already returns the full, correct
    result;
  - only if the bug could actually apply, WebGIS falls back to querying feature IDs first
    (`returnIdsOnly`, not affected by the bbox limitation) and then fetches the features in
    batches by ID. A further upfront `returnCountOnly` request against the *real* query
    geometry/where-clause decides whether the (comparatively small) result can be resolved via a
    single unbounded ID request, or whether keyset paging is required to safely handle very large
    result sets (with guards against non-progressing pages, excessive wall-clock time, and a
    configurable maximum result count to protect performance).

  New configurable settings in `api.config`, section `tool-identify`:
  - `ags-spatial-query-max-result-cap` (default 2000)
  - `ags-spatial-query-default-max-record-count-fallback` (default 1000)
  - `ags-spatial-query-max-parallel-batch-requests` (default 4)
  - `ags-spatial-query-ids-timeout-seconds` (default 20)
  - `ags-spatial-query-ids-paging-threshold` (default 50000)
  [docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#werkzeug-identify)

- Query results table (`webgis_queryResultsTable`): client-side paging for large result sets to
  keep the table responsive when many features are loaded (e.g. via the AGS workaround above).
  Paging only kicks in once the result count exceeds a configurable threshold, so existing/smaller
  result sets keep their previous, unpaged behavior. Includes a jump-to-page input and an exact
  result counter (previously abbreviated, e.g. "3K...").

  New configurable options in `custom.js`, `webgis.usability.queryResultsTable`:
  - `pageSize` (default 100)
  - `pagingThreshold` (default 1000)
  [docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/usability.html#ergebnisliste)

- Query results list (`webgis_queryResultsList`, used e.g. in the mobile view without paging):
  now renders at most a configurable number of entries. If more results are available, a static
  notice above the list informs the user how many of the total results are shown and that the
  table view can be opened to see all of them.

  New configurable option in `custom.js`, `webgis.usability.queryResultsList.maxItems`
  (default 1000).
  [docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/usability.html#ergebnisliste)

- Query results table/list: when the AGS spatial-query workaround (or any other query) actually
  hits its configured result limit, a persistent, non-dismissible notification is now shown above
  the results, stating how many results were loaded and that the query should be narrowed down to
  retrieve all matching features.

- Query results table: each row now has a dedicated "..." (menu) button before the marker/bubble
  icon, opening the same tools menu previously only reachable via right-click.
  [Discussion #451](https://github.com/e-netze/webgis-community/discussions/451)

- Query results export (CSV, etc.): the export now includes all query results, not just the
  features of the currently displayed table page. A progress indicator is shown while exporting.

- CMS: the CMS app is now multilingual (German/English). Schema class ``DisplayName``/
  ``Category``/``Description`` attributes can reference ``#key`` markdown entries localized via
  ``l10n/de|en`` files; the navbar now has a language dropdown, persisted in ``localStorage``.


- API: localization texts can be overridden with Markdown files in ``etc/api/l10n/{language}``.
  [docs](https://docs.webgiscloud.com/de/webgis/extended_config/tool_texts.html)

- CMS DeployService: the ``services`` allow-list configured for a deployment is now also applied
  when scanning for warnings before a deploy. Only warnings belonging to services included in the
  list (or all services, if the list is empty/unset) are reported/block the deploy.

- ``IGeoServiceRequestLogger`` (raw GeoService request/response tracing, enabled via
  ``Api:trace``/``Api:logging-log-service-requests``) now has a ``Microsoft.Extensions.Logging``
  based implementation (``MicrosoftGeoServiceRequestLogger``, logged at ``LogLevel.Trace``), used
  automatically when ``Api:logging-type`` is ``microsoft``. Previously only the file based
  ``SimpleServiceRequestLogger`` was available.

- Performance logging (``webgis_performance.csv``): the ``GetPrint`` entry now also reports the
  map center (``X``/``Y``) and the requested print scale (``SCALE``), previously always ``0``.
  ``SERVICE`` now shows ``{LayoutName}-{Size}.{Orientation}-{Dpi}dpi`` (e.g.
  ``Standard-A4.Landscape-150dpi``) instead of embedding the scale that now has its own column.
  ``MAPNAME`` was also missing for all print-related entries (``GetPrint``, ``GetPrintImage`` and
  the ``GetMap`` requests issued internally while composing a print) because the print request
  never sent a map name to the server in the first place (only regular ``GetMap``/
  ``GetSelection``/``GetLegend`` requests did); the print request now includes it too, so it is
  populated the same way for all of these.
  [Issue #461](https://github.com/e-netze/webgis-community/issues/461)

- ``Api:logging-type: microsoft`` (``Microsoft.Extensions.Logging`` based performance logging, as
  an alternative to the CSV files, which remain fully supported/unchanged for existing setups)
  has been modernized further:
  - Every log line now carries a stable ``EventId`` (per ``GeoServiceCommand``, and one each for
    OGC/Usage/DataLinq/Warnings/Exceptions/GeoService-request-tracing), so log backends
    (Seq, Application Insights, ...) can filter/alert on a specific request kind independent of
    the free-text message.
  - A failed request (``ILog.Success == false``) is now logged at ``Warning`` instead of always
    ``Information``, so failures surface via plain log-level filters/alerts without having to
    parse message text - and are no longer silently dropped when only ``Warning``-and-above is
    configured.
  - New ``System.Diagnostics.Metrics``/``System.Diagnostics.ActivitySource`` instrumentation
    (``WebGIS.GeoServices``, see ``webgis.ServiceDefaults``) records request duration/count and a
    trace span for every GeoService/OGC/Usage/DataLinq performance-logged request, independent of
    the configured log level. A ``GetPrint`` request now shows up as a distributed trace with
    child spans for the individual services it fetched, and duration/error-rate dashboards can be
    built without parsing log lines at all.
  - Every request in the API pipeline now runs inside an ``ILogger`` scope (request id, path,
    method), so all log lines belonging to one request can be correlated (also exported via
    OpenTelemetry, since log scopes are included there).
  - ``IUsagePerformanceLogger``/``IDatalinqPerformanceLogger`` now build their message lazily
    (``IsEnabled`` short-circuit), matching ``IGeoServicePerformanceLogger``/
    ``IOgcPerformanceLogger`` - no more unnecessary string concatenation when disabled.
  - The simple, fixed-level loggers (``MicrosoftWarningsLogger``, ``MicrosoftExceptionLogger``,
    ``MicrosoftGeoServiceRequestLogger``) now use compile-time generated ``[LoggerMessage]``
    logging methods instead of hand-written ``IsEnabled`` checks.
  - ``MicrosoftExceptionLogger`` now passes the ``Exception`` itself to the logger (instead of
    ``ex.Message``/``ex.StackTrace`` as plain strings), so it is captured natively (full stack
    trace, exception grouping) by providers that support it.
  - Trace span names are more "speaking" now, making it possible to tell requests apart in a
    trace list without opening each one: the ASP.NET Core root span (previously named after the
    generic route template, e.g. ``POST rest/services/{id}/{request}``, identical for every
    request to that route) is now renamed to the actual resolved request path (e.g.
    ``POST rest/services/12345/GetMap``); the query string is deliberately left out to avoid
    leaking sensitive query parameters (e.g. HMAC tokens) into the tracing backend. This rename
    has to happen via ``EnrichWithHttpResponse`` (fired at request end) rather than
    ``EnrichWithHttpRequest`` (fired at request start), since the ASP.NET Core instrumentation
    itself renames the span from the resolved route pattern once routing has run - which happens
    in between the two and would otherwise overwrite an earlier rename. The nested
    ``WebGIS.GeoServices`` span (previously just the generic header, e.g. "WebGIS.API GeoService
    Performance", identical for every GeoService request) is now named
    ``{category}:{command} {service}`` (e.g. ``geoservice:GetMap MyMapService``).

- Logging sinks: ``Api``, ``Cms`` and ``Portal`` now export logs/metrics/traces via
  OpenTelemetry (OTLP) in Release builds too, not just local development - simply set the
  standard ``OTEL_EXPORTER_OTLP_ENDPOINT`` environment variable, no rebuild needed. This makes it
  straightforward to plug WebGIS into most modern observability backends (OpenTelemetry
  Collector, Grafana/Loki/Tempo, Elastic, Seq, Jaeger, Datadog, Azure Monitor, ...), since they
  all accept OTLP natively.

  Additionally, all three hosts now run [Serilog](https://serilog.net/) alongside the existing
  ``ILogger`` pipeline (augmenting rather than replacing it), purely config-driven via a new
  ``Serilog`` section in ``appsettings.json`` - with no such section, nothing changes. This
  allows writing log events directly into SQL Server or PostgreSQL without needing an
  OpenTelemetry Collector (``Serilog.Sinks.MSSqlServer``/``Serilog.Sinks.PostgreSQL``). There is
  currently no well-maintained Serilog sink for Oracle; Oracle-only setups should use the OTLP
  path instead.

  Both are also configurable purely via the `_config` directory (which is typically the only
  thing mounted/editable in a Kubernetes deployment, e.g. via a ConfigMap volume): an optional
  ``_config/logging.json`` is merged into the configuration the same way ``appsettings.json`` is
  (so the ``Serilog``/``Logging``/``OTEL_*`` keys shown in the docs can be placed there instead),
  and an optional ``_config/logging.env`` (simple ``KEY=VALUE`` lines, like a Docker
  ``--env-file``) is loaded as real process environment variables before the app starts, for the
  cases (like the standard OpenTelemetry ``OTEL_*`` variables) where an env var is more natural
  than nested JSON. Both are no-ops if the file doesn't exist. The shared loading logic
  (``ConfigDirectory``, ``EnvFileLoader``, ``AddConfigDirectoryJsonFile()``) lives in
  ``E.Standard.Configuration``, so all three hosts behave identically.

  See [docs/logging.md](docs/logging.md) for configuration examples.
  [Issue #461](https://github.com/e-netze/webgis-community/issues/461)

- ``Api:logging-type`` (GeoService performance/exception logging) is now a comma-separated list
  instead of a single value, so multiple backends run side by side, e.g. ``"files,microsoft"``
  logs both ``webgis_performance.csv``/``webgis_exceptions.csv`` *and* through
  ``Microsoft.Extensions.Logging`` at the same time (previously mutually exclusive). Three new
  types write into ``webgis_performance``/``webgis_exceptions`` **database** tables, auto-created
  on first use, no manual schema step: ``sqlserver``, ``postgres`` and ``oracle``, plus
  ``sqlite`` for a
  dependency-free local file DB - configured via the new
  ``logging-sqlserver-connectionstring``/``logging-postgres-connectionstring``/
  ``logging-oracle-connectionstring``/
  ``logging-sqlite-connectionstring``
  ``_api.config`` keys. Entries for these four types are buffered in memory and written to the
  database in batches (one connection/transaction per batch of up to 200 entries, flushed at
  least every 5 seconds, or immediately via the existing ``Instance/Logging?flush=true``
  endpoint) rather than opening a new DB connection per request, so logging itself does not
  become a bottleneck under many concurrent requests. GeoService code now starts
  performance/exception logging through two new injectable aggregator services,
  ``GeoServicePerformanceLogService``/``ExceptionLogService``
  (``E.Standard.WebMapping.Core.Logging``), which fan a single ``Start...()``/``LogException()``
  call out to every backend configured above - callers no longer need to know how many/which
  backends are active.
  [Issue #461](https://github.com/e-netze/webgis-community/issues/461)

- The ``sqlserver``/``postgres``/``sqlite``/``oracle`` ``webgis_performance``/``webgis_exceptions`` tables
  now also carry the same extra per-request columns the ``files`` CSV log already has:
  ``session_id``, ``map_request_id``, ``client_ip``, ``user``, ``center_x``, ``center_y``,
  ``scale``. How the ``user`` column/field is populated - for *all*
  ``IGeoServicePerformanceLogger``/``IExceptionLogger`` backends, not just the database ones -
  is controlled by the new ``logging-username-mode`` ``_api.config`` key: ``plaintext``
  (default), ``hash`` (one-way SHA-256 hash, so log rows can still be correlated to "the same
  user" without persisting personally identifiable information), or ``none`` (username never
  recorded). Since ``user`` is a reserved word in SQL Server/PostgreSQL/Oracle, it is always
  quoted (``"user"``, or ``"USER"`` for Oracle, which folds unquoted identifiers to uppercase) in
  generated SQL. Tables created by an earlier version of this feature get the
  missing columns added automatically (``ALTER TABLE ... ADD ...``) the next time the app
  starts - no manual migration needed.
  [Issue #461](https://github.com/e-netze/webgis-community/issues/461)

- Added ``oracle`` as a fourth database backend for ``Api:logging-type``, on par with
  ``sqlserver``/``postgres``/``sqlite`` (``logging-oracle-connectionstring`` ``_api.config``
  key). Since Oracle has no auto-increment column syntax compatible with every supported
  version, the ``id`` primary key of ``webgis_performance``/``webgis_exceptions`` is instead
  filled via a ``BEFORE INSERT`` trigger reading from a dedicated sequence, auto-created
  alongside the table - the same pattern already used elsewhere in WebGIS for Oracle "serial"
  columns.
  [Issue #461](https://github.com/e-netze/webgis-community/issues/461)


- ``MicrosoftWarningsLogger``/``MicrosoftExceptionLogger`` logged the ``service`` value twice
  instead of ``server``/``service`` (a copy-paste mistake in the message placeholders).

- ``Api:logging-type: microsoft`` GeoService performance logging always logged an empty username,
  regardless of ``logging-username-mode`` - ``MicrosoftGeoServicePerformanceLogger`` never passed
  a user identification to the underlying log entry in the first place. It now falls back to the
  username from the map's environment (the same source ``files``/DB logging already use for
  GeoService requests), so the ``{user}`` placeholder is populated as expected.
  [Issue #461](https://github.com/e-netze/webgis-community/issues/461)

- Metadata button: Group Metadata buttons ony shown when ``webgis.usability.show_metadata_i_button_toc`` is true
  [Issue #506](https://github.com/e-netze/webgis-community/issues/506)

- Burger menu (app menu): custom items (e.g. Login/Logout buttons) can now be added via ``custom.js``
  using ``webgis.custom.appMenuItems.add({ name, command, tooltip, image, command_target })``
  [Issue #505](https://github.com/e-netze/webgis-community/issues/505)
  [Docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/appmenuitems.html)

- CMS Playground: added an Expressions tool to test AutoValue and table-column expressions
  against GeoJSON Features and FeatureCollections without publishing, including projected
  geometry functions. It now identifies the selected expression syntax, links to the expression
  documentation, lists available fields/functions, formats GeoJSON, and limits input to 5 MB and
  1,000 features. Added a local edit-form and AutoValue simulator that reads CMS field
  configuration and evaluates selected values against sample GeoJSON and a simulated edit context.
  ``db_select`` AutoValues (SQL or DataLinq endpoint) are executed, with CMS secrets resolved for a
  selectable deployment. Added a RegEx playground for testing patterns, options, capture groups, and
  replacement previews using .NET regular expression syntax.
  Input validation is the default mode, with per-line results and editable examples for usernames,
  email, numbers, Austrian cadastral/parcel numbers, and postcodes.
  Playground pages now follow the CMS UI language.
  [Expressions docs](https://docs.webgiscloud.com/de/webgis/annex/expressions.html)

### Fixed

- Editmask: press ``n`` on select2 combo opens snapping dialog
  [Issue #515](https://github.com/e-netze/webgis-community/issues/515)

- compamy default.css will be loaded in Apps
  [Issue #508](https://github.com/e-netze/webgis-community/issues/508)

- Detailsearch: reset dependent autocomplete values in ``input`` event
  [Issue #507](https://github.com/e-netze/webgis-community/issues/507)

## 8.26.3701

### Added

- Sketch-Info-Overlay: sketch/graphics tools that show a ``UISketchInfoContainer`` (e.g. measuring
  tools) no longer render it inline in the tool dialog. Instead it now floats as an overlay directly
  above the coordinate display, growing upward as more info (snapping, construction tool) is shown.
  It automatically hides when the mouse leaves the map area, is visually styled as an overlay
  (shadow), animates in/out, and can be temporarily dismissed with an "x" button. Its width now
  matches the width of the coordinate display/side frame.
- Sketch-Info-Overlay: also shown for the MapMarkup tool (Polyline/Polygon and, behaving the same
  way, Dimline/DimPolygon/Hectoline). All other MapMarkup geometry types now at least show their
  localized geometry type.
- Sketch-Info-Overlay: clicking the coordinate display while a sketch/graphics tool is active now
  opens the "Coordinates (absolute)" construction tool instead of the XYZ tool; clicking the segment
  area of the overlay opens the "Direction/Distance" construction tool.
- Sketch-Info-Overlay: also shown while editing features with the Edit tool (Desktop
  Insert/UpdateFeature, Mobile UpdateFeature) - without falling back to the inline rendering on
  layouts that don't support the overlay.
- New user preference "Sketch-Info Anzeige" (Burger menu -> Einstellungen -> Benutzer Einstellungen)
  to control how the Sketch-Info-Overlay behaves: ``Standard`` (as before), ``Nicht anzeigen``
  (never show it) or ``Nur Snapping/Konstruktion`` (only show snapping/construction tool info).
  Admins can set a default via
  ``webgis.defaults["user.preferences.sketch-info-display-mode"] = "minimal";`` (``default``/``hidden``/``minimal``).

### Fixed

- site.overlay.css: not loaded corretly in portal pages, when app is behind a reverse proxy.

## 8.26.3402

### Added

- CMS DeployService: deployments can now define an optional ``services`` allow-list to restrict
  which services (ArcGIS Server, ImageServer, WMS, WMTS, ...) are actually included in the
  exported target XML. Services are matched by their url-name (folder name) or, for the rare
  case of duplicate folder names across different service types, by their full relative path.
  Services not listed are skipped during export. Leaving the list empty/unset keeps the previous
  behavior of exporting all services.

### Fixed

- TileService: `CreateImageUrlTemplate` dropped non-standard ports (e.g. `http://localhost:5001/...`)
  when building the `{s}` domain template for tile Urls served from multiple servers/aliases,
  since only the host name was used. The port is now kept whenever it isn't the scheme's
  standard port (80/443); for scheme-relative Urls (`//host:port/...`), where the actual
  scheme is unknown, an explicitly given port is always kept as-is.
- Styling: (MapBuilder Sidebar)
- Coordinates Tool: CSV export ("Koordinaten herunterladen") showed "Rechtswert" twice as column
  header instead of "Rechtswert" and "Hochwert" (Easting/Northing).
  [Issue #499](https://github.com/e-netze/webgis-community/issues/499)

## 8.26.3202

### Fixed

- Styling Bugs: image and background-size corrections:
  [Issue #498](https://github.com/e-netze/webgis-community/issues/498)

## 8.26.3201

### Added

- CSS Custom WebGIS Brand Properties (Variables)
  [Issue #473](https://github.com/e-netze/webgis-community/issues/473)
  [docs](https://docs.webgiscloud.com/en/webgis/config/css-styling/index.html)

  **!! Braking Change !!**
  This change may cause changes in existing stylings of the viewer!

- custom.js: added usability constant ``expandBasemapsOnAddServices`` to control whether the
  basemap group in the TOC stays expanded/collapsed when services are added to the map
  [docs](https://docs.webgiscloud.com/en/webgis/apps/viewer/customjs/usability.html#hintergrundkarten-basemaps)

- CMS Editing Commit Action: new ``Success Message`` property. It is shown in the viewer after a
  commit action was executed successfully (Insert/Update/Delete, regardless of Before/After timing).
  The message text may contain ``[FIELDNAME]`` placeholders that are resolved with the values of the
  current feature. By default the message is shown as a toast notification; prefixing it with
  ``dialog:`` shows it in a dialog instead.

- DataLinq Configuration:
  Added ``datalinq:ImageRequestWhiteList:0`` array to ``api.config``
  [docs](https://docs.webgiscloud.com/en/webgis/config/api/index.html#datalinq)

### Fixed

- MapMarkup: draw rectangle with distance/direction Tool
  [Issue #497](https://github.com/e-netze/webgis-community/issues/497)

## 8.26.3101

### Added

- DataLinq Update: 8.26.3101
  fixed/solved:
  [DataLinq Issue #49](https://github.com/e-netze/datalinq-community/issues/49)
  [DataLinq Issue #47](https://github.com/e-netze/datalinq-community/issues/47)

- WebGIS Help: 
  * Admin Pages in english (with switch to german)
  * User Manuel in user selected language (with switch en/de)

## 8.26.2804

### Added

- Esri Map Service Query Results with Dates: consider time-zone
  [Issue #484](https://github.com/e-netze/webgis-community/issues/484)

- Show Service and Layer Metadatalinks in Copyright & Info Section
  - custom.js: ``show_presentation_metadata_in_copyright``: ``true``;

- custom.js: added usability constants:
  - ``show_metadata_i_button_toc``: ``true``
  - ``show_link_button_in_toc``: ``true``  // custom-recommendation => ``webgis.usability.show_metadata_i_button_toc = webgis.isMobileDevice() !== true;``

- Metadata: CMS configurable popup dialog size
  [Issue #459](https://github.com/e-netze/webgis-community/issues/459)

### Fixed

- TilingService Bug & Improovments:
  [Issue #480](https://github.com/e-netze/webgis-community/issues/480)

## 8.26.2401

### Added

- Quicksearch: make ``minLength`` and ``debounceDelay`` configurable
  [Issue #469](https://github.com/e-netze/webgis-community/issues/469)

- Editing: Before/After Commit Actions
  sending HTTP GET/POST Request to a service before and/or after an insert/update/delete

- SOLR Search: Added placehoders ``{{roles}}`` and ``{{namespace-roles}}`` (EXPERIMENTAL, NOT SUPPORTED!)
  [Issue #481](https://github.com/e-netze/webgis-community/issues/481)

### Fixed

- Tool Coordinates: Remove all vertices
  [Issue 471](https://github.com/e-netze/webgis-community/issues/471)

- Measuring Tools: Change tool => UI is not updating sketch values
  [Issue #465](https://github.com/e-netze/webgis-community/issues/465)

- Tool Identify: Line/Polyselection not always works in desktop/professional layout
  [discussion #399](https://github.com/e-netze/webgis-community/discussions/399)

- Editing: save-and-continue-sketch not worked with desktop/professional layout

## 8.26.2201

### Added

- MapSeriesPrint: Added "Create One per Feature" Method.
  Increased Usability with creation descrtion and preview.
  [Issue #464](https://github.com/e-netze/webgis-community/issues/464)

- Endpoint Security (cache/clear) etc, ``<section name="security">``
  api.config [docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#security)
  portal.config [docs](https://docs.webgiscloud.com/de/webgis/config/portal/index.html#abschnitt-security)

### Fixed

- Tool Profile: Can't download print PDF document
  [Issue #479](https://github.com/e-netze/webgis-community/issues/479)

## 8.26.1801

### Added

- TileCache Rendering Mode: ScaleDependentLayers (for printing cached MapServices)
  [Issue #458](https://github.com/e-netze/webgis-community/issues/458)

- Map Series Print: api.config - allow increase max intersection iterations
  [Issue #464](https://github.com/e-netze/webgis-community/issues/464)

### Fixed

## 8.26.1702

### Added

- GeoCodes: allow GeoCodes (UTMRef, etc) in Coordinates-Tool and Quick Search 
  [discussion #449](https://github.com/e-netze/webgis-community/discussions/449)
  [docs GeoCodes](https://docs.webgiscloud.com/de/webgis/annex/geocodes.html)
  [docs CoordsTool](https://docs.webgiscloud.com/de/webgis/extended_config/etc/xz.html#default-xml)
  [docs api.config](https://docs.webgiscloud.com/de/webgis/config/api/index.html#abschnitt-proj4-database-geocodes)

- DataLinq Upgrade: 8.26.1502

- Basic Authentication for SOLR search services
  [Issue #456](https://github.com/e-netze/webgis-community/issues/456)

### Fixed

- Bug: API Crashes on start, if DataLinq is not included in api.config

- Print: Error ``Service with url #service not found``
  [Issue #453](https://github.com/e-netze/webgis-community/issues/453)

- MapMarkup: Snapping not works with MapMarkup before zoom/pan
  [Issue #455](https://github.com/e-netze/webgis-community/issues/455)

- MapSeriesPrint: Save/Load failed, if series only had one page

- Bug: can't open side-by-side app in AppBuilder
  [Issue #457](https://github.com/e-netze/webgis-community/issues/457)

- Tooltip mass attribution tool added:
  [Issue #446](https://github.com/e-netze/webgis-community/issues/446)

- Ignore wrong/error elevation-values in GPX files
  [Issue #430](https://github.com/e-netze/webgis-community/issues/430)

- UI (wrapping Buttongroups)
  [Issue #454](https://github.com/e-netze/webgis-community/issues/454)

## 8.26.1402

### Added

- ResultTable: new sorting algorithm ``number_de``
  [dicussion #440](https://github.com/e-netze/webgis-community/discussions/440)

- WMTS: adding more exotic services
  [dicussion #447](https://github.com/e-netze/webgis-community/discussions/447)

- DataLinq: parameters of 1:n links from resulttable can be posted to internal datalinq instance (experimental)
  **api.config** => ``use-cache-token-for-one-2-n-links`` 
  [docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#datalinq)

- Layout for Tablets (``portal/viewerlayouts/w1024.html``): Usability improvments
  TOC/Tooldialog on the left side, etc

- DataLinq Upgrade: 8.26.1402

### Fixed

- Bug: Print Measure Polygon calculate wrong circumference value
  [Issue #452](https://github.com/e-netze/webgis-community/issues/452)

## 8.26.1301

### Added

- Logging: User information logs (like no query found in this area, etc) will only logged
  in ``webgis-exceptions.log`` if LogLevel is ``Information``

### Fixed

- Bug: TokenRequiredError 
  [Issue #422](https://github.com/e-netze/webgis-community/issues/442)

- Bug: QueryResults 1:n - Linktype ``dialog`` also opens a new browser tab
  [Issue #443](https://github.com/e-netze/webgis-community/issues/443)

## 8.26.1203

### Added

- TimeFilter UI:
  * Switcher: **Point in time** / **Span of Time**
  * Show **All time-dependent services** option only if there is more than one time-dependent service in map 

- DataLinq Upgrade: 8.26.1201

### Fixed

- Linux/ContainerImages: Fixed problem with localization (Localization/DefaultCulture)
  [Issue #422](https://github.com/e-netze/webgis-community/issues/422)

- Timefilter: not applyed to print
  [Issue #438](https://github.com/e-netze/webgis-community/issues/438)

- QueryResult: show ``showQueryLayerNotVisbleNotification`` only once
  [Issue #439](https://github.com/e-netze/webgis-community/issues/439)

- MapSeriesPrint: German messages
  [Issue #436](https://github.com/e-netze/webgis-community/issues/436)

- Typos 
  [Issue #441](https://github.com/e-netze/webgis-community/issues/441)

## 8.26.1201

### Added

- ``api.config``:
  **Security:** Disable Antiforgery by configuration (not recommeneded!)
  [docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#security)
  **Middleware:** Add XForwarded Middleware explicitly (if needed)
  [docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#middleware)

## 8.26.1101

### Added

- ``appsettings.json``: DefaultCulture and ``/instance/_culture`` Endpoints
  [Issue #422](https://github.com/e-netze/webgis-community/issues/422)

- Appearence of DimLines and DimPolygins (MapMarkup) on PDFs (not yellow box and no EPSG Code)
  [Issue #434](https://github.com/e-netze/webgis-community/issues/434)

- Better error message, when parsing GPX/GeoJson uploaded files failed
  [Issue #430](https://github.com/e-netze/webgis-community/issues/430)

- Links in result table: Added ``datalinq_pdf_report`` as type in WebGIS-CMS to directly download DataLinq PDF Reports 

### Fixed

- Language can not set by user, whenn running WebGIS in Containers
  [Issue #421](https://github.com/e-netze/webgis-community/issues/421)

## 8.26.1001

### Added

- custom.js: set visibility for tools: ``webgis.usability.toolProperties['webgis.tools.serialization.savemap'] = { visibility: 'hidden' };``
  [dicussion #408](https://github.com/e-netze/webgis-community/discussions/408)

- api.config: tool section ``<add key="allow-anoymous-access" value="false" />``
  [dicussion #408](https://github.com/e-netze/webgis-community/discussions/408)
  [docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#werkzeug-konfiguration)

- api.config: Tool MapMarkup - added key ``save-name-maxlength``
  [dicussion #204](https://github.com/e-netze/webgis-community/discussions/204)
  [docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#werkzeug-mapmarkup)

### Fixed

- Security: Avoid 3DES (use AES) Algorithm
  [Issue #423](https://github.com/e-netze/webgis-community/issues/423)

  **Breaking Change**
  CMS Upload only works if both (WebGIS API and WebGIS CMS) have a version >= 8.26.1001.
  So you have to update WebGIS CMS an WebCMS API in one step.

- Datalinq: Environment variable (test/dev/prod) not applyed to connectionstring in WebGIS Datalinq Engines
  [DataLinq Issue #41](https://github.com/e-netze/datalinq-community/issues/41)

- Remove Query Result Button not works on quick search results with service-query-theme
  [Issue #426](https://github.com/e-netze/webgis-community/issues/426)

## 8.26.901

### Fixed

- Download GPX from measure/edit sketch not worked correctly

## 8.26.901

### Added

- Health Checks: added ``/health`` and ``/alive`` endpionts 
  [Issue #414](https://github.com/e-netze/webgis-community/issues/414)

### Fixed

- MapSeries Print: Works with WebMercator (and different display spatial reference)
  [discusson #401]()https://github.com/e-netze/webgis-community/discussions/401

- Undo Button: not (always) works in Mobile-Editing mode
  [discusson #403]()https://github.com/e-netze/webgis-community/discussions/403

## 8.26.802

### Added

- Editing: Added field type ``Date_DateOnly`` to show a DatePicker without a time (hour/minute) selector
  [Issue #409](https://github.com/e-netze/webgis-community/issues/409)

### Fixed

- Map Series Print: Check max iteration when creating map servies from features (Raster, Intersection, Along polyline)
  This avoid heavy CPU usage
  [discussion #402](https://github.com/e-netze/webgis-community/discussions/402)

- Custom Authentication via database role extension: Fixed Role/Roleparameter confusion
  [discussion #394](https://github.com/e-netze/webgis-community/discussions/394)

## 8.26.801

### Added

- QueryResults: config selection/highlight color and fillcolor in api.config
  [docs](https://docs.webgiscloud.com/de/webgis/config/api/index.html#abschnitt-query-results)
  [discussion #353](https://github.com/e-netze/webgis-community/discussions/353)

### Fixed

- MapMarkup: Donat polygons produced incorrect map-markup polygons
  [Issue #413](https://github.com/e-netze/webgis-community/issues/413)

- Usabliby: Markerinfo dock window is collapsed
  [Issue #412](https://github.com/e-netze/webgis-community/issues/412)

- MapMarkup: Wrong unit calculation m² => ar, ha
  [Issue #407](https://github.com/e-netze/webgis-community/issues/407)

## 8.26.501

### Added

- Sketch/Markup upload: allow ``.geojson`` extension
  [discussion #369](https://github.com/e-netze/webgis-community/discussions/369)

## 8.26.401

### Added

- SecurityConfig: added ``roleClaimType`` and ``roleClaimValueSeparator`` for AzureAD and OpenID Connect Authentication
  [docs](https://docs.webgiscloud.com/de/webgis/config/authentication/openid.html)

- CMS Upload: Increased min secret length to 32. With this realease both ``api`` and ``cms`` must be updated to at least version 8.26.401, otherwise cms upload will not work.

## 8.26.303

### Fixed

- CMS-Dockerfile: fixed typo ``WORKDIR /app#`` => ``WORKDIR /app``

## 8.26.302

### Added

- custom.js: add ``modify_event`` to custom tools to e.g. calculate world coordinates in a different coordinate system
  [docs](https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/customtools.html)

### Fixed

- Bug: Timefilter - UI not refreshed after adding or removing service to map
  [discussion #390](https://github.com/e-netze/webgis-community/discussions/390)

## 8.26.202

### Fixed

- Show Layername in legend for AGS-Raster layers
  [discussion #376](https://github.com/e-netze/webgis-community/discussions/376)

## 8.26.201

### Fixed

- Bug: dont show queries in detail search with only invisible query items
- Bug: DataLinq: Keep changes in documents after changing tabs

## 8.25.5101

### Added

- DateTimeFields: Added the possibility to set sorting algorithm for DateTime fields in CMS configuration
  Possible values:
  - `default`: Default sorting behavior (string based)
  - `date_dd_mm_yyyy`: Sort as date with format dd.MM.yyyy
  [discussion #379](https://github.com/e-netze/webgis-community/discussions/379)

### Fixed

- Bug: Remove-Filter, etc tools will not be shown in quicktools bar,
       if not quicktools are selected in map builder
