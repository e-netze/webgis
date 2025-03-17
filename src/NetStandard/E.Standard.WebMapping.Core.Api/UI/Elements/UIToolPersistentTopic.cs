using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Extensions;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIToolPersistentTopic : UIDiv
{
    public UIToolPersistentTopic(IApiButton tool)
    {
        this.id = $"{tool.GetType().ToToolId().Replace(".", "-")}-persistent-topic";

        css = UICss.ToClass(new[] { "webgis-tool-persistent-topic" });
    }
}
