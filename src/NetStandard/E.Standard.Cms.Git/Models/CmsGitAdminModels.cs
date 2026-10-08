using System;

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

    public CmsGitBranchDeploy(string branch, string gitBranch, string commit, string workspacePath, Action release)
    {
        Branch = branch;
        GitBranch = gitBranch;
        Commit = commit;
        WorkspacePath = workspacePath;
        _release = release;
    }

    /// <summary>
    /// Name of the deployed branch. On the default branch: {user}-{default-branch}
    /// </summary>
    public string Branch { get; }

    /// <summary>
    /// Current git branch of the working copy
    /// </summary>
    public string GitBranch { get; }

    public string Commit { get; }

    public string WorkspacePath { get; }

    public void Dispose()
        => System.Threading.Interlocked.Exchange(ref _release, null)?.Invoke();
}