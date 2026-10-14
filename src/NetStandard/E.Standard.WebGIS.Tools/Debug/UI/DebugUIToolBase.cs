using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using E.Standard.Extensions.Compare;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;

namespace E.Standard.WebGIS.Tools.Debug.UI;

/// <summary>
/// Base class for the debug UI tools (container "Debug").
/// The derived tools are only exported (=> available in the viewer/map builder) in DEBUG builds,
/// see the <c>#if DEBUG</c> around their <c>[Export(typeof(IApiButton))]</c> attribute.
/// They render demos of UI elements to check the rendering and the callbacks manually.
/// They are (interactive) polygon sketch tools: only the current map tool receives
/// <c>servertoolcommand</c> callbacks, and the sketch is sent with every callback.
/// </summary>
public abstract class DebugUIToolBase : IApiServerToolAsync
{
    public const string DebugContainer = "Debug";
    public const string EchoCommand = "echo";

    #region IApiButton

    public abstract string Name { get; }

    public string Container => DebugContainer;

    public virtual string Image => "hash.png";

    public abstract string ToolTip { get; }

    public bool HasUI => true;

    #endregion

    #region IApiTool

    public ToolType Type => ToolType.sketch2d;

    public ToolCursor Cursor => ToolCursor.Custom_Pen;

    #endregion

    #region IApiServerToolAsync

    public abstract Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e);

    public virtual Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e)
        => Task.FromResult(EchoResponse(e, "Echo: OnEvent"));

    #endregion

    /// <summary>
    /// The UI element/setter types demonstrated by this tool (used to check the coverage of all UI element types).
    /// </summary>
    public abstract IEnumerable<Type> DemonstratedTypes { get; }

    #region Commands

    [ServerToolCommand(EchoCommand)]
    public ApiEventResponse OnEcho(IBridge bridge, ApiToolEventArguments e)
        => EchoResponse(e);

    #endregion

    #region Helpers

    /// <summary>
    /// A collapsable section with the demos of one element type.
    /// </summary>
    static protected UIDiv Section(string title, params IUIElement[] elements)
        => new UIDiv()
        {
            title = title,
            CollapseState = UICollapsableElement.CollapseStatus.Collapsed
        }.AddChildren(elements);

    /// <summary>
    /// Sketch controls of the polygon sketch tool (remove sketch + sketch info).
    /// </summary>
    static protected UIDiv SketchSection()
        => new UIDiv()
        {
            title = "sketch (polygon)",
            CollapseState = UICollapsableElement.CollapseStatus.Expanded
        }.AddChildren(
            Hint("Draw a polygon in the map. The sketch is sent with every 'Echo' (_sketch, _sketchWgs84, ...)."),
            new UIButtonContainer(
                new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.removesketch)
                    .WithStyles(UICss.CancelButtonStyle)
                    .WithText("Remove sketch")),
            new UISketchInfoContainer());

    static protected UIParagraph Hint(string text)
        => new UIParagraph(text) { style = "color:#888;font-style:italic" };

    /// <summary>
    /// A button, that sends all current tool parameters to the server (servertoolcommand "echo").
    /// The server shows the received arguments in a dialog.
    /// </summary>
    static protected UIButton EchoButton(string text = "Echo tool parameters", string argument = null, string command = EchoCommand)
        => new UIButton(UIButton.UIButtonType.servertoolcommand, command)
        {
            text = text,
            css = UICss.ToClass(new[] { UICss.DefaultButtonStyle }),
            buttoncommand_argument = argument
        };

    /// <summary>
    /// Shows all arguments received by the server (tool parameters first, internal "_..." arguments last).
    /// The receiving tool is shown too (=> callbacks routed to another tool, e.g. servertoolcommand_ext, can be checked).
    /// </summary>
    protected ApiEventResponse EchoResponse(ApiToolEventArguments e, string title = null, params IUIElement[] additionalElements)
    {
        var arguments = ((IDictionary<string, object>)e.RawEvent)
            .Select(kv => (key: kv.Key, value: kv.Value?.ToString() ?? "<null>"))
            .ToArray();

        var dialog = new UIDiv()
            .WithTarget(UIElementTarget.modaldialog)
            .WithTargetTitle(title ?? $"Echo: {e.ServerCommandMethod.OrTake(EchoCommand)}")
            .AddChild(new UILiteralBold() { literal = $"tool: {Name} ({GetType().Name})" })
            .AddChild(new UIBreak())
            .AddChild(new UILiteralBold() { literal = $"command: {e.ServerCommandMethod}, argument: {e.ServerCommandArgument}" })
            .AddChild(new UIBreak())
            .AddChild(new UILiteral() { literal = e.Sketch is null
                ? "sketch: none"
                : $"sketch: {e.Sketch.GetType().Name}, srs: {e.Sketch.SrsId}" })
            .AddChild(new UIBreak())
            .AddChildren(additionalElements ?? []);

        dialog.AddChild(new UITitle() { label = "Tool parameters" });
        AddArguments(dialog, arguments.Where(a => !a.key.StartsWith("_")));

        dialog.AddChild(new UITitle() { label = "Internal arguments (_...)" });
        AddArguments(dialog, arguments.Where(a => a.key.StartsWith("_")));

        return new ApiEventResponse().AddUIElement(dialog);
    }

    static private void AddArguments(UIDiv dialog, IEnumerable<(string key, string value)> arguments)
    {
        var list = arguments.OrderBy(a => a.key).ToArray();
        if (list.Length == 0)
        {
            dialog.AddChild(Hint("none"));
            return;
        }

        foreach (var (key, value) in list)
        {
            dialog.AddChild(new UIParagraph($"{key} = {(value.Length > 300 ? value.Substring(0, 300) + "..." : value)}"));
        }
    }

    #endregion
}
