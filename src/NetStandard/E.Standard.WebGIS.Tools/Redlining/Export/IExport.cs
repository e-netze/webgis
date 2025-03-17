using E.Standard.GeoJson;

namespace E.Standard.WebGIS.Tools.Redlining.Export;

public interface IExport
{
    void AddFeatures(GeoJsonFeatures features);

    int FeatureCount { get; }

    byte[] GetBytes(bool throwExcetionIfEmpty);
}
