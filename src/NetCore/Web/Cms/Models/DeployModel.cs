using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Git.Models;
using E.Standard.Localization.Abstractions;

namespace Cms.Models;

public class DeployModel
{
    public CmsConfig.CmsItem CmsItem { get; set; }
    public bool IsIFramed { get; set; }

    /// <summary>
    /// only set, if git is configured for the cms-item
    /// </summary>
    public CmsGitDeployInfo GitDeployInfo { get; set; }
    public string GitDeployRunningBy { get; set; }
    public ILocalizer GitLocalizer { get; set; }
}
