using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiServerToolAsync : IApiTool
{
    Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e);
    Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e);
}
