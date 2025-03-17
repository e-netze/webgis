namespace E.Standard.WebMapping.Core;

public class LayerLegendItem
{
    public string Label { get; set; }
    public byte[] Data { get; set; }
    public string ContentType { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string[] Values { get; set; }
}
