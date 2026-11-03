using System;
using System.Collections.Generic;

using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

namespace E.Standard.WebGIS.Tools.MapMarkup.Extensions;

static internal class UIElementExtensions
{
    #region By Symbol Type (line, point, text)
    static public List<IUIElement> AddSymbolStyleElements(this List<IUIElement> collection, IBridge bridge, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false)
    {
        var holder = CreateHolder(e, stagedOnly, new IUIElement[]
        {
                new UISymbolSelector(bridge, localizer.Localize("symbology.symbol"),
                            buttonCommand: ApiClientButtonCommand.setgraphicssymbol,
                            symbolId: (string)e.GetValue("mapmarkup-symbol",null)
                        ) {
                    //css=UICss.ToClass(new string[]{UICss.ToolParameterPersistent}),
                    id="mapmarkup-symbol-symbol",
                }
        });

        if (!e.UseMobileBehavior() && stagedOnly == true)
        {
            holder.VisibilityDependency = VisibilityDependency.None;
            holder.style = "display:none";
        }

        collection.Add(holder);

        return collection;
    }

    static public List<IUIElement> AddPointStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        collection.Add(CreateHolder(e, stagedOnly, new IUIElement[]
        {
            new UIColorSelector(localizer.Localize("symbology.point-color"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicspointcolor, false)
            {
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value = e.GetValue("mapmarkup-pointcolor","#ff0000"),
                id = "mapmarkup-point-color"
            },
            new UILineWieghtSelector(localizer.Localize("symbology.point-size"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicspointsize){
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior = ExpandMode(stagedOnly),
                value = e.GetValue("mapmarkup-pointsize", 10),
                id = "mapmarkup-point-size"
            },
        }));

        return collection;
    }

    static public List<IUIElement> AddTextStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        collection.Add(CreateHolder(e, stagedOnly, new IUIElement[]
        {
            new UIFontSizeSelector(localizer.Localize("symbology.font-size"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicstextsize)
            {
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value = e.GetValue("mapmarkup-fontsize", 12),
                //css = UICss.ToClass(new string[] { UICss.ToolParameter, UICss.ToolParameterPersistent }),
                id = "mapmarkup-text-fontsize"
            },
            new UIFontStyleSelector(localizer.Localize("symbology.font-style"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicstextstyle)
            {
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value = e.GetValue("mapmarkup-fontstyle","regular"),
                //css = UICss.ToClass(new string[] { UICss.ToolParameter, UICss.ToolParameterPersistent }),
                id = "mapmarkup-text-fontstyle"
            },
            new UIColorSelector(localizer.Localize("symbology.font-color"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicstextcolor, false)
            {
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value = e.GetValue("mapmarkup-fontcolor","#000000"),
                //css = UICss.ToClass(new string[] { UICss.ToolParameter, UICss.ToolParameterPersistent }),
                id = "mapmarkup-text-fontcolor"
            }
        }));

        return collection;
    }

    static public List<IUIElement> AddFreehandStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        var lineColor = e.GetValue("mapmarkup-color", "#ff0000")?.ToString();
        if (String.IsNullOrWhiteSpace(lineColor) || lineColor == "none")
        {
            lineColor = "#ff0000";
        }

        collection.Add(CreateHolder(e, stagedOnly, new IUIElement[]
        {
            new UIColorSelector(localizer.Localize("symbology.line-color"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslinecolor, false) {
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=lineColor,
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-color") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-line-linecolor"
            },
            new UILineWieghtSelector(localizer.Localize("symbology.line-weight"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslineweight){
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-lineweight",4),
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-lineweight") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-line-lineweight"
            }
        }));

        return collection;
    }

    static public List<IUIElement> AddLineStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        var lineColor = e.GetValue("mapmarkup-color", "#ff0000")?.ToString();
        if (String.IsNullOrWhiteSpace(lineColor) || lineColor == "none")
        {
            lineColor = "#ff0000";
        }

        collection.Add(CreateHolder(e, stagedOnly, new IUIElement[]
        {
                new UIColorSelector(localizer.Localize("symbology.line-color"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslinecolor, false) {
                    CollapseState = IsCollapsed(isCollapsed),
                    ExpandBehavior=ExpandMode(collapseExclusive),
                    value=lineColor,
                    //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-color") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                    id="mapmarkup-line-linecolor"
                },
                new UILineWieghtSelector(localizer.Localize("symbology.line-weight"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslineweight){
                    CollapseState = IsCollapsed(/*isCollapsed*/true),
                    ExpandBehavior=ExpandMode(collapseExclusive),
                    value=e.GetValue("mapmarkup-lineweight", 4),
                    //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-lineweight") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                    id="mapmarkup-line-lineweight"
                },
                new UILineStyleSelector(localizer.Localize("symbology.line-style"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslinestyle){
                    CollapseState = IsCollapsed(/*isCollapsed*/true),
                    ExpandBehavior=ExpandMode(collapseExclusive),
                    //css=UICss.ToClass(new string[]{e.IsEmpty("mapmarkup-linestyle")? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                    id="mapmarkup-line-linestyle",
                    value=e.GetValue("mapmarkup-linestyle", "1")
                }
        }));

        return collection;
    }

    static public List<IUIElement> Add2DStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        collection.Add(CreateHolder(e, stagedOnly, new IUIElement[]
        {
            new UIColorSelector(localizer.Localize("symbology.fill-color"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicsfillcolor, true){
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-fillcolor", "#ffff00"),
                //css=UICss.ToClass(new string[]{e.IsEmpty("mapmarkup-fillcolor")?UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-polyline-fillcolor"
            },
            new UIOpacitySelector(localizer.Localize("symbology.fill-opacity"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsfillopacity)
            {
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-fillopacity", "20"),
                //css=UICss.ToClass(new string[]{e.IsEmpty("mapmarkup-fillcolor")?UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-polyline-fillopacity"
            },
            new UIColorSelector(localizer.Localize("symbology.line-color"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslinecolor, true){
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-color","#ff0000"),
                //css=UICss.ToClass(new string[]{e.IsEmpty("mapmarkup-color")?UICss.ToolParameterPersistent: UICss.ToolParameterPersistentImportant}),
                id="mapmarkup-polyline-linecolor"
            },
            new UILineWieghtSelector(localizer.Localize("symbology.line-weight"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslineweight){
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-lineweight",4),
                //css=UICss.ToClass(new string[]{e.IsEmpty("mapmarkup-lineweight")?UICss.ToolParameterPersistent: UICss.ToolParameterPersistentImportant}),
                id="mapmarkup-polyline-lineweight"
            },
            new UILineStyleSelector(localizer.Localize("symbology.line-style"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslinestyle){
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior=ExpandMode(collapseExclusive),
                //css=UICss.ToClass(new string[]{e.IsEmpty("mapmarkup-linestyle")?UICss.ToolParameterPersistent: UICss.ToolParameterPersistentImportant}),
                id="mapmarkup-polyline-linestyle",
                value=e.GetValue("mapmarkup-linestyle","1")
            }
        }));

        return collection;
    }

    static public List<IUIElement> AddDistanceCircleStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        collection.Add(CreateHolder(e, stagedOnly, new IUIElement[]
        {
            new UIColorSelector(localizer.Localize("symbology.line-color"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicslinecolor, true) {
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-color","#ff0000"),
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-color") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-distance_circle-linecolor"
            },
            new UILineWieghtSelector(localizer.Localize("symbology.line-weight"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslineweight){
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-lineweight",4),
                //css=UICss.ToClass(new string[]{e.IsEmpty("mapmarkup-lineweight")?UICss.ToolParameterPersistent: UICss.ToolParameterPersistentImportant}),
                id="mapmarkup-distance_circle-lineweight"
            },
            new UIColorSelector(localizer.Localize("symbology.fill-color"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicsfillcolor, true){
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-fillcolor", "#ffff00"),
                //css=UICss.ToClass(new string[]{e.IsEmpty("mapmarkup-fillcolor")?UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-distance_circle-fillcolor"
            },
            new UIOptionContainer()
            {
                title = localizer.Localize("symbology.properties"),
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior=ExpandMode(collapseExclusive),
                elements = new IUIElement[]
                    {
                        new UILabel(){
                            label = $"{localizer.Localize("symbology.num-circles")}:"
                        },
                        new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsdistancecirclesteps)
                        {
                            options=new UISelect.Option[]
                            {
                                new UISelect.Option() { value="1", label="1" },
                                new UISelect.Option() { value="2", label="2" },
                                new UISelect.Option() { value="3", label="3" },
                                new UISelect.Option() { value="4", label="4" },
                                new UISelect.Option() { value="5", label="5" },
                                new UISelect.Option() { value="6", label="6" },
                                new UISelect.Option() { value="7", label="7" },
                                new UISelect.Option() { value="8", label="8" },
                                new UISelect.Option() { value="10", label="10" },
                                new UISelect.Option() { value="12", label="12" },
                                new UISelect.Option() { value="15", label="15" },
                                new UISelect.Option() { value="20", label="20" },
                            },
                            id="mapmarkup-distance_circle-steps",
                            css=UICss.ToClass(new string[]{ UICss.GraphicsDistanceCircleSteps })
                        },
                        new UIBreak(),
                        new UILabel(){
                            label=$"{localizer.Localize("symbology.radius")}:"
                        },
                        new UIInputNumber() {
                            MaxValue = double.MaxValue,
                            id = "mapmarkup-distance_circle-radius",
                            value = e.GetValue("mapmarkup-distance_circle-radius", "1000"),
                            css = UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.ToolParameterPersistent, UICss.GraphicsDistanceCircleRadius })
                        },
                        new UIButtonContainer(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsdistancecircleradius)
                        {
                            text = localizer.Localize("symbology.apply-radius"),
                            css = UICss.ToClass(new string[]{ UICss.CancelButtonStyle })
                        })
                    }
            }
        }));

        return collection;
    }

    static public List<IUIElement> AddCompassRoseStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        collection.Add(CreateHolder(e, stagedOnly, new IUIElement[]
        {
            new UIColorSelector(localizer.Localize("symbology.line-color"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicslinecolor, true) {
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior= ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-color","#ff0000"),
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-color") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistantImportant }),
                id="mapmarkup-compass-rose-linecolor"
            },
            new UILineWieghtSelector(localizer.Localize("symbology.line-weight"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslineweight){
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior= ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-lineweight",4),
                //css=UICss.ToClass(new string[]{e.IsEmpty("mapmarkup-lineweight")?UICss.ToolParameterPersistent: UICss.ToolParameterPersistantImportant}),
                id="mapmarkup-compass-rose-lineweight"
            },
            new UIOptionContainer()
            {
                title = localizer.Localize("symbology.properties"),
                CollapseState = IsCollapsed(stagedOnly),
                ExpandBehavior= ExpandMode(stagedOnly),
                elements = new IUIElement[]
                    {
                        new UILabel(){
                            label = localizer.Localize("symbology.num-angle-segments")
                        },
                        new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicscompassrosesteps)
                        {
                            options=new UISelect.Option[]
                            {
                                new UISelect.Option() { value="8", label="8" },
                                new UISelect.Option() { value="16", label="16" },
                                new UISelect.Option() { value="32", label="32" },
                                new UISelect.Option() { value="36", label="36" }
                            },
                            id="mapmarkup-compass-rose-steps",
                            css=UICss.ToClass(new string[]{ UICss.GraphicsCompassRoseSteps })
                        }
                    }
            }
        }));

        return collection;
    }

    static public List<IUIElement> AddDimLineStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        var lineColor = e.GetValue("mapmarkup-color", "#000000")?.ToString();

        collection.Add(CreateHolder(e, stagedOnly, new IUIElement[]
        {
            new UIColorSelector(localizer.Localize("symbology.line-color"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicslinecolor, false) {
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior= ExpandMode(collapseExclusive),
                value=lineColor,
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-color") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-color"
            },
            new UILineWieghtSelector(localizer.Localize("symbology.line-weight"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicslineweight){
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-lineweight", 2),
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-lineweight") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-lineweight"
            },
            new UIFontSizeSelector(localizer.Localize("symbology.font-size"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicstextsize)
            {
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value = e.GetValue("mapmarkup-fontsize", 14),
                //css = UICss.ToClass(new string[] { UICss.ToolParameter, UICss.ToolParameterPersistent }),
                id = "mapmarkup-fontsize"
            },
            new UIOptionContainer()
            {
                title = localizer.Localize("symbology.properties"),
                CollapseState = IsCollapsed(stagedOnly),
                elements = new IUIElement[]
                {
                    new UILabel()
                    {
                        label = localizer.Localize("symbology.unit")
                    },
                    new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsdimlinelengthunit)
                    {
                        options=new UISelect.Option[]
                        {
                            new UISelect.Option() { value="m", label = $"{localizer.Localize("meters")} [m]" },
                            new UISelect.Option() { value="km", label = $"{localizer.Localize("kilometers")} [km]" }
                        },
                        id="mapmarkup-dimline-unit",
                        css=UICss.ToClass(new string[]{ UICss.GraphicsDimLineLengthUnit })
                    },
                    new UILabel()
                    {
                        label = $"{localizer.Localize("symbology.label-total-length")}:"
                    },
                    new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsdimlinelabeltotallength)
                    {
                        options=new UISelect.Option[]
                        {
                            new UISelect.Option() { value="true", label= localizer.Localize("yes") },
                            new UISelect.Option() { value="false", label= localizer.Localize("no") }
                        },
                        id="mapmarkup-dimline-label-edges",
                        css=UICss.ToClass(new string[]{ UICss.GraphicsDimLineLabelTotalLength })
                    },
                    new UILabel()
                    {
                        label = $"{localizer.Localize("symbology.show-angles")}:"
                    },
                    DimAngleUnitSelect(localizer, ApiClientButtonCommand.setgraphicsdimlineangleunit, "mapmarkup-dimline-angle-unit", UICss.GraphicsDimLineAngleUnit),
                    new UILabel()
                    {
                        label = $"{localizer.Localize("symbology.angle-type")}:"
                    },
                    new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsdimlineangletype)
                    {
                        options=new UISelect.Option[]
                        {
                            new UISelect.Option() { value="inner", label= localizer.Localize("symbology.angle-type-inner") },
                            new UISelect.Option() { value="deflection", label= localizer.Localize("symbology.angle-type-deflection") },
                            new UISelect.Option() { value="azimuth", label= localizer.Localize("symbology.angle-type-azimuth") }
                        },
                        id="mapmarkup-dimline-angle-type",
                        css=UICss.ToClass(new string[]{ UICss.GraphicsDimLineAngleType })
                    }
                }
            }
        }));

        return collection;
    }

    static public List<IUIElement> AddDimPolygonStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        var lineColor = e.GetValue("mapmarkup-color", "#000000")?.ToString();
        var fillColor = e.GetValue("mapmarkup-fillcolor", "#ffff00")?.ToString();

        collection.Add(CreateHolder(e, stagedOnly, new IUIElement[]
        {
            new UIColorSelector(localizer.Localize("symbology.fill-color"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicsfillcolor, true){
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value= fillColor,
                //css=UICss.ToClass(new string[]{e.IsEmpty("mapmarkup-fillcolor")?UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-fillcolor"
            },
            new UIColorSelector(localizer.Localize("symbology.line-color"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicslinecolor, false) {
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior= ExpandMode(collapseExclusive),
                value=lineColor,
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-color") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-color"
            },
            new UILineWieghtSelector(localizer.Localize("symbology.line-weight"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicslineweight){
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-lineweight", 2),
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-lineweight") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-lineweight"
            },
            new UIFontSizeSelector(localizer.Localize("symbology.font-size"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicstextsize)
            {
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value = e.GetValue("mapmarkup-fontsize", 14),
                //css = UICss.ToClass(new string[] { UICss.ToolParameter, UICss.ToolParameterPersistent }),
                id = "mapmarkup-fontsize"
            },
            new UIOptionContainer()
            {
                title = localizer.Localize("symbology.properties"),
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior=ExpandMode(collapseExclusive),
                elements = new IUIElement[]
                {
                    new UILabel(){
                            label = $"{localizer.Localize("symbology.area-unit")}:"
                    },
                    new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsdimpolygonareaunit)
                    {
                        options=new UISelect.Option[]
                        {
                            new UISelect.Option() { value="m²", label=localizer.Localize("square-meters") },
                            new UISelect.Option() { value="km²", label=localizer.Localize("square-kilometers") },
                            new UISelect.Option() { value="ha", label=localizer.Localize("square-hectares") },
                            new UISelect.Option() { value="a", label=localizer.Localize("square-a") },
                        },
                        id="mapmarkup-dimpolygon-area-unit",
                        css=UICss.ToClass(new string[]{ UICss.GraphicsDimPolygonAreaUnit })
                    },
                    new UILabel(){
                            label = $"{localizer.Localize("symbology.label-edges")}:"
                    },
                    new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsdimpolygonlabeledges)
                    {
                        options=new UISelect.Option[]
                        {
                            new UISelect.Option() { value="true", label= localizer.Localize("yes") },
                            new UISelect.Option() { value="false", label= localizer.Localize("no") }
                        },
                        id="mapmarkup-dimpolygon-label-edges",
                        css=UICss.ToClass(new string[]{ UICss.GraphicsDimPolygonLabelEdges })
                    },
                    new UILabel(){
                            label = $"{localizer.Localize("symbology.show-angles")}:"
                    },
                    DimAngleUnitSelect(localizer, ApiClientButtonCommand.setgraphicsdimpolygonangleunit, "mapmarkup-dimpolygon-angle-unit", UICss.GraphicsDimPolygonAngleUnit)
                }
            }
        }));

        return collection;
    }

    static public List<IUIElement> AddHectoLineStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        var lineColor = e.GetValue("mapmarkup-color", "#000000")?.ToString();

        collection.Add(CreateHolder(e, stagedOnly, new IUIElement[]
        {
             new UIColorSelector(localizer.Localize("symbology.line-color"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslinecolor, false) {
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                 ExpandBehavior=ExpandMode(collapseExclusive),
                value=lineColor,
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-color") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-color"
            },
            new UILineWieghtSelector(localizer.Localize("symbology.line-weight"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslineweight){
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-lineweight", 2),
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-lineweight") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-lineweight"
            },
            new UIFontSizeSelector(localizer.Localize("symbology.font-size"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicstextsize)
            {
                CollapseState = IsCollapsed(/*isCollapsed*/true),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value = e.GetValue("mapmarkup-fontsize", 14),
                //css = UICss.ToClass(new string[] { UICss.ToolParameter, UICss.ToolParameterPersistent }),
                id = "mapmarkup-fontsize"
            },
            new UIOptionContainer()
            {
                title = localizer.Localize("symbology.properties"),
                CollapseState = IsCollapsed(stagedOnly),
                elements = new IUIElement[]
                    {
                        new UILabel(){
                            label = localizer.Localize("symbology.unit")
                        },
                        new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicshectolineunit)
                        {
                            options=new UISelect.Option[]
                            {
                                new UISelect.Option() { value="m", label = $"{localizer.Localize("meters")} [m]" },
                                new UISelect.Option() { value="km", label = $"{localizer.Localize("kilometers")} [km]" }
                            },
                            id="mapmarkup-hectoline-unit",
                            css=UICss.ToClass(new string[]{ UICss.GraphicsHectolineUnit })
                        },
                        new UIBreak(),
                        new UILabel(){
                            label = $"{localizer.Localize("symbology.segment-unit")}:"
                        },
                        new UIInputNumber() {
                            MinValue = 1,
                            MaxValue = double.MaxValue,
                            id = "mapmarkup-hectoline-interval",
                            value = e.GetValue("mapmarkup-hectoline-interval", "100"),
                            css = UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.ToolParameterPersistent, UICss.GraphicsHectolineInterval })
                        },
                        new UIButtonContainer(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicshectolineinterval)
                        {
                            text = localizer.Localize("symbology.apply-segment"),
                            css = UICss.ToClass(new string[]{ UICss.CancelButtonStyle })
                        })
                    }
            }
        }));

        return collection;
    }

    static public List<IUIElement> AddViewAngleStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        collection.Add(CreateHolder(e, stagedOnly, 
        [
            .. AngleBaseStyleElements(e, localizer, collapseExclusive),
            new UIOptionContainer()
            {
                title = localizer.Localize("symbology.properties"),
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior = ExpandMode(collapseExclusive),
                elements = new IUIElement[]
                {
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.unit")}:"
                    },
                    AngleUnitSelect(localizer, ApiClientButtonCommand.setgraphicsviewangleunit, "mapmarkup-view_angle-unit", UICss.GraphicsViewAngleUnit),
                    new UIBreak(),
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.angle")}:"
                    },
                    new UIInputNumber() {
                        MinValue = 0,
                        MaxValue = 400,
                        id = "mapmarkup-view_angle-angle",
                        value = e.GetValue("mapmarkup-view_angle-angle", "30"),
                        css = UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.ToolParameterPersistent, UICss.GraphicsViewAngleAngle })
                    },
                    new UIButtonContainer(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsviewangleangle)
                    {
                        text = localizer.Localize("symbology.apply-angle"),
                        css = UICss.ToClass(new string[]{ UICss.CancelButtonStyle })
                    }),
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.label-axis-length")}:"
                    },
                    YesNoSelect(localizer, ApiClientButtonCommand.setgraphicsviewanglelabellength, "mapmarkup-view_angle-label-length", UICss.GraphicsViewAngleLabelLength, defaultYes: false)
                }
            }
        ]));

