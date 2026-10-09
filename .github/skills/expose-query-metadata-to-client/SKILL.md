---
mode: agent
description: Exposes a new server-side query/feature-collection signal (flag, count, ...) to the client via FeaturesDTO.Meta, so client-side JS can react to it.
---

# Skill: Expose Query/Feature-Collection Metadata to the Client

Use this skill whenever a signal that already exists (or is newly added) on
`E.Standard.WebMapping.Core.Collections.FeatureCollection` (e.g. `HasMore`, `HasAttachments`,
`Warnings`) needs to reach the WebGIS viewer client so the UI can react to it (show a notice,
change behavior, ...).

This is a recurring pattern in this repo: **`FeatureCollection` flag (set by query/layer logic) →
`RestHelperService.PrepareFeatureCollection` (re-created collection sent to the client) →
`FeaturesDTO.Meta` (serialized JSON) → client JS reads `features.metadata.<name>`**. Skipping the
middle step is a common, silent bug — see the gotcha in step 2.

## 0. Confirm/add the flag on FeatureCollection

- Check `src/NetStandard/E.Standard.WebMapping.Core/Collections/FeatureCollection.cs` for the
  property (e.g. `HasMore`, `MaximumReached`, `Warnings`/`AddWarning(...)`,
  `Informations`/`AddInformation(...)`). Add a new property here if it doesn't exist yet.
- Set the flag where the actual query/paging logic determines its value — e.g.
  `E.Standard.Api.App/QueryEngine.cs` (`PerformAsync`, the batch-fetch loop already tracks
  `this.HasMore` correctly: it is `true` only when the loop stopped because the configured result
  limit was hit while more data was genuinely still available, `false` when all matches were
  actually retrieved) or a specific layer implementation (e.g.
  `ArcServer/Rest/FeatureLayer.cs`, `AXL/FeatureLayer.cs`).

## 1. CRITICAL GOTCHA: PrepareFeatureCollection re-creates the collection

- `src/NetCore/Web/Api/AppCode/Services/Rest/RestHelperService.cs`,
  `PrepareFeatureCollection(...)`, builds a **brand-new** `FeatureCollection` (`returnFeatures`)
  that is what actually gets sent to the client — it does **not** automatically copy over
  arbitrary properties from the source `queryFeatures`.
- Existing signals (`HasAttachments`, `Warnings`, `Informations`) are copied over **explicitly**,
  each in its own `#region` block near the end of the method. **Any new flag must be added there
  too**, e.g.:

  ```csharp
  #region HasMore
  returnFeatures.HasMore = queryFeatures.HasMore;
  #endregion
  ```

  Forgetting this step means the flag is set correctly on the server but silently never reaches
  the client — verify with an actual network response, not just a green build (see step 5).
- Also check `ExportGeoFeaturesService.cs` and other call sites of `PrepareFeatureCollection` if
  the new flag should also be honored there.

## 2. Add the property to FeaturesDTO.Meta

- Open `src/NetStandard/E.Standard.Api.App/DTOs/FeaturesDTO.cs`.
- Add a nullable property to the `Meta` class next to `HasAttachments`/`Warnings`, with both
  attribute styles used in this file:

  ```csharp
  [JsonProperty("has_more", NullValueHandling = NullValueHandling.Ignore)]
  [System.Text.Json.Serialization.JsonPropertyName("has_more")]
  [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
  public bool? HasMore { get; set; }
  ```

  Use snake_case for the wire property name.
- In the constructor, populate it (`features.HasMore == true ? true : null` for a bool flag, so it
  is omitted entirely when not set), and check the `if (tool != Meta.Tool.Unknown || select || ...)`
  guard that decides whether the `metadata` object is created at all — extend that condition with
  the new flag if it can legitimately be the *only* reason to emit metadata (as done for
  `features.HasMore` when this signal was added).

## 3. Read it on the client

- The value shows up as `features.metadata.<snake_case_name>` (or `result.metadata...`,
  `response.features.metadata...` depending on the call site) in JS.
- Add the rendering logic where it's needed, e.g. `webgis.map.queryresults.js` (table view) and/or
  `webgis_queryResults.js` (list view, e.g. mobile). Look at the existing `metadata.warnings`
  handling (`webgis.map.queryresults.js`) as the closest reference, but note it uses a dismissible,
  modal-style panel (`.webgis-result-warning-panel`) — do **not** reuse that mechanism for routine,
  non-critical notices; prefer a plain, non-interactive `<div>` with its own CSS class unless the
  user explicitly wants a dismiss/click behavior.

## 4. l10n

- Add any new user-facing text as a key in both
  `src/NetCore/Web/Api/wwwroot/scripts/api/webgis.l10n.de.js` and `webgis.l10n.en.js` — never
  hardcode user-facing strings in JS, even for a quick first version.
- Use `{placeholder}` tokens (resolved via `.replace('{placeholder}', value)`) for any numbers/names
  interpolated into the message, matching the existing convention in this file.

## 5. Verify end-to-end

- Build the touched C# projects individually (not the whole solution) — see the build quirk note
  in the `add-api-config-setting` skill (alternate `BaseOutputPath` if the dev server locks the
  shared `bin/` folder).
- `node --check` every touched JS file.
- If possible, trigger the actual scenario (e.g. a query that hits the limit) and confirm the field
  is present in the network response — a green C# build does **not** prove the flag actually
  reaches the client, precisely because of the gotcha in step 1.
