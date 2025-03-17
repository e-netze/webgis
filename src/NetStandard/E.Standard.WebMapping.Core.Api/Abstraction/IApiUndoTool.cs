using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiUndoTool
{
    Task<ApiEventResponse> PerformUndo(IBridge bridge, ToolUndoDTO toolUndo);
}
