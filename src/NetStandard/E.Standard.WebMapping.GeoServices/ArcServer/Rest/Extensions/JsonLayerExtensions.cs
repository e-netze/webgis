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
}
