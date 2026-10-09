using System;
using System.Collections.Generic;

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
    // branch deploy contains uncommitted changes of the working copy (Commit is the base commit)
    public bool Uncommitted { get; set; }

    // fast deploy (branch deploy only): export file snapshot of the user, null => no fast deploy
    public string? ExportCacheFile { get; set; }
    // ignore an existing snapshot and read the whole tree from disk (the snapshot is rebuilt)
    public bool ExportFull { get; set; }
    // files with uncommitted changes in the working copy (paths relative to the tree, '/' separated)
    public IReadOnlyCollection<string>? UncommittedFiles { get; set; }
    // paths changed between a commit and the deployed commit, null if the commit is unknown
    public Func<string, IReadOnlyCollection<string>?>? ChangedPathsSince { get; set; }

    // solve warnings: solve the warnings of the user's last branch deploy (instead of the production deploy)
    public bool BranchWarnings { get; set; }
}
