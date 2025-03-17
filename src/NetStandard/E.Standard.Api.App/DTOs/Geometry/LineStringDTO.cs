using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Api.App.DTOs.Geometry;

public sealed class LineStringDTO : GeometryDTO
{
    public LineStringDTO(WebMapping.Core.Geometry.Polyline polyline)
        : base("LineString")
    {
        List<double[]> COORDS = new List<double[]>();
        List<double[]> coords = new List<double[]>();
        List<int> partIndex = new List<int>();
        List<bool?> snapped = new List<bool?>();

        using (var transformer = new WebMapping.Core.Geometry.GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, polyline.SrsId, 4326))
        {
            for (int i = 0; i < polyline.PathCount; i++)
            {
                partIndex.Add(coords.Count);
                for (int p = 0; p < polyline[i].PointCount; p++)
                {
                    var point = polyline[i][p];
                    double[] x = new double[] { point.X }, y = new double[] { point.Y };

                    List<double> xy = new List<double>(new double[] { x[0], y[0] });

                    if (polyline.HasZ == true)
                    {
                        xy.Add(point.Z);
                    }

                    if (polyline.HasM == true && point is WebMapping.Core.Geometry.PointM && ((WebMapping.Core.Geometry.PointM)point).M is double)
                    {
                        xy.Add((double)((WebMapping.Core.Geometry.PointM)point).M);
                    }

                    COORDS.Add(xy.ToArray());

                    transformer.Transform(x, y);
                    coords.Add(new double[] { x[0], y[0] });

                    snapped.Add(point.IsSnapped);
                }
            }
        }
        this.COORDINATES = COORDS.ToArray();
        this.coordinates = coords.ToArray();
        this.COORDINATES_srefid = polyline.SrsId;
        this.COORDINATES_sref_p4 = polyline.SrsP4Parameters;
        this.partindex = partIndex.Count > 1 ? partIndex.ToArray() : null;
        this.snapped_coordinates = snapped.Where(s => s == true).Count() > 0 ? snapped.ToArray() : null;
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public object[] coordinates { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public object[] COORDINATES { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int[] partindex { get; set; }
    public int COORDINATES_srefid { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string COORDINATES_sref_p4 { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool?[] snapped_coordinates { get; set; }
}