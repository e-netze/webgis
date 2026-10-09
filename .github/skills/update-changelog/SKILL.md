---
mode: agent
description: Updates the changelog and the release notes of the current major version with the current (unreleased) changes of the repo.
---

# Skill: Update Changelog & Release Notes

Changes are documented per major version in two files in the repo root:

| File | Audience | Content |
|------|----------|---------|
| `changelog-v{major}.md` | developers | detailed, technical description of every notable change (source of truth) |
| `release-notes-v{major}.md` | admins / CMS authors installing the software | compact summary of only the admin-relevant changes, condensed from the changelog |

This skill always keeps **both** files in sync. Always follow this order:

## 0. Determine current version and target files

- Read the current version number from the class `E.Standard.Platform.WebGISVersion`
  (file: `src/NetStandard/E.Standard.Platform/WebGISVersion.cs`, field `_version`).
- The major version (first number, e.g. `8` in `8.26.4102`) selects the target files:
  `changelog-v8.md` and `release-notes-v8.md`.
- If the files for this major version do not exist yet (new major version), create them with the
  same header as the files of the previous major version (adjusted major number, cross-links to
  each other and to the previous major's release notes) and an empty `## Unreleased` section.
  Also add the new major to the table in `README.md` (section "Release Notes & Changelog").
- The version is otherwise only used for context. Do **not** create a new version section and do not
  change the version number in `WebGISVersion.cs`, unless the user explicitly asks for it.
- Versions encode the release date (`Major.{Year-2000}.{CalendarWeek}{Build}`), so no dates are
  written to either file.

## 1. Review changes

- Identify the not-yet-documented changes, e.g. via:
  - `git diff` / `git log` since the last tag or since the last entry in the changelog,
  - descriptions provided by the user, links to issues/discussions
    (`https://github.com/e-netze/webgis-community/issues/...`) or to the documentation
    (`https://docs.webgiscloud.com/...`).
- If the user provides issue or documentation links, match them to the corresponding changes and include them
  in the same format as existing entries (e.g. `[Issue #123](https://github.com/e-netze/webgis-community/issues/123)`).
- If it is unclear what a change refers to, or whether it is a bugfix or a feature, ask the user instead of guessing.

## 2. Update the changelog (`changelog-v{major}.md`)

### 2.1 Summarize

- Summarize each change in 1-3 concise lines, in the style of the existing entries
  (short title, optionally with bullet points using `*`/`-` underneath for details).
- If multiple independent topics are included, create a separate entry for each topic.
- For breaking changes: additionally mark the entry with `**!! Breaking Change !!**` and a short
  explanation of how it affects existing configurations/stylings.

### 2.2 Add to the "Unreleased" section

- Find the `## Unreleased` section (at the very top, below the header).
- This section has the subsections `### Added` and `### Fixed`. If a subsection is missing, create it
  in this order (`Added` before `Fixed`).
- Categorize each change:
  - **Fixed**: if it is clearly recognizable as a bugfix (e.g. description like
    "Bug", "Error", "does not work", "crash", etc.).
  - **Added**: in all other cases (new features, improvements, configuration options) and always
    when it is not clearly recognizable as a bugfix.
- Add new entries at the end of the respective subsection, to preserve the chronological order.
- If an architecture doc exists for the feature (`docs/architecture/<feature-name>.md`, see
  `update-architecture-docs.prompt.md`), add the line
  `[Architecture](docs/architecture/<feature-name>.md)` to the entry.
- Never modify already published version sections (e.g. `## 8.26.3101`).

## 3. Update the release notes (`release-notes-v{major}.md`)

The release notes are condensed **from the changelog entries of step 2** - never add something to the
release notes that is not in the changelog.

### 3.1 Relevance filter

Include only what matters to someone installing/configuring WebGIS:

- **include**: resolved community issues/discussions, new features, new configuration options
  (`api.config`, `portal.config`, `appsettings.json`, `custom.js`, CMS properties), breaking changes
  and required upgrade steps, security-relevant fixes, user-visible bug fixes.
- **exclude**: internal refactorings, code structure, performance internals, implementation details,
  pure developer tooling.
- If several changelog entries belong to the same topic (e.g. several logging entries), merge them
  into one release-notes entry.

### 3.2 Structure

- Same `## Unreleased` section at the top, with the subsections in this order (create missing ones):
  - `### Upgrade notes` - breaking changes and required admin actions (e.g. "API and CMS must be
    updated together", "run setup"). Start breaking changes with `**Breaking change:**`.
  - `### New` - corresponds to changelog `Added`.
  - `### Fixed` - corresponds to changelog `Fixed`.
- Keep empty subsections in `Unreleased` as placeholders. In published versions, omit empty subsections.

### 3.3 Entry format

- One sentence per entry; optionally a second sentence only if an admin action is required
  (e.g. "Set `QueryStrategy = BoundingBoxProblem` in the CMS").
- Name exact config keys/option paths in backticks, no implementation details.
- Links (issue/discussion/docs) each on its own line, indented under the entry, as Markdown links
  `[Issue #123](...)`, `[Discussion #123](...)`, `[Docs](...)`. Links are optional - relevance decides,
  not the existence of a link.
- Language: English.

## 4. Releasing a version (only if the user explicitly asks)

- Rename `## Unreleased` to `## {version}` in **both** files, remove empty subsections from the new
  version section, and insert a fresh, empty `## Unreleased` section above it
  (changelog: `### Added`/`### Fixed`; release notes: `### Upgrade notes`/`### New`/`### Fixed`).
- If a version has no admin-relevant changes, the release notes still get the version section with
  the single line `Maintenance release, no admin-relevant changes.` (so admins see a gap-free version list).

## 5. Formatting conventions

- Use the section headings exactly as in existing sections (no emojis, no different casing).
- Never modify published version sections in either file, unless the user explicitly asks for it.

## 6. Wrap-up

- Show the user the newly added lines of both files for review before reporting the task as complete.
- Do not make any other changes to other files, unless the user explicitly asks for it.
