using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;

namespace E.Standard.WebMapping.Core.Api.EventResponse;

public class ApiNeutralUIEventresponse : ApiEventResponse
{
    public ApiNeutralUIEventresponse()
    {
        this.AppendUIElements = true;

        this.UIElements = new IUIElement[] { new UINeutral() };
    }
}