        return collection;
    }

    static public List<IUIElement> AddIntersectionAngleStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        collection.Add(CreateHolder(e, stagedOnly, 
        [
            .. AngleBaseStyleElements(e, localizer, collapseExclusive),
            new UIOptionContainer()
            {
                title = localizer.Localize("symbology.properties"),
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior = ExpandMode(collapseExclusive),
                elements = new IUIElement[]
                {
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.unit")}:"
                    },
                    AngleUnitSelect(localizer, ApiClientButtonCommand.setgraphicsintersectionangleunit, "mapmarkup-intersection_angle-unit", UICss.GraphicsIntersectionAngleUnit),
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.show-angles")}:"
                    },
                    new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsintersectionangleangles)
                    {
                        options = new UISelect.Option[]
                        {
                            new UISelect.Option() { value = "angle", label = localizer.Localize("symbology.show-angles-intersection") },
                            new UISelect.Option() { value = "adjacent", label = localizer.Localize("symbology.show-angles-adjacent") },
                            new UISelect.Option() { value = "both", label = localizer.Localize("symbology.show-angles-both") },
                            new UISelect.Option() { value = "all", label = localizer.Localize("symbology.show-angles-all") }
                        },
                        id = "mapmarkup-intersection_angle-angles",
                        css = UICss.ToClass(new string[] { UICss.GraphicsIntersectionAngleAngles })
                    }
                }
            }
        ]));

        return collection;
    }

    static public List<IUIElement> AddCircleArcStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        collection.Add(CreateHolder(e, stagedOnly,
        [
            .. AngleBaseStyleElements(e, localizer, collapseExclusive),
            new UIOptionContainer()
            {
                title = localizer.Localize("symbology.properties"),
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior = ExpandMode(collapseExclusive),
                elements = new IUIElement[]
                {
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.show-labels")}:"
                    },
                    new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicscirclearclabels)
                    {
                        options = new UISelect.Option[]
                        {
                            new UISelect.Option() { value = "all", label = localizer.Localize("symbology.show-labels-all") },
                            new UISelect.Option() { value = "radius", label = localizer.Localize("symbology.show-labels-radius") },
                            new UISelect.Option() { value = "length", label = localizer.Localize("symbology.show-labels-arc-length") },
                            new UISelect.Option() { value = "angle", label = localizer.Localize("symbology.show-labels-central-angle") }
                        },
                        id = "mapmarkup-circle_arc-labels",
                        css = UICss.ToClass(new string[] { UICss.GraphicsCircleArcLabels })
                    },
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.unit")}:"
                    },
                    AngleUnitSelect(localizer, ApiClientButtonCommand.setgraphicscirclearcunit, "mapmarkup-circle_arc-unit", UICss.GraphicsCircleArcUnit)
                }
            }
        ]));

        return collection;
    }

    static public List<IUIElement> AddPerpendicularStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        collection.Add(CreateHolder(e, stagedOnly,
        [
            .. AngleBaseStyleElements(e, localizer, collapseExclusive),
            new UIOptionContainer()
            {
                title = localizer.Localize("symbology.properties"),
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior = ExpandMode(collapseExclusive),
                elements = new IUIElement[]
                {
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.station-format")}:"
                    },
                    new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsperpendicularstationformat)
                    {
                        options = new UISelect.Option[]
                        {
                            new UISelect.Option() { value = "m", label = localizer.Localize("symbology.station-format-m") },
                            new UISelect.Option() { value = "km", label = localizer.Localize("symbology.station-format-km") },
                            new UISelect.Option() { value = "none", label = localizer.Localize("symbology.station-format-none") }
                        },
                        id = "mapmarkup-perpendicular-station-format",
                        css = UICss.ToClass(new string[] { UICss.GraphicsPerpendicularStationFormat })
                    },
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.length-unit")}:"
                    },
                    new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsperpendicularlengthunit)
                    {
                        options = new UISelect.Option[]
                        {
                            new UISelect.Option() { value = "m", label = $"{localizer.Localize("meters")} [m]" },
                            new UISelect.Option() { value = "km", label = $"{localizer.Localize("kilometers")} [km]" }
                        },
                        id = "mapmarkup-perpendicular-length-unit",
                        css = UICss.ToClass(new string[] { UICss.GraphicsPerpendicularLengthUnit })
                    }
                }
            }
        ]));

        return collection;
    }

    static public List<IUIElement> AddObjectDistanceStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        collection.Add(CreateHolder(e, stagedOnly,
        [
            .. AngleBaseStyleElements(e, localizer, collapseExclusive),
            new UIOptionContainer()
            {
                title = localizer.Localize("symbology.properties"),
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior = ExpandMode(collapseExclusive),
                elements = new IUIElement[]
                {
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.length-unit")}:"
                    },
                    new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsobjectdistancelengthunit)
                    {
                        options = new UISelect.Option[]
                        {
                            new UISelect.Option() { value = "m", label = $"{localizer.Localize("meters")} [m]" },
                            new UISelect.Option() { value = "km", label = $"{localizer.Localize("kilometers")} [km]" }
                        },
                        id = "mapmarkup-object_distance-length-unit",
                        css = UICss.ToClass(new string[] { UICss.GraphicsObjectDistanceLengthUnit })
                    },
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.show-number")}:"
                    },
                    new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsobjectdistanceshownumber)
                    {
                        options = new UISelect.Option[]
                        {
                            new UISelect.Option() { value = "false", label = localizer.Localize("no") },
                            new UISelect.Option() { value = "true", label = localizer.Localize("yes") }
                        },
                        id = "mapmarkup-object_distance-show-number",
                        css = UICss.ToClass(new string[] { UICss.GraphicsObjectDistanceShowNumber })
                    }
                }
            }
        ]));

        return collection;
    }

    // measure_grid: the align/origin holders are hidden by the client without a calc crs (webgis.map.graphics.refreshMeasureGridUIElements)
    static public List<IUIElement> AddMeasureGridStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        collection.Add(CreateHolder(e, stagedOnly,
        [
            .. AngleBaseStyleElements(e, localizer, collapseExclusive),
            new UILineStyleSelector(localizer.Localize("symbology.line-style"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicslinestyle)
            {
                CollapseState = IsCollapsed(true),
                ExpandBehavior = ExpandMode(collapseExclusive),
                id = "mapmarkup-measure_grid-linestyle",
                value = e.GetValue("mapmarkup-linestyle", "1")
            },
            new UIOptionContainer()
            {
                title = localizer.Localize("symbology.properties"),
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior = ExpandMode(collapseExclusive),
                elements = new IUIElement[]
                {
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.grid-cell-width")}:"
                    },
                    new UIInputNumber() {
                        MinValue = 0,
                        MaxValue = double.MaxValue,
                        StepWidth = 0.01,
                        id = "mapmarkup-measure_grid-cell-width",
                        value = e.GetValue("mapmarkup-measure_grid-cell-width", "10"),
                        css = UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.ToolParameterPersistent, UICss.GraphicsMeasureGridCellWidth })
                    },
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.grid-cell-height")}:"
                    },
                    new UIInputNumber() {
                        MinValue = 0,
                        MaxValue = double.MaxValue,
                        StepWidth = 0.01,
                        id = "mapmarkup-measure_grid-cell-height",
                        value = e.GetValue("mapmarkup-measure_grid-cell-height", "10"),
                        css = UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.ToolParameterPersistent, UICss.GraphicsMeasureGridCellHeight })
                    },
                    new UIButtonContainer(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsmeasuregridcellsize)
                    {
                        text = localizer.Localize("symbology.apply-grid-cell-size"),
                        css = UICss.ToClass(new string[]{ UICss.CancelButtonStyle })
                    }),
                    new UIBreak(),
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.grid-labels")}:"
                    },
                    new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsmeasuregridlabels)
                    {
                        options = new UISelect.Option[]
                        {
                            new UISelect.Option() { value = "cells", label = localizer.Localize("symbology.grid-labels-cells") },
                            new UISelect.Option() { value = "border", label = localizer.Localize("symbology.grid-labels-border") },
                            new UISelect.Option() { value = "coords", label = localizer.Localize("symbology.grid-labels-coords") },
                            new UISelect.Option() { value = "none", label = localizer.Localize("symbology.grid-labels-none") }
                        },
                        id = "mapmarkup-measure_grid-labels",
                        css = UICss.ToClass(new string[] { UICss.GraphicsMeasureGridLabels })
                    },
                    new UIDiv()
                    {
                        css = UICss.ToClass(new string[] { UICss.GraphicsMeasureGridAlignHolder }),
                        elements = new IUIElement[]
                        {
                            new UILabel(){
                                label = $"{localizer.Localize("symbology.grid-align")}:"
                            },
                            YesNoSelect(localizer, ApiClientButtonCommand.setgraphicsmeasuregridaligntogrid, "mapmarkup-measure_grid-align", UICss.GraphicsMeasureGridAlign, defaultYes: false)
                        }
                    },
                    new UIDiv()
                    {
                        css = UICss.ToClass(new string[] { UICss.GraphicsMeasureGridOriginHolder }),
                        elements = new IUIElement[]
                        {
                            new UIBreak(),
                            new UILabel(){
                                label = $"{localizer.Localize("symbology.grid-origin-x")}:"
                            },
                            new UIInputNumber() {
                                MinValue = -double.MaxValue,
                                MaxValue = double.MaxValue,
                                StepWidth = 0.01,
                        id = "mapmarkup-measure_grid-origin-x",
                                css = UICss.ToClass(new string[]{ UICss.GraphicsMeasureGridOriginX })
                            },
                            new UILabel(){
                                label = $"{localizer.Localize("symbology.grid-origin-y")}:"
                            },
                            new UIInputNumber() {
                                MinValue = -double.MaxValue,
                                MaxValue = double.MaxValue,
                                StepWidth = 0.01,
                        id = "mapmarkup-measure_grid-origin-y",
                                css = UICss.ToClass(new string[]{ UICss.GraphicsMeasureGridOriginY })
                            },
                            new UIButtonContainer(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsmeasuregridorigin)
                            {
                                text = localizer.Localize("symbology.apply-grid-origin"),
                                css = UICss.ToClass(new string[]{ UICss.CancelButtonStyle })
                            })
                        }
                    }
                }
            }
        ]));

        return collection;
    }

    static public List<IUIElement> AddBufferZoneStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true, bool fromSelection = false)
    {
        collection.Add(CreateHolder(e, stagedOnly,
        [
            new UIColorSelector(localizer.Localize("symbology.fill-color"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsfillcolor, true){
                CollapseState = IsCollapsed(true),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value = e.GetValue("mapmarkup-fillcolor", "#aaaaaa")?.ToString(),
                id = "mapmarkup-fillcolor"
            },
            new UIOpacitySelector(localizer.Localize("symbology.fill-opacity"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsfillopacity)
            {
                CollapseState = IsCollapsed(true),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value = e.GetValue("mapmarkup-fillopacity", "20"),
                id = "mapmarkup-fillopacity"
            },
            .. AngleBaseStyleElements(e, localizer, collapseExclusive),
            new UIOptionContainer()
            {
                title = localizer.Localize("symbology.properties"),
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior = ExpandMode(collapseExclusive),
                elements = fromSelection
                    ? BufferZoneFromSelectionPropertyElements(e, localizer)
                    : new IUIElement[]
                {
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.buffer-source-type")}:"
                    },
                    new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsbufferzonesourcetype)
                    {
                        options = new UISelect.Option[]
                        {
                            new UISelect.Option() { value = "point", label = localizer.Localize("symbology.buffer-source-point") },
                            new UISelect.Option() { value = "multipoint", label = localizer.Localize("symbology.buffer-source-multipoint") },
                            new UISelect.Option() { value = "line", label = localizer.Localize("symbology.buffer-source-line") },
                            new UISelect.Option() { value = "polygon", label = localizer.Localize("symbology.buffer-source-polygon") }
                        },
                        id = "mapmarkup-buffer_zone-source-type",
                        css = UICss.ToClass(new string[] { UICss.GraphicsBufferZoneSourceType })
                    },
                    new UIBreak(),
                    new UILabel(){
                        label = $"{localizer.Localize("symbology.buffer-width")} [m]:"
                    },
                    new UIInputNumber() {
                        MinValue = -double.MaxValue,  // negative => inner buffer (polygons only)
                        MaxValue = double.MaxValue,
                        id = "mapmarkup-buffer_zone-width",
                        value = e.GetValue("mapmarkup-buffer_zone-width", "10"),
                        css = UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.ToolParameterPersistent, UICss.GraphicsBufferZoneWidth })
                    },
                    new UIButtonContainer(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsbufferzonewidth)
                    {
                        text = localizer.Localize("symbology.apply-buffer-width"),
                        css = UICss.ToClass(new string[]{ UICss.CancelButtonStyle })
                    })
                }
            }
        ]));

        return collection;
    }

    // add from selection: the source type follows from the selected geometries, the width is sent with the add-from-selection command
    static private IUIElement[] BufferZoneFromSelectionPropertyElements(ApiToolEventArguments e, ILocalizer localizer)
        => new IUIElement[]
        {
            new UILabel(){
                label = $"{localizer.Localize("symbology.buffer-width")} [m]:"
            },
            new UIInputNumber() {
                MinValue = -double.MaxValue,  // negative => inner buffer (polygons only)
                MaxValue = double.MaxValue,
                id = "mapmarkup-from-selection-bz-width",
                value = e.GetValue("mapmarkup-buffer_zone-width", "10"),
                css = UICss.ToClass(new string[]{ UICss.ToolParameter })
            }
        };

    static private IUIElement[] AngleBaseStyleElements(ApiToolEventArguments e, ILocalizer localizer, bool collapseExclusive)
        => new IUIElement[]
        {
            new UIColorSelector(localizer.Localize("symbology.line-color"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicslinecolor, false) {
                CollapseState = IsCollapsed(true),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value = e.GetValue("mapmarkup-color", "#000000")?.ToString(),
                id = "mapmarkup-color"
            },
            new UILineWieghtSelector(localizer.Localize("symbology.line-weight"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicslineweight){
                CollapseState = IsCollapsed(true),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value = e.GetValue("mapmarkup-lineweight", 2),
                id = "mapmarkup-lineweight"
            },
            new UIFontSizeSelector(localizer.Localize("symbology.font-size"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicstextsize)
            {
                CollapseState = IsCollapsed(true),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value = e.GetValue("mapmarkup-fontsize", 14),
                id = "mapmarkup-fontsize"
            }
        };

    // dimline/dimpolygon vertex angles: none (default, previous behavior) | deg | gon | both
    static private UISelect DimAngleUnitSelect(ILocalizer localizer, ApiClientButtonCommand command, string id, string css)
        => new UISelect(UIButton.UIButtonType.clientbutton, command)
        {
            options = new UISelect.Option[]
            {
                new UISelect.Option() { value = "none", label = localizer.Localize("symbology.angle-unit-none") },
                new UISelect.Option() { value = "deg", label = $"{localizer.Localize("symbology.angle-unit-deg")} [°]" },
                new UISelect.Option() { value = "gon", label = $"{localizer.Localize("symbology.angle-unit-gon")} [gon]" },
                new UISelect.Option() { value = "both", label = $"{localizer.Localize("symbology.angle-unit-both")} [° / gon]" }
            },
            id = id,
            css = UICss.ToClass(new string[] { css })
        };

    static private UISelect AngleUnitSelect(ILocalizer localizer, ApiClientButtonCommand command, string id, string css)
        => new UISelect(UIButton.UIButtonType.clientbutton, command)
        {
            options = new UISelect.Option[]
            {
                new UISelect.Option() { value = "deg", label = $"{localizer.Localize("symbology.angle-unit-deg")} [°]" },
                new UISelect.Option() { value = "gon", label = $"{localizer.Localize("symbology.angle-unit-gon")} [gon]" },
                new UISelect.Option() { value = "both", label = $"{localizer.Localize("symbology.angle-unit-both")} [° / gon]" }
            },
            id = id,
            css = UICss.ToClass(new string[] { css })
        };

    static private UISelect YesNoSelect(ILocalizer localizer, ApiClientButtonCommand command, string id, string css, bool defaultYes)
        => new UISelect(UIButton.UIButtonType.clientbutton, command)
        {
            options = defaultYes
                ? new UISelect.Option[]
                {
                    new UISelect.Option() { value = "true", label = localizer.Localize("yes") },
                    new UISelect.Option() { value = "false", label = localizer.Localize("no") }
                }
                : new UISelect.Option[]
                {
                    new UISelect.Option() { value = "false", label = localizer.Localize("no") },
                    new UISelect.Option() { value = "true", label = localizer.Localize("yes") }
                },
            id = id,
            css = UICss.ToClass(new string[] { css })
        };

    static private UICollapsableElement.CollapseStatus IsCollapsed(bool stageOnly)
        => stageOnly
            ? UICollapsableElement.CollapseStatus.Collapsed
            : UICollapsableElement.CollapseStatus.Expanded;

    static private UICollapsableElement.ExpandBehaviorMode ExpandMode(bool stagedOnly)
        => stagedOnly
            ? UICollapsableElement.ExpandBehaviorMode.Exclusive
            : UICollapsableElement.ExpandBehaviorMode.Normal;

    static private UIDiv CreateHolder(ApiToolEventArguments e, bool stagedOnly, ICollection<IUIElement> childElements)
        => new UIDiv()
        {
            VisibilityDependency =
                stagedOnly && !e.UseMobileBehavior()
                    ? VisibilityDependency.HasGraphicsStagedElement
                    : VisibilityDependency.None,
            elements = childElements
        };

    static public List<IUIElement> AsStagedStyleElements(this List<IUIElement> elements, ApiToolEventArguments e)
    {
        if (!e.UseMobileBehavior())
        {
            elements.Clear();  // clear it... no need for this anymore... in desktop behavoir
        }

        return elements;
    }

    #endregion

    #region By Symbol (stroke-color, fill-opacity, ...)

    static public List<IUIElement> AddSymbolIdElements(this List<IUIElement> collection, IBridge bridge)
    {
        collection.Add(
            new UISymbolSelector(bridge, "Symbol",
                    buttonCommand: ApiClientButtonCommand.setgraphics_symbol_and_apply_to_selected,
                    symbolId: null)
            {
                id = "mapmarkup-symbol-symbol",
                AllowNullValues = true,
            }
        );

        return collection;
    }

    static public List<IUIElement> AddPointColorElements(this List<IUIElement> collection, bool expanded = true)
    {
        collection.Add(
            new UIColorSelector(
                        "Punkt Farbe",
                        UIButton.UIButtonType.clientbutton,
                        ApiClientButtonCommand.setgraphics_point_color_and_apply_to_selected,
                        false
                     )
            {
                AllowNullValues = true,
                CollapseState = expanded ? UICollapsableElement.CollapseStatus.Expanded : UICollapsableElement.CollapseStatus.Collapsed,
                value = null,
                id = "mapmarkup-point-color"
            }
        );

        return collection;
    }

    static public List<IUIElement> AddPointSizeElements(this List<IUIElement> collection, bool expanded = true)
    {
        collection.Add(
            new UILineWieghtSelector(
                        "Punkt Größe",
                        UIButton.UIButtonType.clientbutton,
                        ApiClientButtonCommand.setgraphics_point_size_and_apply_to_selected
                    )
            {
                AllowNullValues = true,
                CollapseState = expanded ? UICollapsableElement.CollapseStatus.Expanded : UICollapsableElement.CollapseStatus.Collapsed,
                value = null,
                id = "mapmarkup-point-size"
            }
        );

        return collection;
    }

    static public List<IUIElement> AddTextColorElements(this List<IUIElement> collection, bool expanded = true)
    {
        collection.Add(
            new UIColorSelector(
                        "Schriftfarbe",
                        UIButton.UIButtonType.clientbutton,
                        ApiClientButtonCommand.setgraphics_text_color_and_apply_to_selected,
                        false
                    )
            {
                AllowNullValues = true,
                CollapseState = expanded ? UICollapsableElement.CollapseStatus.Expanded : UICollapsableElement.CollapseStatus.Collapsed,
                value = null,
                id = "mapmarkup-text-fontcolor"
            }
        );

        return collection;
    }

    static public List<IUIElement> AddTextSizeElements(this List<IUIElement> collection, bool expanded = true)
    {
        collection.Add(
           new UIFontSizeSelector(
                    "Schriftgröße",
                    UIButton.UIButtonType.clientbutton,
                    ApiClientButtonCommand.setgraphics_text_size_and_apply_to_selected
                )
           {
               AllowNullValues = true,
               CollapseState = expanded ? UICollapsableElement.CollapseStatus.Expanded : UICollapsableElement.CollapseStatus.Collapsed,
               value = null,
               id = "mapmarkup-text-fontsize"
           }
        );

        return collection;
    }

    static public List<IUIElement> AddStrokeColorElements(this List<IUIElement> collection, bool expanded = true)
    {
        collection.Add(
           new UIColorSelector(
                    "Linienfarbe",
                    UIButton.UIButtonType.clientbutton,
                    ApiClientButtonCommand.setgraphics_stroke_color_and_apply_to_selected,
                    false
                )
           {
               AllowNullValues = true,
               CollapseState = expanded ? UICollapsableElement.CollapseStatus.Expanded : UICollapsableElement.CollapseStatus.Collapsed,
               value = null,
               id = "mapmarkup-line-linecolor"
           }
        );

        return collection;
    }

    static public List<IUIElement> AddStrokeWeightElements(this List<IUIElement> collection, bool expanded = true)
    {
        collection.Add(
            new UILineWieghtSelector(
                        "Linienstärke",
                        UIButton.UIButtonType.clientbutton,
                        ApiClientButtonCommand.setgraphics_stroke_weight_and_apply_to_selected
                    )
            {
                AllowNullValues = true,
                CollapseState = expanded ? UICollapsableElement.CollapseStatus.Expanded : UICollapsableElement.CollapseStatus.Collapsed,
                value = null,
                id = "mapmarkup-line-lineweight"
            }
        );

        return collection;
    }

    static public List<IUIElement> AddStrokeStyleElements(this List<IUIElement> collection, bool expanded = true)
    {
        collection.Add(
            new UILineStyleSelector(
                        "Linienart",
                        UIButton.UIButtonType.clientbutton,
                        ApiClientButtonCommand.setgraphics_stroke_style_and_apply_to_selected
                    )
            {
                AllowNullValues = true,
                CollapseState = expanded ? UICollapsableElement.CollapseStatus.Expanded : UICollapsableElement.CollapseStatus.Collapsed,
                id = "mapmarkup-line-linestyle",
                value = null
            }
        );

        return collection;
    }

    static public List<IUIElement> AddFillColorElements(this List<IUIElement> collection, bool expanded = true)
    {
        collection.Add(
            new UIColorSelector(
                        "Füllfarbe",
                        UIButton.UIButtonType.clientbutton,
                        ApiClientButtonCommand.setgraphics_fill_color_and_apply_to_selected,
                        true
                    )
            {
                AllowNullValues = true,
                CollapseState = expanded ? UICollapsableElement.CollapseStatus.Expanded : UICollapsableElement.CollapseStatus.Collapsed,
                value = null,
                id = "mapmarkup-polyline-fillcolor"
            }
        );

        return collection;
    }

    static public List<IUIElement> AddFillOpacityElements(this List<IUIElement> collection, bool expanded = true)
    {
        collection.Add(
            new UIOpacitySelector(
                        "Deckkraft",
                        UIButton.UIButtonType.clientbutton,
                        ApiClientButtonCommand.setgraphics_fill_opacity_and_apply_to_selected
                    )
            {
                AllowNullValues = true,
                CollapseState = expanded ? UICollapsableElement.CollapseStatus.Expanded : UICollapsableElement.CollapseStatus.Collapsed,
                value = null,
                id = "mapmarkup-polyline-fillopacity"
            }
        );

        return collection;
    }

    #endregion
}
