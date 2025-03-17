using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiClientTool : IApiTool
{
    ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e);
}
