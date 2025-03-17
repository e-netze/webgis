using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using System;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest;

class TileLayer : RestLayer
{
    private readonly string _id = String.Empty;

    public TileLayer(string name, string id, IMapService service)
        : base(name, id, service, false)
    {
        _service = service;
        _id = id;
    }
    public TileLayer(string name, string id, LayerType type, IMapService service)
        : base(name, id, type, service, false)
    {
        _service = service;
        _id = id;
    }

    override public ILayer Clone(IMapService parent)
    {
        if (parent is null)
        {
            return null;
        }

        TileLayer clone = new TileLayer(this.Name, this.ID, this.Type, parent);
        clone.ClonePropertiesFrom(this);
        base.CloneParentLayerIdsTo(clone);
        return clone;
    }

    public override Task<bool> GetFeaturesAsync(QueryFilter query, FeatureCollection result, IRequestContext requestContext)
    {
        return Task.FromResult(true);
    }
}
