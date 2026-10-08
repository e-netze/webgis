using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using E.Standard.Cms.Git.Exceptions;
using E.Standard.Cms.Git.Models;

using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace E.Standard.Cms.Git.Services;

public record CmsGitSettings(
    string RemoteUrl,
    string Username,
    string Token,
    string DefaultBranch,
    string CommitterName = null,
    string CommitterEmail = null);

/// <summary>
/// All git operations on one working copy (user workspace or deploy clone).
/// Not thread safe => callers have to serialize the access per working copy (see <see cref="CmsGitService"/>).
/// </summary>
public class CmsGitWorkspace
{
    public const string RemoteName = "origin";

    private static readonly string[] GitIgnoreLines = { ".versioninfo.xml" };
    private static readonly string[] GitAttributesLines = { "* -text" };

    private readonly CmsGitSettings _settings;

    public CmsGitWorkspace(string path, CmsGitSettings settings)
    {
        WorkspacePath = path;
        _settings = settings;
    }

    public string WorkspacePath { get; }

    public string DefaultBranch => String.IsNullOrWhiteSpace(_settings.DefaultBranch) ? "main" : _settings.DefaultBranch.Trim();

    public bool Exists => Directory.Exists(Path.Combine(WorkspacePath, ".git"));

    #region Create

    /// <summary>
    /// Clones the remote into the workspace.
    /// If the remote is empty, the repository is initialized with the content of <paramref name="initialTreePath"/> and pushed.
    /// </summary>
    public void Create(string initialTreePath, CmsGitUser user, string initialCommitMessage)
    {
        if (Exists)
        {
            return;
        }

        var remoteRefs = ListRemoteReferences();

        if (remoteRefs.Count == 0)
        {
            try
            {
                InitializeFromTree(initialTreePath, user, initialCommitMessage);
                return;
            }
            catch (NonFastForwardException)
            {
                // someone initialized the remote in the meantime => clone it
                DeleteDirectory(WorkspacePath);
                remoteRefs = ListRemoteReferences();
            }
        }

        Clone(remoteRefs);
    }

    private void Clone(IEnumerable<Reference> remoteRefs)
    {
        PrepareEmptyDirectory(WorkspacePath);

        var options = new CloneOptions()
        {
            BranchName = remoteRefs.Any(r => r.CanonicalName == $"refs/heads/{DefaultBranch}") ? DefaultBranch : null
        };
        ApplyFetchOptions(options.FetchOptions);

        try
        {
            Repository.Clone(_settings.RemoteUrl, WorkspacePath, options);
        }
        catch (LibGit2SharpException ex)
        {
            DeleteDirectory(WorkspacePath);
            throw new CmsGitException(CmsGitErrors.RemoteNotReachable, ex.Message, ex);
        }

        using var repo = Open();
        ConfigureRepository(repo);
    }

    private void InitializeFromTree(string initialTreePath, CmsGitUser user, string message)
    {
        if (String.IsNullOrWhiteSpace(initialTreePath)
            || !Directory.Exists(initialTreePath)
            || !Directory.EnumerateFileSystemEntries(initialTreePath).Any())
        {
            throw new CmsGitException(CmsGitErrors.NoInitialTree, initialTreePath);
        }

        PrepareEmptyDirectory(WorkspacePath);

        try
        {
            Repository.Init(WorkspacePath);
            File.WriteAllText(Path.Combine(WorkspacePath, ".git", "HEAD"), $"ref: refs/heads/{DefaultBranch}\n");

            CopyDirectory(initialTreePath, WorkspacePath);
            WriteRepositoryFiles(WorkspacePath);

            using var repo = Open();
            ConfigureRepository(repo);

            repo.Network.Remotes.Add(RemoteName, _settings.RemoteUrl);

            Commands.Stage(repo, "*");
            repo.Commit(message, AuthorSignature(user), CommitterSignature(user));

            SetUpstream(repo, repo.Head);
            PushBranch(repo, repo.Head);
        }
        catch
        {
            DeleteDirectory(WorkspacePath);
            throw;
        }
    }

    #endregion

    #region Status

    public CmsGitStatus GetStatus(bool fetch)
    {
        using var repo = Open();

        string fetchError = null;
        if (fetch)
        {
            try
            {
                Fetch(repo);
            }
            catch (Exception ex)
            {
                fetchError = ex.Message;
            }
        }

        var head = repo.Head;
        var tracking = head.IsTracking ? head.TrackingDetails : null;
        var isMerging = repo.Info.CurrentOperation != CurrentOperation.None;

        return new CmsGitStatus()
        {
            HasWorkspace = true,
            Branch = head.FriendlyName,
            DefaultBranch = DefaultBranch,
            IsDefaultBranch = head.FriendlyName == DefaultBranch,
            HasUpstream = head.IsTracking && head.TrackedBranch?.Tip != null,
            Ahead = tracking?.AheadBy ?? 0,
            Behind = tracking?.BehindBy ?? 0,
            IsMerging = isMerging,
            MergeSource = isMerging ? MergeSourceName(repo) : null,
            ConflictCount = isMerging ? GetConflicts(repo).Count : 0,
            Stale = fetchError != null,
            FetchError = fetchError,
            LastCommit = head.Tip?.Sha,
            Changes = GetChanges(repo).ToArray()
        };
    }

    public void Fetch()
    {
        using var repo = Open();
        Fetch(repo);
    }

    #endregion

    #region Commit / Pull / Push

    public string Commit(CmsGitUser user, string message)
    {
        if (String.IsNullOrWhiteSpace(message))
        {
            throw new CmsGitException(CmsGitErrors.MessageRequired);
        }

        using var repo = Open();
        EnsureNotMerging(repo);

        if (!IsDirty(repo))
        {
            throw new CmsGitException(CmsGitErrors.NothingToCommit);
        }

        Commands.Stage(repo, "*");
        var commit = repo.Commit(message.Trim(), AuthorSignature(user), CommitterSignature(user));

        return commit.Sha;
    }

    /// <summary>
    /// Fetch + merge of the upstream branch. Requires a clean working copy.
    /// </summary>
    /// <returns>true, if the files in the working copy changed</returns>
    public bool Pull(CmsGitUser user)
    {
        using var repo = Open();
        EnsureNotMerging(repo);
        EnsureClean(repo);

        Fetch(repo);

        return MergeUpstream(repo, user);
    }

    /// <summary>
    /// Pushes the current branch. If the remote branch has new commits, these are merged first.
    /// On merge conflicts the merge state is kept (see <see cref="GetConflicts()"/>) and <see cref="CmsGitErrors.MergeConflicts"/> is thrown.
    /// </summary>
    /// <returns>true, if the files in the working copy changed (merge of remote commits)</returns>
    public bool Push(CmsGitUser user)
    {
        using var repo = Open();
        EnsureNotMerging(repo);

        if (repo.Head.Tip == null)
        {
            throw new CmsGitException(CmsGitErrors.NothingToCommit);
        }

        return PushCurrentBranch(repo, user);
    }

    #endregion

    #region Merge

    /// <summary>
    /// Merges the remote default branch ("main") into the current branch. Requires a clean working copy.
    /// On conflicts the merge state is kept and <see cref="CmsGitErrors.MergeConflicts"/> is thrown.
    /// </summary>
    /// <returns>true, if the files in the working copy changed</returns>
    public bool MergeFromDefault(CmsGitUser user)
    {
        using var repo = Open();
        EnsureNotMerging(repo);
        EnsureClean(repo);
        EnsureNotOnDefaultBranch(repo);

        FetchOrThrow(repo);

        return MergeBranch(repo, RemoteDefaultBranch(repo), user, FastForwardStrategy.Default);
    }

