using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UIInfoBox : UIDiv
{
    public UIInfoBox(string text)
    {
        this.css = UICss.ToClass(new string[] { "webgis-info" });
        this.elements = new IUIElement[]
        {
            new UILiteral()
            {
                 literal = text
            },
        };
    }

    public UIInfoBox(string targetId, IDictionary<string, string> conditions)
    {
        List<UIElement> divs = new List<UIElement>();

        foreach (var key in conditions.Keys)
        {
            var div = new UIConditionDiv()
            {
                ConditionType = UIConditionDiv.ConditionTypes.ElementValue,
                ContitionElementId = targetId,
                ConditionResult = true,
                ConditionArguments = new[] { key },
                elements = new IUIElement[]
                {
                    new UILiteral()
                    {
                         literal = conditions[key]
                    },
                }
            };

            divs.Add(div);
        }

        this.css = UICss.ToClass(new string[] { "webgis-info" });
        this.elements = divs.ToArray();
    }
}
