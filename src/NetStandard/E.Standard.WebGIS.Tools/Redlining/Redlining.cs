using E.Standard.Extensions.Compare;
using E.Standard.GeoJson;
using E.Standard.GeoJson.Extensions;
using E.Standard.Json;
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
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Redlining;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(ClientDeviceDependent = true, SelectionInfoDependent = true, MapCrsDependent = true)]
[ToolConfigurationSection("redlining")]
[ToolHelp("tools/general/redlining/index.html")]
public class Redlining : IApiServerTool, IApiButtonResources, IGraphicsTool, IApiToolConfirmation
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

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        List<IUIElement> uiImageButtons = new List<IUIElement>();

        bool allowAdvancedTools = bridge.UserAgent.IsInternetExplorer == false;

        uiImageButtons.AddRange(new IUIElement[]{
                        new UIImageButton(this.GetType(),"pointer",UIButton.UIButtonType.servertoolcommand,"pointer"){
                            value="pointer",
                            text="Auswählen"
                        },
                        new UIImageButton(this.GetType(),"symbol",UIButton.UIButtonType.servertoolcommand,"symbol"){
                            value="symbol",
                            text="Symbol"
                        },
                        new UIImageButton(this.GetType(), "text", UIButton.UIButtonType.servertoolcommand, "text")
                        {
                            value = "text",
                            text="Text"
                        },
                        new UIImageButton(this.GetType(), "point", UIButton.UIButtonType.servertoolcommand, "point")
                        {
                            value ="point",
                            text = "Punkt"
                        },
                        new UIImageButton(this.GetType(), "freehand", UIButton.UIButtonType.servertoolcommand, "freehand")
                        {
                            value = "freehand",
                            text="Freihand"
                        },
                        new UIImageButton(this.GetType(),"line",UIButton.UIButtonType.servertoolcommand,"line"){
                            value="line",
                            text="Linie"
                        },
                        new UIImageButton(this.GetType(),"polygon",UIButton.UIButtonType.servertoolcommand,"polygon"){
                            value="polygon",
                            text="Fläche"
                        },
                        new UIImageButton(this.GetType(),"rectangle", UIButton.UIButtonType.servertoolcommand,"rectangle")
                        {
                            value="rectangle",
                            text="Rechteck"
                        },
                        new UIImageButton(this.GetType(),"circle", UIButton.UIButtonType.servertoolcommand,"circle")
                        {
                            value="circle",
                            text="Kreis"
                        },
                        new UIImageButton(this.GetType(),"distance_circle", UIButton.UIButtonType.servertoolcommand,"distance_circle")
                        {
                            value="distance_circle",
                            text="Umgebungs kreis"
                        },
                        new UIImageButton(this.GetType(),"compass", UIButton.UIButtonType.servertoolcommand,"compass_rose")
                        {
                            value="compass_rose",
                            text="Kompass Rose"
                        },
                        new UIImageButton(this.GetType(),"dimline", UIButton.UIButtonType.servertoolcommand,"dimline")
                        {
                            value="dimline",
                            text="Bemaßung"
                        },
                        new UIImageButton(this.GetType(),"hectoline", UIButton.UIButtonType.servertoolcommand,"hectoline")
                        {
                            value="hectoline",
                            text="Hektometrierungslinine"
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

        if (bridge.CurrentUser != null && !bridge.CurrentUser.IsAnonymous)
        {
            uiImageButtons.AddRange(new IUIElement[]{
                        new UIImageButton(this.GetType(),"save",UIButton.UIButtonType.servertoolcommand,"save"){
                            value="save",
                            text="Zeichnung speichern"
                        },
                        new UIImageButton(this.GetType(),"open",UIButton.UIButtonType.servertoolcommand,"open"){
                            value="open",
                            text="Zeichnung laden"
                        }
                        //,new UIHidden(){
                        //    id="redlining-tool",
                        //    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsTool})
                        //}
            });
        }

        uiImageButtons.AddRange(new IUIElement[]{
                        new UIImageButton(this.GetType(),"share",UIButton.UIButtonType.servertoolcommand,"share"){
                            value="share",
                            text="Teilen"
                        },
                        new UIImageButton(this.GetType(),"upload",UIButton.UIButtonType.servertoolcommand,"upload"){
                            value="upload",
                            text="Hochladen (GPX, ...)"
                        },
                        new UIImageButton(this.GetType(),"download",UIButton.UIButtonType.servertoolcommand,"download"){
                            value="download",
                            text="Herunterladen (GPX, ...)"
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

    public ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e)
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

    virtual public string Name
    {
        get { return "Zeichnen (Redlining)"; }
    }

    public string Container
    {
        get { return "Werkzeuge"; }
    }

    public string Image
    {
        get { return UIImageButton.ToolResourceImage(this, "redlining"); }
    }

    public string ToolTip
    {
        get { return "Einfaches Zeichnen in der Karte"; }
    }

    public bool HasUI
    {
        get { return true; }
    }

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
    async public Task<ApiEventResponse> OnSymbolToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        var uiElements = new List<IUIElement>().AddSymbolStyleElements(bridge, e, true);

        if (e.UseMobileBehavior())
        {
            uiElements.AddRange(new IUIElement[]
            {
                        new UIBreak(),
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.removecurrentgraphicselement){
                            text="Sketch entfernen",
                            css=UICss.ToClass(new string[] { UICss.CancelButtonStyle })
                        },
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.assumecurrentgraphicselement){
                            text="Symbol übernehmen"
                        }
            });
        }

        if (e.SelectionInfo != null)
        {
            if (e.GetConfigBool(ConfigAllowAddFromSelection, false))
            {
                uiElements.Add(new UIButton(UIButton.UIButtonType.servertoolcommand, "add-from-selection-dialog")
                {
                    text = $"Symbole aus Selektion {await e.SelectionInfo.GetQueryName(bridge)} übernehmen...",
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
                    targettitle="Symbol setzen",
                    elements=uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("point")]
    async public Task<ApiEventResponse> OnPointToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        var uiElements = new List<IUIElement>().AddPointStyleElements(e, true).AsStagedStyleElements(e);

        if (e.SelectionInfo != null)
        {
            if (e.GetConfigBool(ConfigAllowAddFromSelection, false) && e.SelectionInfo.GeometryType == "point")
            {
                uiElements.Add(new UIButton(UIButton.UIButtonType.servertoolcommand, "add-from-selection-dialog")
                {
                    text = $"Punkte aus Selektion {await e.SelectionInfo.GetQueryName(bridge)} übernehmen...",
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
                    targettitle = "Punkt setzen",
                    elements = uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("text")]
    async public Task<ApiEventResponse> OnTextToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        var uiElements = new List<IUIElement>().AddTextStyleElements(e, true).AsStagedStyleElements(e);

        if (e.GetConfigBool(ConfigAllowAddFromSelection, false))
        {
            if (e.SelectionInfo != null)
            {
                uiElements.Add(new UIButton(UIButton.UIButtonType.servertoolcommand, "add-from-selection-dialog")
                {
                    text = $"Texte aus Selektion {await e.SelectionInfo.GetQueryName(bridge)} übernehmen...",
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
                    target= "#"+toolContainerId, //UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle="Text setzen",
                    elements= uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("freehand")]
    public ApiEventResponse OnFreehandToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        var uiElements = new List<IUIElement>().AddFreehandStyleElements(e, true).AsStagedStyleElements(e);

        if (e.UseMobileBehavior())
        {
            uiElements.AddRange(new IUIElement[]
            {
                        new UIBreak(),
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.removecurrentgraphicselement){
                            text="Sketch entfernen",
                            css=UICss.ToClass(new string[] { UICss.CancelButtonStyle })
                        },
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.assumecurrentgraphicselement){
                            text="Linie übernehmen"
                        }
            });
        }

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Freehand },
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target="#"+toolContainerId, //UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle="Freihand zeichnen",
                    elements= uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("line")]
    async public Task<ApiEventResponse> OnLineToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        var uiElements = new List<IUIElement>().AddLineStyleElements(e, true).AsStagedStyleElements(e);

        if (e.UseMobileBehavior())
        {
            uiElements.AddRange(new IUIElement[]
            {
                        new UIBreak(),
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.removecurrentgraphicselement){
                            text="Sketch entfernen",
                            css=UICss.ToClass(new string[] { UICss.CancelButtonStyle })
                        },
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.assumecurrentgraphicselement){
                            text="Linie übernehmen"
                        }
            });
        }

        if (e.GetConfigBool(ConfigAllowAddFromSelection, false))
        {
            if (e.SelectionInfo != null && e.SelectionInfo.GeometryType == "line")
            {
                uiElements.Add(new UIButton(UIButton.UIButtonType.servertoolcommand, "add-from-selection-dialog")
                {
                    text = $"Linien aus Selektion {await e.SelectionInfo.GetQueryName(bridge)} übernehmen...",
                    css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.LineBreakButton })
                });
            }
        }

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Line },
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target="#"+toolContainerId, //UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle="Linie zeichnen",
                    elements= uiElements.ToArray()
                }
            }
        };
    }

    async public Task<ApiEventResponse> On2DToolClick(IBridge bridge, ApiToolEventArguments e, GraphicsTool tool)
    {
        var uiElements = new List<IUIElement>().Add2DStyleElements(e, true).AsStagedStyleElements(e);

        if (e.UseMobileBehavior())
        {
            uiElements.AddRange(new IUIElement[]
            {
                                new UIBreak(),
                                new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.removecurrentgraphicselement){
                                    text="Sketch entfernen",
                                    css=UICss.ToClass(new string[] { UICss.CancelButtonStyle })
                                },
                                new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.assumecurrentgraphicselement){
                                    text="Polygon übernehmen"
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
                        text = $"Flächen aus Selektion {await e.SelectionInfo.GetQueryName(bridge)} übernehmen...",
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
                    targettitle="Polygon zeichnen",
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
    public Task<ApiEventResponse> OnPolygonToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        return On2DToolClick(bridge, e, GraphicsTool.Polygon);
    }

    [ServerToolCommand("rectangle")]
    public Task<ApiEventResponse> OnRectangleToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        return On2DToolClick(bridge, e, GraphicsTool.Rectangle);
    }

    [ServerToolCommand("circle")]
    public Task<ApiEventResponse> OnCircleToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        return On2DToolClick(bridge, e, GraphicsTool.Circle);
    }

    [ServerToolCommand("distance_circle")]
    public ApiEventResponse OnDistanceCircleClick(IBridge bridge, ApiToolEventArguments e)
    {
        var uiElements = new List<IUIElement>().AddDistanceCircleStyleElements(e, true).AsStagedStyleElements(e);

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Distance_Circle },
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target="#"+toolContainerId, //UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle="Umgebungskreis zeichnen",
                    elements= uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("compass_rose")]
    public ApiEventResponse OnCompassRoseClick(IBridge bridge, ApiToolEventArguments e)
    {
        var uiElements = new List<IUIElement>().AddCompassRoseStyleElements(e, true).AsStagedStyleElements(e);

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Compass_Rose },
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target="#"+toolContainerId, //UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle="Kompass Rose zeichnen",
                    elements= uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("dimline")]
    public ApiEventResponse OnDimLineToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        var uiElements = new List<IUIElement>().AddDimLineStyleElements(e, true).AsStagedStyleElements(e);

        if (e.UseMobileBehavior())
        {
            uiElements.AddRange(new IUIElement[]
            {
                        new UIBreak(),
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.removecurrentgraphicselement){
                            text="Sketch entfernen",
                            css=UICss.ToClass(new string[] { UICss.CancelButtonStyle })
                        },
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.assumecurrentgraphicselement){
                            text="Linie übernehmen"
                        }
            });
        }

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.DimLine },
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target="#"+toolContainerId, //UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle="Bemaßung zeichnen",
                    elements= uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("hectoline")]
    public ApiEventResponse OnHectoLineToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        var uiElements = new List<IUIElement>().AddHectoLineStyleElements(e).AsStagedStyleElements(e);

        if (e.UseMobileBehavior())
        {
            uiElements.AddRange(new IUIElement[]
            {
                        new UIBreak(),
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.removecurrentgraphicselement){
                            text="Sketch entfernen",
                            css=UICss.ToClass(new string[] { UICss.CancelButtonStyle })
                        },
                        new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.assumecurrentgraphicselement){
                            text="Linie übernehmen"
                        }
            });
        }

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.HectoLine },
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target="#"+toolContainerId, //UIElementTarget.tool_sidebar_left.ToString(),
                    targettitle="Hektometrierungslinie zeichnen",
                    elements= uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("element-selected")]
    async public Task<ApiEventResponse> OnElementClick(IBridge bridge, ApiToolEventArguments e)
    {
        ApiEventResponse resp = null;
        switch (e["redlining-tool"])
        {
            case "symbol":
                resp = await OnSymbolToolClick(bridge, e);
                break;
            case "point":
                resp = await OnPointToolClick(bridge, e);
                break;
            case "line":
                resp = await OnLineToolClick(bridge, e);
                break;
            case "polygon":
                resp = await OnPolygonToolClick(bridge, e);
                break;
            case "freehand":
                resp = OnFreehandToolClick(bridge, e);
                break;
            case "distance_circle":
                resp = OnDistanceCircleClick(bridge, e);
                break;
            case "compass_rose":
                resp = OnCompassRoseClick(bridge, e);
                break;
            case "circle":
                resp = await OnCircleToolClick(bridge, e);
                break;
            case "rectangle":
                resp = await OnRectangleToolClick(bridge, e);
                break;
            case "text":
                resp = await OnTextToolClick(bridge, e);
                break;
            case "dimline":
                resp = OnDimLineToolClick(bridge, e);
                break;
            case "hectoline":
                resp = OnHectoLineToolClick(bridge, e);
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
    public ApiEventResponse OnShowSymbolSelector(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .AddUIElements(
                new UIDiv()
                    .WithTarget(UIElementTarget.tool_modaldialog_noblocking_closable.ToString())
                        .WithTargetTitle($"Symbolik änderen: {bridge.Globalize(e["redlinig-symbol-type"])}")
                        .WithTargetOnClose(ApiClientButtonCommand.refreshgraphicsui.ToString())
                        .AddChildren(e["redlinig-symbol-type"] switch
                        {
                            "symbol" => new List<IUIElement>().AddSymbolStyleElements(bridge, e),
                            "text" => new List<IUIElement>().AddTextStyleElements(e, collapseExclusive: false, isCollapsed: false),
                            "point" => new List<IUIElement>().AddPointStyleElements(e, collapseExclusive: false, isCollapsed: false),
                            "freehand" => new List<IUIElement>().AddFreehandStyleElements(e, collapseExclusive: false, isCollapsed: false),
                            "line" => new List<IUIElement>().AddLineStyleElements(e, collapseExclusive: false, isCollapsed: false),
                            "polygon" => new List<IUIElement>().Add2DStyleElements(e, collapseExclusive: false, isCollapsed: false),
                            "rectangle" => new List<IUIElement>().Add2DStyleElements(e, collapseExclusive: false, isCollapsed: false),
                            "circle" => new List<IUIElement>().Add2DStyleElements(e, collapseExclusive: false, isCollapsed: false),
                            "distance_circle" => new List<IUIElement>().AddDistanceCircleStyleElements(e, collapseExclusive: false, isCollapsed: false),
                            "compass_rose" => new List<IUIElement>().AddCompassRoseStyleElements(e, collapseExclusive: false, isCollapsed: false),
                            "dimline" => new List<IUIElement>().AddDimLineStyleElements(e, collapseExclusive: false, isCollapsed: false),
                            "hectoline" => new List<IUIElement>().AddHectoLineStyleElements(e, collapseExclusive: false, isCollapsed: false),
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
    public ApiEventResponse OnUploadClick(IBridge bridge, ApiToolEventArguments e)
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
                    targettitle="Hochladen (Gpx, ...)",
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements = new IUIElement[]
                    {
                        new UILabel()
                        {
                            label=bridge.GetCustomTextBlock(this, "label2", "Hier können Redlining Objekte hochgeladen werden. Gültige Dateiendungen sind hier *.gpx")
                        },
                        new UIBreak(2),
                        new UISelect()
                        {
                            id="redlining-upload-replaceelements",
                            css = UICss.ToClass(new string[]{ UICss.ToolParameter }),
                            options=new UISelect.Option[]
                            {
                                new UISelect.Option() { value = "false", label = "Bestehendes Redlining erweitern" },
                                new UISelect.Option() { value = "true", label = "Bestehendes Redlining ersetzen" }
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
    public ApiEventResponse OnDownloadClick(IBridge bridge, ApiToolEventArguments e)
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
                    targettitle="Herunterladen (Gpx, Shape)",
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
                            label=bridge.GetCustomTextBlock(this, "label1", "Hier können Redlining Objekte herunter geladen werden.")
                        },
                        new UIButtonContainer(new UIButton(UIButton.UIButtonType.servertoolcommand, "download-objects")
                        {
                            css = UICss.ToClass(new string[] { UICss.DefaultButtonStyle }),
                            text = "Herunterladen"
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
                                    label=bridge.GetCustomTextBlock(this, "label-gpx", "Bei GPX werden nur die gezeichneten Linien als 'Tracks' und die Texte bzw. Symbole als 'Waypoints' exportiert.")
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
                                    label=bridge.GetCustomTextBlock(this, "label-shp", "Für ESRI Shape Dateien muss noch zusätzlich die Zielprojektion angegeben werden. Für jeden Geometrietyp wird ein Shapefile angelegt und in ein ZIP File verpackt.")
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
                                    label=bridge.GetCustomTextBlock(this, "label-json", "Bei Redlining Projekten werden alle Objekte (plus Darstellung) als GeoJSON herunter geladen und können später wieder hochgeladen werden.")
                                }
                            }
                        },
                        new UIHidden(){
                            id="redlining-download-geojson",
                            css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsGeoJson})
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
    async public Task<ApiEventResponse> AddFromSelectionDialog(IBridge bridge, ApiToolEventArguments e)
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
                    targettitle="Aus Selektion übernehmen",
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
                                            label=bridge.GetCustomTextBlock(this, "label1", $"Die selektierten Objekte aus { query.Name } können ins Redlining übernommen werden.|Die Darstellung (Farben) werden aus den aktuellen Redlining Einstellungen übernommen und können nachher für jedes Objekt wieder einzeln geändert werden.|Um die Redlining Element besser zu unterscheiden, können sie später über das hier angegeben Feld identifiziert werden:")
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
                                            text = "Aus Selektion übernehmen"
                                        })
                                    }
                                },
                                new UIColumn(320)
                                {
                                    elements = new IUIElement[]
                                    {
                                        new UILabel() {label = "Symbolik:"},
                                        new UIDiv() {
                                            elements = e["redlining-tool"] switch
                                            {
                                                "symbol" => new List<IUIElement>().AddSymbolStyleElements(bridge, e),
                                                "text" => new List<IUIElement>().AddTextStyleElements(e),
                                                "point" => new List<IUIElement>().AddPointStyleElements(e),
                                                "line" => new List<IUIElement>().AddLineStyleElements(e),
                                                "polygon" => new List<IUIElement>().Add2DStyleElements(e),
                                                "dimline" => new List<IUIElement>().AddDimLineStyleElements(e),
                                                "hectoline" => new List<IUIElement>().AddHectoLineStyleElements(e),
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
    async public Task<ApiEventResponse> AddFromSelection(IBridge bridge, ApiToolEventArguments e)
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
                throw new Exception($"Selectionen mit Objekten mit mehr als {maxVertices} Stützpunkten dürfen nicht ins Redlining übernommen werden.");
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
    public ApiEventResponse OnSaveClick(IBridge bridge, ApiToolEventArguments e)
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
                    targettitle="Zeichnung speichern",
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements=new UIElement[]{
                        new UILabel(){label="Name"},
                        new UIBreak(),
                        new UIInputAutocomplete(UIInputAutocomplete.MethodSource(bridge,this.GetType(),"autocomplete-projects"),0){
                            id="redlining-io-save-name",
                            css=UICss.ToClass(new string[]{UICss.ToolParameter}),
                        },
                        new UIButtonContainer(new UIButton(UIButton.UIButtonType.servertoolcommand,"save-project") {
                            text="Speichern..."
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
    public ApiEventResponse OnSaveProject(IBridge bridge, ApiToolEventArguments e)
    {
        string name = e["redlining-io-save-name"];

        if (!name.IsValidFilename(out string invalidChars))
        {
            throw new Exception($"Ungültiges Zeichen im Namen. Vermeinden Sie folgende Zeichen: {invalidChars}");
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
    public ApiEventResponse OnOpenClick(IBridge bridge, ApiToolEventArguments e)
    {
        var names = bridge.Storage.GetNames();

        if (names == null || names.Length == 0)
        {
            throw new Exception("Unter ihrem Benutzer sind bisher noch keine Redlining Projete gespeichert worden. Speichern sie ein Redlining Projekt, bevor sie dieses Werkzeug verwenden.");
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
                    targettitle="Zeichnung laden",
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements=new UIElement[]{
                        new UILabel(){label="Name"},
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
                                    text="Löschen..."
                                },
                                new UIButton(UIButton.UIButtonType.servertoolcommand,"load-project") {
                                    text="Laden..."
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
    [ToolCommandConfirmation("Soll das Redlining Projekt '{redlining-io-load-name}' wirklich gelöscht werden?", ApiToolConfirmationType.YesNo, ApiToolConfirmationEventType.ButtonClick)]
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
