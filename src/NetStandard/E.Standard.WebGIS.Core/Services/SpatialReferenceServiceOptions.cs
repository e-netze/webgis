namespace E.Standard.WebGIS.Core.Services;

public class SpatialReferenceServiceOptions
{
    public string AppRootPath { get; set; }
    public string Proj4DatabaseConnectionStringConfigKey { get; set; }
    public string ServerSideConfigurationPathConfigKey { get; set; }
}
