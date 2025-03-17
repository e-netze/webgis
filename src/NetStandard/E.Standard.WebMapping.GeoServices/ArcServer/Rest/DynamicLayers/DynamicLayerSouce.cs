namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.DynamicLayers;

public class DynamicLayerSouce
{
    public DynamicLayerSouce()
    {
        this.type = "mapLayer";
    }
    public string type { get; set; }

    public int mapLayerId { get; set; }
}
