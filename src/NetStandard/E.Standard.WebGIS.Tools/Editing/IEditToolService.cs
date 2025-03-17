using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing;

internal interface IEditToolService
{
    Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e);

    Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e);
}
