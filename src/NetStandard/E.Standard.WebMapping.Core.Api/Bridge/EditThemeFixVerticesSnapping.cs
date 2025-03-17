using E.Standard.WebMapping.Core.Abstraction;

namespace E.Standard.WebMapping.Core.Api.Bridge;

public class EditThemeFixVerticesSnapping
{
    public EditThemeFixVerticesSnapping(IMapService service, string[] layerIds, string[] snappingTypes)
    {
        this.Service = service;
        this.LayerIds = layerIds;
        this.SnappingTypes = snappingTypes;
    }

    public IMapService Service { get; }
    public string[] LayerIds { get; }
    public string[] SnappingTypes { get; }
}
