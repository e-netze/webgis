using System;

namespace E.Standard.Cms.Git.Exceptions;

/// <summary>
/// Expected, user facing git error. <see cref="L10nKey"/> is localized by the UI layer.
/// </summary>
public class CmsGitException : Exception
{
    public CmsGitException(string l10nKey, string details = null, Exception inner = null)
        : base(String.IsNullOrEmpty(details) ? l10nKey : $"{l10nKey}: {details}", inner)
    {
        L10nKey = l10nKey;
        Details = details;
    }

    public string L10nKey { get; }
    public string Details { get; }
}

public static class CmsGitErrors
{
    public const string Busy = "error-busy";
    public const string CommitFirst = "error-commit-first";
    public const string NothingToCommit = "error-nothing-to-commit";
    public const string MessageRequired = "error-message-required";
    public const string InvalidBranchName = "error-invalid-branch-name";
    public const string BranchExists = "error-branch-exists";
    public const string BranchNotFound = "error-branch-not-found";
    public const string CannotDeleteCurrentBranch = "error-cannot-delete-current-branch";
    public const string CannotDeleteDefaultBranch = "error-cannot-delete-default-branch";
    public const string MergeConflicts = "error-merge-conflicts";
    public const string MergeInProgress = "error-merge-in-progress";
    public const string NotMerging = "error-not-merging";
    public const string ConflictsRemaining = "error-conflicts-remaining";
    public const string ConflictNotFound = "error-conflict-not-found";
    public const string InvalidConflictChoice = "error-invalid-conflict-choice";
    public const string AlreadyOnDefaultBranch = "error-already-on-default-branch";
    public const string InvalidPath = "error-invalid-path";
    public const string PushRejected = "error-push-rejected";
    public const string RemoteNotReachable = "error-remote-not-reachable";
    public const string NoInitialTree = "error-no-initial-tree";
}
