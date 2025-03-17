using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;

namespace E.Standard.Api.App;

class UnloadedRequestVisFiltersContext : IUnloadedRequestVisFiltersContext
{
    private readonly Bridge _bridge;
    private readonly VisFilterDefinitionDTO[] _unloadedVisFilters;
    public UnloadedRequestVisFiltersContext(Bridge bridge)
    {
        _bridge = bridge;
        _unloadedVisFilters = bridge.RequestFilters;

        _bridge.RequestFilters = null;
    }

    public void Dispose()
    {
        _bridge.RequestFilters = _unloadedVisFilters;
    }
}