    /// <summary>
    /// Merges the current branch into the default branch ("main") and pushes both.
    /// 1. remote changes of the current branch and of main are merged into the current branch
    ///    (on conflicts the merge state is kept on the current branch => resolve and call again)
    /// 2. the current branch is pushed
    /// 3. switch to main, update main, merge the branch (merge commit), push main
    /// 4. optional: delete the branch (local and remote)
    /// </summary>
    public void MergeIntoDefault(CmsGitUser user, bool deleteBranch)
    {
        using var repo = Open();
        EnsureNotMerging(repo);
        EnsureClean(repo);
        EnsureNotOnDefaultBranch(repo);

        FetchOrThrow(repo);

        var branchName = repo.Head.FriendlyName;
        SetUpstream(repo, repo.Head);

        MergeUpstream(repo, user);
        MergeBranch(repo, RemoteDefaultBranch(repo), user, FastForwardStrategy.Default);
        PushCurrentBranch(repo, user);

        var defaultBranch = repo.Branches[DefaultBranch];
        if (defaultBranch == null)
        {
            defaultBranch = repo.CreateBranch(DefaultBranch, RemoteDefaultBranch(repo).Tip);
            SetUpstream(repo, defaultBranch);
            defaultBranch = repo.Branches[DefaultBranch];
        }
        Commands.Checkout(repo, defaultBranch);

        MergeUpstream(repo, user);
        MergeBranch(repo, repo.Branches[branchName], user, FastForwardStrategy.NoFastForward);
        PushCurrentBranch(repo, user);

        if (deleteBranch)
        {
            DeleteBranch(repo, branchName, deleteRemote: true);
        }
    }

    /// <summary>
    /// Unresolved conflicts of the running merge, grouped by CMS node
    /// </summary>
    public IEnumerable<CmsGitConflict> GetConflicts()
    {
        using var repo = Open();
        EnsureMerging(repo);

        return GetConflicts(repo);
    }

    /// <summary>
    /// Resolves the conflict of a node by taking the own (<see cref="CmsGitConflictChoices.Mine"/>)
    /// or the other version (<see cref="CmsGitConflictChoices.Theirs"/>).
    /// For deleted folders the whole sub tree is restored/deleted.
    /// </summary>
    /// <param name="nodePath">the node path from <see cref="GetConflicts()"/> or "*" for all conflicts</param>
    public void ResolveConflict(string nodePath, string choice)
    {
        if (choice != CmsGitConflictChoices.Mine && choice != CmsGitConflictChoices.Theirs)
        {
            throw new CmsGitException(CmsGitErrors.InvalidConflictChoice, choice);
        }

        using var repo = Open();
        EnsureMerging(repo);

        var conflicts = GetConflicts(repo);
        var selected = nodePath == "*"
            ? conflicts
            : conflicts.Where(c => c.NodePath.Equals(NormalizeNodePath(nodePath), StringComparison.OrdinalIgnoreCase)).ToList();

        if (selected.Count == 0)
        {
            throw new CmsGitException(CmsGitErrors.ConflictNotFound, nodePath);
        }

        var source = choice == CmsGitConflictChoices.Mine ? repo.Head.Tip : MergeHeadCommit(repo);

        foreach (var conflict in selected)
        {
            IEnumerable<string> paths = conflict.Files.Select(f => f.Path);

            if (conflict.Kind == CmsGitConflictKinds.DeletedByMe || conflict.Kind == CmsGitConflictKinds.DeletedByThem)
            {
                // the whole node (sub tree) is taken from the chosen version
                paths = paths
                    .Concat(NodeFiles(repo.Head.Tip, conflict.NodePath))
                    .Concat(NodeFiles(MergeHeadCommit(repo), conflict.NodePath))
                    .Concat(repo.Index.Select(e => e.Path.Replace('\\', '/')).Where(p => BelongsToNode(p, conflict.NodePath)))
                    .Distinct(StringComparer.Ordinal);
            }

            foreach (var path in paths.ToArray())
            {
                var blob = source[path]?.Target as Blob;
                if (blob != null)
                {
                    WriteBlob(path, blob);
                    repo.Index.Add(path);
                }
                else
                {
                    DeleteWorkspaceFile(path);
                    repo.Index.Remove(path);
                }
            }
        }

        repo.Index.Write();
    }

    /// <summary>
    /// Creates the merge commit after all conflicts are resolved
    /// </summary>
    public string CompleteMerge(CmsGitUser user)
    {
        using var repo = Open();
        EnsureMerging(repo);

        if (repo.Index.Conflicts.Any())
        {
            throw new CmsGitException(CmsGitErrors.ConflictsRemaining);
        }

        return CommitMerge(repo, user).Sha;
    }

    /// <summary>
    /// Aborts the running merge => back to the last commit (a merge always starts with a clean working copy)
    /// </summary>
    public void AbortMerge()
    {
        using var repo = Open();
        EnsureMerging(repo);

        repo.Reset(ResetMode.Hard, repo.Head.Tip);
    }

    /// <summary>
    /// Discards the uncommitted changes of a node (and its sub tree). New nodes are deleted.
    /// An empty <paramref name="nodePath"/> discards all changes.
    /// </summary>
    /// <returns>false, if there was nothing to discard</returns>
    public bool Discard(string nodePath)
    {
        nodePath = NormalizeNodePath(nodePath);

        using var repo = Open();
        EnsureNotMerging(repo);

        var changes = GetChanges(repo).Where(c => BelongsToNode(c.Path, nodePath)).ToArray();
        if (changes.Length == 0)
        {
            return false;
        }

        var tip = repo.Head.Tip;
        var tracked = changes.Where(c => tip?[c.Path] != null).Select(c => c.Path).ToArray();
        var untracked = changes.Where(c => tip?[c.Path] == null).Select(c => c.Path).ToArray();

        if (tracked.Length > 0)
        {
            repo.CheckoutPaths(tip.Sha, tracked, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });
        }

        if (untracked.Length > 0)
        {
            foreach (var path in untracked)
            {
                DeleteWorkspaceFile(path);
                if (repo.Index[path] != null)
                {
                    repo.Index.Remove(path);
                }
            }
            repo.Index.Write();
        }

