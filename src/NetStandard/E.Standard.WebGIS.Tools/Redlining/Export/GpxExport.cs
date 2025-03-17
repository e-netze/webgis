using E.Standard.GeoJson;
using E.Standard.Gpx;
using E.Standard.Gpx.Schema;
using E.Standard.OGC.Schema;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.WebGIS.Tools.Redlining.Export;

class GpxExport : IExport
{
    private readonly gpxType _gpx;
    private int _featuresCount = 0;

    public GpxExport()
    {
        _gpx = new gpxType();
        _gpx.version = "1.1";

        #region Gpx Metadata

        _gpx.metadata = new metadataType();

        #endregion
    }

    public int FeatureCount => _featuresCount;

    public void AddFeatures(GeoJsonFeatures features)
    {
        int trkCount = 0, wkpCount = 0;

        List<wptType> wptPoints = new List<wptType>();

        if (features.Features != null)
        {
            foreach (var feature in features.Features)
            {
                string toolType = feature.GetPropery<string>("_meta.tool");
                string text = feature.GetPropery<string>("_meta.text");

                if (toolType == "line")
                {
                    var trk = GpxHelper.FromPolyline(_gpx, feature.ToShape() as Polyline);
                    if (trk != null)
                    {
                        trk.name = String.IsNullOrWhiteSpace(text) ? $"Track {(++trkCount)}" : text;
                        GpxHelper.AppendTrack(_gpx, trk);
                        _featuresCount++;
                    }
                }
                else if (toolType == "symbol" || toolType == "text" || toolType == "point")
                {
                    wptType pointType = GpxHelper.FromPoint(feature.ToShape() as Point);
                    if (pointType != null)
                    {
                        pointType.name = String.IsNullOrWhiteSpace(text) ? $"Track {(++wkpCount)}" : text;
                        wptPoints.Add(pointType);
                        _featuresCount++;
                    }
                }
            }
        }

        if (wptPoints.Count != 0)
        {
            _gpx.wpt = wptPoints.ToArray();
        }
    }

    public byte[] GetBytes(bool throwExcetionIfEmpty)
    {
        if (throwExcetionIfEmpty && _featuresCount == 0)
        {
            throw new Exception("Für den Gpx Export wurden keine Objekte gefunden. Es können nur Linien und Punkte/Symbole exportiert werden");
        }

        Serializer<gpxType> ser = new Serializer<gpxType>();
        string gpxXml = ser.Serialize(_gpx);

        return Encoding.UTF8.GetBytes(gpxXml);
    }
}
