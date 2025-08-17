using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using System;
using System.Collections.Generic;

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
                CollapseState = IsCollapsed(isCollapsed),
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
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value = e.GetValue("mapmarkup-fontstyle","regular"),
                //css = UICss.ToClass(new string[] { UICss.ToolParameter, UICss.ToolParameterPersistent }),
                id = "mapmarkup-text-fontstyle"
            },
            new UIColorSelector(localizer.Localize("symbology.font-color"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicstextcolor, false)
            {
                CollapseState = IsCollapsed(isCollapsed),
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
                CollapseState = IsCollapsed(isCollapsed),
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
                    CollapseState = IsCollapsed(isCollapsed),
                    ExpandBehavior=ExpandMode(collapseExclusive),
                    value=e.GetValue("mapmarkup-lineweight", 4),
                    //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-lineweight") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                    id="mapmarkup-line-lineweight"
                },
                new UILineStyleSelector(localizer.Localize("symbology.line-style"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslinestyle){
                    CollapseState = IsCollapsed(isCollapsed),
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
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-fillopacity", "20"),
                //css=UICss.ToClass(new string[]{e.IsEmpty("mapmarkup-fillcolor")?UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-polyline-fillopacity"
            },
            new UIColorSelector(localizer.Localize("symbology.line-color"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslinecolor, true){
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-color","#ff0000"),
                //css=UICss.ToClass(new string[]{e.IsEmpty("mapmarkup-color")?UICss.ToolParameterPersistent: UICss.ToolParameterPersistentImportant}),
                id="mapmarkup-polyline-linecolor"
            },
            new UILineWieghtSelector(localizer.Localize("symbology.line-weight"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslineweight){
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-lineweight",4),
                //css=UICss.ToClass(new string[]{e.IsEmpty("mapmarkup-lineweight")?UICss.ToolParameterPersistent: UICss.ToolParameterPersistentImportant}),
                id="mapmarkup-polyline-lineweight"
            },
            new UILineStyleSelector(localizer.Localize("symbology.line-style"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslinestyle){
                CollapseState = IsCollapsed(isCollapsed),
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
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-color","#ff0000"),
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-color") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-distance_circle-linecolor"
            },
            new UILineWieghtSelector(localizer.Localize("symbology.line-weight"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslineweight){
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-lineweight",4),
                //css=UICss.ToClass(new string[]{e.IsEmpty("mapmarkup-lineweight")?UICss.ToolParameterPersistent: UICss.ToolParameterPersistentImportant}),
                id="mapmarkup-distance_circle-lineweight"
            },
            new UIColorSelector(localizer.Localize("symbology.fill-color"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicsfillcolor, true){
                CollapseState = IsCollapsed(isCollapsed),
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
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior= ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-color","#ff0000"),
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-color") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistantImportant }),
                id="mapmarkup-compass-rose-linecolor"
            },
            new UILineWieghtSelector(localizer.Localize("symbology.line-weight"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslineweight){
                CollapseState = IsCollapsed(isCollapsed),
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
                        new UISelect(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicsdistancecirclesteps)
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
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior= ExpandMode(collapseExclusive),
                value=lineColor,
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-color") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-color"
            },
            new UILineWieghtSelector(localizer.Localize("symbology.line-weight"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicslineweight){
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-lineweight", 2),
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-lineweight") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-lineweight"
            },
            new UIFontSizeSelector(localizer.Localize("symbology.font-size"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicstextsize)
            {
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value = e.GetValue("mapmarkup-fontsize", 14),
                //css = UICss.ToClass(new string[] { UICss.ToolParameter, UICss.ToolParameterPersistent }),
                id = "mapmarkup-fontsize"
            },
        }));

        return collection;
    }

    static public List<IUIElement> AddDimPolygonStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        var lineColor = e.GetValue("mapmarkup-color", "#000000")?.ToString();

        collection.Add(CreateHolder(e, stagedOnly, new IUIElement[]
        {
            new UIColorSelector(localizer.Localize("symbology.line-color"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicslinecolor, false) {
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior= ExpandMode(collapseExclusive),
                value=lineColor,
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-color") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-color"
            },
            new UILineWieghtSelector(localizer.Localize("symbology.line-weight"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicslineweight){
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-lineweight", 2),
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-lineweight") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-lineweight"
            },
            new UIFontSizeSelector(localizer.Localize("symbology.font-size"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicstextsize)
            {
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior = ExpandMode(collapseExclusive),
                value = e.GetValue("mapmarkup-fontsize", 14),
                //css = UICss.ToClass(new string[] { UICss.ToolParameter, UICss.ToolParameterPersistent }),
                id = "mapmarkup-fontsize"
            },
        }));

        return collection;
    }

    static public List<IUIElement> AddHectoLineStyleElements(this List<IUIElement> collection, ApiToolEventArguments e, ILocalizer localizer, bool stagedOnly = false, bool collapseExclusive = true, bool isCollapsed = true)
    {
        var lineColor = e.GetValue("mapmarkup-color", "#000000")?.ToString();

        collection.Add(CreateHolder(e, stagedOnly, new IUIElement[]
        {
             new UIColorSelector(localizer.Localize("symbology.line-color"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslinecolor, false) {
                CollapseState = IsCollapsed(isCollapsed),
                 ExpandBehavior=ExpandMode(collapseExclusive),
                value=lineColor,
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-color") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-color"
            },
            new UILineWieghtSelector(localizer.Localize("symbology.line-weight"), UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setgraphicslineweight){
                CollapseState = IsCollapsed(isCollapsed),
                ExpandBehavior=ExpandMode(collapseExclusive),
                value=e.GetValue("mapmarkup-lineweight", 2),
                //css=UICss.ToClass(new string[]{ e.IsEmpty("mapmarkup-lineweight") ? UICss.ToolParameterPersistent : UICss.ToolParameterPersistentImportant }),
                id="mapmarkup-lineweight"
            },
            new UIFontSizeSelector(localizer.Localize("symbology.font-size"), UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setgraphicstextsize)
            {
                CollapseState = IsCollapsed(isCollapsed),
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
                                new UISelect.Option() { value="km", label = $"{localizer.Localize("kilometers")} [m]" }
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
