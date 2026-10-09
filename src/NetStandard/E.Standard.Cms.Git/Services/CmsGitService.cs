using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Git.Exceptions;
using E.Standard.Cms.Git.Models;

namespace E.Standard.Cms.Git.Services;

/// <summary>
/// Git operations for cms-items with git configuration.
/// Serializes all operations per working copy and invalidates cached CMS trees after file changes.
/// </summary>
public class CmsGitService
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ShortLockTimeout = TimeSpan.FromSeconds(2);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.OrdinalIgnoreCase);

    private readonly CmsConfigurationService _ccs;
    private readonly CmsManagerResolver _resolver;

    public CmsGitService(CmsConfigurationService ccs, CmsManagerResolver resolver)
    {
        _ccs = ccs;
        _resolver = resolver;
    }

    public bool IsEnabled(string cmsId) => _ccs.IsGitEnabled(cmsId);

    #region User Workspace

    public CmsGitStatus GetStatus(string cmsId, string username, bool fetch)
    {
        var workspace = UserWorkspace(cmsId, username);

        if (!workspace.Exists)
        {
            return new CmsGitStatus()
            {
                HasWorkspace = false,
                DefaultBranch = workspace.DefaultBranch,
                SuggestedBranchPrefix = SuggestedBranchPrefix(username)
            };
        }

        return Locked(workspace.WorkspacePath, () => Decorate(workspace.GetStatus(fetch), username));
    }

    public CmsGitStatus CreateWorkspace(string cmsId, string username, CmsGitUser user, string initialCommitMessage)
    {
        var workspace = UserWorkspace(cmsId, username);

        // serialize per cms-item too: only one user may initialize an empty remote
        Locked($"init:{cmsId}", () =>
            Locked(workspace.WorkspacePath, () =>
            {
                _resolver.DeleteExportCache(cmsId, username);
                workspace.Create(CmsItem(cmsId).Path, user, initialCommitMessage);
                return true;
            }));

        _resolver.Invalidate(workspace.WorkspacePath);

        return GetStatus(cmsId, username, false);
    }

    public CmsGitStatus Fetch(string cmsId, string username)
        => GetStatus(cmsId, username, true);

    public CmsGitStatus Pull(string cmsId, string username, CmsGitUser user)
        => Run(cmsId, username, ws => ws.Pull(user), invalidate: true);

    public CmsGitStatus Commit(string cmsId, string username, CmsGitUser user, string message)
        => Run(cmsId, username, ws => ws.Commit(user, message), invalidate: false);

    public CmsGitStatus Push(string cmsId, string username, CmsGitUser user)
        => Run(cmsId, username, ws => ws.Push(user), invalidate: true);

    public IEnumerable<CmsGitBranch> GetBranches(string cmsId, string username)
    {
        var workspace = ExistingUserWorkspace(cmsId, username);

        return Locked(workspace.WorkspacePath, () => workspace.GetBranches());
    }

    public CmsGitStatus CreateBranch(string cmsId, string username, string name)
    {
        var pushed = true;
        var status = Run(cmsId, username, ws => pushed = ws.CreateBranch(name), invalidate: false);

        if (!pushed)
        {
            status.Stale = true;
        }

        return status;
    }

    public CmsGitStatus Checkout(string cmsId, string username, string name)
        => Run(cmsId, username, ws => { ws.Checkout(name); return true; }, invalidate: true);

    public CmsGitStatus DeleteBranch(string cmsId, string username, string name, bool deleteRemote)
        => Run(cmsId, username, ws => { ws.DeleteBranch(name, deleteRemote); return true; }, invalidate: false);

    public CmsGitStatus MergeFromDefault(string cmsId, string username, CmsGitUser user)
        => Run(cmsId, username, ws => ws.MergeFromDefault(user), invalidate: true);

    public CmsGitStatus MergeIntoDefault(string cmsId, string username, CmsGitUser user, bool deleteBranch)
        => Run(cmsId, username, ws => { ws.MergeIntoDefault(user, deleteBranch); return true; }, invalidate: true);

    public IEnumerable<CmsGitConflict> GetConflicts(string cmsId, string username)
    {
        var workspace = ExistingUserWorkspace(cmsId, username);

        return Locked(workspace.WorkspacePath, () => workspace.GetConflicts());
    }

    public CmsGitStatus ResolveConflict(string cmsId, string username, string nodePath, string choice)
        => Run(cmsId, username, ws => { ws.ResolveConflict(nodePath, choice); return true; }, invalidate: true);

    public CmsGitStatus CompleteMerge(string cmsId, string username, CmsGitUser user)
        => Run(cmsId, username, ws => ws.CompleteMerge(user), invalidate: true);

    public CmsGitStatus AbortMerge(string cmsId, string username)
        => Run(cmsId, username, ws => { ws.AbortMerge(); return true; }, invalidate: true);

    public CmsGitStatus Discard(string cmsId, string username, string nodePath)
        => Run(cmsId, username, ws => ws.Discard(nodePath), invalidate: true);

    public IEnumerable<CmsGitChangedNode> GetChangedNodes(string cmsId, string username)
    {
        var workspace = ExistingUserWorkspace(cmsId, username);

        return Locked(workspace.WorkspacePath, () => workspace.GetChangedNodes());
    }

    public CmsGitFileDiff GetWorkingFileDiff(string cmsId, string username, string path)
    {
        var workspace = ExistingUserWorkspace(cmsId, username);

        return Locked(workspace.WorkspacePath, () => workspace.GetWorkingFileDiff(path));
    }

    /// <summary>
    /// Restores a node from a commit or the default branch (see <see cref="CmsGitWorkspace.Restore"/>)
    /// </summary>
    public CmsGitRestoreResult Restore(string cmsId, string username, string source, string nodePath, bool nodeOnly, bool previewOnly)
    {
        var workspace = ExistingUserWorkspace(cmsId, username);

        var result = Locked(workspace.WorkspacePath, () => workspace.Restore(source, nodePath, nodeOnly, previewOnly));

        if (!previewOnly && result.Total > 0)
        {
            _resolver.Invalidate(workspace.WorkspacePath);
        }

        return result;
    }

    public CmsGitDefaultBranchDiff GetDefaultBranchDiff(string cmsId, string username, bool fetch)
    {
        var workspace = ExistingUserWorkspace(cmsId, username);

        return Locked(workspace.WorkspacePath, () => workspace.GetDefaultBranchDiff(fetch));
    }

    public CmsGitFileDiff GetDefaultBranchFileDiff(string cmsId, string username, string path)
    {
        var workspace = ExistingUserWorkspace(cmsId, username);

        return Locked(workspace.WorkspacePath, () => workspace.GetDefaultBranchFileDiff(path));
    }

    /// <summary>
    /// true, if the user's working copy has a running merge => editing the CMS tree is not allowed
    /// </summary>
    public bool IsMerging(string cmsId, string username)
        => IsEnabled(cmsId) && UserWorkspace(cmsId, username).IsMerging;

    #endregion

    #region History

    public CmsGitHistory GetHistory(string cmsId, string username, bool fetch, bool allBranches, int limit, string node = null, bool nodeOnly = false)
    {
        var workspace = ExistingUserWorkspace(cmsId, username);

        string deployedSha = null;
        try
        {
            var deployWorkspace = new CmsGitWorkspace(_resolver.DeployWorkspacePath(cmsId), Settings(cmsId));
            deployedSha = Locked(deployWorkspace.WorkspacePath, () => deployWorkspace.HeadCommit()?.Sha, ShortLockTimeout);
        }
        catch { /* busy => unknown */ }

        return Locked(workspace.WorkspacePath, () => workspace.GetHistory(fetch, allBranches, limit, deployedSha, node, nodeOnly));
    }

    public CmsGitCommitDetails GetCommitDetails(string cmsId, string username, string sha, string node = null, bool nodeOnly = false)
    {
        var workspace = ExistingUserWorkspace(cmsId, username);

        return Locked(workspace.WorkspacePath, () => workspace.GetCommitDetails(sha, node, nodeOnly));
    }

    public CmsGitFileDiff GetCommitFileDiff(string cmsId, string username, string sha, string path)
    {
        var workspace = ExistingUserWorkspace(cmsId, username);

        return Locked(workspace.WorkspacePath, () => workspace.GetCommitFileDiff(sha, path));
    }

    #endregion

    #region Deploy

    private static readonly ConcurrentDictionary<string, string> RunningDeployments = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Only one deployment per cms-item at a time (all deployments read from the same deploy clone).
    /// Has to be released with <see cref="EndDeploy"/>.
    /// </summary>
    /// <exception cref="CmsGitException">another deployment is running</exception>
    public void BeginDeploy(string cmsId, string username)
    {
        if (!RunningDeployments.TryAdd(cmsId, username ?? String.Empty))
        {
            RunningDeployments.TryGetValue(cmsId, out var runningBy);
            throw new CmsGitException(CmsGitErrors.DeployRunning, runningBy);
        }
    }

    public void EndDeploy(string cmsId)
        => RunningDeployments.TryRemove(cmsId, out _);

    public bool IsDeployRunning(string cmsId)
        => RunningDeployments.ContainsKey(cmsId);

    /// <summary>
    /// User who started the running deployment (null, if no deployment is running)
    /// </summary>
    public string RunningDeployUser(string cmsId)
        => RunningDeployments.TryGetValue(cmsId, out var username) ? username : null;

    /// <summary>
    /// Branch deploy: the current state of the user's working copy (including uncommitted changes) is exported.
    /// The working copy must not have a running merge.
    /// The working copy stays locked (no git operations) until the result is disposed.
    /// Only one deploy per cms-item and branch at a time.
    /// </summary>
    /// <exception cref="CmsGitException">merge in progress, busy or deploy running</exception>
    public CmsGitBranchDeploy BeginBranchDeploy(string cmsId, string username)
    {
        var workspace = ExistingUserWorkspace(cmsId, username);
        var semaphore = Locks.GetOrAdd(workspace.WorkspacePath, _ => new SemaphoreSlim(1, 1));

        if (!semaphore.Wait(ShortLockTimeout))
        {
            throw new CmsGitException(CmsGitErrors.Busy);
        }

        string runningKey = null;
        try
        {
            var status = workspace.GetStatus(false);

            if (status.IsMerging)
            {
                throw new CmsGitException(CmsGitErrors.MergeInProgress);
            }
            var uncommittedFiles = (status.Changes ?? Enumerable.Empty<CmsGitChange>())
                .Select(c => c.Path)
                .Where(p => !String.IsNullOrEmpty(p))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var commit = workspace.HeadCommit()?.Sha;
            var branch = BranchDeployName(username, status);

            runningKey = BranchDeployKey(cmsId, branch);
            if (!RunningDeployments.TryAdd(runningKey, username ?? String.Empty))
            {
                RunningDeployments.TryGetValue(runningKey, out var runningBy);
                runningKey = null;
                throw new CmsGitException(CmsGitErrors.DeployRunning, runningBy);
            }

            var key = runningKey;
            return new CmsGitBranchDeploy(branch, status.Branch, commit, uncommittedFiles, workspace.WorkspacePath,
                since => workspace.ChangedPathsSince(since),
                () =>
            {
                RunningDeployments.TryRemove(key, out _);
                semaphore.Release();
            });
        }
        catch
        {
            if (runningKey != null)
            {
                RunningDeployments.TryRemove(runningKey, out _);
            }
            semaphore.Release();
            throw;
        }
    }

    /// <summary>
    /// The branch name used for a branch deploy: the current branch, or {user}-{default-branch} on the default branch
    /// </summary>
    public string BranchDeployName(string username, CmsGitStatus status)
        => status == null || !status.HasWorkspace || String.IsNullOrEmpty(status.Branch)
            ? null
            : status.IsDefaultBranch
                ? $"{SafeUserName(username)}-{status.Branch}"
                : status.Branch;

    private static string BranchDeployKey(string cmsId, string branch) => $"{cmsId}|branch:{branch}";

    /// <summary>
    /// Brings the deploy clone to the latest commit of the remote default branch.
    /// </summary>
    /// <returns>the commit hash</returns>
    public string UpdateDeployWorkspace(string cmsId)
    {
        var workspace = new CmsGitWorkspace(_resolver.DeployWorkspacePath(cmsId), Settings(cmsId));

        var sha = Locked(workspace.WorkspacePath, () => workspace.UpdateToRemoteDefaultBranch());

        _resolver.Invalidate(workspace.WorkspacePath);

        return sha;
    }

    /// <summary>
    /// Deletes the deploy clone. It will be cloned again with the next deployment.
    /// The caller has to make sure, that no deployment is running.
    /// </summary>
    public void ResetDeployWorkspace(string cmsId)
    {
        var path = _resolver.DeployWorkspacePath(cmsId);

        Locked(path, () =>
        {
            CmsGitWorkspace.DeleteDirectory(path);
            return true;
        });

        _resolver.Invalidate(path);
    }

    /// <summary>
    /// Which commit will be deployed + state of the user's working copy (unpublished changes)
    /// </summary>
    public CmsGitDeployInfo GetDeployInfo(string cmsId, string username)
    {
        var settings = Settings(cmsId);
        var deployWorkspace = new CmsGitWorkspace(_resolver.DeployWorkspacePath(cmsId), settings);
        var userWorkspace = UserWorkspace(cmsId, username);

        var info = new CmsGitDeployInfo() { DefaultBranch = deployWorkspace.DefaultBranch };

        try
        {
            info.LastDeployed = Locked(deployWorkspace.WorkspacePath, () => deployWorkspace.HeadCommit(), ShortLockTimeout);
        }
        catch { /* busy => unknown */ }

        try
        {
            if (userWorkspace.Exists)
            {
                Locked(userWorkspace.WorkspacePath, () =>
                {
                    info.UserStatus = Decorate(userWorkspace.GetStatus(true), username);
                    info.RemoteHead = userWorkspace.RemoteDefaultBranchCommit();
                    return true;
                });

                if (info.UserStatus.Stale)
                {
                    info.Error = info.UserStatus.FetchError;
                }
            }
            else
            {
                var sha = userWorkspace.RemoteDefaultBranchSha();
                info.RemoteHead = String.IsNullOrEmpty(sha) ? null : new CmsGitCommitInfo() { Sha = sha };
            }
        }
        catch (CmsGitException ex)
        {
            info.Error = String.IsNullOrEmpty(ex.Details) ? ex.Message : ex.Details;
        }
        catch (Exception ex)
        {
            info.Error = ex.Message;
        }

        return info;
    }

    #endregion

    #region Admin

    /// <summary>
    /// All user working copies of a cms-item
    /// </summary>
    public IEnumerable<CmsGitWorkspaceInfo> GetWorkspaces(string cmsId, string currentUsername)
    {
        var root = _resolver.UsersRootPath(cmsId);
        if (!Directory.Exists(root))
        {
            return Array.Empty<CmsGitWorkspaceInfo>();
        }

        var settings = Settings(cmsId);
        var currentName = String.IsNullOrWhiteSpace(currentUsername) ? null : CmsManagerResolver.SafeName(currentUsername);
        var result = new List<CmsGitWorkspaceInfo>();

        foreach (var directory in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileName(directory);
            var workspace = new CmsGitWorkspace(directory, settings);
            var info = new CmsGitWorkspaceInfo()
            {
                Name = name,
                IsCurrentUser = name.Equals(currentName, StringComparison.OrdinalIgnoreCase),
                LastModified = LastModified(directory)
            };

            try
            {
                if (!workspace.Exists)
                {
                    info.Error = "no repository";
                }
                else
                {
                    var status = Locked(directory, () => workspace.GetStatus(false), ShortLockTimeout);

                    info.Branch = status.Branch;
                    info.Changes = status.Changes?.Count() ?? 0;
                    info.Ahead = status.Ahead;
                    info.HasUpstream = status.HasUpstream;
                    info.IsMerging = status.IsMerging;
                }
            }
            catch (CmsGitException ex)
            {
                info.Error = ex.L10nKey;
            }
            catch (Exception ex)
            {
                info.Error = ex.Message;
            }

            result.Add(info);
        }

        return result;
    }

    /// <summary>
    /// Deletes a user's working copy (uncommitted/unpushed changes are lost)
    /// </summary>
    /// <param name="name">folder name of the working copy (<see cref="CmsGitWorkspaceInfo.Name"/>)</param>
    public void DeleteWorkspace(string cmsId, string name)
    {
        var root = Path.GetFullPath(_resolver.UsersRootPath(cmsId)).TrimEnd('\\', '/');
        var path = Path.GetFullPath(_resolver.UserWorkspacePath(cmsId, name));

        if (!String.Equals(Path.GetDirectoryName(path)?.TrimEnd('\\', '/'), root, StringComparison.OrdinalIgnoreCase))
        {
            throw new CmsGitException(CmsGitErrors.InvalidPath, name);
        }

        Locked(path, () =>
        {
            CmsGitWorkspace.DeleteDirectory(path);
            _resolver.DeleteExportCache(cmsId, name);
            return true;
        });

        _resolver.Invalidate(path);
    }

    private static DateTime? LastModified(string directory)
    {
        try
        {
            return new[]
                {
                    Path.Combine(directory, ".git", "index"),
                    Path.Combine(directory, ".git", "HEAD")
                }
                .Where(File.Exists)
                .Select(File.GetLastWriteTime)
                .DefaultIfEmpty(Directory.GetLastWriteTime(directory))
                .Max();
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Helper

    private CmsGitStatus Run<T>(string cmsId, string username, Func<CmsGitWorkspace, T> action, bool invalidate)
    {
        var workspace = ExistingUserWorkspace(cmsId, username);

        return Locked(workspace.WorkspacePath, () =>
        {
            try
            {
                action(workspace);
            }
            finally
            {
                if (invalidate)
                {
                    _resolver.Invalidate(workspace.WorkspacePath);
                }
            }

            return Decorate(workspace.GetStatus(false), username);
        });
    }

    private CmsGitWorkspace UserWorkspace(string cmsId, string username)
        => new CmsGitWorkspace(_resolver.UserWorkspacePath(cmsId, username), Settings(cmsId));

    private CmsGitWorkspace ExistingUserWorkspace(string cmsId, string username)
    {
        var workspace = UserWorkspace(cmsId, username);
        if (!workspace.Exists)
        {
            throw new CmsGitWorkspaceNotFoundException(cmsId, workspace.WorkspacePath);
        }

        return workspace;
    }

    private CmsConfig.CmsItem CmsItem(string cmsId)
    {
        var cmsItem = _ccs.GetCmsItem(cmsId);
        if (cmsItem?.IsGitEnabled != true)
        {
            throw new InvalidOperationException($"Git is not configured for cms-item {cmsId}");
        }

        return cmsItem;
    }

    private CmsGitSettings Settings(string cmsId)
    {
        var git = CmsItem(cmsId).Git;

        return new CmsGitSettings(
            git.RemoteUrl,
            git.Username,
            git.ResolvedToken,
            git.DefaultBranch,
            git.CommitterName,
            git.CommitterEmail);
    }

    private static CmsGitStatus Decorate(CmsGitStatus status, string username)
    {
        status.SuggestedBranchPrefix = SuggestedBranchPrefix(username);
        return status;
    }

    private static string SuggestedBranchPrefix(string username)
    {
        try
        {
            return $"{SafeUserName(username)}/";
        }
        catch
        {
            return String.Empty;
        }
    }

    // DOMAIN\user => user
    private static string SafeUserName(string username)
        => CmsManagerResolver.SafeName(username?.Split('\\', '/')[^1]);

    private static T Locked<T>(string key, Func<T> func, TimeSpan? timeout = null)
    {
        var semaphore = Locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        if (!semaphore.Wait(timeout ?? LockTimeout))
        {
            throw new CmsGitException(CmsGitErrors.Busy);
        }

        try
        {
            return func();
        }
        finally
        {
            semaphore.Release();
        }
    }

    #endregion
}
