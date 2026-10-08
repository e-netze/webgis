namespace E.Standard.Cms.Services;

public class CmsToolContext
{
    public string CmsId { get; set; } = "";
    public object Deployment { get; set; } = "";
    public string Username { get; set; } = "";
    public string ContentRootPath { get; set; } = "";
    public string? CmsTreePath { get; set; }

    // branch deploy: encoded branch name (CmsBranches.Encode), original branch name and commit sha
    public string? Branch { get; set; }
    public string? BranchName { get; set; }
    public string? Commit { get; set; }
}
