using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UIPrintSketchSelector : UIOptionContainer
{
    public const string PrintTookSketch = "print_tool_sketch";
    public const string PrintToolSketchLables = "print_tool_sketch_labels";

    public UIPrintSketchSelector(string id)
    {
        this.title = "Werkzeug Sketch";
        this.CollapseState = UICollapsableElement.CollapseStatus.Collapsed;

        this.AddChild(new UIDiv()
        {
            elements = new IUIElement[]
            {
                new UILabel() { label = "Im Ausdruck anzeigen:" },
                new UIInputElementStack(new IUIElement[]
                {
                    new UISketchSelect()
                    {
                        css = UICss.ToClass(new[] { UICss.ToolParameter }),
                        id=$"{ id }--{ PrintTookSketch }"
                    },
                    new UIConditionDiv() {
                        ContitionElementId = $"{ id }--{ PrintTookSketch }",
                        ConditionType = UIConditionDiv.ConditionTypes.ElementValue,
                        ConditionResult = true,
                        ConditionArguments=new[]{ "*" },
                        elements = new UIElement[]{
                            new UISelect()
                            {
                                css=UICss.ToClass(new[] { UICss.ToolParameter, UICss.ToolParameterPersistent }),
                                id=$"{ id }--{ PrintToolSketchLables }",
                                options = new UISelect.Option[]
                                {
                                    new UISelect.Option() { value="general", label = "Gesamt Länge/Fläche beschriften" },
                                    new UISelect.Option() { value="segments", label="zusätzlich Einzelsegmente beschriften"}
                                }
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
