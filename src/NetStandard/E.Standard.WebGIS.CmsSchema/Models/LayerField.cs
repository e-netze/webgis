namespace E.Standard.WebGIS.CmsSchema.Models;

public class LayerField
{
    public LayerField() { this.HasDomain = false; }
    public LayerField(string name)
        : this()
    {
        this.Name = this.Aliasname = name;
    }
    public LayerField(string name, string aliasname)
        : this()
    {
        this.Name = name;
        this.Aliasname = aliasname;
    }

    public string Name { get; set; }
    public string Aliasname { get; set; }
    public bool HasDomain { get; set; }
}
