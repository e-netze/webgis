namespace E.Standard.Api.App.DTOs;

public sealed class FeaturesCount : VersionDTO
{
    public bool success { get; set; }
    public bool hasfeatures { get; set; }
    public int count { get; set; }
}
