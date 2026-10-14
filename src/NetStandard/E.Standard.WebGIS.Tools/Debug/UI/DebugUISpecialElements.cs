using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using E.Standard.WebGIS.Core.Reflection;
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
/// Debug tool: special elements, that depend on viewer state (not on map services)
/// (sketchselect, circleradiuscombo, tool persistent topic, graphics info container/stage, liveshare).
/// </summary>
#if DEBUG
[Export(typeof(IApiButton))]
#endif
public class DebugUISpecialElements : DebugUIToolBase
{
    private const string IdPrefix = "debug-ui-special-";

    private const string TopicReplaceCommand = "topic-replace";
    private const string TopicAppendCommand = "topic-append";

    public override string Name => "UI: Special Elements";

    public override string ToolTip => "Debug: sketch select, circle radius combo, persistent topic, graphics info, liveshare";

    public override IEnumerable<Type> DemonstratedTypes =>
    [
        typeof(UISketchSelect), typeof(UIMarkerCircleRadiusCombo), typeof(UIToolPersistentTopic),
        typeof(UIGraphicsInfoContainer), typeof(UIGraphicsInfoStage), typeof(UILiveShareElement),
        typeof(UISketchInfoContainer)
    ];

    public override Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var response = new ApiEventResponse()
            .AddUIElements(
                SketchSection(),

                Section("sketchselect",
                    Hint("Lists the current sketches of all (other) sketch tools. Draw a sketch with another debug tool (e.g. 'UI: Inputs'), come back here: selecting it loads the sketch into this tool, '--- nicht anzeigen ---' removes the sketch."),
                    new UISketchSelect() { id = Id("sketchselect"), css = UICss.ToolParameter },
                    EchoButton()),

                Section("circleradiuscombo",
                    Hint("Changing the radius calls map.showCircleMarker(null, radius): a circle is only shown, if a circle marker position exists (place one with the 'Circumference Circle' tool first), otherwise only the radius for the next marker is set. The preselected option is the current marker radius of the map."),
                    new UILabel() { label = "default radii (webgis.const.circleMarkerRadii)" },
                    new UIMarkerCircleRadiusCombo() { id = Id("radius-default"), css = UICss.ToolParameter },
                    new UILabel() { label = "custom radii [5, 25, 100, 250] with value = 250 (is the value used?)" },
                    new UIMarkerCircleRadiusCombo()
                    {
                        id = Id("radius-custom"),
                        css = UICss.ToolParameter,
                        radii = [5, 25, 100, 250],
                        value = 250
                    },
                    new UIButtonContainer([
                        EchoButton(),
                        new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.removecirclemarker)
                        {
                            text = "removecirclemarker",
                            css = UICss.ToClass([UICss.CancelButtonStyle])
                        }
                    ])),

                Section("tool persistent topic",
                    Hint("Elements with target 'tool_persistent_topic' are inserted into the topic below (replace: emptied first, append: added). Fill it, select another tool and come back: the content (incl. events, e.g. the echo button) is restored. Content with persistent tool parameters is not restored by design."),
                    new UIButtonContainer([
                        EchoButton("Set topic content (replace)", command: TopicReplaceCommand),
                        EchoButton("Add to topic (append)", command: TopicAppendCommand)
                    ]),
                    new UIToolPersistentTopic(this) { style = "border:1px dashed #aaa;min-height:24px;padding:4px" }),

                Section("graphics-info-container / -stage",
                    Hint("The container lists the map markup graphics (draw some with the 'Map Markup' tool first) with select/edit/delete buttons."),
                    new UIGraphicsInfoContainer(),
                    Hint("graphics-info-stage (below): there is no webgis_graphicsInfoStage plugin => an empty div is expected."),
                    new UIGraphicsInfoStage()),

                Section("ui-liveshare",
                    Hint("Shows the users of the current liveshare session and the 'share map extent/layer visibility' checkboxes. The user list is only filled inside a liveshare session ('Live Share' tool)."),
                    new UILiveShareElement()));

        return Task.FromResult(response);
    }

    #region Commands

    [ServerToolCommand(TopicReplaceCommand)]
    public ApiEventResponse OnTopicReplace(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .AddUIElement(TopicContent("replaced"));

    [ServerToolCommand(TopicAppendCommand)]
    public ApiEventResponse OnTopicAppend(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .UIElementsBehavoir(AppendUIElementsMode.Append)
            .AddUIElement(TopicContent("appended"));

    #endregion

    #region Helpers

    static private string Id(string name) => IdPrefix + name;

    static private IUIElement TopicContent(string action)
        => new UIDiv()
            .WithTarget(UIElementTarget.tool_persistent_topic)
            .AddChildren(
                new UIParagraph($"{action} at {DateTime.Now:HH:mm:ss}"),
                EchoButton("echo (from topic)", argument: $"topic-{action}"));

    #endregion
}
