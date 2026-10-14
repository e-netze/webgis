---
name: create-webgis-tool
description: How to write a WebGIS viewer tool (button/server tool) in E.Standard.WebGIS.Tools - interfaces, tool id/registration in webgis.tools.js, UI elements, targets, setters, server commands/callbacks, resources, l10n and the DEBUG UI test tools. Use when creating or extending a viewer tool, or when adding/changing a server-side UIElement.
---

# Skill: Create a WebGIS Viewer Tool

Viewer tools are C# classes in `src/NetStandard/E.Standard.WebGIS.Tools` (e.g. `Coordinates.cs`).
The server exports them via MEF, sends their UI as JSON (`IUIElement` tree), and the viewer JS
(`webgis.ui.builder.*.js` + `webgis.ui.plugins.*.js`) renders it. Callbacks from the UI go back to
the tool as server commands.

## 1. Choose the interface

| Interface (namespace `E.Standard.WebMapping.Core.Api.Abstraction`) | Use for |
|---|---|
| `IApiServerButton(Async)` | button: one click => server response (e.g. a dialog), no map interaction |
| `IApiServerTool(Async)` | map tool: stays active, receives map events (`OnEvent`) and sketches |
| `IApiClientButton` / `IApiClientTool` | client-only button/tool (no server code) |
| `...Localizable<T>` / `...LocalizableAsync<T>` variants | same, with an `ILocalizer<T>` parameter for translated texts |

Optional interfaces: `IApiButtonResources` (images), `IApiToolConfirmation` (confirm dialogs),
`IApiUndoTool`, `IApiToolPersistenceContext`, `IApiButtonDependency`, `IApiChildTool`, ...

Important semantic difference:
- **Only the active map tool** receives `servertoolcommand` callbacks. A tool whose UI offers
  callbacks (buttons, combos with `changecommand`, ...) should be a map tool (`IApiServerTool*`,
  `ToolType.click`/`sketch0d`/`sketch1d`/`sketch2d`/...).
- Callbacks into another tool (or from a button) use `servertoolcommand_ext` together with the target
  tool id (e.g. `UIQueryBuilder.CallbackToolId = GetType().ToToolId()`).

## 2. Skeleton

```csharp
[Export(typeof(IApiButton))]                       // MEF export => tool is available
[ToolHelp("tools/xyz.html")]                       // optional: help page
[ToolConfigurationSection("xyz")]                  // optional: api.config tool section
public class MyTool : IApiServerToolLocalizableAsync<MyTool>, IApiButtonResources
{
    public string Name => "My tool";               // default texts; translated via l10n
    public string Container => "Query";            // toolbox category
    public string Image => UIImageButton.ToolResourceImage(this, "my-icon");
    public string ToolTip => "...";
    public bool HasUI => true;

    public ToolType Type => ToolType.sketch0d;
    public ToolCursor Cursor => ToolCursor.Custom_Pen;

    public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<MyTool> localizer)
        => Task.FromResult(new ApiEventResponse().AddUIElements(/* ... */));

    public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e, ILocalizer<MyTool> localizer)
        => /* map event (click/sketch) */;

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
        => toolResourceManager.AddImageResource("my-icon", Properties.Resources.my_icon);

    [ServerToolCommand("my-command")]
    public ApiEventResponse OnMyCommand(IBridge bridge, ApiToolEventArguments e, ILocalizer<MyTool> localizer)
    {
        var value = e["my-tool-param-id"];      // tool parameters by element id
        var arg = e.ServerCommandArgument;      // buttoncommand_argument
        return new ApiEventResponse() /* ... */;
    }
}
```

Further handler attributes: `[ServerEventHandler(ServerEventHandlers.OnVertexAdded | OnChangeVertex | OnUpdateCombo)]`.

## 3. Register the tool id in the viewer (mandatory)

- Tool id = full type name, lowercased, without the leading `e.standard.`
  (`E.Standard.WebGIS.Tools.Debug.UI.DebugUITargets` => `webgis.tools.debug.ui.debuguitargets`;
  `Type.ToToolId()` in `E.Standard.WebMapping.Core.Extensions`).
- Add the id to a category map in `src/NetCore/Web/Api/wwwroot/scripts/api/webgis.tools.js`
  (`this.query`, `this.io`, `this.debug`, ...). The portal map builder resolves tools via
  `webgis.tools.getToolName(id)`; unknown ids break the toolbox in the map builder.
- Check with `node --check` on the changed JS file.

## 4. Building the UI

