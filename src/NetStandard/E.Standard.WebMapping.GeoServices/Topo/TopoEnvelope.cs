using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.Topo;

class TopoEnvelope
{
    int _lowerLeft, _upperRight;

    public TopoEnvelope(int lowerLeft, int upperRight)
    {
        _lowerLeft = lowerLeft;
        _upperRight = upperRight;
    }

    [JsonProperty(PropertyName = "ll")]
    [System.Text.Json.Serialization.JsonPropertyName("ll")]
    public int LowerLeft
    {
        get { return _lowerLeft; }
    }

    [JsonProperty(PropertyName = "ur")]
    [System.Text.Json.Serialization.JsonPropertyName("ur")]
    public int UpperRight
    {
        get { return _upperRight; }
    }
}
