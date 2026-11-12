---
name: add-api-config-setting
description: Makes a hardcoded constant/setting configurable via api.config using the options pattern (XxxOptions + IConfiguration bind extension + services.Configure in Startup + IOptions<T> DI). Use when a hardcoded value should become an api.config setting.
---

# Skill: Add an api.config Setting

Use this skill whenever a hardcoded constant (e.g. a static class with fixed default values) should
become configurable per-instance via `api.config`.

## Preferred: options pattern (all new settings)

**Do not create new static settings classes** (especially not under `/Services`). For new settings
use **options class (compiled defaults) → `api.config` key → `IConfiguration` bind extension →
`services.Configure<T>` in `Startup.cs` → `IOptions<T>` via DI**. Reference implementation:
`SubscriberSecurityOptions`.

1. **Options class**: plain (non-static) class `XxxOptions` with defaults as property initializers,
   e.g. `src/NetStandard/E.Standard.Api.App/Configuration/SubscriberSecurityOptions.cs`. Real
   constants (not configurable) may stay `const`. Add a `<remarks>` block listing the `api.config`
   keys → properties.
2. **Keys**: add constants to `src/NetStandard/E.Standard.Api.App/Configuration/ApiConfigKeys.cs`
   (e.g. `SecurityPasswordMinLength = ConfigurationSectionName + ":security:password-min-length"`)
   and the key with its default + comment to `src/NetCore/Web/Api/_setup/proto/_api.config`
   (see step 1 below).
3. **Bind extension**: add `static public void BindXxxOptions(this IConfiguration config, XxxOptions options)`
   to `src/NetStandard/E.Standard.Api.App/Extensions/ConfigurationServiceExtensions.cs`. Only
   override a default when parsing succeeds (`int.TryParse`, `Enum.TryParse(..., true, ...)`, ...);
   keys are never mandatory.
4. **Register** in `src/NetCore/Web/Api/Startup.cs` (fitting `#region`, e.g. `Security`):

   ```csharp
   services.Configure<XxxOptions>(Configuration.BindXxxOptions);
   ```

5. **Consume** via DI: constructor parameter `IOptions<XxxOptions> options` → `_options = options.Value`.
   Razor views: `@inject Microsoft.Extensions.Options.IOptions<XxxOptions> XxxOptions`.
   Static helpers/extension methods get the options passed in as a parameter (no static state).
6. Do **not** read these keys in `ApiGlobalsService`.

Then continue with steps 4–6 below (docs, build, changelog).

## Legacy: existing static settings classes

Only for **already existing** static settings classes whose consumers have no DI access (e.g.
`AgsQuerySettings` deep in GeoServices). Do not use this for new settings. The legacy plumbing is:
**static settings class (compiled defaults) → `api.config` key → `ApiGlobalsService` read →
override the default at startup**.

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
- Propose documenting the new keys there via the `update-user-docs` skill
  (`.github/skills/update-user-docs/SKILL.md`, edits the sibling `webgis-docs` repo, DE + EN) and
  use the resulting `/en/` docs URL in the changelog entry (see step 6).

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

- Follow the `update-changelog` skill (`.github/skills/update-changelog/SKILL.md`) to add an
  `Added` entry to the changelog and a `New` entry to the release notes of the current major
  version, listing the new key names, their defaults, and the `api.config` docs link from step 4.
