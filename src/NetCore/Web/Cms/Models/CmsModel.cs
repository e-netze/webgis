namespace Cms.Models;

public class CmsModel
{
    public string CmsId { get; set; }
    public string RootName { get; set; }
    public bool CanImport { get; set; }
    public bool CanClear { get; set; }
}
