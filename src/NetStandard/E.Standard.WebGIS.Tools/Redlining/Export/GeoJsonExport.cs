using E.Standard.GeoJson;
using E.Standard.Json;
using System;
using System.Text;

namespace E.Standard.WebGIS.Tools.Redlining.Export;

class GeoJsonExport : IExport
{
    private byte[] _json;
    private int _featuresCount = 0;

    public GeoJsonExport()
    {
    }

    public int FeatureCount => _featuresCount;

    public void AddFeatures(GeoJsonFeatures features)
    {
        if (features.Features != null)
        {
            _featuresCount += features.Features.Length;
            _json = Encoding.UTF8.GetBytes(JSerializer.Serialize(features, pretty: true));
        }
    }

    public byte[] GetBytes(bool throwExcetionIfEmpty)
    {
        if (throwExcetionIfEmpty && _featuresCount == 0)
        {
            throw new Exception("Für den GeoJson Export wurden keine Objekte gefunden.");
        }

        return _json ?? new byte[0];
    }
}
