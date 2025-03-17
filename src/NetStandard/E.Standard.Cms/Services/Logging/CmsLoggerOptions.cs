namespace E.Standard.Cms.Services.Logging;

public class CmsLoggerOptions
{
    public string ConnectionString { get; set; } = "";
    public int MaxFileSizeBytes { get; set; }
}
