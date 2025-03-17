using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest;

abstract class RestLayer : Layer
{
    private int[] _parentLayerIds = null;

    public RestLayer(string name, string id, IMapService service, bool queryable)
        : base(name, id, service, queryable: queryable)
    {
    }
    public RestLayer(string name, string id, LayerType type, IMapService service, bool queryable)
        : base(name, id, type, service, queryable: queryable)
    {

    }

    public List<ParentLayer> ParentLayers { get; set; }

    public void CalcParentLayerIds(JsonLayer layer, JsonLayer[] layers)
    {
        List<int> ids = new List<int>();
        while (layer != null)
        {
            if (layer.ParentLayer == null)
            {
                break;
            }

            ids.Add(layer.ParentLayer.Id);
            layer = (from l in layers where l.Id == layer.ParentLayer.Id select l).FirstOrDefault();
        }
        _parentLayerIds = ids.ToArray();
    }

    public int[] ParentLayerIds
    {
        get
        {
            if (_parentLayerIds == null)
            {
                return new int[] { -1 };
            }

            return _parentLayerIds;
        }
    }

    public void CloneParentLayerIdsTo(RestLayer clone)
    {
        if (_parentLayerIds == null)
        {
            clone._parentLayerIds = null;
        }
        else
        {
            clone._parentLayerIds = new int[_parentLayerIds.Length];
            for (int i = 0; i < _parentLayerIds.Length; i++)
            {
                clone._parentLayerIds[i] = _parentLayerIds[i];
            }
        }
    }
}
