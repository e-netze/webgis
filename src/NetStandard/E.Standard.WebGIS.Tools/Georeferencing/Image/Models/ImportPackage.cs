namespace E.Standard.WebGIS.Tools.Georeferencing.Image.Models;

class ImportPackage
{
    public string Name { get; set; }
    public string ImageExtension { get; set; }

    public byte[] ImageData { get; set; }
    public ImageWorldfile WorldFile { get; set; }
    public string ProjectionWKT { get; set; }
    public GeorefImageMetadata Metadata { get; set; }
}
