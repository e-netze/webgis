using E.Standard.Cms.Configuration.Models;

namespace Cms.Models;

public class DeployModel
{
    public CmsConfig.CmsItem CmsItem { get; set; }
    public bool IsIFramed { get; set; }
}
