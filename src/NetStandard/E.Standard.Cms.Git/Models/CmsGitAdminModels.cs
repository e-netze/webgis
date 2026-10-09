using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace E.Standard.Cms.Git.Models;

public class CmsGitCommitInfo
{
    [JsonProperty("sha")]
    [System.Text.Json.Serialization.JsonPropertyName("sha")]
    public string Sha { get; set; }

    [JsonProperty("short_sha")]
    [System.Text.Json.Serialization.JsonPropertyName("short_sha")]
    public string ShortSha => Sha?.Length > 8 ? Sha.Substring(0, 8) : Sha;

    [JsonProperty("author")]
    [System.Text.Json.Serialization.JsonPropertyName("author")]
    public string Author { get; set; }

    [JsonProperty("date")]
    [System.Text.Json.Serialization.JsonPropertyName("date")]
    public DateTimeOffset? Date { get; set; }

    [JsonProperty("message")]
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public string Message { get; set; }
}

/// <summary>
/// What a deployment will publish (remote default branch) and the state of the current user's working copy
/// </summary>
public class CmsGitDeployInfo
{
    [JsonProperty("default_branch")]
    [System.Text.Json.Serialization.JsonPropertyName("default_branch")]
    public string DefaultBranch { get; set; }

    /// <summary>
    /// Latest commit of the remote default branch => will be deployed
    /// </summary>
    [JsonProperty("remote_head")]
    [System.Text.Json.Serialization.JsonPropertyName("remote_head")]
    public CmsGitCommitInfo RemoteHead { get; set; }

    /// <summary>
    /// Commit of the deploy clone => deployed last time (null, if never deployed)
    /// </summary>
    [JsonProperty("last_deployed")]
    [System.Text.Json.Serialization.JsonPropertyName("last_deployed")]
    public CmsGitCommitInfo LastDeployed { get; set; }

    /// <summary>
    /// Status of the user's working copy (null, if the user has no working copy)
    /// </summary>
    [JsonProperty("user_status")]
    [System.Text.Json.Serialization.JsonPropertyName("user_status")]
    public CmsGitStatus UserStatus { get; set; }

    [JsonProperty("error")]
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public string Error { get; set; }

    /// <summary>
    /// true, if the user has changes that are not part of the deployment (uncommitted, unpushed or on another branch)
    /// </summary>
    [JsonProperty("has_unpublished_changes")]
    [System.Text.Json.Serialization.JsonPropertyName("has_unpublished_changes")]
    public bool HasUnpublishedChanges
        => UserStatus != null && UserStatus.HasWorkspace &&
           (UserStatus.IsMerging
            || UserStatus.Changes?.GetEnumerator().MoveNext() == true
            || UserStatus.Ahead > 0
            || !UserStatus.IsDefaultBranch
            || !UserStatus.HasUpstream);
}

/// <summary>
/// Working copy of a user (admin overview)
/// </summary>
public class CmsGitWorkspaceInfo
{
    /// <summary>
    /// Folder name (=safe username)
    /// </summary>
    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty("is_current_user")]
    [System.Text.Json.Serialization.JsonPropertyName("is_current_user")]
    public bool IsCurrentUser { get; set; }

    [JsonProperty("branch")]
    [System.Text.Json.Serialization.JsonPropertyName("branch")]
    public string Branch { get; set; }

    [JsonProperty("changes")]
    [System.Text.Json.Serialization.JsonPropertyName("changes")]
    public int Changes { get; set; }

    [JsonProperty("ahead")]
    [System.Text.Json.Serialization.JsonPropertyName("ahead")]
    public int Ahead { get; set; }

    [JsonProperty("has_upstream")]
    [System.Text.Json.Serialization.JsonPropertyName("has_upstream")]
    public bool HasUpstream { get; set; }

    [JsonProperty("is_merging")]
    [System.Text.Json.Serialization.JsonPropertyName("is_merging")]
    public bool IsMerging { get; set; }

    [JsonProperty("last_modified")]
    [System.Text.Json.Serialization.JsonPropertyName("last_modified")]
    public DateTime? LastModified { get; set; }

    [JsonProperty("error")]
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public string Error { get; set; }
}

/// <summary>
/// A running branch deploy: the user's working copy is locked until disposed
/// </summary>
public sealed class CmsGitBranchDeploy : IDisposable
{
    private Action _release;

    private readonly Func<string, IReadOnlyCollection<string>> _changedPathsSince;

    public CmsGitBranchDeploy(string branch, string gitBranch, string commit, bool uncommitted, string workspacePath, Action release)
        : this(branch, gitBranch, commit, Array.Empty<string>(), workspacePath, null, release)
    {
        Uncommitted = uncommitted;
    }

    public CmsGitBranchDeploy(string branch,
                              string gitBranch,
                              string commit,
                              IReadOnlyCollection<string> uncommittedFiles,
                              string workspacePath,
                              Func<string, IReadOnlyCollection<string>> changedPathsSince,
                              Action release)
    {
        Branch = branch;
        GitBranch = gitBranch;
        Commit = commit;
        UncommittedFiles = uncommittedFiles ?? Array.Empty<string>();
        Uncommitted = UncommittedFiles.Count > 0;
        WorkspacePath = workspacePath;
        _changedPathsSince = changedPathsSince;
        _release = release;
    }

    /// <summary>
    /// Paths (relative to the tree, '/' separated) of the uncommitted files (incl. untracked) when the deploy started
    /// </summary>
    public IReadOnlyCollection<string> UncommittedFiles { get; }

    /// <summary>
    /// Paths of the files that differ between the given commit and <see cref="Commit"/>.
    /// null, if the commit is not available. Only valid while the deploy is running (working copy locked).
    /// </summary>
    public IReadOnlyCollection<string> ChangedPathsSince(string commit)
        => _changedPathsSince?.Invoke(commit);

    /// <summary>
    /// Name of the deployed branch. On the default branch: {user}-{default-branch}
    /// </summary>
    public string Branch { get; }

    /// <summary>
    /// Current git branch of the working copy
    /// </summary>
    public string GitBranch { get; }

    /// <summary>
    /// HEAD commit of the working copy (base commit, if Uncommitted)
    /// </summary>
    public string Commit { get; }

    /// <summary>
    /// true, if the working copy contains uncommitted changes, that are deployed too
    /// </summary>
    public bool Uncommitted { get; }

    public string WorkspacePath { get; }

    public void Dispose()
        => System.Threading.Interlocked.Exchange(ref _release, null)?.Invoke();
}