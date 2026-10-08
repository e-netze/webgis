namespace Portal.Core.Models.Portal;

public class PortalModel
{
    public string PortalPageId { get; set; }
    public string PortalPageName { get; set; }
    public string PortalPageDescription { get; set; }
    public bool IsMapAuthor { get; set; }
    public bool IsPortalPageOwner { get; set; }

    public string HtmlMetaTags { get; set; }

    public bool AllowUserAccessSettings { get; set; }
    public bool IsContentAuthor { get; set; }
    public string BannerId { get; set; }
    public string CurrentUsername { get; set; }
    public bool AllowLogout { get; set; }
    public bool AllowLogin { get; set; }

    public string ManifestUrl { get; set; }
    public string ScopeUrl { get; set; }

    public bool ShowOptimizationFilter { get; set; }

    // deployed cms branches (incl. main => Encoded == ""); empty, if branches are not allowed or the user may not select a branch
    public E.Standard.WebGIS.Core.Models.ApiBranchDTO[] ConfigBranches { get; set; }

    public bool CanSelectBranch => (IsMapAuthor || IsPortalPageOwner) && ConfigBranches?.Length > 1;
}
