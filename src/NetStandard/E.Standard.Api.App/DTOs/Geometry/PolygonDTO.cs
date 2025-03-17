using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Api.App.DTOs.Geometry;

public sealed class PolygonDTO : GeometryDTO
{
    public PolygonDTO(WebMapping.Core.Geometry.Polygon polygon)
        : base("Polygon")
    {
        List<object> rings = new List<object>();
        List<object> RINGs = new List<object>();

        List<int> partIndex = new List<int>();
        List<bool?> snapped = new List<bool?>();

        using (var transformer = new WebMapping.Core.Geometry.GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, polygon.SrsId, 4326))
        {
            for (int i = 0; i < polygon.RingCount; i++)
            {
                List<double[]> COORDS = new List<double[]>();
                List<double[]> coords = new List<double[]>();
                partIndex.Add(coords.Count);

                for (int p = 0; p < polygon[i].PointCount; p++)
                {
                    //double[] x = new double[] { polygon[i][p].X }, y = new double[] { polygon[i][p].Y };
                    //COORDS.Add(new double[] { x[0], y[0] });
                    var point = polygon[i][p];
                    double[] x = new double[] { point.X }, y = new double[] { point.Y };

                    List<double> xy = new List<double>(new double[] { x[0], y[0] });

                    if (polygon.HasZ == true)
                    {
                        xy.Add(point.Z);
                    }

                    if (polygon.HasM == true && point is WebMapping.Core.Geometry.PointM && ((WebMapping.Core.Geometry.PointM)point).M is double)
                    {
                        xy.Add((double)((WebMapping.Core.Geometry.PointM)point).M);
                    }

                    COORDS.Add(xy.ToArray());

                    transformer.Transform(x, y);
                    coords.Add(new double[] { x[0], y[0] });

                    snapped.Add(point.IsSnapped);
                }
                rings.Add(coords.ToArray());
                RINGs.Add(COORDS.ToArray());
            }
        }
        this.COORDINATES = RINGs.ToArray();
        this.coordinates = rings.ToArray();
        this.COORDINATES_srefid = polygon.SrsId;
        this.COORDINATES_sref_p4 = polygon.SrsP4Parameters;
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

    public bool?[] snapped_coordinates { get; set; }
}