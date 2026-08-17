---
mode: agent
description: Makes a hardcoded constant/setting configurable via api.config, following the existing tool-identify pattern.
---

# Skill: Add an api.config Setting

Use this skill whenever a hardcoded constant (e.g. a static class with fixed default values) should
become configurable per-instance via `api.config`.

This is a recurring, simple plumbing pattern in this repo: **settings class (compiled defaults) →
`api.config` key → `ApiGlobalsService` read → override the default at startup**.

## 0. Locate the settings class

- Find the static class holding the constant(s) as `public static` properties with compiled-in
  default values (e.g. `AgsQuerySettings.cs` in
  `src/NetStandard/E.Standard.WebMapping.GeoServices/ArcServer/`).
- Note the type of each value (`int`, `bool`, `string`, ...) — this determines the parsing method
  used in step 2.

## 1. Add the config key(s) to the proto config

- Open `src/NetCore/Web/Api/_setup/proto/_api.config`.
- Add the new key(s) under the appropriate existing section (e.g. `tool-identify`). Pick the
  section based on where the setting is conceptually used, not where the class physically lives.
- Use kebab-case key names, e.g. `ags-spatial-query-max-result-cap`.
- Include the default value as the example value in the proto file (so the documented default and
  the compiled-in default stay in sync), optionally with a short inline comment.

## 2. Read the key in ApiGlobalsService

- Open `src/NetCore/Web/Api/AppCode/Services/ApiGlobalsService.cs`.
- Add a `using` for the settings class's namespace if not already present.
- Add a parse block that only overrides the compiled-in default when parsing succeeds, following
  the exact pattern of neighboring keys in the same section, e.g. for an `int`:

  ```csharp
  if (int.TryParse(config[ApiConfigKeys.ToKey("tool-identify:ags-spatial-query-max-result-cap")], out int agsMaxResultCap))
  {
      AgsQuerySettings.MaxSpatialQueryResultCap = agsMaxResultCap;
  }
  ```

  For `bool`/`string` settings, use `bool.TryParse`/a plain non-empty check respectively — copy the
  pattern of an existing key of the same CLR type in the same file rather than inventing a new one.
- Never make the config key mandatory: absence must fall back silently to the compiled-in default.

## 3. Document the setting on the class itself

- Add or extend an XML-doc `<remarks>` block on the settings class listing the new `api.config`
  keys and which property they map to, so the mapping is discoverable directly from the code.

## 4. Reference the docs site

- The public documentation for `api.config` sections lives at
  `https://docs.webgiscloud.com/de/webgis/config/api/index.html#<section-anchor>`
  (e.g. `#werkzeug-identify` for the `tool-identify` section).
- If the user provides the docs link, use it in the changelog entry (see step 6). Do not attempt to
  edit the external docs site itself unless the user explicitly asks and gives access/instructions
  for the docs repo.

## 5. Verify

- Build only the affected `.csproj`s (the settings class's project and
  `src/NetCore/Web/Api/webgis-api.csproj`) individually rather than the whole solution.
- **Known build quirk in this repo**: if a dev-server instance for `webgis-api` is already running
  against the same in-place checkout, `dotnet build webgis-api.csproj` fails with
  `MSB3027`/`MSB3021` "file locked" errors because both share the same `bin/` output folder. This is
  **not** a compile error. Work around it by building to an alternate output path instead of asking
  the user to stop their dev server:

  ```powershell
  dotnet build src\NetCore\Web\Api\webgis-api.csproj -p:BaseOutputPath="$env:TEMP\webgis-api-build\"
  ```

  Confirm success by checking for `0 Error(s)`/no `error CS` lines in the output, not just the exit
  code.

## 6. Changelog

- Add an entry under `## Unreleased` / `### Added` in `changelog.md` (see the
  `update-changelog` skill), listing the new key names, their defaults, and the `api.config` docs
  link from step 4.
