using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs.Geometry;

[System.Text.Json.Serialization.JsonPolymorphic()]
[System.Text.Json.Serialization.JsonDerivedType(typeof(GeometryDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(PointDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(MultiPointDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(LineStringDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(PolygonDTO))]
public class GeometryDTO
{
    public GeometryDTO(string geometryType)
    {
        this.type = geometryType;
    }
    public string type
    {
        get;
        private set;
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? hasM { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? hasZ { get; set; }

    public static GeometryDTO FromShape(WebMapping.Core.Geometry.Shape shape)
    {
        GeometryDTO geometry = null;

        if (shape is WebMapping.Core.Geometry.Point)
        {
            geometry = new PointDTO((WebMapping.Core.Geometry.Point)shape);
        }

        if(shape is WebMapping.Core.Geometry.MultiPoint)
        {
            geometry = new MultiPointDTO((WebMapping.Core.Geometry.MultiPoint)shape);
        }

        if (shape is WebMapping.Core.Geometry.Polyline)
        {
            geometry = new LineStringDTO((WebMapping.Core.Geometry.Polyline)shape);
        }

        if (shape is WebMapping.Core.Geometry.Polygon)
        {
            geometry = new PolygonDTO((WebMapping.Core.Geometry.Polygon)shape);
        }

        if (geometry != null)
        {
            if (shape.HasM == true)
            {
                geometry.hasM = true;
            }

            if (shape.HasZ == true)
            {
                geometry.hasZ = true;
            }
        }

        return geometry;
    }
}