        return true;
    }

    #endregion

    #region Branches

    public IEnumerable<CmsGitBranch> GetBranches()
    {
        using var repo = Open();

        var branches = new Dictionary<string, CmsGitBranch>();

        foreach (var branch in repo.Branches.OrderBy(b => b.IsRemote))
        {
            string name;
            if (branch.IsRemote)
            {
                if (branch.RemoteName != RemoteName || branch.CanonicalName.EndsWith("/HEAD"))
                {
                    continue;
                }
                name = branch.FriendlyName.Substring(RemoteName.Length + 1);
            }
            else
            {
                name = branch.FriendlyName;
            }

            if (!branches.TryGetValue(name, out var item))
            {
                branches[name] = item = new CmsGitBranch()
                {
                    Name = name,
                    IsDefault = name == DefaultBranch,
                    LastCommitDate = branch.Tip?.Committer.When,
                    LastCommitAuthor = branch.Tip?.Author.Name
                };
            }

            if (branch.IsRemote)
            {
                item.IsRemote = true;
            }
            else
            {
                item.IsLocal = true;
                item.IsCurrent = branch.IsCurrentRepositoryHead;
            }
        }

        return branches.Values
                       .OrderByDescending(b => b.IsDefault)
                       .ThenBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
                       .ToArray();
    }

    /// <summary>
    /// Creates a new branch from the current HEAD, switches to it (uncommitted changes are kept) and pushes it.
    /// </summary>
    /// <returns>false, if the branch could not be pushed (remote offline) => only local</returns>
    public bool CreateBranch(string name)
    {
        name = ValidateBranchName(name);

        using var repo = Open();
        EnsureNotMerging(repo);

        if (repo.Head.Tip == null)
        {
            throw new CmsGitException(CmsGitErrors.NothingToCommit);
        }

        try { Fetch(repo); } catch { /* offline => local check only */ }

        if (repo.Branches[name] != null || repo.Branches[$"{RemoteName}/{name}"] != null)
        {
            throw new CmsGitException(CmsGitErrors.BranchExists, name);
        }

        var branch = repo.CreateBranch(name);
        SetUpstream(repo, branch);
        Commands.Checkout(repo, repo.Branches[name]);

        try
        {
            PushBranch(repo, repo.Head);
            return true;
        }
        catch (Exception ex) when (ex is not CmsGitException)
        {
            return false;
        }
    }

    /// <summary>
    /// Switches to a branch (local or remote). Requires a clean working copy.
    /// </summary>
    public void Checkout(string name)
    {
        name = ValidateBranchName(name);

        using var repo = Open();
        EnsureNotMerging(repo);
        EnsureClean(repo);

        var branch = repo.Branches[name];
        if (branch == null)
        {
            var remoteBranch = repo.Branches[$"{RemoteName}/{name}"];
            if (remoteBranch == null)
            {
                try { Fetch(repo); } catch { }
                remoteBranch = repo.Branches[$"{RemoteName}/{name}"];
            }
            if (remoteBranch == null)
            {
                throw new CmsGitException(CmsGitErrors.BranchNotFound, name);
            }

            branch = repo.CreateBranch(name, remoteBranch.Tip);
            SetUpstream(repo, branch);
            branch = repo.Branches[name];
        }

        Commands.Checkout(repo, branch);
    }

    public void DeleteBranch(string name, bool deleteRemote)
    {
        using var repo = Open();

        DeleteBranch(repo, name, deleteRemote);
    }

    private void DeleteBranch(Repository repo, string name, bool deleteRemote)
    {
        name = ValidateBranchName(name);

        if (name == DefaultBranch)
        {
            throw new CmsGitException(CmsGitErrors.CannotDeleteDefaultBranch, name);
        }

        if (repo.Head.FriendlyName == name)
        {
            throw new CmsGitException(CmsGitErrors.CannotDeleteCurrentBranch, name);
        }

        var localBranch = repo.Branches[name];
        var remoteBranch = repo.Branches[$"{RemoteName}/{name}"];

        if (localBranch == null && (remoteBranch == null || !deleteRemote))
        {
            throw new CmsGitException(CmsGitErrors.BranchNotFound, name);
        }

        if (deleteRemote && remoteBranch != null)
        {
            var pushError = default(string);
            var options = PushOptions(e => pushError = e);
            try
            {
                repo.Network.Push(repo.Network.Remotes[RemoteName], $":refs/heads/{name}", options);
            }
            catch (LibGit2SharpException ex)
            {
                throw new CmsGitException(CmsGitErrors.RemoteNotReachable, ex.Message, ex);
            }
            if (pushError != null)
            {
                throw new CmsGitException(CmsGitErrors.PushRejected, pushError);
            }

            // libgit2 usually removes the tracking ref on a successful delete push already
            if (repo.Refs[$"refs/remotes/{RemoteName}/{name}"] != null)
            {
                repo.Refs.Remove($"refs/remotes/{RemoteName}/{name}");
            }
        }

        if (localBranch != null)
        {
            repo.Branches.Remove(localBranch);
        }
    }

    #endregion

    #region History

    public const int MaxHistoryLimit = 5000;

    /// <summary>
    /// Node history: max. number of commits that are inspected
    /// </summary>
    public const int MaxNodeHistoryScan = 20000;

    /// <summary>
    /// Commit graph (newest first, parents always after their children) of the local and remote branches
    /// </summary>
    /// <param name="allBranches">false => only the current branch and the default branch</param>
    /// <param name="deployedSha">Commit of the last deployment (marker only)</param>
    /// <param name="node">only commits that changed this node (and its sub tree), null/empty => all commits</param>
    /// <param name="nodeOnly">only the files of the node itself (without the sub tree)</param>
    public CmsGitHistory GetHistory(bool fetch, bool allBranches, int limit, string deployedSha = null, string node = null, bool nodeOnly = false)
    {
        node = NormalizeNodePath(node);

        using var repo = Open();

        if (node.Length > 0)
        {
            node = ResolveNodeCase(repo, node);
        }

        string fetchError = null;
        if (fetch)
        {
            try
            {
                Fetch(repo);
            }
            catch (Exception ex)
            {
                fetchError = ex.Message;
            }
        }

        limit = Math.Clamp(limit, 1, MaxHistoryLimit);

        var head = repo.Head;
        var currentBranch = head.FriendlyName;

        var branches = repo.Branches
            .Where(b => b.Tip != null)
            .Where(b => !b.IsRemote || (b.RemoteName == RemoteName && !b.CanonicalName.EndsWith("/HEAD")))
            .ToList();

        string ShortName(Branch b) => b.IsRemote ? b.FriendlyName.Substring(RemoteName.Length + 1) : b.FriendlyName;

        var visibleBranches = branches
            .Where(b => allBranches || ShortName(b) == DefaultBranch || ShortName(b) == currentBranch)
            .OrderByDescending(b => b.IsCurrentRepositoryHead)
            .ThenBy(b => b.IsRemote)
            .ThenBy(b => b.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var tips = visibleBranches.Select(b => b.Tip).ToList();
        if (head.Tip != null)
        {
            tips.Add(head.Tip);
        }

        var history = new CmsGitHistory()
        {
            Branch = currentBranch,
            DefaultBranch = DefaultBranch,
            Head = head.Tip?.Sha,
            Deployed = deployedSha,
            AllBranches = allBranches,
            Stale = fetchError != null,
            FetchError = fetchError,
            Node = node.Length > 0 ? node : null,
            NodeOnly = node.Length > 0 && nodeOnly
        };

        if (tips.Count == 0)
        {
            return history;
        }

        var refs = new Dictionary<string, List<CmsGitRef>>();
        foreach (var branch in visibleBranches)
        {
            if (!refs.TryGetValue(branch.Tip.Sha, out var list))
            {
                refs[branch.Tip.Sha] = list = new List<CmsGitRef>();
            }
            list.Add(new CmsGitRef()
            {
                Name = branch.FriendlyName,
                Type = branch.IsRemote ? CmsGitRefTypes.Remote : CmsGitRefTypes.Local
            });
        }

        var unpushed = UnpushedCommits(repo, branches);

        var query = repo.Commits.QueryBy(new CommitFilter()
        {
            IncludeReachableFrom = tips.Distinct().ToList(),
            SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
        });

        var commits = node.Length > 0
            ? query
                .Take(MaxNodeHistoryScan)
                .Where(c => ScopeSignature(c, node, nodeOnly) != ScopeSignature(c.Parents.FirstOrDefault(), node, nodeOnly))
                .Take(limit + 1)
                .ToList()
            : query.Take(limit + 1).ToList();

        history.HasMore = commits.Count > limit;

        foreach (var commit in commits.Take(limit))
        {
            history.Commits.Add(new CmsGitHistoryCommit()
            {
                Sha = commit.Sha,
                Parents = commit.Parents.Select(p => p.Sha).ToArray(),
                Author = commit.Author?.Name,
                Date = commit.Author?.When,
                Message = commit.MessageShort,
                Refs = refs.TryGetValue(commit.Sha, out var commitRefs) ? commitRefs : new List<CmsGitRef>(),
                Unpushed = unpushed.Contains(commit.Sha)
            });
        }

        return history;
    }

    /// <summary>
    /// Commit details including the changed files compared to the first parent
    /// </summary>
    /// <param name="node">only the changes of this node (and its sub tree), null/empty => all changes</param>
    /// <param name="nodeOnly">only the files of the node itself (without the sub tree)</param>
    public CmsGitCommitDetails GetCommitDetails(string sha, string node = null, bool nodeOnly = false)
    {
        node = NormalizeNodePath(node);

        using var repo = Open();

        if (node.Length > 0)
        {
            node = ResolveNodeCase(repo, node);
        }

        var commit = LookupCommit(repo, sha);
        var parent = commit.Parents.FirstOrDefault();

        var changes = new List<CmsGitChange>();
        using (var treeChanges = repo.Diff.Compare<TreeChanges>(parent?.Tree, commit.Tree))
        {
            foreach (var change in treeChanges)
            {
                switch (change.Status)
                {
                    case ChangeKind.Added:
                    case ChangeKind.Copied:
                        changes.Add(new CmsGitChange() { Path = change.Path, State = CmsGitChangeStates.Added });
                        break;
                    case ChangeKind.Deleted:
                        changes.Add(new CmsGitChange() { Path = change.OldPath, State = CmsGitChangeStates.Deleted });
                        break;
                    case ChangeKind.Renamed:
                        changes.Add(new CmsGitChange() { Path = change.OldPath, State = CmsGitChangeStates.Deleted });
                        changes.Add(new CmsGitChange() { Path = change.Path, State = CmsGitChangeStates.Added });
                        break;
                    case ChangeKind.Modified:
                    case ChangeKind.TypeChanged:
                        changes.Add(new CmsGitChange() { Path = change.Path, State = CmsGitChangeStates.Modified });
                        break;
                }
            }
        }

        return new CmsGitCommitDetails()
        {
            Sha = commit.Sha,
            Parents = commit.Parents.Select(p => p.Sha).ToArray(),
            Author = commit.Author?.Name,
            AuthorEmail = commit.Author?.Email,
            Date = commit.Author?.When,
            Message = commit.Message?.TrimEnd(),
            Changes = changes
                .Select(c => { c.Path = c.Path.Replace('\\', '/'); return c; })
                .Where(c => node.Length == 0 || InScope(c.Path, node, nodeOnly))
                .OrderBy(c => c.Path, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    /// <summary>
    /// Content of a file before (first parent) and after the commit
    /// </summary>
    public CmsGitFileDiff GetCommitFileDiff(string sha, string path)
    {
        path = NormalizeFilePath(path);

        using var repo = Open();

        var commit = LookupCommit(repo, sha);
        var parent = commit.Parents.FirstOrDefault();

        return new CmsGitFileDiff()
        {
            Path = path,
            Before = BlobText(parent?[path]?.Target as Blob),
            After = BlobText(commit[path]?.Target as Blob)
        };
    }

    /// <summary>
    /// Uncommitted changes of a file: content of the last commit (HEAD) and of the working copy
    /// </summary>
    public CmsGitFileDiff GetWorkingFileDiff(string path)
    {
        path = NormalizeFilePath(path);

        using var repo = Open();

        var fullPath = FullPath(path);

        return new CmsGitFileDiff()
        {
            Path = path,
            Before = BlobText(repo.Head.Tip?[path]?.Target as Blob),
            After = File.Exists(fullPath) ? File.ReadAllText(fullPath) : null
        };
    }

    /// <summary>
    /// Source keyword for <see cref="Restore"/>: the (remote) default branch instead of a commit sha
    /// </summary>
    public const string DefaultBranchSource = "default";

    /// <summary>
    /// Restores the exact state of a node (and its sub tree) from a commit or the remote default branch
    /// into the working copy: files are written, created and deleted. The result is an uncommitted change.
    /// </summary>
    /// <param name="source">commit sha or <see cref="DefaultBranchSource"/></param>
    /// <param name="nodeOnly">only the files of the node itself (without the sub tree)</param>
    /// <param name="previewOnly">true => only count the changes, nothing is written</param>
    public CmsGitRestoreResult Restore(string source, string nodePath, bool nodeOnly, bool previewOnly)
    {
        nodePath = NormalizeNodePath(nodePath);
        if (nodePath.Length == 0)
        {
            throw new CmsGitException(CmsGitErrors.InvalidPath, nodePath);
        }

        using var repo = Open();
        EnsureNotMerging(repo);

        nodePath = ResolveNodeCase(repo, nodePath);

        var target = DefaultBranchSource.Equals(source, StringComparison.OrdinalIgnoreCase)
            ? RemoteDefaultBranch(repo).Tip
            : LookupCommit(repo, source);

        var targetFiles = new Dictionary<string, Blob>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in ScopeFiles(target, nodePath, nodeOnly))
        {
            if (target[path]?.Target is Blob blob)
            {
                targetFiles[path] = blob;
            }
        }

        var currentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in ScopeFiles(repo.Head.Tip, nodePath, nodeOnly)
                                .Concat(GetChanges(repo).Select(c => c.Path).Where(p => InScope(p, nodePath, nodeOnly))))
        {
            if (File.Exists(FullPath(path)))
            {
                currentFiles.Add(path);
            }
        }

        var result = new CmsGitRestoreResult();
        var write = new List<string>();
        var delete = new List<string>();

        foreach (var file in targetFiles)
        {
            var fullPath = FullPath(file.Key);
            if (!File.Exists(fullPath))
            {
                result.Added++;
                write.Add(file.Key);
            }
            else if (!SameContent(file.Value, fullPath))
            {
                result.Modified++;
                write.Add(file.Key);
            }
        }

        foreach (var path in currentFiles.Where(p => !targetFiles.ContainsKey(p)))
        {
            result.Deleted++;
            delete.Add(path);
        }

        if (previewOnly || result.Total == 0)
        {
            return result;
        }

        foreach (var path in write)
        {
            WriteBlob(path, targetFiles[path]);
        }

        if (delete.Count > 0)
        {
            var tip = repo.Head.Tip;
            foreach (var path in delete)
            {
                DeleteWorkspaceFile(path);
                if (tip?[path] == null && repo.Index[path] != null)
                {
                    repo.Index.Remove(path);
                }
            }
            repo.Index.Write();
        }

        return result;
    }

    /// <summary>
    /// Differences between the working copy and the remote default branch (last fetched state), grouped by node
    /// </summary>
    public CmsGitDefaultBranchDiff GetDefaultBranchDiff(bool fetch)
    {
        using var repo = Open();

        if (fetch)
        {
            FetchOrThrow(repo);
        }

        var main = RemoteDefaultBranch(repo).Tip;
        var head = repo.Head.Tip;

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var treeChanges = repo.Diff.Compare<TreeChanges>(main.Tree, head?.Tree))
        {
            foreach (var change in treeChanges)
            {
                paths.Add(change.Path.Replace('\\', '/'));
                if (!String.IsNullOrEmpty(change.OldPath))
                {
                    paths.Add(change.OldPath.Replace('\\', '/'));
                }
            }
        }
        foreach (var change in GetChanges(repo))
        {
            paths.Add(change.Path);
        }

        var changes = new List<CmsGitChange>();
        foreach (var path in paths)
        {
            var blob = main[path]?.Target as Blob;
            var fullPath = FullPath(path);
            var exists = File.Exists(fullPath);

            if (blob == null && exists)
            {
                changes.Add(new CmsGitChange() { Path = path, State = CmsGitChangeStates.Added });
            }
            else if (blob != null && !exists)
            {
                changes.Add(new CmsGitChange() { Path = path, State = CmsGitChangeStates.Deleted });
            }
            else if (blob != null && !SameContent(blob, fullPath))
            {
                changes.Add(new CmsGitChange() { Path = path, State = CmsGitChangeStates.Modified });
            }
        }

        var fetchHead = Path.Combine(WorkspacePath, ".git", "FETCH_HEAD");

        return new CmsGitDefaultBranchDiff()
        {
            DefaultBranch = DefaultBranch,
            Commit = new CmsGitCommitInfo()
            {
                Sha = main.Sha,
                Author = main.Author?.Name,
                Date = main.Author?.When,
                Message = main.MessageShort
            },
            FetchedAt = File.Exists(fetchHead) ? new DateTimeOffset(File.GetLastWriteTime(fetchHead)) : null,
            Nodes = GroupChangesByNode(changes).ToArray()
        };
    }

    /// <summary>
    /// Content of a file in the remote default branch (last fetched state) and in the working copy
    /// </summary>
    public CmsGitFileDiff GetDefaultBranchFileDiff(string path)
    {
        path = NormalizeFilePath(path);

        using var repo = Open();

        var main = RemoteDefaultBranch(repo).Tip;
        var fullPath = FullPath(path);

        return new CmsGitFileDiff()
        {
            Path = path,
            Before = BlobText(main[path]?.Target as Blob),
            After = File.Exists(fullPath) ? File.ReadAllText(fullPath) : null
        };
    }

    private static bool SameContent(Blob blob, string fullPath)
    {
        var info = new FileInfo(fullPath);
        if (!info.Exists || info.Length != blob.Size)
        {
            return false;
        }

        using var content = blob.GetContentStream();
        using var memory = new MemoryStream();
        content.CopyTo(memory);

        return memory.ToArray().AsSpan().SequenceEqual(File.ReadAllBytes(fullPath));
    }

    private static string NormalizeFilePath(string path)
    {
        if (String.IsNullOrWhiteSpace(path) || path.Split('/', '\\').Any(p => p == ".." || p == "."))
        {
            throw new CmsGitException(CmsGitErrors.InvalidPath, path);
        }

        return path.Replace('\\', '/').Trim('/');
    }

    private static string BlobText(Blob blob)
        => blob == null ? null : blob.IsBinary ? "(binary)" : blob.GetContentText();

    private static Commit LookupCommit(Repository repo, string sha)
    {
        if (String.IsNullOrWhiteSpace(sha) || sha.Length < 4 || sha.Length > 40 || !sha.All(Uri.IsHexDigit))
        {
            throw new CmsGitException(CmsGitErrors.CommitNotFound, sha);
        }

        return repo.Lookup<Commit>(sha) ?? throw new CmsGitException(CmsGitErrors.CommitNotFound, sha);
    }

    /// <summary>
    /// Commits reachable from local branches, but not from any remote branch
    /// </summary>
    private static HashSet<string> UnpushedCommits(Repository repo, IEnumerable<Branch> branches)
    {
        var localTips = branches.Where(b => !b.IsRemote).Select(b => b.Tip).ToList();
        var remoteTips = branches.Where(b => b.IsRemote).Select(b => b.Tip).ToList();

        if (localTips.Count == 0)
        {
            return new HashSet<string>();
        }

        var filter = new CommitFilter() { IncludeReachableFrom = localTips };
        if (remoteTips.Count > 0)
        {
            filter.ExcludeReachableFrom = remoteTips;
        }

        return new HashSet<string>(repo.Commits.QueryBy(filter).Take(MaxHistoryLimit * 2).Select(c => c.Sha));
    }

    #endregion

    #region Deploy Clone

    /// <summary>
    /// Read-only clone for deployments: clone or fetch + hard reset to the remote default branch.
    /// </summary>
    /// <returns>The commit hash of the deployed tree</returns>
    public string UpdateToRemoteDefaultBranch()
    {
        if (!Exists)
        {
            Clone(ListRemoteReferences());
        }

        using var repo = Open();

        try
        {
            Fetch(repo);
        }
        catch (LibGit2SharpException ex)
        {
            throw new CmsGitException(CmsGitErrors.RemoteNotReachable, ex.Message, ex);
        }

        var remoteBranch = repo.Branches[$"{RemoteName}/{DefaultBranch}"];
        if (remoteBranch?.Tip == null)
        {
            throw new CmsGitException(CmsGitErrors.BranchNotFound, DefaultBranch);
        }

        var localBranch = repo.Branches[DefaultBranch] ?? repo.CreateBranch(DefaultBranch, remoteBranch.Tip);
        if (!localBranch.IsCurrentRepositoryHead)
        {
            Commands.Checkout(repo, localBranch, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });
        }

        repo.Reset(ResetMode.Hard, remoteBranch.Tip);
        repo.RemoveUntrackedFiles();

        return remoteBranch.Tip.Sha;
    }

    /// <summary>
    /// Current HEAD commit of the working copy (null, if the working copy does not exist)
    /// </summary>
    public CmsGitCommitInfo HeadCommit()
    {
        if (!Exists)
        {
            return null;
        }

        using var repo = Open();

        return CommitInfo(repo.Head?.Tip);
    }

    /// <summary>
    /// Latest known commit of the remote default branch (no fetch)
    /// </summary>
    public CmsGitCommitInfo RemoteDefaultBranchCommit()
    {
        if (!Exists)
        {
            return null;
        }

        using var repo = Open();

        return CommitInfo(repo.Branches[$"{RemoteName}/{DefaultBranch}"]?.Tip);
    }

    /// <summary>
    /// Commit hash of the remote default branch without a local working copy (ls-remote)
    /// </summary>
    public string RemoteDefaultBranchSha()
        => ListRemoteReferences()
            .FirstOrDefault(r => r.CanonicalName == $"refs/heads/{DefaultBranch}")?
            .TargetIdentifier;

    private static CmsGitCommitInfo CommitInfo(Commit commit)
        => commit == null
            ? null
            : new CmsGitCommitInfo()
            {
                Sha = commit.Sha,
                Author = commit.Author?.Name,
                Date = commit.Author?.When,
                Message = commit.MessageShort
            };

    #endregion

    #region Helper

    private Repository Open() => new Repository(WorkspacePath);

    private static void ConfigureRepository(Repository repo)
    {
        repo.Config.Set("core.autocrlf", false);
        repo.Config.Set("core.longpaths", true);
    }

    private List<Reference> ListRemoteReferences()
    {
        try
        {
            return Repository.ListRemoteReferences(_settings.RemoteUrl, CredentialsProvider()).ToList();
        }
        catch (LibGit2SharpException ex)
        {
            throw new CmsGitException(CmsGitErrors.RemoteNotReachable, ex.Message, ex);
        }
    }

    private void Fetch(Repository repo)
    {
        var remote = repo.Network.Remotes[RemoteName];
        var options = new FetchOptions() { Prune = true };
        ApplyFetchOptions(options);

        Commands.Fetch(repo, remote.Name, remote.FetchRefSpecs.Select(r => r.Specification), options, null);
    }

    private void FetchOrThrow(Repository repo)
    {
        try
        {
            Fetch(repo);
        }
        catch (LibGit2SharpException ex)
        {
            throw new CmsGitException(CmsGitErrors.RemoteNotReachable, ex.Message, ex);
        }
    }

    private Branch RemoteDefaultBranch(Repository repo)
        => repo.Branches[$"{RemoteName}/{DefaultBranch}"] is Branch branch && branch.Tip != null
            ? branch
            : throw new CmsGitException(CmsGitErrors.BranchNotFound, DefaultBranch);

    /// <summary>
    /// Pushes the current branch. On non-fast-forward the remote branch is merged first.
    /// </summary>
    /// <returns>true, if files changed (merge)</returns>
    private bool PushCurrentBranch(Repository repo, CmsGitUser user)
    {
        SetUpstream(repo, repo.Head);

        try
        {
            PushBranch(repo, repo.Head);
            return false;
        }
        catch (NonFastForwardException)
        {
            if (IsDirty(repo))
            {
                throw new CmsGitException(CmsGitErrors.PushRejected);
            }
        }

        FetchOrThrow(repo);
        var changed = MergeUpstream(repo, user);

        try
        {
            PushBranch(repo, repo.Head);
        }
        catch (NonFastForwardException ex)
        {
            throw new CmsGitException(CmsGitErrors.PushRejected, ex.Message, ex);
        }

        return changed;
    }

    /// <returns>true, if files changed</returns>
    private bool MergeUpstream(Repository repo, CmsGitUser user)
    {
        var head = repo.Head;

        return MergeBranch(repo, head.IsTracking ? head.TrackedBranch : null, user, FastForwardStrategy.Default);
    }

    /// <summary>
    /// Merges a branch into the current branch.
    /// Conflicting files keep the own version in the working copy (=> valid XML for the CMS),
    /// .itemorder.xml conflicts are resolved automatically.
    /// If conflicts remain, the merge state is kept and <see cref="CmsGitErrors.MergeConflicts"/> is thrown.
    /// </summary>
    /// <returns>true, if files changed</returns>
    private bool MergeBranch(Repository repo, Branch branch, CmsGitUser user, FastForwardStrategy fastForwardStrategy)
    {
        var tip = branch?.Tip;
        var headTip = repo.Head.Tip;

        if (tip == null || tip.Sha == headTip?.Sha)
        {
            return false;
        }

        if (headTip != null && repo.ObjectDatabase.FindMergeBase(headTip, tip)?.Sha == tip.Sha)
        {
            // already contained
            return false;
        }

        var result = repo.Merge(branch, AuthorSignature(user), new MergeOptions()
        {
            FastForwardStrategy = fastForwardStrategy,
            CommitOnSuccess = false,
            FileConflictStrategy = CheckoutFileConflictStrategy.Ours
        });

        switch (result.Status)
        {
            case MergeStatus.UpToDate:
                return false;
            case MergeStatus.FastForward:
                return true;
            case MergeStatus.Conflicts:
                AutoResolveItemOrder(repo);
                if (repo.Index.Conflicts.Any())
                {
                    throw new CmsGitException(CmsGitErrors.MergeConflicts);
                }
                break;
        }

        CommitMerge(repo, user);
        return true;
    }

    private Commit CommitMerge(Repository repo, CmsGitUser user)
    {
        Commands.Stage(repo, "*");

        return repo.Commit(
            ReadMergeMessage(repo) ?? $"Merge {MergeSourceName(repo)}",
            AuthorSignature(user),
            CommitterSignature(user),
            new CommitOptions() { AllowEmptyCommit = true });
    }

    private static string ReadMergeMessage(Repository repo)
    {
        var file = Path.Combine(repo.Info.Path, "MERGE_MSG");
        if (!File.Exists(file))
        {
            return null;
        }

        var lines = File.ReadAllLines(file)
                        .Where(l => !l.StartsWith("#"))
                        .ToArray();
        var message = String.Join("\n", lines).Trim()
                            .Replace("'refs/remotes/", "'")
                            .Replace("'refs/heads/", "'");

        return String.IsNullOrEmpty(message) ? null : message;
    }

    private static string MergeSourceName(Repository repo)
    {
        var message = ReadMergeMessage(repo);
        if (message != null)
        {
            var match = System.Text.RegularExpressions.Regex.Match(message, "'([^']+)'");
            if (match.Success)
            {
                var name = match.Groups[1].Value;
                return name.StartsWith($"{RemoteName}/") ? name.Substring(RemoteName.Length + 1) : name;
            }
        }

        return MergeHeadTip(repo)?.Sha?.Substring(0, 7);
    }

    private static Commit MergeHeadTip(Repository repo)
    {
        var file = Path.Combine(repo.Info.Path, "MERGE_HEAD");
        var sha = File.Exists(file) ? File.ReadLines(file).FirstOrDefault()?.Trim() : null;

        return String.IsNullOrEmpty(sha) ? null : repo.Lookup<Commit>(sha);
    }

    private static Commit MergeHeadCommit(Repository repo)
        => MergeHeadTip(repo) ?? throw new CmsGitException(CmsGitErrors.NotMerging);

    /// <summary>
    /// .itemorder.xml: union of both lists (own order first, then the new items of the other version).
    /// Items without node are ignored by the CMS (<c>ItemOrder</c>).
    /// </summary>
    private void AutoResolveItemOrder(Repository repo)
    {
        var resolved = false;

        foreach (var conflict in repo.Index.Conflicts.ToArray())
        {
            if (conflict.Ours == null || conflict.Theirs == null)
            {
                continue;
            }

            var path = conflict.Ours.Path.Replace('\\', '/');
            if (!FileName(path).Equals(".itemorder.xml", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var merged = UnionItemOrder(BlobText(repo, conflict.Ours), BlobText(repo, conflict.Theirs));
            if (merged == null)
            {
                continue;
            }

            var fullPath = FullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            merged.Save(fullPath);

            repo.Index.Add(path);
            resolved = true;
        }

        if (resolved)
        {
            repo.Index.Write();
        }
    }

    internal static System.Xml.XmlDocument UnionItemOrder(string mine, string theirs)
    {
        try
        {
            var names = new List<string>();
            foreach (var xml in new[] { mine, theirs })
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(xml);
                foreach (System.Xml.XmlNode item in doc.SelectNodes("items/item[@name]"))
                {
                    var name = item.Attributes["name"].Value;
                    if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        names.Add(name);
                    }
                }
            }

            var result = new System.Xml.XmlDocument();
            var itemsNode = result.AppendChild(result.CreateElement("items"));
            foreach (var name in names)
            {
                var itemNode = result.CreateElement("item");
                itemNode.SetAttribute("name", name);
                itemsNode.AppendChild(itemNode);
            }

            return result;
        }
        catch
        {
            return null;
        }
    }

    private static List<CmsGitConflict> GetConflicts(Repository repo)
    {
        var mine = repo.Head.Tip;
        var theirs = MergeHeadTip(repo);
        var conflicts = new Dictionary<string, CmsGitConflict>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in repo.Index.Conflicts)
        {
            var path = (entry.Ours ?? entry.Theirs ?? entry.Ancestor).Path.Replace('\\', '/');

            var kind = entry.Ours == null ? CmsGitConflictKinds.DeletedByMe :
                       entry.Theirs == null ? CmsGitConflictKinds.DeletedByThem :
                       entry.Ancestor == null ? CmsGitConflictKinds.Added :
                       CmsGitConflictKinds.Modified;

            var node = NodeOf(path);
            if (kind == CmsGitConflictKinds.DeletedByMe)
            {
                node = TopMostDeletedNode(mine, node);
            }
            else if (kind == CmsGitConflictKinds.DeletedByThem)
            {
                node = TopMostDeletedNode(theirs, node);
            }

            if (!conflicts.TryGetValue(node, out var conflict))
            {
                conflicts[node] = conflict = new CmsGitConflict() { NodePath = node, Kind = kind };
            }
            else if (kind == CmsGitConflictKinds.DeletedByMe || kind == CmsGitConflictKinds.DeletedByThem)
            {
                conflict.Kind = kind;
            }

            conflict.Files.Add(new CmsGitConflictFile()
            {
                Path = path,
                Mine = BlobText(repo, entry.Ours),
                Theirs = BlobText(repo, entry.Theirs)
            });
        }

        return conflicts.Values.OrderBy(c => c.NodePath, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string BlobText(Repository repo, IndexEntry entry)
        => entry == null ? null : repo.Lookup<Blob>(entry.Id)?.GetContentText();

    /// <summary>
    /// If a folder was deleted, the conflict concerns the whole (top most) deleted folder node
    /// </summary>
    private static string TopMostDeletedNode(Commit deletingSide, string node)
    {
        if (deletingSide == null)
        {
            return node;
        }

        var current = node;
        while (true)
        {
            var parent = ParentPath(current);
            if (parent.Length == 0 || deletingSide[parent] != null)
            {
                return current;
            }
            current = parent;
        }
    }

    /// <summary>
    /// CMS node of a file: "a/b.xml" => "a/b", "a/b.acl" => "a/b", "a/b/.general.xml" => "a/b"
    /// </summary>
    internal static string NodeOf(string path)
    {
        var dir = ParentPath(path);
        var name = FileName(path);

        if (name.StartsWith("."))
        {
            return dir;
        }

        return CombinePath(dir, Path.GetFileNameWithoutExtension(name));
    }

    /// <summary>
    /// File belongs to the node: the node file(s) "a/b.*", or anything in the sub tree "a/b/..."
    /// </summary>
    internal static bool BelongsToNode(string path, string node)
    {
        if (node.Length == 0)
        {
            return true;
        }

        if (path.Equals(node, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith($"{node}/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return ParentPath(path).Equals(ParentPath(node), StringComparison.OrdinalIgnoreCase)
            && Path.GetFileNameWithoutExtension(FileName(path)).Equals(FileName(node), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// File is in the scope of a node: <paramref name="nodeOnly"/> => only the node's own files, otherwise the sub tree too
    /// </summary>
    internal static bool InScope(string path, string node, bool nodeOnly)
        => nodeOnly
            ? NodeOf(path).Equals(node, StringComparison.OrdinalIgnoreCase)
            : BelongsToNode(path, node);

    private static IEnumerable<string> ScopeFiles(Commit commit, string node, bool nodeOnly)
        => NodeFiles(commit, node).Where(p => InScope(p, node, nodeOnly));

    /// <summary>
    /// Cheap fingerprint of the node's files in a commit (tree/blob ids) => changed, if the fingerprint differs from the parent
    /// </summary>
    private static string ScopeSignature(Commit commit, string node, bool nodeOnly)
    {
        if (commit == null)
        {
            return String.Empty;
        }

        var sb = new StringBuilder();

        if (commit[node]?.Target is Tree nodeTree)
        {
            if (nodeOnly)
            {
                foreach (var entry in nodeTree.Where(e => e.TargetType == TreeEntryTargetType.Blob && e.Name.StartsWith(".")))
                {
                    sb.Append(entry.Name).Append(':').Append(entry.Target.Sha).Append(';');
                }
            }
            else
            {
                sb.Append("/:").Append(nodeTree.Sha).Append(';');
            }
        }

        var parent = ParentPath(node);
        var parentTree = parent.Length == 0 ? commit.Tree : commit[parent]?.Target as Tree;
        if (parentTree != null)
        {
            foreach (var entry in parentTree.Where(e => e.TargetType == TreeEntryTargetType.Blob))
            {
                if (BelongsToNode(CombinePath(parent, entry.Name), node))
                {
                    sb.Append(entry.Name).Append(':').Append(entry.Target.Sha).Append(';');
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Git paths are case sensitive, the CMS (windows) is not => use the spelling of the current commit
    /// </summary>
    private static string ResolveNodeCase(Repository repo, string node)
    {
        var tree = repo.Head.Tip?.Tree;
        if (tree == null)
        {
            return node;
        }

        var segments = node.Split('/');
        for (int i = 0; i < segments.Length && tree != null; i++)
        {
            var segment = segments[i];
            var entry = tree.FirstOrDefault(e => e.TargetType == TreeEntryTargetType.Tree && e.Name.Equals(segment, StringComparison.OrdinalIgnoreCase))
                     ?? (i == segments.Length - 1
                            ? tree.FirstOrDefault(e => e.TargetType == TreeEntryTargetType.Blob
                                                    && Path.GetFileNameWithoutExtension(e.Name).Equals(segment, StringComparison.OrdinalIgnoreCase))
                            : null);

            if (entry == null)
            {
                break;
            }

            segments[i] = entry.TargetType == TreeEntryTargetType.Tree ? entry.Name : Path.GetFileNameWithoutExtension(entry.Name);
            tree = entry.Target as Tree;
        }

        return String.Join("/", segments);
    }

    private static IEnumerable<string> NodeFiles(Commit commit, string node)
    {
        if (commit == null)
        {
            yield break;
        }

        if (commit[node]?.Target is Tree tree)
        {
            foreach (var file in TreeFiles(tree, node))
            {
                yield return file;
            }
        }

        var parent = ParentPath(node);
        var parentTree = parent.Length == 0 ? commit.Tree : commit[parent]?.Target as Tree;
        if (parentTree != null)
        {
            foreach (var entry in parentTree.Where(e => e.TargetType == TreeEntryTargetType.Blob))
            {
                var path = CombinePath(parent, entry.Name);
                if (BelongsToNode(path, node))
                {
                    yield return path;
                }
            }
        }
    }

    private static IEnumerable<string> TreeFiles(Tree tree, string prefix)
    {
        foreach (var entry in tree)
        {
            var path = CombinePath(prefix, entry.Name);
            if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                foreach (var sub in TreeFiles((Tree)entry.Target, path))
                {
                    yield return sub;
                }
            }
            else if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                yield return path;
            }
        }
    }

    private static string ParentPath(string path)
    {
        var index = path.LastIndexOf('/');
        return index < 0 ? String.Empty : path.Substring(0, index);
    }

    private static string FileName(string path)
        => path.Substring(path.LastIndexOf('/') + 1);

    private static string CombinePath(string dir, string name)
        => dir.Length == 0 ? name : $"{dir}/{name}";

    private static string NormalizeNodePath(string nodePath)
    {
        nodePath = (nodePath ?? String.Empty).Replace('\\', '/').Trim().Trim('/');

        if (nodePath.Split('/').Any(p => p == ".." || p == "." || p.Equals(".git", StringComparison.OrdinalIgnoreCase)))
        {
            throw new CmsGitException(CmsGitErrors.InvalidPath, nodePath);
        }

        return nodePath;
    }

    private string FullPath(string relativePath)
    {
        var root = Path.GetFullPath(WorkspacePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));

        if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || IsGitPath(Path.GetRelativePath(root, fullPath)))
        {
            throw new CmsGitException(CmsGitErrors.InvalidPath, relativePath);
        }

        return fullPath;
    }

    private void WriteBlob(string relativePath, Blob blob)
    {
        var fullPath = FullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

        using var content = blob.GetContentStream();
        using var file = File.Create(fullPath);
        content.CopyTo(file);
    }

    private void DeleteWorkspaceFile(string relativePath)
    {
        var fullPath = FullPath(relativePath);

        if (File.Exists(fullPath))
        {
            File.SetAttributes(fullPath, FileAttributes.Normal);
            File.Delete(fullPath);
        }

        // remove empty folders (deleted nodes)
        var root = Path.GetFullPath(WorkspacePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var dir = Path.GetDirectoryName(fullPath);
        while (dir != null
               && dir.Length > root.Length
               && Directory.Exists(dir)
               && !Directory.EnumerateFileSystemEntries(dir).Any())
        {
            Directory.Delete(dir);
            dir = Path.GetDirectoryName(dir);
        }
    }

    private void PushBranch(Repository repo, Branch branch)
    {
        string pushError = null;

        try
        {
            repo.Network.Push(branch, PushOptions(e => pushError = e));
        }
        catch (NonFastForwardException)
        {
            throw;
        }
        catch (LibGit2SharpException ex)
        {
            throw new CmsGitException(CmsGitErrors.RemoteNotReachable, ex.Message, ex);
        }

        if (pushError != null)
        {
            if (pushError.Contains("fast-forward", StringComparison.OrdinalIgnoreCase)
                || pushError.Contains("fetch first", StringComparison.OrdinalIgnoreCase))
            {
                throw new NonFastForwardException(pushError);
            }
            throw new CmsGitException(CmsGitErrors.PushRejected, pushError);
        }
    }

    private static void SetUpstream(Repository repo, Branch branch)
    {
        if (!branch.IsTracking || branch.RemoteName != RemoteName)
        {
            repo.Branches.Update(branch,
                b => b.Remote = RemoteName,
                b => b.UpstreamBranch = branch.CanonicalName);
        }
    }

    private static IEnumerable<CmsGitChange> GetChanges(Repository repo)
    {
        var status = repo.RetrieveStatus(new StatusOptions()
        {
            IncludeUntracked = true,
            RecurseUntrackedDirs = true,
            IncludeIgnored = false
        });

        foreach (var entry in status)
        {
            var state = entry.State;

            string changeState =
                state.HasFlag(FileStatus.Conflicted) ? CmsGitChangeStates.Conflicted :
                state.HasFlag(FileStatus.NewInWorkdir) || state.HasFlag(FileStatus.NewInIndex) ? CmsGitChangeStates.Added :
                state.HasFlag(FileStatus.DeletedFromWorkdir) || state.HasFlag(FileStatus.DeletedFromIndex) ? CmsGitChangeStates.Deleted :
                state == FileStatus.Unaltered || state.HasFlag(FileStatus.Ignored) ? null :
                CmsGitChangeStates.Modified;

            if (changeState != null)
            {
                yield return new CmsGitChange() { Path = entry.FilePath.Replace('\\', '/'), State = changeState };
            }
        }
    }

    private static bool IsDirty(Repository repo) => GetChanges(repo).Any();

    /// <summary>
    /// Uncommitted changes grouped by CMS node
    /// </summary>
    public IEnumerable<CmsGitChangedNode> GetChangedNodes()
    {
        using var repo = Open();

        return GroupChangesByNode(GetChanges(repo)).ToArray();
    }

    /// <summary>
    /// Groups changed files by their CMS node (see <see cref="NodeOf"/>)
    /// </summary>
    public static IEnumerable<CmsGitChangedNode> GroupChangesByNode(IEnumerable<CmsGitChange> changes)
        => (changes ?? Enumerable.Empty<CmsGitChange>())
            .GroupBy(c => NodeOf(c.Path), StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var files = g.OrderBy(c => c.Path, StringComparer.OrdinalIgnoreCase).ToArray();

                return new CmsGitChangedNode()
                {
                    Node = g.Key,
                    Name = FileName(g.Key),
                    State = files.Any(f => f.State == CmsGitChangeStates.Conflicted) ? CmsGitChangeStates.Conflicted
                          : files.All(f => f.State == CmsGitChangeStates.Added) ? CmsGitChangeStates.Added
                          : files.All(f => f.State == CmsGitChangeStates.Deleted) ? CmsGitChangeStates.Deleted
                          : CmsGitChangeStates.Modified,
                    OrderChanged = files.Any(f => FileName(f.Path).Equals(".itemorder.xml", StringComparison.OrdinalIgnoreCase)),
                    Files = files
                };
            })
            .OrderBy(n => n.Node, StringComparer.OrdinalIgnoreCase);

    private static void EnsureClean(Repository repo)
    {
        if (IsDirty(repo))
        {
            throw new CmsGitException(CmsGitErrors.CommitFirst);
        }
    }

    private static void EnsureNotMerging(Repository repo)
    {
        if (repo.Info.CurrentOperation != CurrentOperation.None)
        {
            throw new CmsGitException(CmsGitErrors.MergeInProgress);
        }
    }

    private static void EnsureMerging(Repository repo)
    {
        if (repo.Info.CurrentOperation == CurrentOperation.None)
        {
            throw new CmsGitException(CmsGitErrors.NotMerging);
        }
    }

    private void EnsureNotOnDefaultBranch(Repository repo)
    {
        if (repo.Head.FriendlyName == DefaultBranch)
        {
            throw new CmsGitException(CmsGitErrors.AlreadyOnDefaultBranch, DefaultBranch);
        }
    }

    /// <summary>
    /// Cheap check without opening the repository (used by the edit lock of the CMS)
    /// </summary>
    public bool IsMerging => File.Exists(Path.Combine(WorkspacePath, ".git", "MERGE_HEAD"));

    private static string ValidateBranchName(string name)
    {
        name = name?.Trim();

        if (String.IsNullOrEmpty(name)
            || name.Equals("HEAD", StringComparison.OrdinalIgnoreCase)
            || !Reference.IsValidName($"refs/heads/{name}"))
        {
            throw new CmsGitException(CmsGitErrors.InvalidBranchName, name);
        }

        return name;
    }

    private Signature AuthorSignature(CmsGitUser user)
        => new Signature(user.Name, user.Email, DateTimeOffset.Now);

    private Signature CommitterSignature(CmsGitUser user)
        => new Signature(
                String.IsNullOrWhiteSpace(_settings.CommitterName) ? user.Name : _settings.CommitterName,
                String.IsNullOrWhiteSpace(_settings.CommitterEmail) ? user.Email : _settings.CommitterEmail,
                DateTimeOffset.Now);

    private CredentialsHandler CredentialsProvider()
    {
        if (String.IsNullOrEmpty(_settings.Token))
        {
            return null;
        }

        return (url, usernameFromUrl, types) => new UsernamePasswordCredentials()
        {
            Username = String.IsNullOrWhiteSpace(_settings.Username) ? "git" : _settings.Username,
            Password = _settings.Token
        };
    }

    private void ApplyFetchOptions(FetchOptions options)
    {
        options.CredentialsProvider = CredentialsProvider();
    }

    private PushOptions PushOptions(Action<string> onError)
        => new PushOptions()
        {
            CredentialsProvider = CredentialsProvider(),
            OnPushStatusError = e => onError($"{e.Reference}: {e.Message}")
        };

    private static void WriteRepositoryFiles(string path)
    {
        var gitIgnore = Path.Combine(path, ".gitignore");
        if (!File.Exists(gitIgnore))
        {
            File.WriteAllLines(gitIgnore, GitIgnoreLines);
        }

        var gitAttributes = Path.Combine(path, ".gitattributes");
        if (!File.Exists(gitAttributes))
        {
            File.WriteAllLines(gitAttributes, GitAttributesLines);
        }
    }

    private static void PrepareEmptyDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            // leftover of a failed clone (a valid workspace has a .git folder and never gets here)
            DeleteDirectory(path);
        }

        Directory.CreateDirectory(path);
    }

    private static void CopyDirectory(string source, string target)
    {
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, dir);
            if (IsGitPath(relative))
            {
                continue;
            }
            Directory.CreateDirectory(Path.Combine(target, relative));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            if (IsGitPath(relative))
            {
                continue;
            }
            File.Copy(file, Path.Combine(target, relative), true);
        }
    }

    private static bool IsGitPath(string relativePath)
        => relativePath.Split('\\', '/').First().Equals(".git", StringComparison.OrdinalIgnoreCase);

    internal static void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(path, true);
    }

    #endregion
}
