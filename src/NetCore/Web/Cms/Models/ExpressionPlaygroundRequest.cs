namespace Cms.Models;

public sealed class ExpressionPlaygroundRequest
{
    public string GeoJson { get; set; }

    public string Expression { get; set; }

    public string ExpressionType { get; set; }
}
