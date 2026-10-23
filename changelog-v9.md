# Change Log - WebGIS V9

All notable changes to WebGIS V9 are documented in this file.
A compact, admin-oriented summary is available in [release-notes-v9.md](release-notes-v9.md).
Changes of the previous major version: [changelog-v8.md](changelog-v8.md).

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## Unreleased

### Added

- CMS git support (optional, per cms-item): with a ``git`` section in ``cms.config``
  (``remote-url``, ``username``, ``token``, ``default-branch``, ``workspace-root``,
  ``author-email-domain``, ``committer-name``, ``committer-email``) the CMS tree is versioned in
  an external (private) git repository via LibGit2Sharp. Without git the CMS behaves as before.
  * Each CMS user works in an own working copy (workspace) of the tree; global default
    ``git-workspace-root`` in ``cms.config``, overridable per cms-item.
  * Branches, commit, pull/push and merge into the default branch including conflict resolution
    in the CMS; direct commits to ``main`` are allowed.
  * History dialog with commit graph, branch/HEAD/deploy markers, commit details and per-file diffs;
    working-copy diffs with property table view and a "My changes" dialog with suggested commit messages.
  * Node history with restore and "compare with main" (with take-over from main).
  * Deployments always use the latest state of the remote default branch (separate deploy clone,
    one deployment per cms-item at a time); the deploy page shows the deployed commit.
  * Workspace admin dialog; invalid git configurations are reported on startup and disable git
    for the cms-item. Docker (Alpine) images contain the musl build of libgit2.

  [Docs](https://docs.webgiscloud.com/en/webgis/apps/cms/git.html),
  [Docs config](https://docs.webgiscloud.com/en/webgis/config/cms/index.html#cms-config-git),
  [Architecture](docs/architecture/cms-git.md)

- CMS branch deploys: with ``allowBranchDeploy`` on a deployment, a user can deploy the current
  state of the own workspace (including uncommitted changes, flagged in the deploy info) as a branch.
  * File targets: ``{target-dir}/branches/{encoded-branch}/...``; url targets: upload with a branch
    parameter. ``{branch}`` placeholder in ``postEvents`` (empty for the main deploy).
  * The WebGIS API discovers deployed branches automatically if ``allow-branches`` is set in
    ``api.config``.
  * Deployed branches are listed and can be removed on the deploy page; stale branches are removed
    automatically. Deploy tiles show a "main only" / "main + branches" badge.
  * Link warnings of a branch deploy are written to a per-user warnings file, so an aborted branch
    deploy offers its own "solve warnings" tile without touching the main deploy.

  [Docs](https://docs.webgiscloud.com/en/webgis/apps/cms/deploy.html#cms-deploy-branch),
  [Docs api.config](https://docs.webgiscloud.com/en/webgis/config/api/index.html),
  [Architecture](docs/architecture/cms-deployment.md)

- Branch selection in portal and viewer: CMS branches can be selected per map (select, badge,
  dialog, ``?branch=`` url parameter, secured branch links with tokens). Stale branches are reset,
  and warnings are shown when publishing/saving maps with a selected branch.

  [Docs](https://docs.webgiscloud.com/en/webgis/apps/portal/branches.html)

- Fast branch deploy: branch deploys keep a per-user snapshot of the read CMS files
  (``{workspace-root}/{cms-id}/cache/{user}.snapshot``). Subsequent branch deploys only re-read
  the files changed since the snapshot (git diff + uncommitted changes); the result is identical
  to a full read. The deploy dialog shows the snapshot date/commit and offers
  "Read everything again". Main deploys are unchanged.
  * Git status no longer detects renames: renamed files are listed as deleted + added.

  [Docs](https://docs.webgiscloud.com/en/webgis/apps/cms/deploy.html#cms-deploy-fast),
  [Architecture](docs/architecture/cms-deployment.md)

- CMS deploy performance: single-pass export with link warnings collected during the export
  (no separate warnings scan), cached directory listings and per-phase timings in the deploy console.

  [Architecture](docs/architecture/cms-deployment.md)

- Modernized CMS look: design tokens (``--webgis-ui-*``), breadcrumb chevrons, resizable tree,
  refreshed tiles, dialogs and forms. In the deploy dialog the main deploy is now labeled
  "Published state (main)" instead of "Production".

- Debug UI tools (only in ``DEBUG`` builds, toolbox container "Debug"): a set of viewer tools in
  ``E.Standard.WebGIS.Tools/Debug/UI`` that render all server-side UI elements, targets/dialogs,
  setters and UI plugins reachable via UI elements, to check their rendering and callbacks manually
  (echo of all received tool parameters). New skill ``.github/skills/create-webgis-tool``.

- Viewer UI JavaScript restructured (WebGIS API ``wwwroot/scripts``):
  * Leaflet plugins ``webgisservicelayer.js`` and ``webgisgraphics.js`` split into one file per class
    (``leaflet/plugins/webgis-servicelayer.*.js``, ``webgis-graphics.*.js``).
  * ``webgis_uibuilder.js`` split into ``webgis.ui.builder.*.js`` (stateless build functions,
    core in ``webgis.ui.builder.core.js``) and ``webgis.ui.plugins.*.js``.
  * Legacy ``$.fn.webgis_*`` jQuery plugins converted to ``webgis.ui.definePlugin`` (one
    ``webgis.ui.plugins.<name>.js`` file each, instances released on ``remove``/``empty``);
    unused scripts (``webgis_queryBuilder.js``, ``webgis_graphics_info_container_old.js``) removed.
  * Static plugin methods used from other files are called through flat ``webgis.ui.<verbNoun>``
    functions (e.g. ``webgis.ui.hideMapContextMenu()``, ``webgis.ui.closeDockPanel(...)``).
  * Modal dialogs: ``webgis_modal.js``/``webgis_modalprogress.js`` replaced by
    ``webgis.ui.plugins.modal.js`` with ``webgis.ui.showModal(options)``, ``closeModal``,
    ``getModalContent``, ``hideModal``, ``fitModal``, ... and ``webgis.ui.showProgressModal``/
    ``hideProgressModal``. ``$.fn.webgis_modal`` and ``$.fn.webgis_modalprogress`` remain as
    legacy facades for custom code (e.g. ``custom.js``). Portal scripts use the new functions.
  * Context menu: ``webgis_contextMenu.js`` replaced by ``webgis.ui.plugins.contextMenu.js``
    (``webgis.ui.showContextMenu(options)``, no ``$.fn.webgis_contextMenu`` anymore).
  * The portal page loads ``api-ui.min.js`` (modal dialogs) for content authors and page owners.

  **!! Breaking Change !!** Single script files were renamed or removed (see above). Custom pages
  that include them directly (e.g. ``webgis_modal.js``, ``webgis_uibuilder.js``,
  ``webgisgraphics.js``) must include the bundles (``api.min.js``, ``api-ui.min.js``, Leaflet
  bundle) instead. Custom code calling ``$.fn.webgis_contextMenu`` or static plugin methods via
  ``$.fn.<plugin>.<method>`` must use the ``webgis.ui.*`` functions.

  [Architecture](docs/architecture/ui-builder.md)

- Viewer debug output: ``webgis.log(...)`` (and ``webgis.logGroup``/``logGroupEnd``) only writes to the
  console when ``webgis.logLevel`` is set to ``'debug'`` (``console.log``) or ``'trace'``
  (``console.trace``) in the browser console; off by default. All debug ``console.log`` calls of the
  API scripts use it now.
  * UI plugins log ``init``/``destroy``; cascading removals are shown as nested console groups.
  * Errors in a plugin ``destroy`` are reported with ``console.error`` instead of being swallowed.

  [Architecture](docs/architecture/ui-builder.md)

- Single API bundle (recommended for clients): ``scripts/api/webgis-api.min.js`` (``api.min.js`` +
  ``api-ui.min.js``) and ``content/styles/webgis-api.min.css`` (minified ``default.css`` +
  ``content/api/ui.icons.css``), built additionally by the bundler. The previous files remain and are
  still supported.
  * ``content/api/ui.css`` split into ``ui.icons.css`` (icons, part of the bundle) and ``ui.app.css``
    (full-page app behavior: no text selection, no pull-to-refresh); ``ui.css`` imports both.
  * All internal pages (viewer, MapBuilder, AppBuilder, portal pages, DataLinq reports, API pages),
    the portal/MapBuilder HTML templates and the plugin docs now use the single bundle; viewer,
    builders and templates additionally load ``ui.app.css``. The portal page loads
    ``webgis-api.min.js`` for all users.
  * ``webgis.baseUrl`` falls back to ``document.currentScript`` if no ``id="webgis-api-script"`` is set.
  * Viewer style overlays renamed to ``webgis-api.bg-dark.css``, ``webgis-api.bg-light.css`` and
    ``webgis-api.space-saving.css`` and loaded minified (``*.min.css``) in the viewer.

  [Docs](https://docs.webgiscloud.com/en/webgis-dev/API/API_JavaScript/index.html#api-javascript-single-bundle),
  [Architecture](docs/architecture/api-javascript-bundles.md)

- Tool events (``/rest/toolevent``) send their parameters as a JSON object in the form field
  ``eventBody`` instead of the pipe-separated ``eventString``.
  * Server (``ApiToolEventArguments.ParseEventBody``) normalizes JSON values to strings
    (numbers/objects as raw JSON, bools ``true``/``false``, arrays comma-joined), so tools are unchanged.
  * Removed without fallback: pipe format and ``~~~&PIPE;~~~`` escaping, ``RawEventString``,
    ``webgis.tools.toolParameterString``/``toolDependencyString``, event ``toEventString``/``toClickString``
    (now ``toEventParameters``/``toClickParameters``).
  * Values containing ``|`` are no longer split (also for the ``NameValueCollection`` based tool methods).

  **!! Breaking Change !!** Custom code calling the removed ``webgis.tools`` string functions or
  posting ``eventString`` to ``/rest/toolevent`` must be adapted.

  [Architecture](docs/architecture/tool-events.md)

- Detachable windows (viewer): the toolbar and the query results table of the desktop layout can be
  detached into a separate window (Document Picture-in-Picture, Chromium browsers only). Enabled with
  ``webgis.usability.detachableWindows = true`` in ``custom.js`` (default: ``false``).
  * New ``webgis.windowManager`` (``webgis.window-manager.js``): layout elements with the class
    ``webgis-detachable`` (``data-detachable-id/-title/-order/-size/-icon-target``) get a detach
    button; all detached areas share one window, a placeholder stays in the main window.
  * The real DOM nodes are moved; jQuery hooks (``$()``, ``.find()``, ``.closest()``) also resolve
    the detached elements, so other viewer scripts are unchanged. Modal dialogs and context menus
    open in the detached window if they belong to it.
  * Detached toolbar: all tools on one page, tab names as block titles (compact, wrapping blocks).
  * If the active tool hides a detached area (e.g. print), the window shows a hint with a
    "Close active tool" button.
  * Detached areas and window size are stored in ``localStorage``; on the next start a toast asks
    to restore them.

  [Docs](https://docs.webgiscloud.com/en/webgis/apps/viewer/customjs/usability.html#customjs-detachable-windows),
  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/general/ui.html#ui-detachable-windows),
  [Architecture](docs/architecture/detachable-windows.md)

- Viewer table of contents: context menu for presentations and groups (right mouse button or the
  burger icon that is shown on hover). The browser context menu is suppressed in the whole TOC.
  * Presentation menu: Metadata (if available), Query Builder (submenu "All" + one entry per
    queryable layer), Properties. Group menu: Metadata (if available), Properties.
  * The Query Builder no longer opens an empty tool dialog (``suppressToolDialog``).
  * Context menu plugin supports submenus (hover), separators and checked items.
  * Properties dialog with tabs: General (name, description, services, layers with visibility,
    scale range, queryable, filter; actions "Zoom to visible scale" and "Remove filter"), Legend
    (all layers, also invisible ones), Info & Copyright and Technical (dvid, service ids/types,
    layer ids/full names/scales/query ids with copy buttons; only if
    ``webgis.globals.portal.isMapAuthor``, set by the portal for the map author).
  * Groups: name, contained presentations with status and a combined legend.

  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/presentation/index.html#toc-context-menu),
  [Architecture](docs/architecture/toc-context-menu.md)

- User-defined layer presentation ("Darstellung") in the viewer TOC for ArcGIS Server map services
  with ``supportsDynamicLayers`` (feature layers only):
  * Context menu entry "Darstellung" (submenu per layer) opens an editor: "Default" (service
    presentation) or "Custom" with single symbol, unique values ("Load values", max classes
    configurable) or class breaks ("Calculate classes" from the field's min/max). Symbols for
    point, line and polygon; only QueryBuilder fields are offered and accepted.
  * Neutral, versioned renderer model (``UserRenderer``) with server validation; sent as
    ``renderers`` (POST) with map, legend and print requests and translated to AGS
    ``dynamicLayers`` (``drawingInfo.renderer``). Invalid renderers are ignored (default drawing).
  * Renderers are stored per service layer and serialized with the map. The legend uses
    ``dynamicLayers``; for unique values/class breaks the field alias is appended to the layer name.
  * Enable with the CMS service property ``allow_user_renderers`` (authorizable) and
    ``allow-toc-userrenderer`` / ``userrenderer-max-unique-value-classes`` in the ``api.config``
    section ``toc-contextmenu``.

  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/presentation/index.html#toc-user-renderer),
  [Docs api.config](https://docs.webgiscloud.com/en/webgis/config/api/index.html#api-config-toc-contextmenu),
  [Docs CMS](https://docs.webgiscloud.com/en/webgis/apps/cms/add_ags_service.html#cms-ags-user-renderer-labeling),
  [Architecture](docs/architecture/user-renderer-labeling.md)

- User-defined labeling ("Beschriftung") in the viewer TOC (same prerequisites as the user
  presentation): modes "Default", "No labeling" and "Custom" with a label text template
  (``"Area: [FIELD] m²"``, field insert, max 200 characters/3 lines), font size, font style, font
  color and halo. Sent as ``userlabels`` and translated to AGS ``labelingInfo`` (combined with a user
  renderer in the same dynamic layer). Enable with the CMS service property ``allow_user_labeling``
  and ``allow-toc-userlabeling`` in the ``api.config`` section ``toc-contextmenu``.
  **!! Breaking Change !!** The legacy tool "Labeling" (``webgis.tools.presentation.labeling``),
  the CMS labeling nodes and the ``labels`` request parameter were removed; labels stored in old
  maps are ignored.

  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/presentation/index.html#toc-user-labeling),
  [Docs CMS](https://docs.webgiscloud.com/en/webgis/apps/cms/add_ags_service.html#cms-ags-user-renderer-labeling),
  [Architecture](docs/architecture/user-renderer-labeling.md)

- Viewer TOC: badges at presentations with a user presentation, user labeling, filter or metadata
  (clicking a badge opens the context menu), and context menu entries "Reset filter",
  "Reset presentation" and "Reset labeling".

  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/presentation/index.html#toc-badges)

- ``api.config``: the section ``tool-visfilter`` was renamed to ``toc-contextmenu``
  (``allow-toc-visfilter``, ``allow-toc-userrenderer``, ``userrenderer-max-unique-value-classes``,
  ``allow-toc-userlabeling``). **!! Breaking Change !!** Existing ``tool-visfilter`` settings must be
  moved to the new section.

  [Docs api.config](https://docs.webgiscloud.com/en/webgis/config/api/index.html#api-config-toc-contextmenu)

- Query results: if the result limit is reached, the notification above the table also shows the
  total number of hit features (e.g. "limit of 6,000 results reached - a total of 12,345 features
  were hit").
  * New ``FeatureCollection.TotalCount`` / ``FeaturesDTO.Meta.TotalCount`` (``total_count``).
  * ArcGIS Server: the count already determined by the ``BoundingBoxProblem`` query strategy is
    reused; otherwise ``QueryEngine`` sends one additional ``returnCountOnly`` request
    (``ILayer3.FeaturesCountOnly``) only when the limit was reached. Errors are ignored.
  * Other service types (without count support) keep the previous notification.

- QueryBuilder (TOC display filter): value suggestions after selecting a field, depending on the
  field type (new ``UIQueryBuilder.ValuesSource`` / ``values_source`` and ``field_type``).
  * Strings: autocomplete from the first character (``LIKE '%...%'``, max. 20 values).
  * Numbers: ``input type=number`` with a datalist if the layer has ≤ 25 distinct values,
    otherwise a placeholder; operators ``<``, ``>``, ``<=``, ``>=``.
  * Dates: Flatpickr date picker, ``DATE 'yyyy-mm-dd'`` literals (also parsed by the where
    parser), operators ``<``, ``>``, ``<=``, ``>=``.
  * Values are queried live via the new ``VisFilter`` server command ``querybuilder-values``
    (``IBridge.QueryLayerDistinctAsync``, currently ArcGIS Server only). Active display filters are
    not applied, locked filters always are. Thresholds are constants in ``UIQueryBuilder``.
  * Demo in the debug UI tool "Advanced Selectors".

  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/presentation/index.html#toc-querybuilder),
  [Architecture](docs/architecture/querybuilder.md)

- Query result restriction: the results table has a new table tool "Restrict" that opens the
  QueryBuilder to narrow down a query/identify result (result filter, not a display filter).
  * The original query is re-run on the server with the QueryBuilder where as an additional
    condition, including the query geometry (identify, buffer, bbox) and the detail search items.
  * The origin is sent to the client as a protected token (``QueryRestrictionDescriptor``) in the
    new ``FeaturesDTO.Meta`` field ``query_restriction`` (only if the QueryBuilder is allowed for the
    service and fields are enabled); new tool ``QueryResultRestriction`` with the commands
    ``restrict``, ``remove-restriction`` and ``show-geometry``.
  * The dialog shows the origin of the result (query geometry, detail search with values, original
    where); "Show" previews the query geometry briefly (new client command ``showpreviewgeometry``).
  * Restricted results show a hint row with the where; the restriction can be removed with the
    "×" in the hint or with "Remove restriction" in the dialog (``UIQueryBuilder.RemoveButtonLabel``).
  * While the request runs, the buttons are hidden and a loading indicator is shown (double clicks
    are prevented); new optional ``complete`` callback for ``webgis.ajax`` / tool requests.

  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/search_identify/operations/results_desktop/results_desktop.html#results-query-restriction),
  [Architecture](docs/architecture/querybuilder.md)

- TOC context menu "Table": shows the features of a layer (identify-enabled query that allows an
  empty search) in the results table without a search term or query geometry; the result can be
  narrowed down with the table tool "Restrict".
  * Client request ``_source=toc-table`` with the new ``QueryDTO`` flag ``allow_empty_search``;
    active display filters are applied. Submenu per query if a layer has several queries.
  * Feature limit: the maximum of the service/query, otherwise the new ``api.config`` key
    ``table-feature-limit`` in the section ``toc-contextmenu`` (default ``1000``).
  * New ``QueryRestrictionDescriptor`` source ``Table`` (descriptor without geometry), shown as
    origin in the restriction dialog.
  * The menu item can be hidden with ``webgis.usability.allowTocTable = false`` in ``custom.js``.

  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/presentation/index.html#toc-table),
  [Docs api.config](https://docs.webgiscloud.com/en/webgis/config/api/index.html#api-config-toc-contextmenu),
  [Docs custom.js](https://docs.webgiscloud.com/en/webgis/apps/viewer/customjs/usability.html#customjs-toc-table),
  [Architecture](docs/architecture/querybuilder.md)

- QueryBuilder: new operators ``not like``, ``not in`` (strings, numbers), ``is null`` and
  ``is not null`` (all field types; the value input is hidden). Supported by ``UIQueryBuilder``
  (operator lists, where parser, sanitizing) and the server-side where validation.
  * The TOC context menu entry and the dialog title are renamed to
    "Display filter (DefinitionQuery)" (DE: "Darstellungsfilter (DefinitionQuery)") to distinguish
    it from the result restriction in the table.
  * Wider operator column in the QueryBuilder (``td.operators``), narrower field column.

  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/presentation/index.html#toc-querybuilder),
  [Architecture](docs/architecture/querybuilder.md)

- QueryBuilder validation: empty or invalid values (e.g. text in a number field) are reported with
  a hint and nothing is sent. A result restriction without hits raises an ``InfoException``
  (with the service warnings); the dialog stays open with the entered conditions and the previous
  result is kept.

  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/search_identify/operations/results_desktop/results_desktop.html#results-query-restriction)

- Viewer animations via CSS transitions (first step towards removing jQuery from the API):
  ``webgis.effects.css_transitions.js`` overrides ``$.fn.slideUp/slideDown/slideToggle`` and
  ``fadeIn/fadeOut/fadeToggle`` with transient ``webgis-fx-*`` classes (``default.css``); the final
  state is the same as with jQuery (inline ``display:none``), call sites are unchanged.
  * Duration ``--webgis-fx-duration`` (250 ms, 0 with ``prefers-reduced-motion``); height animation
    via ``interpolate-size`` (Chromium only).
  * Opt-out ``webgis.usability.cssAnimations = false`` (default ``true``) restores the jQuery effects.
  * Remaining ``animate()``/``scrollTop()`` calls replaced by native ``scrollTo``/``scrollTop`` and a
    CSS transition (wizard tabs).

  [Docs](https://docs.webgiscloud.com/en/webgis/apps/viewer/customjs/usability.html#customjs-css-animations)

- Own autocomplete plugin ``webgis_autocomplete`` (``webgis.ui.plugins.autocomplete.js``) replaces
  twitter typeahead and jQuery UI autocomplete in the viewer API (next step towards removing
  external libs from ``webgis-api.min.js``).
  * Used by quick search/topbar, search forms, tool inputs (``input-autocomplete``), query combo,
    query builder, geosearch, smartmap and the MapBuilder.
  * Options ``source`` (url or function), ``minLength``, ``renderItem``, ``onSelect``, ``onEnter``,
    ... and methods ``val``, ``search``, ``open``, ``close``, ``focusedItem``, ``fitMenu``;
    fixed-positioned menu, keyboard navigation with label preview (Esc restores the query),
    per-instance cache, basic ARIA.
  * Removed ``_appendAutocomplete``/``_triggerAutocomplete``/``_autocompleteFitMenu`` from
    ``webgis.js``; ``registerUIEvents`` is a no-op kept for compatibility.
  * The typeahead script tags were removed from the viewer pages and templates
    (CMS, multiselect and REST pages still use typeahead).
  * **!! Breaking Change !!** CSS classes renamed: ``.twitter-typeahead``, ``.tt-menu``,
    ``.tt-suggestion``, ``.tt-cursor``, ... → ``.webgis-autocomplete-wrapper``,
    ``.webgis-autocomplete-menu``, ``.webgis-autocomplete-suggestion``,
    ``.webgis-autocomplete-cursor``, ...; custom stylings using the old classes must be adapted.

  [Docs](https://docs.webgiscloud.com/en/webgis-dev/plugins/smartmap-simple.html)

### Fixed

- CMS export: ``root.acl`` was read from a wrong path, so a root-level ACL was never applied.
- UI builder: ``UIConditionDiv`` (element value) with ``ConditionResult = false`` was not hidden
  when the value matched one of the condition arguments.
- Validation messages of tool parameters (incl. edit masks) were shown at the bottom of the dialog
  instead of directly below the input field (new ``.webgis-validation-holder`` wrapper).
- Edit theme combo/tree: the ``db_rights`` property was read as ``dbrights`` and therefore ignored,
  so e.g. the mobile insert/select tools listed edit themes without the required rights.
- UI setters: ``UISelectOptionsSetter`` crashed without a map in the options, the option container
  ``UICssSetter`` did not set the container value, and value setters on ``textarea`` only changed
  the default value.
- ``UIEmpty`` (close target) did not close dialogs opened with the targets ``modaldialog_hidden``,
  ``tool_modaldialog_hidden``, ``tool_modaldialog_noblocking`` and
  ``tool_modaldialog_noblocking_closable``; only their content was removed.
- Tool parameters with non-string values (e.g. ``UILineWieghtSelector``, ``UISymbolSelector`` used
  as tool parameters) caused ``val.replace is not a function``; they are now sent as string/JSON.
- Time filter and visibility filter UI plugins: removing one instance unsubscribed the map events
  of all other instances, refresh subscriptions of the filter lists were never released and
  optgroups of removed services stayed in the visibility filter combo.
- Toolbox (advanced tool behaviour, ``no-toolbox``): after closing a tool dialog with "x", the
  active tool was no longer highlighted (``selected``) in the toolbar, because the re-initialized
  toolbox did not register its ``onchangeactivetool`` handler again.
- User presentation: "Load values" (unique values) and "Calculate classes" ignored the display
  filter set for the layer (e.g. via the QueryBuilder) and returned the values of all features.
  * ``IBridge.QueryLayerDistinctAsync`` now always applies the locked (CMS) filters of the layer and,
    with ``applyRequestVisFilters: true``, also the display filters of the request.
  * ``UserRenderer`` is ``VisFilterDependent``; the renderer editor sends ``visfilter_dependent``
    for its ad-hoc tool calls, so the ``filters`` parameter is transmitted.

  [Docs manual](https://docs.webgiscloud.com/en/webgis-manual/mapviewer/presentation/index.html#toc-user-renderer),
  [Architecture](docs/architecture/querybuilder.md)
