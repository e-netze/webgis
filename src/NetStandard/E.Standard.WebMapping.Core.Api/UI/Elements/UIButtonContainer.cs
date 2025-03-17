using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIButtonContainer : UIDiv
{
    public UIButtonContainer() : base()
    {
    }

    public UIButtonContainer(IUIElement element) : this()
    {
        this.elements = new IUIElement[] { element };
    }

    public UIButtonContainer(IEnumerable<IUIElement> elements)
    {
        this.elements = elements.ToArray();
    }

    new public string css
    {
        get { return UICss.ToClass(new string[] { UICss.UIButtonContainer }); }
        set { }
    }
}
