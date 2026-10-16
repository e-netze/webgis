# Architecture / Developer Documentation

Developer-oriented documentation of WebGIS features and subsystems: how they work technically,
which projects and classes are involved, flow diagrams, design decisions and pitfalls.

- For **what** changed per version see the changelogs ([V9](../../changelog-v9.md), [V8](../../changelog-v8.md), [V7](../../changelog-v7.md)).
- For admin-oriented summaries see the release notes ([V9](../../release-notes-v9.md), [V8](../../release-notes-v8.md), [V7](../../release-notes-v7.md)).
- New docs are created from [_template.md](_template.md) via the
  [update-architecture-docs](../../.github/skills/update-architecture-docs/SKILL.md) skill.

## Features

| Feature | Area | Description |
|---------|------|-------------|
| [System Overview](system-overview.md) | All | Start here: applications, how they interact, project layout, configuration, deployment. |
| [AGS Query Strategy](ags-query-strategy.md) | API / GeoServices | Workaround for incomplete ArcGIS Server spatial query results (bounding-box pre-filter bug). |
| [API JavaScript & CSS Bundles](api-javascript-bundles.md) | API / Viewer | Viewer script/style bundles: core/UI bundles and the single bundle `webgis-api.min.js`/`.min.css`. |
| [Authentication & Roles](authentication-roles.md) | Portal / API / CMS | Login methods, roles and role parameters, Portal-API HMAC, CMS item authorization. |
| [CMS Deployment](cms-deployment.md) | CMS / API / Portal | Main deploy, branch deploys (API discovery, branch selection), fast deploy file snapshot, single-pass export. |
| [CMS Git Support](cms-git.md) | CMS | Optional git versioning per cms-item: user working copies, branches, push/pull, merge and conflict resolution. |
| [Detachable Windows](detachable-windows.md) | Viewer | `webgis.windowManager`: detach toolbar/query results into a Document Picture-in-Picture window, jQuery hooks, restore state. |
| [Expressions & AutoValues](expressions-autovalues.md) | API / Editing / CMS | Expression language (parser, syntax detection), Editing AutoValues, table column expressions. |
| [Logging & Observability](logging-observability.md) | API / CMS / Portal | Logging backends and fan-out, DB batch logging, OpenTelemetry, Serilog. |
| [Query Results](query-results.md) | API / Viewer | Query results end-to-end: QueryEngine, DTO metadata, result table/list paging, export. |
| [TOC Context Menu & Properties](toc-context-menu.md) | Viewer / Portal | TOC item/group context menu (provider list, hover submenus), Query Builder per layer, properties dialog. |
| [Tool Events](tool-events.md) | API / Viewer | `/rest/toolevent` request: client parameter collection, JSON `eventBody`, server-side parsing into `ApiToolEventArguments`. |
| [UI Builder](ui-builder.md) | API / Viewer | Server UI elements to viewer DOM: webgis_uibuilder, builder plugin registry, characterization tests. |
| [User Renderer & User Labeling](user-renderer-labeling.md) | API / GeoServices / Viewer | User-defined layer presentation and labeling from the TOC via AGS dynamicLayers: model, validation, translators, editors, badges. |
