using E.Standard.WebMapping.Core.Api.UI.Abstractions;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UICollapsableHelp : UIDiv
{
    public UICollapsableHelp(string title, string content)
        : base()
    {
        this.title = title;

        this.CollapseState = CollapseStatus.Collapsed;
        this.ExpandBehavior = ExpandBehaviorMode.Normal;

        this.elements = new IUIElement[]
        {
            new UILabel()
            {
                label = content
            }
        };

        this.css = "webgis-info";
    }
}
