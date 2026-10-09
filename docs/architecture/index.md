# Architecture / Developer Documentation

Developer-oriented documentation of WebGIS features and subsystems: how they work technically,
which projects and classes are involved, flow diagrams, design decisions and pitfalls.

- For **what** changed per version see the changelogs ([V8](../../changelog-v8.md), [V7](../../changelog-v7.md)).
- For admin-oriented summaries see the release notes ([V8](../../release-notes-v8.md), [V7](../../release-notes-v7.md)).
- New docs are created from [_template.md](_template.md) via the
  [update-architecture-docs](../../.github/skills/update-architecture-docs/SKILL.md) skill.

## Features

| Feature | Area | Description |
|---------|------|-------------|
| [System Overview](system-overview.md) | All | Start here: applications, how they interact, project layout, configuration, deployment. |
| [AGS Query Strategy](ags-query-strategy.md) | API / GeoServices | Workaround for incomplete ArcGIS Server spatial query results (bounding-box pre-filter bug). |
| [Authentication & Roles](authentication-roles.md) | Portal / API / CMS | Login methods, roles and role parameters, Portal-API HMAC, CMS item authorization. |
| [Expressions & AutoValues](expressions-autovalues.md) | API / Editing / CMS | Expression language (parser, syntax detection), Editing AutoValues, table column expressions. |
| [Logging & Observability](logging-observability.md) | API / CMS / Portal | Logging backends and fan-out, DB batch logging, OpenTelemetry, Serilog. |
| [Query Results](query-results.md) | API / Viewer | Query results end-to-end: QueryEngine, DTO metadata, result table/list paging, export. |
