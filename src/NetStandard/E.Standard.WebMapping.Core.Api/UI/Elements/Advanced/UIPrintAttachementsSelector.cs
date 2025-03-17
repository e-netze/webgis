using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UIPrintAttachementsSelector : UIOptionContainer
{
    public const string AttachQueryResultsId = "attach_query_results";
    public const string AttachCoordinatesId = "attach_coords";
    public const string CoordinatesFieldId = "coords_field";

    public UIPrintAttachementsSelector(string id)
    {
        this.title = "Anhänge (Attachments)";
        this.CollapseState = CollapseStatus.Collapsed;

        this.AddChild(new UIDiv()
        {
            css = UICss.ToClass(new string[] { "webgis-info" }),
            elements = new IUIElement[]
                {
                    new UILiteral() { literal="Werden Anhänge hinzugefügt, ist das Ergebnis eine ZIP Datei, in der die PDF Datei mit der Karte und die Anhänge enthalten sind." }
                }
        });

        this.AddChild(new UIDiv()
        {
            VisibilityDependency = VisibilityDependency.QueryResultsExists | VisibilityDependency.QueryFeaturesHasTableProperties,
            elements = new IUIElement[]
            {
                new UILabel() { label = "Abfrageergebnisse" },
                new UISelect()
                {
                    css=UICss.ToClass(new[] { UICss.ToolParameter, UICss.ToolParameterPersistent }),
                    id=$"{ id }--{ AttachQueryResultsId }",
                    options=new UISelect.Option[]
                    {
                        new UISelect.Option() { value="", label="--- nicht hinzufügen ---" },
                        new UISelect.Option() { value="csv", label="Als CSV Datei hinzufügen" },
                        new UISelect.Option() { value="csv-excel", label="Als MS Excel (CSV) hinzufügen" }
                    }
                },
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
                        css=UICss.ToClass(new[] { UICss.ToolParameter, UICss.ToolParameterPersistent }),
                        id=$"{ id }--{ AttachCoordinatesId }",
                        options=new UISelect.Option[]
                        {
                            new UISelect.Option() { value="", label="--- nicht hinzufügen ---" },
                            new UISelect.Option() { value="csv", label="Als CSV Datei hinzufügen" }
                        }
                    },
                    new UIConditionDiv() {
                        ContitionElementId = $"{ id }--{ AttachCoordinatesId }",
                        ConditionType = UIConditionDiv.ConditionTypes.ElementValue,
                        ConditionResult = true,
                        ConditionArguments=new[]{ "csv" },
                        elements = new UIElement[]{
                            new UIPrintCoordinateLabelFieldCombo()
                            {
                                ShowCoordinatePairsOnly = true,
                                css = UICss.ToClass(new[] { UICss.ToolParameter, UICss.ToolParameterPersistent }),
                                id = $"{ id }--{ CoordinatesFieldId }"
                            }
                        }
                    }
                })
            }
        });
    }

    #region Static Members

    static public string GetValue(ApiToolEventArguments e, string id, string subId)
    {
        return e[$"{id}--{subId}"];
    }

    #endregion
}