Elements: `E.Standard.WebMapping.Core.Api.UI.Elements` (+ `.Advanced`), fluent extensions in
`E.Standard.WebMapping.Core.Api.Extensions` (`WithId`, `WithStyles`, `WithText`, `WithTarget`,
`AddChild(ren)`, `AsToolParameter()`, `WithVisibilityDependency`, ...).

- **Tool parameters**: every input whose value should be sent with requests needs an `id` and the
  css `UICss.ToolParameter` (`.AsToolParameter()`); `.AsPersistantToolParameter()` keeps the value
  when the tool is reopened. The server reads them with `e["<id>"]`.
- **Option containers** (`UIOptionContainer` + `UIImageButton`s with `value`): the selected value is
  the container value. Values may be strings, numbers or objects (objects are sent as JSON).
- **Callbacks**: `new UIButton(UIButton.UIButtonType.servertoolcommand, "command")` +
  `buttoncommand_argument`; combos/selects: `changetype`/`changecommand`.
  Client commands: `UIButtonType.clientbutton` + `ApiClientButtonCommand`.
- **Collapsable/conditional parts**: `UIDiv` with title/collapse state, `UIConditionDiv`,
  `VisibilityDependency`.
- **Responses**: by default the tool UI is replaced; use
  `.UIElementsBehavoir(AppendUIElementsMode.Append)` to append. Change existing values with setters
  (`AddUISetter(s)`: `UISetter`, `UICssSetter`, `UISelectOptionsSetter`, ...) instead of re-sending UI.
- **Targets** (`UIElementTarget`, `.WithTarget(...)`, `.WithTargetTitle`, `.WithSize`,
  `.WithTargetOnClose(command, type)`): `modaldialog(_hidden)`, `tool_modaldialog(_hidden,
  _noblocking, _noblocking_closable)` (fixed 330px width), `tool_sidebar_bottom/top/left`,
  `tool_persistent_topic`; only as string: `tool_aside` and `#<element-id>`.
  Close a target with `new UIEmpty().WithTarget(<same target>)`.
- **Texts**: don't hardcode user-facing texts. Use `ILocalizer<T>` with the l10n files
  `src/NetCore/Web/Api/l10n/{de,en}/Tools.<ToolName>.md` (`# key: text`). Viewer JS texts go to
  `webgis.l10n.{de,en}.js`.

## 5. New UI elements / JS plugins

A new `IUIElement` needs:
1. A C# class. Its type string is the JS builder key; the JSON property names must match what the
   builder reads (mismatches like `db_rights` vs. `dbrights` fail silently).
2. A JS builder `webgis.ui.builder["<type>"]` (see `organize-api-javascript-files`) and, for widgets,
   a plugin (`create-webgis-ui-plugin`). Widgets holding a value expose their value input via
   `$el.data('webgis-value-element', $valueElement)` so tool parameters can read it.
3. A demo in one of the debug UI tools (section 6).

## 6. Debug UI tools (manual testing of UI elements)

`src/NetStandard/E.Standard.WebGIS.Tools/Debug/UI/` contains test tools (toolbox container
"Debug"), exported only in `DEBUG` builds:

```csharp
#if DEBUG
[Export(typeof(IApiButton))]
#endif
public class DebugUIXyz : DebugUIToolBase { ... }
```

- Base class `DebugUIToolBase`: `sketch2d` map tool (so callbacks reach it), helpers `Section`,
  `SketchSection`, `Hint`, `EchoButton` and `EchoResponse` (dialog listing all received tool
  parameters, the command and the argument) - every callback can be checked with an echo.
- Existing tools: Text & Layout, Inputs, Buttons & Menus, Tables, Special Elements, Map Elements,
  Setters, Targets & Dialogs, Advanced Selectors. Each lists its `DemonstratedTypes`.
- When adding/changing a UI element, setter or target: add a demo section to the matching debug
  tool (or a new `DebugUI*` tool + its id in `webgis.tools.debug`), with a `Hint` describing the
  expected behavior.

## 7. Verify

- Build only the tools project (alternate output path if the dev server locks `bin/`):
  `dotnet build src\NetStandard\E.Standard.WebGIS.Tools\E.Standard.WebGIS.Tools.csproj -p:BaseOutputPath="$env:TEMP\tools-build\"`
  and check for `0 Error(s)`.
- `node --check` for changed JS files.
- Test manually in the viewer (DEBUG build); for new elements also with the debug UI tools.
- Consider changelog (`update-changelog`) and user docs (`update-user-docs`) for admin-visible tools.
