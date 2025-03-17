using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiServerTool : IApiTool
{
    ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e);
    ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e);
}
