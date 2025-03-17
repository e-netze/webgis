namespace E.Standard.Cms.Services;
public class CmsToolContext
{
    public string CmsId { get; set; } = "";
    public object Deployment { get; set; } = "";
    public string Username { get; set; } = "";
    public string ContentRootPath { get; set; } = "";
    public string? CmsTreePath { get; set; }
}
