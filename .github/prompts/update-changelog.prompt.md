---
mode: agent
description: Updates changelog.md with the current (unreleased) changes of the repo.
---

# Skill: Update Changelog

You help keep `changelog.md` in the repo root up to date. Always follow this order:

## 0. Determine current version

- Read the current version number from the class `E.Standard.Platform.WebGISVersion`
  (file: `src/NetStandard/E.Standard.Platform/WebGISVersion.cs`, field `_version`).
- This version is only used for context (e.g. to check whether the `Unreleased` section already
  contains content for the next version). Do **not** create a new version section and do not change
  the version number in `WebGISVersion.cs`, unless the user explicitly asks for it.

## 1. Review changes

- Identify the not-yet-documented changes, e.g. via:
  - `git diff` / `git log` since the last tag or since the last entry in `changelog.md`,
  - descriptions provided by the user, links to issues (`https://github.com/e-netze/webgis-community/issues/...`)
    or to the documentation (`https://docs.webgiscloud.com/...`).
- If the user provides issue or documentation links, match them to the corresponding changes and include them
  in the same format as existing entries (e.g. `[Issue #123](https://github.com/e-netze/webgis-community/issues/123)`).
- If it is unclear what a change refers to, or whether it is a bugfix or a feature, ask the user instead of guessing.

## 2. Summarize briefly

- Summarize each change in 1-3 concise lines, in the style of the existing entries in `changelog.md`
  (short title, optionally with bullet points using `*`/`-` underneath for details).
- If multiple independent topics are included, create a separate entry for each topic.
- For breaking changes: additionally mark the entry with `**!! Breaking Change !!**` and a short
  explanation of how it affects existing configurations/stylings (see example in the current
  `Unreleased` section).

## 3. Add to the "Unreleased" section

- Open `changelog.md` and find the `## Unreleased` section (at the very top of the document).
- This section has the subsections `### Added` and `### Fixed`. If a subsection is missing, create it
  in this order (`Added` before `Fixed`).
- Categorize each change:
  - **Fixed**: if it is clearly recognizable as a bugfix (e.g. description like
    "Bug", "Error", "does not work", "crash", etc.).
  - **Added**: in all other cases (new features, improvements, configuration options) and always
    when it is not clearly recognizable as a bugfix.
- Add new entries at the end of the respective subsection (after existing entries of the same
  Unreleased section), to preserve the chronological order within the section.
- Never modify already published version sections (e.g. `## 8.26.3101`).

## 4. Formatting conventions

- Use `## Unreleased`, `### Added`, `### Fixed` exactly as in existing sections (no emojis,
  no different casing).
- Keep the language consistent with the surrounding entry context (title/short description is mostly
  German or English depending on the existing style in the changelog - follow the style of the
  immediately surrounding text).
- Always add links as markdown links `[Text](URL)`, each on its own line indented under the
  main entry, as shown in the existing examples.

## 5. Wrap-up

- Show the user the newly added lines for review before reporting the task as complete.
- Do not make any other changes to other files, unless the user explicitly asks for it.
