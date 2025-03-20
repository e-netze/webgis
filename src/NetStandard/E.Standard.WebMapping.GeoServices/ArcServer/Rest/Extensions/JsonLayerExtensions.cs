using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;

static internal class JsonLayerExtensions
{
    static public double GetEffectiveMinScale(this JsonLayer jsonLayer)
    {
        if (jsonLayer.EffectiveMinScale > 0)
        {
            return jsonLayer.EffectiveMinScale;
        }

        return jsonLayer.MinScale;
    }

    static public double GetEffectiveMaxScale(this JsonLayer jsonLayer)
    {
        if (jsonLayer.EffectiveMaxScale > 0)
        {
            return jsonLayer.EffectiveMaxScale;
        }

        return jsonLayer.MaxScale;
    }

    static public bool DefaultVisbilityIncludesGroups(this JsonLayer jsonLayer)
    {
        if (jsonLayer.DefaultVisibility == false)
        {
            return false;
        }

        if (jsonLayer.ParentLayer == null)
        {
            //return jsonLayer.Type == "Group Layer"
            //    ? false
            //    : jsonLayer.DefaultVisibility;
            return jsonLayer.DefaultVisibility;
        }

        return DefaultVisbilityIncludesGroups(jsonLayer.ParentLayer);
    }
}
