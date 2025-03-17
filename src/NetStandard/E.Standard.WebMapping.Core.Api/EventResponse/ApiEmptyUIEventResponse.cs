using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;

namespace E.Standard.WebMapping.Core.Api.EventResponse;

public class ApiEmptyUIEventResponse : ApiEventResponse
{
    public ApiEmptyUIEventResponse()
    {
        this.UIElements = new IUIElement[] { new UIDiv() };
    }
}
