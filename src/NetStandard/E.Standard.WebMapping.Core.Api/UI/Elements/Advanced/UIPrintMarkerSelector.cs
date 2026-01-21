using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Extensions;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UIPrintMarkerSelector : UIOptionContainer
{
    public const string ShowQueryMarkersId = "show_query_markers";
    public const string ShowCoordinatesMarkersId = "show_coords_markers";
    public const string ShowChainageMarkersId = "show_chainage_markers";
    public const string QueryLabelFieldId = "query_labelfield";
    public const string CoordinatesLabelFieldId = "coords_labelfield";

    private readonly string _id;

    public UIPrintMarkerSelector(IApiButton button, string id)
    {
        _id = id;

        this.title = "Karten Marker";
        this.CollapseState = UICollapsableElement.CollapseStatus.Collapsed;

        this.AddChild(new UIDiv()
        {
            VisibilityDependency = VisibilityDependency.QueryResultsExists,
            elements = new IUIElement[]
            {
                new UILabel() { label = "Abfrageergebnisse" },
                new UIInputElementStack(new IUIElement[]
                {
                    new UISelect()
                    {
                        css = UICss.ToClass(new[] { 
                            UICss.ToolParameter, 
                            UICss.ToolParameterPersistent,
                            button?.GetType().ToToolId() switch {
                                "webgis.tools.mapseriesprint" => UICss.MapSeriesPrintShowQueryMarkersSelect,
                                _ => UICss.PrintShowQueryMarkersSelect
                            }
                        }),
                        id = QueryMarkersVisibilitySelectorId,
                        options=new UISelect.Option[]
                        {
                            new UISelect.Option() { value="", label="--- nicht anzeigen ---" },
                            new UISelect.Option() { value="show", label="Im Ausdruck anzeigen" }
                        }
                    },
                    new UIConditionDiv() {
                        ContitionElementId = $"{ id }--{ ShowQueryMarkersId }",
                        ConditionType = UIConditionDiv.ConditionTypes.ElementValue,
                        ConditionResult = true,
                        ConditionArguments=new[]{ "show" },
                        elements = new UIElement[]{
                            new UIPrintQueryLabelFieldCombo()
                            {
                                css=UICss.ToClass(new[] { UICss.ToolParameter, UICss.ToolParameterPersistent }),
                                id=$"{ id }--{ QueryLabelFieldId }"
                            }
                        }
                    }
                })
            }
        });

        this.AddChild(new UIDiv()
        {
            VisibilityDependency = VisibilityDependency.HasToolResults_Coordinates,
            elements = new IUIElement[]
            {
                new UILabel() { label = "Koordinatenmarker" },
                new UIInputElementStack(new IUIElement[]
                {
                    new UISelect()
                    {
                        css = UICss.ToClass(new[] { 
                            UICss.ToolParameter, 
                            UICss.ToolParameterPersistent,
                            button?.GetType().ToToolId() switch {
                                "webgis.tools.mapseriesprint" => UICss.MapSeriesPrintShowCoordinateMarkersSelect,
                                _ => UICss.PrintShowCoordinateMarkersSelect
                            }
                        }),
                        id = CoodianteMakersVisiblitySelectorId,
                        options=new UISelect.Option[]
                        {
                            new UISelect.Option() { value="", label="--- nicht anzeigen ---" },
                            new UISelect.Option() { value="show", label="Im Ausdruck anzeigen" }
                        }
                    },
                    new UIConditionDiv() {
                        ContitionElementId = $"{ id }--{ ShowCoordinatesMarkersId }",
                        ConditionType = UIConditionDiv.ConditionTypes.ElementValue,
                        ConditionResult = true,
                        ConditionArguments = new[]{ "show" },
                        elements = new UIElement[]{
                            new UIPrintCoordinateLabelFieldCombo()
                            {
                                css=UICss.ToClass(new[] { UICss.ToolParameter, UICss.ToolParameterPersistent }),
                                id=$"{ id }--{ CoordinatesLabelFieldId }"
                            }
                        }
                    }
                })
            }
        });

        this.AddChild(new UIDiv()
        {
            VisibilityDependency = VisibilityDependency.HasToolResults_Chainage,
            elements = new IUIElement[]
            {
                new UILabel() { label = "Stationierungsmarker" },
                new UISelect()
                    {
                        css = UICss.ToClass(new[] {
                            UICss.ToolParameter, 
                            UICss.ToolParameterPersistent,
                            button?.GetType().ToToolId() switch {
                                "webgis.tools.mapseriesprint" => UICss.MapSeriesPrintShowChainageMarkersSelect,
                                _ => UICss.PrintShowChainageMarkersSelect
                            }
                        }),
                        id = ChainageMarkersVisiblitySelectorId,
                        options=new UISelect.Option[]
                        {
                            new UISelect.Option() { value="", label="--- nicht anzeigen ---" },
                            new UISelect.Option() { value="show", label="Im Ausdruck anzeigen" }
                        }
                    },
            }
        });
    }

    public string QueryMarkersVisibilitySelectorId => $"{_id}--{ShowQueryMarkersId}";
    public string CoodianteMakersVisiblitySelectorId => $"{_id}--{ShowCoordinatesMarkersId}";
    public string ChainageMarkersVisiblitySelectorId => $"{_id}--{ShowChainageMarkersId}";

    #region Static Members

    static public string GetValue(ApiToolEventArguments e, string id, string subId)
    {
        return e[$"{id}--{subId}"];
    }

    #endregion
}
