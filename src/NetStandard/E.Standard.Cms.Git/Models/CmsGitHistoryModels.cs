using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace E.Standard.Cms.Git.Models;

public static class CmsGitRefTypes
{
    public const string Local = "local";
    public const string Remote = "remote";
}

public class CmsGitRef
{
    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// local | remote
    /// </summary>
    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; }
}

public class CmsGitHistoryCommit
{
    [JsonProperty("sha")]
    [System.Text.Json.Serialization.JsonPropertyName("sha")]
    public string Sha { get; set; }

    [JsonProperty("short_sha")]
    [System.Text.Json.Serialization.JsonPropertyName("short_sha")]
    public string ShortSha => Sha?.Length > 8 ? Sha.Substring(0, 8) : Sha;

    [JsonProperty("parents")]
    [System.Text.Json.Serialization.JsonPropertyName("parents")]
    public string[] Parents { get; set; }

    [JsonProperty("author")]
    [System.Text.Json.Serialization.JsonPropertyName("author")]
    public string Author { get; set; }

    [JsonProperty("date")]
    [System.Text.Json.Serialization.JsonPropertyName("date")]
    public DateTimeOffset? Date { get; set; }

    [JsonProperty("message")]
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonProperty("refs")]
    [System.Text.Json.Serialization.JsonPropertyName("refs")]
    public List<CmsGitRef> Refs { get; set; } = new();

    /// <summary>
    /// Commit only exists in the local working copy (not pushed yet)
    /// </summary>
    [JsonProperty("unpushed")]
    [System.Text.Json.Serialization.JsonPropertyName("unpushed")]
    public bool Unpushed { get; set; }
}

public class CmsGitHistory
{
    [JsonProperty("branch")]
    [System.Text.Json.Serialization.JsonPropertyName("branch")]
    public string Branch { get; set; }

    [JsonProperty("default_branch")]
    [System.Text.Json.Serialization.JsonPropertyName("default_branch")]
    public string DefaultBranch { get; set; }

    [JsonProperty("head")]
    [System.Text.Json.Serialization.JsonPropertyName("head")]
    public string Head { get; set; }

    /// <summary>
    /// Commit of the deploy clone (last deployment), null if unknown
    /// </summary>
    [JsonProperty("deployed")]
    [System.Text.Json.Serialization.JsonPropertyName("deployed")]
    public string Deployed { get; set; }

    [JsonProperty("all_branches")]
    [System.Text.Json.Serialization.JsonPropertyName("all_branches")]
    public bool AllBranches { get; set; }

    [JsonProperty("has_more")]
    [System.Text.Json.Serialization.JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    [JsonProperty("stale")]
    [System.Text.Json.Serialization.JsonPropertyName("stale")]
    public bool Stale { get; set; }

    [JsonProperty("fetch_error")]
    [System.Text.Json.Serialization.JsonPropertyName("fetch_error")]
    public string FetchError { get; set; }

    [JsonProperty("commits")]
    [System.Text.Json.Serialization.JsonPropertyName("commits")]
    public List<CmsGitHistoryCommit> Commits { get; set; } = new();
}

public class CmsGitCommitDetails
{
    [JsonProperty("sha")]
    [System.Text.Json.Serialization.JsonPropertyName("sha")]
    public string Sha { get; set; }

    [JsonProperty("short_sha")]
    [System.Text.Json.Serialization.JsonPropertyName("short_sha")]
    public string ShortSha => Sha?.Length > 8 ? Sha.Substring(0, 8) : Sha;

    [JsonProperty("parents")]
    [System.Text.Json.Serialization.JsonPropertyName("parents")]
    public string[] Parents { get; set; }

    [JsonProperty("author")]
    [System.Text.Json.Serialization.JsonPropertyName("author")]
    public string Author { get; set; }

    [JsonProperty("author_email")]
    [System.Text.Json.Serialization.JsonPropertyName("author_email")]
    public string AuthorEmail { get; set; }

    [JsonProperty("date")]
    [System.Text.Json.Serialization.JsonPropertyName("date")]
    public DateTimeOffset? Date { get; set; }

    [JsonProperty("message")]
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public string Message { get; set; }

    /// <summary>
    /// Changes compared to the first parent (merge commits: what the merge brought into the branch)
    /// </summary>
    [JsonProperty("changes")]
    [System.Text.Json.Serialization.JsonPropertyName("changes")]
    public CmsGitChange[] Changes { get; set; }
}

public class CmsGitFileDiff
{
    [JsonProperty("path")]
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string Path { get; set; }

    /// <summary>
    /// Content before the commit (first parent), null if the file did not exist
    /// </summary>
    [JsonProperty("before")]
    [System.Text.Json.Serialization.JsonPropertyName("before")]
    public string Before { get; set; }

    /// <summary>
    /// Content after the commit, null if the file was deleted
    /// </summary>
    [JsonProperty("after")]
    [System.Text.Json.Serialization.JsonPropertyName("after")]
    public string After { get; set; }
}
