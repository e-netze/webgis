namespace E.Standard.WebGIS.Tools.Profile.Json;

public class JsonProfile
{
    public double super_elevation { get; set; }

    public double vertical_linestep { get; set; }
    public double horizontal_linestep { get; set; }

    public double min_height { get; set; }

    public bool show_heightbar { get; set; }
    public bool show_statbar { get; set; }
    public bool show_vertices_heightbar { get; set; }
    public bool show_vertices_statbar { get; set; }
    public string earth_color { get; set; }

    public JsonVertex[] vertices { get; set; }

    public class JsonVertex
    {
        public JsonVertex() { }
        public JsonVertex(WebMapping.Core.Geometry.PointM p)
        {
            this.stat = (double)p.M;
            this.z = p.Z;
        }
        public double stat { get; set; }
        public double z { get; set; }
    }
}
