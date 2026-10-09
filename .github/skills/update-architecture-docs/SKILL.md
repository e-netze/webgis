---
name: update-architecture-docs
description: Creates or updates developer-oriented architecture docs (docs/architecture/<feature>.md) for a feature or subsystem. Use after implementing or changing a feature/subsystem, or when asked to document architecture.
---

# Skill: Update Architecture Docs

Developer-oriented "living" documentation of features/subsystems lives in `docs/architecture/`:

- `docs/architecture/index.md` - overview table of all feature docs.
- `docs/architecture/_template.md` - template for new docs (section structure is mandatory).
- `docs/architecture/<feature-name>.md` - one doc per feature/subsystem (kebab-case), describing the
  **current** state. It is updated when the feature changes - never create a second doc for an extension.

Reference example: `docs/architecture/ags-query-strategy.md`.

Always follow this order:

## 0. Decide whether a doc is needed

- Docs are written on **feature/subsystem level** (e.g. "CMS Deployment", "CMS Git Tools",
  "AGS Query Strategy", "Logging").
- **Not** for bugfixes, small config options or cosmetic changes.
- If the change extends an existing feature, update its existing doc (check `index.md` first).
- If unsure whether the change is "big enough" or which doc it belongs to, ask the user.

## 1. Gather facts from the code

- Determine the changed/relevant code via `git diff`/`git log`, the current session's work, or the
  feature name given by the user.
- Trace the feature through the code (entry point → services → persistence/external calls → client).
  Collect: affected `.csproj` projects, key classes/files, config keys (`api.config`, `portal.config`,
  `appsettings.json`, `custom.js`, CMS properties), client-side JS involved.
- Use existing code comments (XML docs, remarks) as a source for design decisions and pitfalls.
- If the "why" of a decision is not visible in code/commits/session, ask the user instead of inventing it.

## 2. Write / update the doc

- New doc: copy the structure of `_template.md` exactly (same headings, same order), file name kebab-case.
- Sections:
  1. **Overview** - purpose, 2-3 sentences.
  2. **Affected projects** - table `Project | Role` (project = `.csproj` name without extension).
  3. **Key classes / files** - table `Class / file | Responsibility`, with repo-relative paths
     (`.../` abbreviation allowed for a path already given in full above).
  4. **Flow** - Mermaid diagram(s): `flowchart` for decision/process flows, `sequenceDiagram` for
     request flows between client/API/CMS/external services, `classDiagram` only if it helps.
     Avoid `<`/`>` in labels (write "below"/"above"), keep diagrams readable (max. ~25 nodes, split otherwise).
  5. **Configuration** - table `Setting | Where | Default | Description`, plus link to user docs
     (`https://docs.webgiscloud.com/...`) if available.
  6. **Design decisions** - why it is built this way, rejected alternatives.
  7. **Pitfalls / things to watch** - gotchas, side effects, invariants, how to test.
  8. **Extension points** - optional, only if meaningful.
  9. **History** - table `Version | Change | Reference`; append one row per change (version from
     `E.Standard.Platform.WebGISVersion._version`, one line, PR link or short commit hash).
     Never rewrite existing history rows.
- When updating: bring all other sections to the **current** state (remove what no longer exists);
  only *History* is append-only.
- Language: English. No implementation-level noise (no full method bodies); explain, don't copy code.

## 3. Verify against the code (mandatory)

- Every class, file path, project, config key and method named in the doc must exist: verify each one
  with a search (glob/grep) before saving. Fix or remove anything that doesn't exist.
- Defaults in the *Configuration* table must match the code.

## 4. Link it

- Add/update the row in `docs/architecture/index.md` (`Feature | Area | Description`, sorted
  alphabetically; `System Overview` always stays the first row).
- If the change also gets a changelog entry (see `update-changelog` skill), add the line
  `[Architecture](docs/architecture/<feature-name>.md)` to that changelog entry.
  Release notes do **not** link architecture docs (admin audience).

## 5. Wrap-up

- Show the user the new/changed doc sections for review before reporting the task as complete.
- Do not commit (see `commit` skill), and do not change other files unless the user asks for it.
