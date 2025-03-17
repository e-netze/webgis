namespace E.Standard.WebMapping.GeoServices.Tiling.Models;

internal class TileData
{
    public string Url { get; set; }
    public int Row { get; set; }
    public int Col { get; set; }
    //public int Level { get; set; }
    public byte[] Data { get; set; }
}
