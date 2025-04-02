using E.Standard.WebMapping.Core.Geometry;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.WebGIS.Core.Models;

[System.Text.Json.Serialization.JsonPolymorphic()]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ProjectionServiceResultDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ProjectionServiceArgumentDTO))]
public class ProjectionServiceResultDTO
{
    [JsonProperty(PropertyName = "srs")]
    [System.Text.Json.Serialization.JsonPropertyName("srs")]
    public int Srs { get; set; }

    [JsonProperty(PropertyName = "coordinates")]
    [System.Text.Json.Serialization.JsonPropertyName("coordinates")]
    public double[,] Coordinates { get; set; }

    #region Members

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public Point Point
    {
        get
        {
            if (Coordinates?.GetLength(0) >= 1)
            {
                return new Point(Coordinates[0, 0], Coordinates[0, 1]);
            }

            return null;
        }
        set
        {
            Coordinates = new double[,] { { value.X, value.Y } };
        }
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public Envelope Envelope
    {
        get
        {
            if (Coordinates.GetLength(0) >= 2)
            {
                return new Envelope(Coordinates[0, 0], Coordinates[0, 1],
                                    Coordinates[1, 0], Coordinates[1, 1]);
            }

            return null;
        }
        set
        {
            Coordinates = new double[,]
            {
                { value.MinX, value.MinY },
                { value.MaxX, value.MaxY }
            };
        }
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public Point[] Points
    {
        get
        {
            List<Point> points = new List<Point>();

            for (int i = 0, length = Coordinates.GetLength(0); i < length; i++)
            {
                points.Add(new Point(Coordinates[i, 0], Coordinates[i, 1]));
            }

            return points.ToArray();
        }
        set
        {
            Coordinates = new double[value.Length, 2];

            for (int i = 0, length = value.Length; i < value.Length; i++)
            {
                Coordinates[i, 0] = value[i].X;
                Coordinates[i, 1] = value[i].Y;
            }
        }
    }

    #endregion
}

public class ProjectionServiceArgumentDTO : ProjectionServiceResultDTO
{
    public ProjectionServiceArgumentDTO() { }  // needed for deserialization
    public ProjectionServiceArgumentDTO(int from, int to, Point point)
    {
        this.Srs = from;
        this.ToSrs = to;
        this.Point = point;
    }

    public ProjectionServiceArgumentDTO(int from, int to, Envelope envelope)
    {
        this.Srs = from;
        this.ToSrs = to;
        this.Envelope = envelope;
    }

    [JsonProperty(PropertyName = "to_srs")]
    [System.Text.Json.Serialization.JsonPropertyName("to_srs")]
    public int ToSrs { get; set; }
}
