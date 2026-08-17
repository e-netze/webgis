---
mode: agent
description: Adds a new configurable client-side usability option (webgis.usability / webgis.queryResultOptions) that admins can override in custom.js.
---

# Skill: Add a Client-Side Usability Option

Use this skill whenever new client-side behavior needs a configurable knob that a customer/admin
can override in their `custom.js`, instead of a hardcoded constant in the viewer JS.

## 0. Find the right options object

- Defaults live in `src/NetCore/Web/Api/wwwroot/scripts/api/webgis.options.js`, mainly in
  `webgis.usability = { ... }` (general viewer behavior) or `webgis.queryResultOptions = { ... }`
  (query-result-specific display options). Pick whichever object already hosts conceptually similar
  settings (e.g. paging/result-list options belong next to
  `webgis.usability.queryResultsTable`/`queryResultsList`).
- Group related settings under a nested object rather than adding flat top-level keys, e.g.:

  ```js
  queryResultsList: {
      maxItems: 1000 // short comment explaining the default and behavior
  }
  ```

## 1. Read the option defensively in the consuming code

- Always guard against the option (or its parent object) being absent/overridden incompletely by a
  customer's `custom.js`, e.g.:

  ```js
  var maxListItems = (webgis.usability.queryResultsList && webgis.usability.queryResultsList.maxItems) || 1000;
  ```

- Never assume a nested options object exists in full — customers may only override a subset of
  keys or an older `custom.js` may predate the new option entirely.

## 2. Document the option

- Add a short inline comment next to the default value in `webgis.options.js` explaining what it
  does and its default (this file effectively doubles as the source-of-truth reference).
- The public docs for these options live at
  `https://docs.webgiscloud.com/de/webgis/apps/viewer/customjs/usability.html#<anchor>` — if the
  user supplies this link, use it in the changelog entry; do not edit the external docs site itself
  unless explicitly asked.

## 3. Verify

- `node --check` every touched JS file.
- Confirm the feature still works with the option entirely absent (simulating an older
  `custom.js`), not just with the new default in place.

## 4. Changelog

- Add an entry under `## Unreleased` / `### Added` in `changelog.md` naming the exact option path
  (e.g. `webgis.usability.queryResultsList.maxItems`), its default, and the docs link from step 2.
