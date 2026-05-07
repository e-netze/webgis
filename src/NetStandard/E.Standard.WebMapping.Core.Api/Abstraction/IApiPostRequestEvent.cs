using System.Threading.Tasks;

using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiPostRequestEvent
{
    Task<ApiEventResponse> PostProcessEventResponseAsync(IBridge bridge, ApiToolEventArguments e, ApiEventResponse response);
}
