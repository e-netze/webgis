namespace E.Standard.WebGIS.SDK.DataLinq;

public class RecordLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public double[] BBox { get; set; }

    public bool BBoxValid
    {
        get { return BBox != null && BBox.Length == 4; }
    }
}
