using E.Standard.Extensions.Compare;
using E.Standard.GeoJson;
using E.Standard.GeoJson.Extensions;
using E.Standard.Json;
using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Core.Extensions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebGIS.Tools.Redlining.Export;
using E.Standard.WebGIS.Tools.Redlining.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Redlining;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(ClientDeviceDependent = true, SelectionInfoDependent = true, MapCrsDependent = true)]
[ToolConfigurationSection("redlining")]
[ToolHelp("tools/general/redlining/index.html")]
public class Redlining : IApiServerToolLocalizable<Redlining>, 
                         IApiButtonResources, 
                         IGraphicsTool, 
                         IApiToolConfirmation
{
    protected string toolContainerId = "webgis-redlining-tool-container";

    private const string ConfigAllowAddFromSelection = "allow-add-from-selection";
    private const string ConfigAllowAddFromSelectionMaxFeatures = "allow-add-from-selection-max-features";
    private const string ConfigAllowAddFromSelectionMaxVertices = "allow-add-from-selection-max-vertices";
    private const string ConfigAllowDownloadFromSelection = "allow-download-from-selection";
    private const string ConfigDeaultDownloadEpsgCode = "default-download-epsg";

    private readonly string[] MobileTools = new string[] { "pointer", "symbol", "freehand", "line", "polygon", "dimline", "share", "save", "open", "upload", "download" };
    private readonly string[] AdvancedTools = new string[] { "text", "distance_circle", "compass", "dimline", "hectoline" };  // Do not use with Internet Explorer

    #region IApiServerTool Member

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        List<IUIElement> uiImageButtons = new List<IUIElement>();

        bool allowAdvancedTools = bridge.UserAgent.IsInternetExplorer == false;

        uiImageButtons.AddRange(new IUIElement[]{
                        new UIImageButton(this.GetType(),"pointer",UIButton.UIButtonType.servertoolcommand,"pointer"){
                            value = "pointer",
                            text = localizer.Localize("tools.select")
                        },
                        new UIImageButton(this.GetType(),"symbol",UIButton.UIButtonType.servertoolcommand,"symbol"){
                            value = "symbol",
                            text = localizer.Localize("tools.symbol")
                        },
                        new UIImageButton(this.GetType(), "text", UIButton.UIButtonType.servertoolcommand, "text")
                        {
                            value = "text",
                            text = localizer.Localize("tools.text")
                        },
                        new UIImageButton(this.GetType(), "point", UIButton.UIButtonType.servertoolcommand, "point")
                        {
                            value = "point",
                            text = localizer.Localize("tools.point")
                        },
                        new UIImageButton(this.GetType(), "freehand", UIButton.UIButtonType.servertoolcommand, "freehand")
                        {
                            value = "freehand",
                            text = localizer.Localize("tools.freehand")
                        },
                        new UIImageButton(this.GetType(),"line",UIButton.UIButtonType.servertoolcommand,"line"){
                            value = "line",
                            text = localizer.Localize("tools.line")
                        },
                        new UIImageButton(this.GetType(),"polygon",UIButton.UIButtonType.servertoolcommand,"polygon"){
                            value = "polygon",
                            text = localizer.Localize("tools.polygon")
                        },
                        new UIImageButton(this.GetType(),"rectangle", UIButton.UIButtonType.servertoolcommand,"rectangle")
                        {
                            value = "rectangle",
                            text = localizer.Localize("tools.rectangle")
                        },
                        new UIImageButton(this.GetType(),"circle", UIButton.UIButtonType.servertoolcommand,"circle")
                        {
                            value = "circle",
                            text = localizer.Localize("tools.circle")
                        },
                        new UIImageButton(this.GetType(),"distance_circle", UIButton.UIButtonType.servertoolcommand,"distance_circle")
                        {
                            value = "distance_circle",
                            text = localizer.Localize("tools.distance-circle")
                        },
                        new UIImageButton(this.GetType(),"compass", UIButton.UIButtonType.servertoolcommand,"compass_rose")
                        {
                            value = "compass_rose",
                            text = localizer.Localize("tools.compass-rose")
                        },
                        new UIImageButton(this.GetType(),"dimline", UIButton.UIButtonType.servertoolcommand,"dimline")
                        {
                            value = "dimline",
                            text = localizer.Localize("tools.dimline")
                        },
                        new UIImageButton(this.GetType(),"hectoline", UIButton.UIButtonType.servertoolcommand,"hectoline")
                        {
                            value = "hectoline",
                            text = localizer.Localize("tools.hectoline")
                        }
                        //,new UIBreak()

        });

        if (!allowAdvancedTools)
        {
            uiImageButtons = new List<IUIElement>(
                uiImageButtons.Where(b =>
                               !(b is UIImageButton) ||
                               !AdvancedTools.Contains(((UIImageButton)b).value?.ToString())));
        }

        if (bridge.CurrentUser?.IsAnonymous == false)
        {
            uiImageButtons.AddRange(new IUIElement[]{
                        new UIImageButton(this.GetType(),"save",UIButton.UIButtonType.servertoolcommand,"save"){
                            value = "save",
                            text = localizer.Localize("tools.save")
                        },
                        new UIImageButton(this.GetType(),"open",UIButton.UIButtonType.servertoolcommand,"open"){
                            value = "open",
                            text = localizer.Localize("tools.open")
                        }
                        //,new UIHidden(){
                        //    id="redlining-tool",
                        //    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsTool})
                        //}
            });
        }

        uiImageButtons.AddRange(new IUIElement[]{
                        new UIImageButton(this.GetType(),"share",UIButton.UIButtonType.servertoolcommand,"share"){
                            value = "share",
                            text = localizer.Localize("tools.share")
                        },
                        new UIImageButton(this.GetType(),"upload",UIButton.UIButtonType.servertoolcommand,"upload"){
                            value = "upload",
                            text = localizer.Localize("tools.upload")
                        },
                        new UIImageButton(this.GetType(),"download",UIButton.UIButtonType.servertoolcommand,"download"){
                            value = "download",
                            text = localizer.Localize("tools.download")
                        }
                        //,new UIHidden(){
                        //    id="redlining-tool",
                        //    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsTool})
                        //}
            });

        List<IUIElement> uiElements = new List<IUIElement>();

        uiElements.Add(new UIOptionContainer()
        {
            id = "webgis-redlining-tool",
            css = UICss.ToClass(new string[] { UICss.OptionContainerWithLabels }),
            style = "width:300px",
            elements = e.UseMobileBehavior() ?
                            uiImageButtons.Where(b =>
                                !(b is UIImageButton) ||
                                MobileTools.Contains(((UIImageButton)b).value?.ToString())).ToArray() :
                            uiImageButtons.ToArray()
        });

        uiElements.AddRange(
            new IUIElement[]{
                new UIHidden(){
                    id="redlining-tool",
                    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsTool})
                },
                new UIHidden(){
                    id="redlining-color",
                    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsColor})
                },
                new UIHidden(){
                    id="redlining-opacity",
                    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsOpacity})
                },
                new UIHidden(){
                    id="redlining-fillcolor",
                    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsFillColor})
                },
                new UIHidden(){
                    id="redlining-fillopacity",
                    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsFillOpacity})
                },
                new UIHidden(){
                    id="redlining-lineweight",
                    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsLineWeight})
                },
                new UIHidden(){
                    id="redlining-linestyle",
                    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsLineStyle})
                },
                new UIHidden(){
                    id="redlining-symbol",
                    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsSymbol})
                },
                new UIHidden(){
                    id="redlining-fontsize",
                    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsFontSize})
                },
                new UIHidden(){
                    id="redlining-fontstyle",
                    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsFontStyle})
                },
                new UIHidden(){
                    id="redlining-fontcolor",
                    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsFontColor})
                },
                new UIHidden(){
                    id = "redlining-pointcolor",
                    css = UICss.ToClass(new []{ UICss.ToolParameter, UICss.AutoSetterMapGraphicsPointColor })
                },
                new UIHidden(){
                    id = "redlining-pointsize",
                    css = UICss.ToClass(new []{ UICss.ToolParameter, UICss.AutoSetterMapGraphicsPointSize })
                }
        });

        uiElements.Add(new UIDiv()
        {
            id = toolContainerId
        });

        if (!e.UseMobileBehavior())
        {
            uiElements.Add(new UIGraphicsInfoStage());
        }

        if (!e.UseMobileBehavior())
        {
            uiElements.Add(new UIGraphicsInfoContainer());
        }

        return new ApiEventResponse()
        {
            UIElements = uiElements.ToArray(),
            Events = new IApiToolEvent[] {
                new ApiToolEvent(ApiToolEvents.Graphics_ElementSelected, "element-selected")
            }
        };
    }

    public ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        return new ApiEventResponse();
    }

    #endregion

    #region IApiTool Member

    public ToolType Type
    {
        get { return ToolType.graphics; }
    }

    public ToolCursor Cursor
    {
        get { return ToolCursor.Crosshair; }
    }

    #endregion

    #region IApiButton Member

    virtual public string Name => "Drawing (Redlining)";

    public string Container => "Tools";

    public string Image => UIImageButton.ToolResourceImage(this, "redlining");
    
    public string ToolTip => "Simple drawing on the map.";

    public bool HasUI => true;

    #endregion

    #region IApiToolResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("redlining", Properties.Resources.redlining);
        toolResourceManager.AddImageResource("pointer", Properties.Resources.pointer);
        toolResourceManager.AddImageResource("symbol", Properties.Resources.symbol);
        toolResourceManager.AddImageResource("point", Properties.Resources.point);
        toolResourceManager.AddImageResource("line", Properties.Resources.polyline);
        toolResourceManager.AddImageResource("freehand", Properties.Resources.freehand);
        toolResourceManager.AddImageResource("polygon", Properties.Resources.polygon);
        toolResourceManager.AddImageResource("distance_circle", Properties.Resources.distance_circle);
        toolResourceManager.AddImageResource("compass", Properties.Resources.compass);
        toolResourceManager.AddImageResource("circle", Properties.Resources.circle);
        toolResourceManager.AddImageResource("rectangle", Properties.Resources.rectangle);
        toolResourceManager.AddImageResource("text", Properties.Resources.text);
        toolResourceManager.AddImageResource("open", Properties.Resources.open);
        toolResourceManager.AddImageResource("save", Properties.Resources.save);
        toolResourceManager.AddImageResource("share", Properties.Resources.share);
        toolResourceManager.AddImageResource("download", Properties.Resources.download);
        toolResourceManager.AddImageResource("upload", Properties.Resources.upload);
        toolResourceManager.AddImageResource("dimline", Properties.Resources.dimline_26);
        toolResourceManager.AddImageResource("hectoline", Properties.Resources.hectoline_26);
    }

    #endregion

    #region IApiToolConfirmation Member

    public ApiToolConfirmation[] ToolConfirmations
    {
        get
        {
            List<ApiToolConfirmation> confirmations = new List<ApiToolConfirmation>();
            confirmations.AddRange(ApiToolConfirmation.CommandComfirmations(typeof(Redlining)));
            return confirmations.ToArray();
        }
    }

    #endregion

    #region Commands

    [ServerToolCommand("pointer")]
    public ApiEventResponse OnPointerClick(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Pointer },
            UIElements = new IUIElement[] {
                new UIEmpty() {
                    target="#"+toolContainerId //UIElementTarget.tool_sidebar_left.ToString()
                }
            }
        };
    }

    [ServerToolCommand("symbol")]
    async public Task<ApiEventResponse> OnSymbolToolClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        var uiElements = new List<IUIElement>().AddSymbolStyleElements(bridge, e, localizer, true);

        if (e.UseMobileBehavior())
        {
            uiElements.AddRange(new IUIElement[]
            {
                        new UIBreak(),
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.removecurrentgraphicselement){
                            text = localizer.Localize("remove-sketch"),
                            css = UICss.ToClass(new string[] { UICss.CancelButtonStyle })
                        },
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.assumecurrentgraphicselement){
                            text = localizer.Localize("apply-symbol")
                        }
            });
        }

        if (e.SelectionInfo != null)
        {
            if (e.GetConfigBool(ConfigAllowAddFromSelection, false))
            {
                uiElements.Add(new UIButton(UIButton.UIButtonType.servertoolcommand, "add-from-selection-dialog")
                {
                    text = String.Format(localizer.Localize("text.symbols-from-selection"), await e.SelectionInfo.GetQueryName(bridge)),
                    css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.LineBreakButton })
                });
            }
        }

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Symbol },
            UIElements = new IUIElement[]
            {
                new UIDiv(){
                    target= $"#{toolContainerId}", //UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle = localizer.Localize("draw-symbol"),
                    elements = uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("point")]
    async public Task<ApiEventResponse> OnPointToolClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        var uiElements = new List<IUIElement>().AddPointStyleElements(e, localizer, true).AsStagedStyleElements(e);

        if (e.SelectionInfo != null)
        {
            if (e.GetConfigBool(ConfigAllowAddFromSelection, false) && e.SelectionInfo.GeometryType == "point")
            {
                uiElements.Add(new UIButton(UIButton.UIButtonType.servertoolcommand, "add-from-selection-dialog")
                {
                    text = String.Format(localizer.Localize("text.points-from-selection"), await e.SelectionInfo.GetQueryName(bridge)),
                    css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.LineBreakButton })
                });
            }
        }

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Point },
            UIElements = new IUIElement[]
            {
                new UIDiv(){
                    target = $"#{toolContainerId}",
                    targettitle = localizer.Localize("draw-point"),
                    elements = uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("text")]
    async public Task<ApiEventResponse> OnTextToolClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        var uiElements = new List<IUIElement>().AddTextStyleElements(e, localizer, true).AsStagedStyleElements(e);

        if (e.GetConfigBool(ConfigAllowAddFromSelection, false))
        {
            if (e.SelectionInfo != null)
            {
                uiElements.Add(new UIButton(UIButton.UIButtonType.servertoolcommand, "add-from-selection-dialog")
                {
                    text = String.Format(localizer.Localize("text.text-from-selection"), await e.SelectionInfo.GetQueryName(bridge)),
                    css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.LineBreakButton })
                });
            }
        }

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Text },
            UIElements = new IUIElement[]
            {
                new UIDiv(){
                    target = "#"+toolContainerId, //UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle = localizer.Localize("draw-text"),
                    elements = uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("freehand")]
    public ApiEventResponse OnFreehandToolClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        var uiElements = new List<IUIElement>().AddFreehandStyleElements(e, localizer, true).AsStagedStyleElements(e);

        if (e.UseMobileBehavior())
        {
            uiElements.AddRange(new IUIElement[]
            {
                        new UIBreak(),
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.removecurrentgraphicselement){
                            text = localizer.Localize("remove-sketch"),
                            css=UICss.ToClass(new string[] { UICss.CancelButtonStyle })
                        },
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.assumecurrentgraphicselement){
                            text = localizer.Localize("apply-line")
                        }
            });
        }

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Freehand },
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target = "#"+toolContainerId, //UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle = localizer.Localize("draw-freehand"),
                    elements = uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("line")]
    async public Task<ApiEventResponse> OnLineToolClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        var uiElements = new List<IUIElement>().AddLineStyleElements(e, localizer, true).AsStagedStyleElements(e);

        if (e.UseMobileBehavior())
        {
            uiElements.AddRange(new IUIElement[]
            {
                        new UIBreak(),
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.removecurrentgraphicselement){
                            text = localizer.Localize("remove-sketch"),
                            css = UICss.ToClass(new string[] { UICss.CancelButtonStyle })
                        },
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.assumecurrentgraphicselement){
                            text = localizer.Localize("apply-line")
                        }
            });
        }

        if (e.GetConfigBool(ConfigAllowAddFromSelection, false))
        {
            if (e.SelectionInfo != null && e.SelectionInfo.GeometryType == "line")
            {
                uiElements.Add(new UIButton(UIButton.UIButtonType.servertoolcommand, "add-from-selection-dialog")
                {
                    text = String.Format(localizer.Localize("text.line-from-selection"), await e.SelectionInfo.GetQueryName(bridge)),
                    css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.LineBreakButton })
                });
            }
        }

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Line },
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target = "#"+toolContainerId, //UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle = localizer.Localize("draw-line"),
                    elements = uiElements.ToArray()
                }
            }
        };
    }

    async public Task<ApiEventResponse> On2DToolClick(IBridge bridge, ApiToolEventArguments e, GraphicsTool tool, ILocalizer<Redlining> localizer)
    {
        var uiElements = new List<IUIElement>().Add2DStyleElements(e, localizer, true).AsStagedStyleElements(e);

        if (e.UseMobileBehavior())
        {
            uiElements.AddRange(new IUIElement[]
            {
                                new UIBreak(),
                                new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.removecurrentgraphicselement){
                                    text = localizer.Localize("remove-sketch"),
                                    css=UICss.ToClass(new string[] { UICss.CancelButtonStyle })
                                },
                                new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.assumecurrentgraphicselement){
                                    text = localizer.Localize("apply-polygon"),
                                }
            });
        }
        else
        {
            if (e.GetConfigBool(ConfigAllowAddFromSelection, false))
            {
                if (e.SelectionInfo != null && e.SelectionInfo.GeometryType == "polygon" && tool == GraphicsTool.Polygon)
                {
                    uiElements.Add(new UIButton(UIButton.UIButtonType.servertoolcommand, "add-from-selection-dialog")
                    {
                        text = String.Format(localizer.Localize("text.polygon-from-selection"), await e.SelectionInfo.GetQueryName(bridge)),
                        css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.LineBreakButton })
                    });
                }
            }
        }

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = tool },
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target="#"+toolContainerId, // UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle= localizer.Localize("draw-polygon"),
                    elements = new IUIElement[] {
                         new UIDiv(){
                            //target=UIElementTarget.tool_sidebar_left.ToString(),
                            elements=uiElements.ToArray()
                        }
                    }
                }
            }
        };
    }

    [ServerToolCommand("polygon")]
    public Task<ApiEventResponse> OnPolygonToolClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        return On2DToolClick(bridge, e, GraphicsTool.Polygon, localizer);
    }

    [ServerToolCommand("rectangle")]
    public Task<ApiEventResponse> OnRectangleToolClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        return On2DToolClick(bridge, e, GraphicsTool.Rectangle, localizer);
    }

    [ServerToolCommand("circle")]
    public Task<ApiEventResponse> OnCircleToolClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        return On2DToolClick(bridge, e, GraphicsTool.Circle, localizer);
    }

    [ServerToolCommand("distance_circle")]
    public ApiEventResponse OnDistanceCircleClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        var uiElements = new List<IUIElement>().AddDistanceCircleStyleElements(e, localizer, true).AsStagedStyleElements(e);

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Distance_Circle },
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target = "#"+toolContainerId, //UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle = localizer.Localize("draw-distance-circle"),
                    elements = uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("compass_rose")]
    public ApiEventResponse OnCompassRoseClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        var uiElements = new List<IUIElement>().AddCompassRoseStyleElements(e, localizer, true).AsStagedStyleElements(e);

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Compass_Rose },
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target = "#"+toolContainerId, //UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle = localizer.Localize("draw-compass-rose"),
                    elements = uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("dimline")]
    public ApiEventResponse OnDimLineToolClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        var uiElements = new List<IUIElement>().AddDimLineStyleElements(e, localizer, true).AsStagedStyleElements(e);

        if (e.UseMobileBehavior())
        {
            uiElements.AddRange(new IUIElement[]
            {
                        new UIBreak(),
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.removecurrentgraphicselement){
                            text = localizer.Localize("remove-sketch"),
                            css=UICss.ToClass(new string[] { UICss.CancelButtonStyle })
                        },
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.assumecurrentgraphicselement){
                            text = localizer.Localize("apply-line")
                        }
            });
        }

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.DimLine },
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target = "#"+toolContainerId, //UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle = localizer.Localize("draw-dimline"),
                    elements = uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("hectoline")]
    public ApiEventResponse OnHectoLineToolClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        var uiElements = new List<IUIElement>().AddHectoLineStyleElements(e, localizer).AsStagedStyleElements(e);

        if (e.UseMobileBehavior())
        {
            uiElements.AddRange(new IUIElement[]
            {
                        new UIBreak(),
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.removecurrentgraphicselement){
                            text = localizer.Localize("remove-sketch"),
                            css=UICss.ToClass(new string[] { UICss.CancelButtonStyle })
                        },
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.assumecurrentgraphicselement){
                            text = localizer.Localize("apply-line")
                        }
            });
        }

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.HectoLine },
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target = "#"+toolContainerId, //UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle = localizer.Localize("draw-hectoline"),
                    elements = uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("element-selected")]
    async public Task<ApiEventResponse> OnElementClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        ApiEventResponse resp = null;
        switch (e["redlining-tool"])
        {
            case "symbol":
                resp = await OnSymbolToolClick(bridge, e, localizer);
                break;
            case "point":
                resp = await OnPointToolClick(bridge, e, localizer);
                break;
            case "line":
                resp = await OnLineToolClick(bridge, e, localizer);
                break;
            case "polygon":
                resp = await OnPolygonToolClick(bridge, e, localizer);
                break;
            case "freehand":
                resp = OnFreehandToolClick(bridge, e, localizer);
                break;
            case "distance_circle":
                resp = OnDistanceCircleClick(bridge, e, localizer);
                break;
            case "compass_rose":
                resp = OnCompassRoseClick(bridge, e, localizer);
                break;
            case "circle":
                resp = await OnCircleToolClick(bridge, e, localizer);
                break;
            case "rectangle":
                resp = await OnRectangleToolClick(bridge, e, localizer);
                break;
            case "text":
                resp = await OnTextToolClick(bridge, e, localizer);
                break;
            case "dimline":
                resp = OnDimLineToolClick(bridge, e, localizer);
                break;
            case "hectoline":
                resp = OnHectoLineToolClick(bridge, e, localizer);
                break;
        }

        if (resp != null)
        {
            resp.UISetters = new IUISetter[]{
                new UISetter("webgis-redlining-tool", e["redlining-tool"])
            };
        }

        return resp;
    }

    [ServerToolCommand("show-symbol-selector")]
    public ApiEventResponse OnShowSymbolSelector(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
        => new ApiEventResponse()
            .AddUIElements(
                new UIDiv()
                    .WithTarget(UIElementTarget.tool_modaldialog_noblocking_closable.ToString())
                        .WithTargetTitle($"{localizer.Localize("symbology.set")}: {localizer.Localize($"tools.{e["redlinig-symbol-type"]}")}")
                        .WithTargetOnClose(ApiClientButtonCommand.refreshgraphicsui.ToString())
                        .AddChildren(e["redlinig-symbol-type"] switch
                        {
                            "symbol" => new List<IUIElement>().AddSymbolStyleElements(bridge, e, localizer),
                            "text" => new List<IUIElement>().AddTextStyleElements(e, localizer, collapseExclusive: false, isCollapsed: false),
                            "point" => new List<IUIElement>().AddPointStyleElements(e, localizer, collapseExclusive: false, isCollapsed: false),
                            "freehand" => new List<IUIElement>().AddFreehandStyleElements(e, localizer, collapseExclusive: false, isCollapsed: false),
                            "line" => new List<IUIElement>().AddLineStyleElements(e, localizer, collapseExclusive: false, isCollapsed: false),
                            "polygon" => new List<IUIElement>().Add2DStyleElements(e, localizer, collapseExclusive: false, isCollapsed: false),
                            "rectangle" => new List<IUIElement>().Add2DStyleElements(e, localizer, collapseExclusive: false, isCollapsed: false),
                            "circle" => new List<IUIElement>().Add2DStyleElements(e, localizer, collapseExclusive: false, isCollapsed: false),
                            "distance_circle" => new List<IUIElement>().AddDistanceCircleStyleElements(e, localizer, collapseExclusive: false, isCollapsed: false),
                            "compass_rose" => new List<IUIElement>().AddCompassRoseStyleElements(e, localizer, collapseExclusive: false, isCollapsed: false),
                            "dimline" => new List<IUIElement>().AddDimLineStyleElements(e, localizer, collapseExclusive: false, isCollapsed: false),
                            "hectoline" => new List<IUIElement>().AddHectoLineStyleElements(e, localizer, collapseExclusive: false, isCollapsed: false),
                            _ => new()
                        })
            );

    [ServerToolCommand("show-single-symbol-selector")]
    public ApiEventResponse OnShowSelectedSymbolSelector(IBridge bridge, ApiToolEventArguments e)
    {
        var styles = e.GetArray<string>("redlinig-styles");
        bool expanded = styles.Length <= 3;

        return new ApiEventResponse()
                .AddUIElements(
                    new UIDiv()
                        .WithTarget(UIElementTarget.tool_modaldialog_noblocking_closable.ToString())
                        .WithTargetTitle("Symbolik ändern")
                        .WithTargetOnClose(ApiClientButtonCommand.refreshgraphicsui.ToString())
                        .AddChildren(styles.SelectMany(style => style switch
                        {
                            "symbol" => new List<IUIElement>().AddSymbolIdElements(bridge),
                            "point-color" => new List<IUIElement>().AddPointColorElements(expanded: expanded),
                            "point-size" => new List<IUIElement>().AddPointSizeElements(expanded: expanded),
                            "text-color" => new List<IUIElement>().AddTextColorElements(expanded: expanded),
                            "text-size" => new List<IUIElement>().AddTextSizeElements(expanded: expanded),
                            "stroke-color" => new List<IUIElement>().AddStrokeColorElements(expanded: expanded),
                            "stroke-weight" => new List<IUIElement>().AddStrokeWeightElements(expanded: expanded),
                            "stroke-style" => new List<IUIElement>().AddStrokeStyleElements(expanded: expanded),
                            "fill-color" => new List<IUIElement>().AddFillColorElements(expanded: expanded),
                            "fill-opacity" => new List<IUIElement>().AddFillOpacityElements(expanded: expanded),
                            _ => new()
                        }))
                );
    }

    #region Upload / Download

    [ServerToolCommand("upload")]
    public ApiEventResponse OnUploadClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Pointer },
            UIElements = new IUIElement[]
            {
                new UIEmpty() {
                    target="#"+toolContainerId //UIElementTarget.tool_sidebar_left.ToString()
                },
                new UIDiv()
                {
                    target = UIElementTarget.modaldialog.ToString(),
                    targettitle = localizer.Localize("tools.upload"),
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements = new IUIElement[]
                    {
                        new UILabel()
                        {
                            label = localizer.Localize("io.upload-label1:body")
                        },
                        new UIBreak(2),
                        new UISelect()
                        {
                            id="redlining-upload-replaceelements",
                            css = UICss.ToClass(new string[]{ UICss.ToolParameter }),
                            options=new UISelect.Option[]
                            {
                                new UISelect.Option() { value = "false", label = localizer.Localize("io.extend-current-session") },
                                new UISelect.Option() { value = "true", label = localizer.Localize("io.replace-current-session") }
                            }
                        },
                        new UIBreak(2),
                        new UIUploadFile(this.GetType(), "upload-objects") {
                            id="upload-file",
                            css=UICss.ToClass(new string[]{UICss.ToolParameter})
                        }
                    }
                }
            },
            UISetters = new IUISetter[]  // select/highlight tool
            {
                 new UISetter("webgis-redlining-tool", "pointer")
            }
        };
    }


    [ServerToolCommand("upload-objects")]
    public ApiEventResponse OnUploadObject(IBridge bridge, ApiToolEventArguments e)
    {
        var format = e["redlining-upload-format"];
        var file = e.GetFile("upload-file");
        var replaceExistingRedlining = e["redlining-upload-replaceelements"] == "true";

        GeoJsonFeatures geoJsonFeatures = file.GetFeatures(e);

        if (geoJsonFeatures?.Features is not null)
        {
            #region Project Features to WGS84 

            var epsg = geoJsonFeatures.Crs.TryGetEpsg().OrTake(Epsg.WGS84);

            if (epsg != Epsg.WGS84)
            {
                foreach (var geoJsonFeature in geoJsonFeatures?.Features?.Where(f => f?.Geometry is not null).ToArray() ?? Array.Empty<GeoJsonFeature>())
                {
                    var shape = geoJsonFeature.ToShape()?.Project(Epsg.WGS84, epsg);
                    geoJsonFeature.FromShape(shape);
                }
            }
            geoJsonFeatures.Crs = null;  // do not return a crs... it shoud be projected to WGS84 now!

            #endregion

            #region add default properties (if not redlining features => only normal geoJson features

            foreach (var geoJsonFeature in geoJsonFeatures?.Features.Where(f => f["_meta.tool"] is null))
            {
                geoJsonFeature.Properties = geoJsonFeature.DefaultProperties();
            }

            #endregion
        }

        return new ApiEventResponse()
        {
            Graphics = geoJsonFeatures == null ? null : new GraphicsResponse(bridge)
            {
                Elements = geoJsonFeatures,
                ActiveGraphicsTool = GraphicsTool.Pointer,
                ReplaceElements = replaceExistingRedlining
            },
            UIElements = new IUIElement[] {
                new UIEmpty(){
                    target = UIElementTarget.modaldialog.ToString(),
                }
            },
            UISetters = new IUISetter[]  // select/highlight tool
            {
                 new UISetter("webgis-redlining-tool", "pointer")
            }
        };
    }

    [ServerToolCommand("download")]
    public ApiEventResponse OnDownloadClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        #region Prj Files für Shape download

        List<UISelect.Option> prjOptions = new List<UISelect.Option>();

        var di = new System.IO.DirectoryInfo($"{bridge.AppEtcPath}/prj");
        if (di.Exists)
        {
            foreach (var fi in di.GetFiles("*.prj"))
            {
                try
                {
                    int epsg = int.Parse(fi.Name.Substring(0, fi.Name.Length - 4));
                    var sRef = bridge.CreateSpatialReference(epsg);

                    if (sRef != null)
                    {
                        prjOptions.Add(new UISelect.Option() { value = epsg.ToString(), label = $"{epsg}: {sRef.Name}" });
                    }
                }
                catch { }
            }
        }

        var defaultEpsgCode = e.GetConfigValue(ConfigDeaultDownloadEpsgCode).OrTake((e.MapCrs ?? 0).ToString());

        #endregion

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Pointer },
            UIElements = new IUIElement[]
            {
                new UIEmpty() {
                    target="#"+toolContainerId //UIElementTarget.tool_sidebar_left.ToString()
                },
                new UIDiv()
                {
                    target = UIElementTarget.modaldialog.ToString(),
                    targettitle = localizer.Localize("tools.download"),
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements = new IUIElement[]
                    {
                        new UISelect()
                        {
                            id="redlining-download-format",
                            css = UICss.ToClass(new string[]{ UICss.ToolParameter }),
                            options=new UISelect.Option[]
                            {
                                new UISelect.Option() { value = "json", label = "Redlining Projekt (Geo-Json)" },
                                new UISelect.Option() { value = "gpx", label = "GPX Datei" },
                                new UISelect.Option() { value = "shp", label = "ESRI Shape Datei" },

                            }
                        },
                        new UIConditionDiv()
                        {
                            ConditionType = UIConditionDiv.ConditionTypes.ElementValue,
                            ContitionElementId = "redlining-download-format",
                            ConditionArguments = new string[] { "shp" },
                            ConditionResult = true,
                            elements=new IUIElement[]
                            {
                                new UISelect()
                                {
                                    id = "redlining-download-prj",
                                    css = UICss.ToClass(new string[]{UICss.ToolParameter}),
                                    options = prjOptions
                                }
                            }
                        },
                        new UILabel()
                        {
                            label = localizer.Localize("io.download-label1:body")
                        },
                        new UIButtonContainer(new UIButton(UIButton.UIButtonType.servertoolcommand, "download-objects")
                        {
                            css = UICss.ToClass(new string[] { UICss.DefaultButtonStyle }),
                            text = localizer.Localize("download")
                        }),
                        new UIConditionDiv()
                        {
                            ConditionType = UIConditionDiv.ConditionTypes.ElementValue,
                            ContitionElementId = "redlining-download-format",
                            ConditionArguments = new string[] { "gpx" },
                            ConditionResult = true,
                            elements=new IUIElement[]
                            {
                                new UILabel()
                                {
                                    label = localizer.Localize("io.download-label-gpx:body")
                                }
                            }
                        },
                        new UIConditionDiv()
                        {
                            ConditionType = UIConditionDiv.ConditionTypes.ElementValue,
                            ContitionElementId = "redlining-download-format",
                            ConditionArguments = new string[] { "shp" },
                            ConditionResult = true,
                            elements=new IUIElement[]
                            {
                                new UILabel()
                                {
                                    label = localizer.Localize("io.download-label-shape:body")
                                }
                            }
                        },
                        new UIConditionDiv()
                        {
                            ConditionType = UIConditionDiv.ConditionTypes.ElementValue,
                            ContitionElementId = "redlining-download-format",
                            ConditionArguments = new string[] { "json" },
                            ConditionResult = true,
                            elements=new IUIElement[]
                            {
                                new UILabel()
                                {
                                    label = localizer.Localize("io.download-label-json:body")
                                }
                            }
                        },
                        new UIHidden(){
                            id = "redlining-download-geojson",
                            css = UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsGeoJson})
                        }
                    }
                }
            },
            UISetters = new IUISetter[]  // select/highlight tool
            {
                 new UISetter("webgis-redlining-tool", "pointer"),
                 new UISetter("redlining-download-prj", defaultEpsgCode)
            }
        };
    }

    [ServerToolCommand("download-objects")]
    public ApiEventResponse OnDownloadObjects(IBridge bridge, ApiToolEventArguments e)
    {
        string format = e["redlining-download-format"];
        string redliningGeoJson = e["redlining-download-geojson"];
        var features = JSerializer.Deserialize<GeoJsonFeatures>(redliningGeoJson);

        if (!e.GetConfigBool(ConfigAllowDownloadFromSelection, false))
        {
            features.Features = features.Features.Where(f => !"selection".Equals(f.GetPropery<string>("_meta.source")))
                                                 .ToArray();
        }

        IExport export = null;
        string name;

        if (format == "gpx")
        {
            export = new GpxExport();
            name = "redlining.gpx";
        }
        else if (format == "shp")
        {
            export = new ShapeFileExport(bridge, e.GetInt("redlining-download-prj"));
            name = "redlining.zip";
        }
        else if (format == "json")
        {
            export = new GeoJsonExport();
            name = "redlining.json";
        }
        else
        {
            throw new Exception($"Unsuported download formant {format}");
        }

        export.AddFeatures(features);

        return new ApiRawDownloadEventResponse(name, export.GetBytes(true));
    }

    #endregion

    #region From Selection

    [ServerToolCommand("add-from-selection-dialog")]
    async public Task<ApiEventResponse> AddFromSelectionDialog(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        if (!e.GetConfigBool(ConfigAllowAddFromSelection, false))
        {
            return null;
        }

        var maxFeatures = e.GetConfigInt(ConfigAllowAddFromSelectionMaxFeatures);
        if (maxFeatures > 0 && e.SelectionInfo.ObjectIds.Count() > maxFeatures)
        {
            throw new Exception($"Es dürfen maximal {maxFeatures} ins Redlining übernommen werden");
        }

        //var query = await bridge.GetFirstLayerQuery(e.SelectionInfo.ServiceId, e.SelectionInfo.LayerId);
        var query = await bridge.GetQuery(e.SelectionInfo.ServiceId, e.SelectionInfo.QueryId);
        if (query == null)
        {
            throw new Exception("Can't determine selelction query");
        }

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[]
            {
                new UIDiv()
                {
                    target = UIElementTarget.modaldialog.ToString(),
                    targettitle = $"{localizer.Localize("selection")}: {query.Name}",
                    css = UICss.ToClass(new string[]{  }),
                    targetwidth = "800px",
                    elements = new IUIElement[]
                    {
                        new UIColumns()
                        {
                            elements = new IUIElement[]
                            {
                                new UIColumn(320)
                                {
                                    elements = new IUIElement[]
                                    {
                                        new UILabel()
                                        {
                                            label = String.Format(localizer.Localize("selection.label1:body"), query.Name) 
                                        },
                                        new UISelect()
                                        {
                                            id="redlining-from-selection-textfield",
                                            css = UICss.ToClass(new string[]{ UICss.ToolParameter }),
                                            options = query.GetSimpleTableFields()
                                                           .Select(keyValuePair => new UISelect.Option(){ value=keyValuePair.Key, label=keyValuePair.Value })
                                                           .ToArray()
                                        },
                                        new UIBreak(2),
                                        new UIButtonContainer(new UIButton(UIButton.UIButtonType.servertoolcommand, "add-from-selection")
                                        {
                                            css = UICss.ToClass(new string[] { UICss.DefaultButtonStyle }),
                                            text = localizer.Localize("selection.take-from")
                                        })
                                    }
                                },
                                new UIColumn(320)
                                {
                                    elements = new IUIElement[]
                                    {
                                        new UILabel() {label = $"{localizer.Localize("selection.symbology")}:"},
                                        new UIDiv() {
                                            elements = e["redlining-tool"] switch
                                            {
                                                "symbol" => new List<IUIElement>().AddSymbolStyleElements(bridge, e, localizer),
                                                "text" => new List<IUIElement>().AddTextStyleElements(e, localizer),
                                                "point" => new List<IUIElement>().AddPointStyleElements(e, localizer),
                                                "line" => new List<IUIElement>().AddLineStyleElements(e, localizer),
                                                "polygon" => new List<IUIElement>().Add2DStyleElements(e, localizer),
                                                "dimline" => new List<IUIElement>().AddDimLineStyleElements(e, localizer),
                                                "hectoline" => new List<IUIElement>().AddHectoLineStyleElements(e, localizer),
                                                _ => new()
                                            }
                                        },
                                    }
                                },
                            }
                        },
                    }
                }
            }
        };
    }

    [ServerToolCommand("add-from-selection")]
    async public Task<ApiEventResponse> AddFromSelection(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        if (!e.GetConfigBool(ConfigAllowAddFromSelection, false))
        {
            return null;
        }

        var sRef4326 = bridge.CreateSpatialReference(4326);
        var selectedFeatures = await e.FeaturesFromSelectionAsync(bridge, featureSpatialReference: sRef4326,
                                                                          suppressResolveAttributeDomains: false);

        int maxVertices = e.GetConfigInt(ConfigAllowAddFromSelectionMaxVertices);
        if (maxVertices > 0)
        {
            var numVertices = selectedFeatures.Where(f => f.Shape != null)
                                              .Select(f => SpatialAlgorithms.ShapePoints(f.Shape, false).Count)
                                              .Sum();

            if (numVertices > maxVertices)
            {
                throw new Exception(String.Format(localizer.Localize("selection.exception-to-many-objects"), maxVertices));
            }
        }

        GeoJsonFeatures geoJsonFeatures = new GeoJsonFeatures();

        string textFieldName = e["redlining-from-selection-textfield"];
        var redliningTool = e["redlining-tool"];

        geoJsonFeatures.Features = selectedFeatures
                                            .Select(selectedFeature =>
                                            {
                                                var geoJsonFeature = new GeoJsonFeature();
                                                var selectedFeatureShape = selectedFeature.Shape;

                                                switch (redliningTool)
                                                {
                                                    case "symbol":
                                                        selectedFeatureShape = selectedFeatureShape.ShapeToPoint(sRef: sRef4326);
                                                        geoJsonFeature.Properties = e.DefaultSymbolJsonProperties(selectedFeature[textFieldName], "selection");
                                                        break;
                                                    case "text":
                                                        selectedFeatureShape = selectedFeatureShape.ShapeToPoint(sRef: sRef4326);
                                                        geoJsonFeature.Properties = e.DefaultTextJsonProperties(selectedFeature[textFieldName], "selection");
                                                        break;
                                                    case "point":
                                                        geoJsonFeature.Properties = e.DefaultPointGeoJsonProperties(selectedFeature[textFieldName], "selection");
                                                        break;
                                                    case "polygon":
                                                        geoJsonFeature.Properties = e.DefaultPolygonGeoJsonProperties(selectedFeature[textFieldName], "selection");
                                                        break;
                                                    case "line":
                                                        geoJsonFeature.Properties = e.DefaultLineGeoJsonProperties(selectedFeature[textFieldName], "selection");
                                                        break;
                                                }

                                                geoJsonFeature.FromShape(selectedFeatureShape);

                                                return geoJsonFeature;
                                            })
                                            .ToArray();


        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge)
            {
                Elements = geoJsonFeatures,
                ActiveGraphicsTool = e["redlining-tool"].ParseOrDefault(GraphicsTool.Pointer),
                ReplaceElements = false
            },
            UIElements = new IUIElement[] {
                new UIEmpty(){
                    target = UIElementTarget.modaldialog.ToString(),
                }
            }
        };
    }

    #endregion

    #region IO

    [ServerToolCommand("save")]
    public ApiEventResponse OnSaveClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Pointer },
            UIElements = new IUIElement[] {
                new UIEmpty() {
                    target="#"+toolContainerId //UIElementTarget.tool_sidebar_left.ToString()
                },
                new UIDiv(){
                    target=UIElementTarget.modaldialog.ToString(),
                    targettitle = localizer.Localize("tools.save"),
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements=new UIElement[]{
                        new UILabel(){ label = localizer.Localize("label-name") },
                        new UIBreak(),
                        new UIInputAutocomplete(UIInputAutocomplete.MethodSource(bridge,this.GetType(),"autocomplete-projects"),0){
                            id="redlining-io-save-name",
                            css=UICss.ToClass(new string[]{UICss.ToolParameter}),
                        },
                        new UIButtonContainer(new UIButton(UIButton.UIButtonType.servertoolcommand,"save-project") {
                            text = localizer.Localize("save")
                        }),
                        new UIHidden(){
                            id="redlining-geojson",
                            css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsGeoJson})
                        }
                    }
                }
            },
            UISetters = new IUISetter[]  // select/highlight tool
            {
                 new UISetter("webgis-redlining-tool", "pointer")
            }
        };
    }

    [ServerToolCommand("autocomplete-projects")]
    public ApiEventResponse OnAutocompleteProject(IBridge bridge, ApiToolEventArguments e)
    {
        List<string> values = new List<string>();
        string term = e["term"].ToLower();

        foreach (string name in bridge.Storage.GetNames())
        {
            if (name.ToLower().Contains(term))
            {
                values.Add(name);
            }
        }

        values.Sort();

        return new ApiRawJsonEventResponse(values.ToArray());
    }

    [ServerToolCommand("save-project")]
    public ApiEventResponse OnSaveProject(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        string name = e["redlining-io-save-name"];

        if (!name.IsValidFilename(out string invalidChars))
        {
            throw new Exception(String.Format(localizer.Localize("io.exception-invalid-char"), invalidChars));
        }

        bridge.Storage.Save(name, e["redlining-geojson"]);

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[] {
                new UIEmpty(){
                    target=UIElementTarget.modaldialog.ToString(),
                }
            }
        };
    }

    [ServerToolCommand("open")]
    public ApiEventResponse OnOpenClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Redlining> localizer)
    {
        var names = bridge.Storage.GetNames();

        if (names == null || names.Length == 0)
        {
            throw new Exception(localizer.Localize("io.exception-no-projects-found"));
        }

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Pointer },
            UIElements = new IUIElement[] {
                 new UIEmpty() {
                    target=$"#{toolContainerId}" //UIElementTarget.tool_sidebar_left.ToString()
                },
                new UIDiv(){
                    target=UIElementTarget.modaldialog.ToString(),
                    targettitle = localizer.Localize("tools.open"),
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements=new UIElement[]{
                        new UILabel(){ label = localizer.Localize("label-name") },
                        new UIBreak(),
                        new UISelect(names){
                              id="redlining-io-load-name",
                              css=UICss.ToClass(new []{ UICss.ToolParameter }),
                        },
                        new UIButtonContainer(
                            new []
                            {
                                new UIButton(UIButton.UIButtonType.servertoolcommand,"delete-project") {
                                    css = UICss.ToClass(new []{ UICss.DangerButtonStyle }),
                                    text = localizer.Localize("delete")
                                },
                                new UIButton(UIButton.UIButtonType.servertoolcommand,"load-project") {
                                    text = localizer.Localize("open")
                                }
                            })
                    }
                }
            },
            UISetters = new IUISetter[]  // select/highlight tool
            {
                 new UISetter("webgis-redlining-tool", "pointer")
            }
        };
    }

    [ServerToolCommand("load-project")]
    public ApiEventResponse OnLoadProject(IBridge bridge, ApiToolEventArguments e)
    {
        string name = e["redlining-io-load-name"];

        string geoJson = bridge.Storage.LoadString(name);
        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { Elements = geoJson },
            UIElements = new IUIElement[] {
                new UIEmpty(){
                    target=UIElementTarget.modaldialog.ToString(),
                }
            }
        };
    }

    [ServerToolCommand("delete-project")]
    [ToolCommandConfirmation("io.confirm-delete-project", ApiToolConfirmationType.YesNo, ApiToolConfirmationEventType.ButtonClick)]
    public ApiEventResponse OnDeleteProject(IBridge bridge, ApiToolEventArguments e)
    {
        string name = e["redlining-io-load-name"];

        if (!String.IsNullOrEmpty(name))
        {
            bridge.Storage.Remove(name);
        }

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[] {
                new UIEmpty(){
                    target=UIElementTarget.modaldialog.ToString(),
                }
            }
        };
    }

    [ServerToolCommand("share")]
    public ApiEventResponse OnShareClick(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveTool = new Serialization.ShareMap()
        };
    }

    #endregion

    #endregion
}
