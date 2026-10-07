using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace E.Standard.Cms.Git.Models;

public static class CmsGitChangeStates
{
    public const string Added = "added";
    public const string Modified = "modified";
    public const string Deleted = "deleted";
    public const string Conflicted = "conflicted";
}

public class CmsGitChange
{
    [JsonProperty("path")]
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonProperty("state")]
    [System.Text.Json.Serialization.JsonPropertyName("state")]
    public string State { get; set; }
}

public class CmsGitStatus
{
    [JsonProperty("enabled")]
    [System.Text.Json.Serialization.JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonProperty("has_workspace")]
    [System.Text.Json.Serialization.JsonPropertyName("has_workspace")]
    public bool HasWorkspace { get; set; }

    [JsonProperty("branch")]
    [System.Text.Json.Serialization.JsonPropertyName("branch")]
    public string Branch { get; set; }

    [JsonProperty("default_branch")]
    [System.Text.Json.Serialization.JsonPropertyName("default_branch")]
    public string DefaultBranch { get; set; }

    [JsonProperty("is_default_branch")]
    [System.Text.Json.Serialization.JsonPropertyName("is_default_branch")]
    public bool IsDefaultBranch { get; set; }

    [JsonProperty("suggested_branch_prefix")]
    [System.Text.Json.Serialization.JsonPropertyName("suggested_branch_prefix")]
    public string SuggestedBranchPrefix { get; set; }

    [JsonProperty("has_upstream")]
    [System.Text.Json.Serialization.JsonPropertyName("has_upstream")]
    public bool HasUpstream { get; set; }

    [JsonProperty("ahead")]
    [System.Text.Json.Serialization.JsonPropertyName("ahead")]
    public int Ahead { get; set; }

    [JsonProperty("behind")]
    [System.Text.Json.Serialization.JsonPropertyName("behind")]
    public int Behind { get; set; }

    [JsonProperty("is_merging")]
    [System.Text.Json.Serialization.JsonPropertyName("is_merging")]
    public bool IsMerging { get; set; }

    /// <summary>
    /// While merging: the name of the merged branch (if known)
    /// </summary>
    [JsonProperty("merge_source")]
    [System.Text.Json.Serialization.JsonPropertyName("merge_source")]
    public string MergeSource { get; set; }

    /// <summary>
    /// While merging: number of nodes with unresolved conflicts
    /// </summary>
    [JsonProperty("conflict_count")]
    [System.Text.Json.Serialization.JsonPropertyName("conflict_count")]
    public int ConflictCount { get; set; }

    /// <summary>
    /// Fetch failed (remote offline...) => ahead/behind may be outdated
    /// </summary>
    [JsonProperty("stale")]
    [System.Text.Json.Serialization.JsonPropertyName("stale")]
    public bool Stale { get; set; }

    [JsonProperty("fetch_error")]
    [System.Text.Json.Serialization.JsonPropertyName("fetch_error")]
    public string FetchError { get; set; }

    [JsonProperty("last_commit")]
    [System.Text.Json.Serialization.JsonPropertyName("last_commit")]
    public string LastCommit { get; set; }

    [JsonProperty("changes")]
    [System.Text.Json.Serialization.JsonPropertyName("changes")]
    public IEnumerable<CmsGitChange> Changes { get; set; } = Array.Empty<CmsGitChange>();
}

public class CmsGitBranch
{
    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty("is_current")]
    [System.Text.Json.Serialization.JsonPropertyName("is_current")]
    public bool IsCurrent { get; set; }

    [JsonProperty("is_default")]
    [System.Text.Json.Serialization.JsonPropertyName("is_default")]
    public bool IsDefault { get; set; }

    [JsonProperty("is_local")]
    [System.Text.Json.Serialization.JsonPropertyName("is_local")]
    public bool IsLocal { get; set; }

    [JsonProperty("is_remote")]
    [System.Text.Json.Serialization.JsonPropertyName("is_remote")]
    public bool IsRemote { get; set; }

    [JsonProperty("last_commit_date")]
    [System.Text.Json.Serialization.JsonPropertyName("last_commit_date")]
    public DateTimeOffset? LastCommitDate { get; set; }

    [JsonProperty("last_commit_author")]
    [System.Text.Json.Serialization.JsonPropertyName("last_commit_author")]
    public string LastCommitAuthor { get; set; }
}

public static class CmsGitConflictKinds
{
    /// <summary>both sides changed the node</summary>
    public const string Modified = "modified";
    /// <summary>both sides created the node</summary>
    public const string Added = "added";
    /// <summary>the node was deleted in the own version and changed in the other</summary>
    public const string DeletedByMe = "deleted-by-me";
    /// <summary>the node was deleted in the other version and changed in the own</summary>
    public const string DeletedByThem = "deleted-by-them";
}

public static class CmsGitConflictChoices
{
    public const string Mine = "mine";
    public const string Theirs = "theirs";
}

public class CmsGitConflictFile
{
    [JsonProperty("path")]
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string Path { get; set; }

    /// <summary>content of the own version, null => deleted</summary>
    [JsonProperty("mine")]
    [System.Text.Json.Serialization.JsonPropertyName("mine")]
    public string Mine { get; set; }

    /// <summary>content of the other version, null => deleted</summary>
    [JsonProperty("theirs")]
    [System.Text.Json.Serialization.JsonPropertyName("theirs")]
    public string Theirs { get; set; }
}

/// <summary>
/// A conflict on a CMS node (node xml + metadata files, for deleted folders the whole sub tree)
/// </summary>
public class CmsGitConflict
{
    [JsonProperty("node")]
    [System.Text.Json.Serialization.JsonPropertyName("node")]
    public string NodePath { get; set; }

    [JsonProperty("kind")]
    [System.Text.Json.Serialization.JsonPropertyName("kind")]
    public string Kind { get; set; }

    [JsonProperty("files")]
    [System.Text.Json.Serialization.JsonPropertyName("files")]
    public List<CmsGitConflictFile> Files { get; set; } = new();
}

/// <summary>
/// The CMS user, that is the author of commits/merges
/// </summary>
public record CmsGitUser(string Name, string Email);
