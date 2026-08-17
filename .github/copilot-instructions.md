# Copilot Instructions for this repo

These are standing working agreements for AI coding agents (Copilot CLI/Chat) in this repo, in
addition to the task-specific skills in `.github/prompts/*.prompt.md`.

## Commit policy

- **Do not commit automatically** after implementing a change, even if it builds and looks done.
  Only commit when the user explicitly confirms the task is finished (e.g. "fertig", "das kann
  eingecheckt werden", "commit"). Report what changed and that it's ready, then wait.
- This applies per logical change/task, not just once per session — after every new implemented
  change, wait for a fresh confirmation before committing it.

## Build verification

- Prefer building only the individual `.csproj`(s) you actually touched over a full solution
  build — it's faster and avoids unrelated pre-existing issues.
- If a `dotnet build` against `src/NetCore/Web/Api/webgis-api.csproj` (or another project with a
  running dev-server instance) fails with `MSB3027`/`MSB3021` "file locked" errors, this usually
  means the user's own dev server is running against the same in-place checkout and locking the
  shared `bin/` output — it is **not** a compile error. Do not kill that process without explicit
  permission. Instead, build to an alternate output path to verify compilation:

  ```powershell
  dotnet build <project>.csproj -p:BaseOutputPath="$env:TEMP\<some-name>\"
  ```

  Confirm success by checking for `0 Error(s)` / absence of `error CS` lines, not just exit code.

## User-facing strings (l10n)

- Never hardcode user-facing text in viewer JS. Add a key to both
  `src/NetCore/Web/Api/wwwroot/scripts/api/webgis.l10n.de.js` and `webgis.l10n.en.js`, and read it
  via `webgis.l10n.get('key')`.

## Changelog

- When a task is confirmed done, consider whether it needs a `changelog.md` entry — follow
  `.github/prompts/update-changelog.prompt.md`. Ask the user for/insert relevant docs links
  (`https://docs.webgiscloud.com/...`) and GitHub issue/discussion links
  (`https://github.com/e-netze/webgis-community/...`) if provided.

## Recurring patterns with dedicated skills

Check `.github/prompts/` before implementing a change that might match one of these recurring
patterns, so no step gets silently skipped:

- **New `api.config` setting** for a hardcoded constant → `add-api-config-setting.prompt.md`.
- **New CMS-configurable property** flowing CMS Schema → DTO → runtime model →
  `extend-cms-schema-property.prompt.md`.
- **Exposing a new server-side query/FeatureCollection signal to the client**
  (e.g. a new flag on `FeatureCollection` that the UI should react to) →
  `expose-query-metadata-to-client.prompt.md` (has a known gotcha around
  `RestHelperService.PrepareFeatureCollection` silently dropping un-copied fields).
- **New client-side usability option** overridable via `custom.js` →
  `add-client-usability-option.prompt.md`.
