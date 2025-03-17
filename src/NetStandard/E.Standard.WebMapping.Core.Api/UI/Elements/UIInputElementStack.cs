using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIInputElementStack : UIElement
{
    public UIInputElementStack(IEnumerable<IUIElement> elements)
        : base("div")
    {
        this.elements = elements?.ToArray();
    }

    new public string css
    {
        get { return "webgis-input-element-stack"; }
        set { }
    }
}
