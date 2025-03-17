using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiPostRequestEvent
{
    Task<ApiEventResponse> PostProcessEventResponseAsync(IBridge bridge, ApiToolEventArguments e, ApiEventResponse response);
}
