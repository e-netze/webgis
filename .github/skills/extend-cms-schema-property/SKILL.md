---
mode: agent
description: Adds a new configurable property to a CMS Schema class and propagates it through the CacheItem/DTO/runtime pipeline so it reaches the WebGIS viewer.
---

# Skill: Extend CMS Schema Property

Use this skill whenever a new configuration attribute must be added to a class in the WebGIS CMS
(`E.Standard.WebGIS.CmsSchema`) so that CMS admins can configure it, and the value must end up
available at runtime for the API/Viewer (e.g. in `EditEnvironment` or similar service classes).

This is a recurring pattern in this repo: **CMS Schema class → CacheItem binding → Viewer DTO →
runtime model → business logic**. Follow all layers below; skipping one will silently break the
feature (e.g. the property gets configured in the CMS but never reaches the viewer).

## 0. Locate the existing feature/class to extend

- Find the CMS Schema class in `src/NetStandard/E.Standard.WebGIS.CmsSchema/*.cs`
  (e.g. `EditingCommitAction.cs`). This is the class CMS admins configure through the schema form
  (`src/NetCore/Web/Cms/schemes/webgis/schema.xml`). Forms for simple property-grid style classes
  are generated automatically via reflection over `[DisplayName]`/`[Description]` attributes — you
  usually do **not** need to touch `schema.xml`.
- Find the corresponding viewer-facing DTO in `src/NetStandard/E.Standard.Api.App/DTOs/*.cs`
  (e.g. `EditThemeDTO.cs`, nested class like `CommitAction`). This DTO is what gets bound from the
  CMS document and serialized/rendered to the viewer (often as inline XML sent to the client).
- Find the runtime/business-logic model that consumes the DTO's rendered output, typically in
  `src/NetStandard/E.Standard.WebGIS.Tools/**` (e.g. `EditEnvironment.cs`, nested classes like
  `EditTheme.CommitAction`), and the service that acts on it (e.g. `CommitActionService.cs`).

## 1. Add a persist-name constant

- Open `src/NetStandard/E.Standard.WebGIS.CMS/PersistNames.cs`.
- Add a new `const string` in the appropriate nested class, next to the related existing constants.
- **Naming rules for the string value (critical):**
  - **Never modify or rename an existing constant's value.** These values are persisted inside
    customer `cms.xml` files. Changing an existing value breaks every running API instance that
    already has that value stored, silently and without a build error.
  - **New constant values must use `snake_case`** (lower-case words separated by underscores),
    e.g. `"success_message"`, not `"successmessage"` or `"SuccessMessage"`.
  - The C# constant name itself should stay `PascalCase` as usual (e.g. `SuccessMessage`); only the
    string literal value must be snake_case.

## 2. Extend the CMS Schema class

In the CMS Schema class (e.g. `EditingCommitAction.cs`):

- Add a public property with `[DisplayName("...")]` and `[Description("""...""")]` attributes.
  Keep the description text in the same language/style as the surrounding properties in that class
  (usually German in this repo) and mention any placeholder syntax the field supports
  (e.g. `[FELDNAME]` placeholders resolved via `Globals.SolveExpression`), if relevant.
- Add loading/saving in the class's `Load(IStreamDocument stream)` / `Save(IStreamDocument stream)`
  overrides, using the new `PersistNames` constant, following the exact pattern of the neighboring
  properties (string/enum/array handling differs — copy the pattern of a property with the same
  CLR type).

## 3. Extend the viewer DTO

In the corresponding DTO class (e.g. `EditThemeDTO.CommitAction`):

- Add a property of the same CLR type, decorated with `[PersistName(PersistNames.<Nested>.<New>)]`.
- If the DTO is populated via `BindCmsNode<T>(cmsNode)` (see
  `E.Standard.WebGIS.CMS/Extensions/PersistanceExtensions.cs`), **no further binding code is
  needed** — binding happens automatically via reflection over `[PersistName]` attributes, and
  therefore **no change to `CacheItem.cs` is required either** as long as the surrounding code
  already uses `BindCmsNode`.
- If the DTO in question is instead populated manually (property-by-property, not via
  `BindCmsNode`), add the corresponding assignment in `CacheItem.cs` at the point where the sibling
  properties of that DTO are populated.
- If the DTO is serialized to XML/JSON for the viewer (look for a `ToXmlString`/inline
  `StringBuilder`/`xml.Append(...)` method in the DTO file), add the new value there too:
  - Use the **same snake_case name** as the `PersistNames` constant for the wire attribute name
    (e.g. `success_message`), so the runtime parser (step 4) can find it by a predictable name.
  - Always call `.EscapeXmlString()` on string values written into XML attributes.
  - Only emit the attribute when the value is non-empty, matching the existing conditional-append
    style used for sibling optional attributes in the same method.

## 4. Extend the runtime model and parsing

In the runtime model (e.g. `EditEnvironment.EditTheme.CommitAction` or similar nested class):

- Add a property with the same name/type as used in the DTO layer.
- Add parsing of the new XML attribute where the sibling attributes of the same XML element are
  parsed (e.g. `commitActionNode.Attributes["success_message"]?.Value ?? ""`), using the exact
  attribute name chosen in step 3.

## 5. Wire the value into business logic

- Find the service/environment class that consumes the runtime model (e.g.
  `CommitActionService.cs`, `EditEnvironment.cs`) and use the new property where the feature
  requires it.
- If the property supports feature-attribute placeholders (`[FELDNAME]`), resolve it using the
  existing `Globals.SolveExpression(feature, expression)` helper — do not reimplement placeholder
  substitution.
- Double check timing/trigger semantics carefully against the user's actual requirement (e.g.
  "after every successful action" vs. "only after the After-timed actions") — a name like
  `SuccessMessage`/`AfterCommitMessage` can be misleading; prefer a property name that precisely
  matches the triggering behavior, and ask the user if in doubt rather than guessing.

## 6. Verify

- Build every project you touched directly (CMS Schema project, DTO/API project, runtime/tools
  project) individually with `dotnet build <project>.csproj`, since a full solution build is
  usually unnecessary and slower. Confirm `0 Fehler`/`0 Error(s)`.
- Grep the repo for the old/partial name after any rename to make sure no reference (persist name,
  XML attribute, property) was left inconsistent across layers.
- Do not add new schema.xml entries, new tests, or new tooling unless the user explicitly asks —
  this pattern is typically pure property plumbing across existing layers.
