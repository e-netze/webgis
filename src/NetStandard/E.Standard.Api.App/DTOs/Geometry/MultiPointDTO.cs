using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Api.App.DTOs.Geometry;

public sealed class MultiPointDTO : GeometryDTO
{
    public MultiPointDTO(WebMapping.Core.Geometry.MultiPoint multipoint)
        : base("MultiPoint")
    {
        List<double[]> COORDS = new List<double[]>();
        List<double[]> coords = new List<double[]>();
        List<bool?> snapped = new List<bool?>();
        using (var transformer = new WebMapping.Core.Geometry.GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, multipoint.SrsId, 4326))
        {
            for (int p = 0; p < multipoint.PointCount; p++)
            {
                var point = multipoint[p];
                double[] x = new double[] { point.X }, y = new double[] { point.Y };
                List<double> xy = new List<double>(new double[] { x[0], y[0] });
                if (multipoint.HasZ == true)
                {
                    xy.Add(point.Z);
                }
                if (multipoint.HasM == true && point is WebMapping.Core.Geometry.PointM && ((WebMapping.Core.Geometry.PointM)point).M is double)
                {
                    xy.Add((double)((WebMapping.Core.Geometry.PointM)point).M);
                }
                COORDS.Add(xy.ToArray());
                transformer.Transform(x, y);
                coords.Add(new double[] { x[0], y[0] });
                snapped.Add(point.IsSnapped);
            }
        }
        this.COORDINATES = COORDS.ToArray();
        this.coordinates = coords.ToArray();
        this.COORDINATES_srefid = multipoint.SrsId;
        this.COORDINATES_sref_p4 = multipoint.SrsP4Parameters;
        this.snapped_coordinates = snapped.Where(s => s == true).Count() > 0 ? snapped.ToArray() : null;
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public object[] coordinates { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public object[] COORDINATES { get; set; }
    public int COORDINATES_srefid { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string COORDINATES_sref_p4 { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool?[] snapped_coordinates { get; set; }
}