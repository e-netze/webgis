using Newtonsoft.Json;
using System.Linq;

namespace E.Standard.Api.App.DTOs.Geometry;

public sealed class PointDTO : GeometryDTO
{
    public PointDTO() : base("Point") { }

    public PointDTO(WebMapping.Core.Geometry.Point point)
        : this()
    {
        if (point == null || point.IsEmpty)
        {
            this.COORDINATES = new double[0];
            this.coordinates = new double[0];
        }
        else
        {
            double[] x = new double[] { point.X }, y = new double[] { point.Y };
            using (var transformer = new WebMapping.Core.Geometry.GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, point.SrsId, 4326))
            {
                this.COORDINATES = new double[] { x[0], y[0] };

                if (point.HasZ == true)
                {
                    this.COORDINATES = this.COORDINATES.Concat(new double[] { point.Z }).ToArray();
                }

                if (point.HasM == true && point is WebMapping.Core.Geometry.PointM && ((WebMapping.Core.Geometry.PointM)point).M is double)
                {
                    this.COORDINATES = this.COORDINATES.Concat(new double[] { (double)((WebMapping.Core.Geometry.PointM)point).M }).ToArray();
                }

                transformer.Transform(x, y);
                this.coordinates = new double[] { x[0], y[0] };
            }
        }
        this.COORDINATES_srefid = point.SrsId;
        this.COORDINATES_sref_p4 = point.SrsP4Parameters;
        this.snapped_coordinates = point.IsSnapped == true ? new bool[] { true } : null;
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public double[] coordinates { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public double[] COORDINATES { get; set; }
    public int COORDINATES_srefid { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string COORDINATES_sref_p4 { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool[] snapped_coordinates { get; set; }
}