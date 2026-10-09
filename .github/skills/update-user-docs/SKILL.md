---
name: update-user-docs
description: Updates the admin/user documentation in the sibling repo webgis-docs (Sphinx, German + English) for admin- or user-relevant changes such as new api.config/portal.config/cms.config keys, custom.js options, CMS properties or visible tool behavior. Use when such a change is done (before update-changelog), or when the user asks to document changes in the docs/webgis-docs.
---

# Skill: Update User/Admin Docs (webgis-docs)

The public documentation (`https://docs.webgiscloud.com/...`) is a separate Sphinx repo,
`webgis-docs`, checked out next to this repo. This skill writes the **source** (`.rst`) changes
there for the current changes of this repo. Building, committing, merging to `master` and
publishing stay with the user.

Order with the other skills: **this skill first, then `update-changelog`**, so the docs URLs are
known when the changelog/release-notes entries are written.

## 0. Locate the docs repo and the branch

- Docs repo: `..\webgis-docs` relative to the root of this repo. If it does not exist, ask the
  user for the path; if none is given, stop.
- Determine the major version from `WebGISVersion._version`
  (`src/NetStandard/E.Standard.Platform/WebGISVersion.cs`, e.g. `8` in `8.26.4102`) and the full
  version (used in step 3).
- Target branch in `webgis-docs`: `webgis{major}` (e.g. `webgis8`; same convention as the existing
  `webgis5`/`webgis6` branches). Never work on `master` - the user decides what gets merged to
  `master` and published, so docs for features of a not-yet-released major are never published by
  accident.
- Check the state of the docs repo (`git -C ..\webgis-docs status --short` and current branch):
  - Uncommitted changes → **stop** and ask the user. Never stash, reset or discard.
  - Clean and already on `webgis{major}` → continue.
  - Clean and on another branch → ask the user before switching.
  - Branch `webgis{major}` does not exist (locally or on `origin`) → create it locally from
    `master` (`git switch -c webgis{major} master`, or track `origin/webgis{major}` if only the
    remote exists) and tell the user. Never push.

## 1. Decide what is docs-relevant

Use the same relevance filter as the release notes (`update-changelog` skill, section 3.1,
`New`/`Upgrade notes`):

- **document**: new/changed keys in `api.config`, `portal.config`, `cms.config`
  (and `appsettings.json` where documented), `custom.js` usability options, new CMS properties,
  new or changed tool/viewer behavior visible to admins or users, breaking changes and required
  upgrade steps.
- **skip**: pure bug fixes (behavior now matches the docs), refactorings, internals, developer
  tooling.
- Identify the changes from `git diff`/`git log` of this repo and from the user's description. If it
  is unclear whether something is relevant or what it does, ask instead of guessing.
- Only document current/unreleased changes. No backfill of older versions unless the user asks.

## 2. Find the right place

Doc projects (each has `de/<project>/source` and `en/<project>/source`):

| Project | Audience | Typical content |
|---------|----------|-----------------|
| `webgis` | admins | `config/api`, `config/cms`, `config/portal`, `config/hardening`, `extended_config/`, `apps/viewer/customjs/` (e.g. `usability.rst`), `apps/cms/`, `annex/` |
| `webgis-manual` | end users (online help in the viewer) | `mapviewer/` (tools, search_identify, presentation, ...) |
| `webgis-dev` | developers | only for changes of the public JS/REST API |

- `webgis-cloud` is **legacy - never touch it**, even though it contains similar config pages.
- Search the existing pages (`grep` for the key, option or feature name) and **extend the existing
  page/table/section** where the topic already lives, e.g. add a row to the matching `list-table`
  in `config/api/index.rst`. Follow the style of the neighboring entries exactly.
- A **new page** (incl. `toctree` entry in the directory's `index.rst`) only for a genuinely new,
  standalone topic - and only after asking the user.

## 3. Write German first, then English

- German (`de/...`) is the leading language; write it first.
- Then apply the equivalent change to the same relative path under `en/...` in the same pass. For
  *how* to translate (what not to translate, exact indentation, heading underlines, file parity),
  follow `..\webgis-docs\.claude\skills\translate-sphinx-docs\SKILL.md` - do not invent different
  rules here.
- Mark new and changed items with the full current version from step 0:
  - new key/option/feature: `.. versionadded:: 8.26.4102`
  - changed behavior/default: `.. versionchanged:: 8.26.4102` plus one sentence what changed.
  - Inside a `list-table` cell put the directive in the cell body (indented like the cell text);
    if that is awkward, add a short sentence "Ab Version 8.26.4102." / "Since version 8.26.4102."
    instead.
- Stable anchors: for every new section or entry that will be linked from the changelog, add an
  explicit label directly above the heading, in the existing style `.. _<area>-<topic>:` (kebab-case,
  e.g. `.. _api-config-tool-identify:`). Use the **same label in DE and EN**, because heading-derived
  anchors differ between the languages. Labels must be unique within the project (grep first).
- Copy new images to the same relative path in both language trees.

## 4. Verify

Run from `..\webgis-docs` for every project you touched:

```bash
python .claude/skills/translate-sphinx-docs/scripts/fix_rst_headings.py de/PROJECT/source en/PROJECT/source
python .claude/skills/translate-sphinx-docs/scripts/check_parity.py de/PROJECT/source en/PROJECT/source
```

`check_parity.py` must report `OK`.

Optionally (if `sphinx-build` is available, e.g. via the repo's `.venv`), build only the touched
project into a temp folder and check for new warnings/errors:

```powershell
sphinx-build -b html -E de\PROJECT\source "$env:TEMP\webgis-docs-check\de-PROJECT" 2>&1 | Select-String -Pattern "warning|error|critical"
```

- **Never** run `build.bat` and never modify `app/` on the `webgis{major}` branch - `app/` is only
  rebuilt on `master` when the user publishes (otherwise generated HTML causes merge conflicts).
- Do not leave a `build/` folder inside the docs repo.

## 5. Hand over

- Do **not** commit, merge or push in `webgis-docs`. Propose a one-sentence commit message
  (see `commit` skill), e.g. "Document tool-identify api.config keys (DE+EN)".
- Report the resulting docs URLs, built from path + label, using the **English** variant:
  `https://docs.webgiscloud.com/en/<project>/<path>.html#<label>`
  (e.g. `https://docs.webgiscloud.com/en/webgis/config/api/index.html#api-config-tool-identify`).
  These URLs only work after the user has merged to `master` and deployed - list them so the user
  can check them after the deploy.
- Then continue with the `update-changelog` skill and use these `/en/` URLs as `[Docs](...)` links
  in both the changelog and the release notes entry.
- Remind the user that publishing is done via `master` (merge, then the docs repo's
  `sync-and-deploy-docs` skill / `build.bat`, then push).

## Pitfalls

- The docs repo's own skills live in `..\webgis-docs\.claude\skills\` (`translate-sphinx-docs`,
  `sync-and-deploy-docs`); read them instead of duplicating their rules.
- The docs site has only one version: without `versionadded`/`versionchanged`, admins of an older
  release cannot tell that an option does not exist in their version yet.
- Windows/NTFS is case-insensitive, git paths are not: keep the exact case of existing directories
  (see the case-only path pitfall in `sync-and-deploy-docs`).
- Do not "fix" unrelated content of the pages you touch; keep the diff minimal and in both
  languages.
