using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

using E.Standard.Cms.Configuration.Models;
using E.Standard.CMS.Core;

namespace E.Standard.Cms.Configuration.Services;

/// <summary>
/// Resolves the physical CMS tree (CMSManager) for a cms-item.
/// Without git configuration this is always the shared tree from <see cref="CmsConfigurationService.CMS"/>.
/// With git configuration every user works in an own working copy, deployments read from a dedicated deploy clone.
/// </summary>
public class CmsManagerResolver
{
    private const string UsersFolder = "users";
    private const string DeployFolder = "deploy";
    private const string CacheFolder = "cache";

    private readonly CmsConfigurationService _ccs;
    private readonly ConcurrentDictionary<string, CMSManager> _cache = new(StringComparer.OrdinalIgnoreCase);

    public CmsManagerResolver(CmsConfigurationService ccs)
    {
        _ccs = ccs;
    }

    public bool IsGitEnabled(string cmsId) => _ccs.IsGitEnabled(cmsId);

    public CMSManager Get(string cmsId, string username, CmsItemTransistantInjectionServicePack servicePack)
    {
        if (!_ccs.IsGitEnabled(cmsId))
        {
            return _ccs.CMS[cmsId];
        }

        return GetGitCmsManager(cmsId, UserWorkspacePath(cmsId, username), servicePack);
    }

    public CMSManager GetDeploySource(string cmsId, CmsItemTransistantInjectionServicePack servicePack)
    {
        if (!_ccs.IsGitEnabled(cmsId))
        {
            return _ccs.CMS[cmsId];
        }

        return GetGitCmsManager(cmsId, DeployWorkspacePath(cmsId), servicePack);
    }

    /// <summary>
    /// Physical tree path for the given user; null => use the configured cms-item path (no git)
    /// </summary>
    public string TreePath(string cmsId, string username)
        => _ccs.IsGitEnabled(cmsId) ? UserWorkspacePath(cmsId, username) : null;

    /// <summary>
    /// Physical tree path for deployments; null => use the configured cms-item path (no git)
    /// </summary>
    public string DeployTreePath(string cmsId)
        => _ccs.IsGitEnabled(cmsId) ? DeployWorkspacePath(cmsId) : null;

    public string UserWorkspacePath(string cmsId, string username)
    {
        if (String.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("A username is required to resolve a git workspace");
        }

        return Path.Combine(GitConfig(cmsId).WorkspaceRoot, SafeName(cmsId), UsersFolder, SafeName(username));
    }

    /// <summary>
    /// Folder containing the working copies of all users of a cms-item
    /// </summary>
    public string UsersRootPath(string cmsId)
        => Path.Combine(GitConfig(cmsId).WorkspaceRoot, SafeName(cmsId), UsersFolder);

    public string DeployWorkspacePath(string cmsId)
        => Path.Combine(GitConfig(cmsId).WorkspaceRoot, SafeName(cmsId), DeployFolder);

    /// <summary>
    /// Fast deploy cache (file snapshot) of a user's working copy. Lives outside the working copy (not versioned).
    /// </summary>
    public string ExportCachePath(string cmsId, string username)
    {
        if (String.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("A username is required to resolve the export cache");
        }

        return Path.Combine(GitConfig(cmsId).WorkspaceRoot, SafeName(cmsId), CacheFolder, SafeName(username) + ".snapshot");
    }

    /// <summary>
    /// Deletes the fast deploy cache of a user (working copy re-created/removed)
    /// </summary>
    public void DeleteExportCache(string cmsId, string username)
    {
        try
        {
            var path = ExportCachePath(cmsId, username);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch { }
    }

    public bool WorkspaceExists(string workspacePath)
        => Directory.Exists(Path.Combine(workspacePath, ".git"));

    /// <summary>
    /// Throws, if git is enabled and the user's working copy does not exist yet.
    /// Has to be called before tools that open the tree by path (CMSManager would create an empty tree otherwise)
    /// </summary>
    public void EnsureUserWorkspace(string cmsId, string username)
    {
        if (_ccs.IsGitEnabled(cmsId) && !WorkspaceExists(UserWorkspacePath(cmsId, username)))
        {
            throw new CmsGitWorkspaceNotFoundException(cmsId, UserWorkspacePath(cmsId, username));
        }
    }

    /// <summary>
    /// true, if git is enabled and a merge is running in the user's working copy (=> editing is locked until the merge is completed/aborted)
    /// </summary>
    public bool IsMerging(string cmsId, string username)
        => _ccs.IsGitEnabled(cmsId)
           && File.Exists(Path.Combine(UserWorkspacePath(cmsId, username), ".git", "MERGE_HEAD"));

    /// <summary>
    /// <see cref="EnsureUserWorkspace"/> + no running merge. Has to be called before tools that modify the tree.
    /// </summary>
    public void EnsureEditableUserWorkspace(string cmsId, string username)
    {
        EnsureUserWorkspace(cmsId, username);

        if (IsMerging(cmsId, username))
        {
            throw new CmsGitMergeInProgressException(cmsId);
        }
    }

    public void EnsureDeployWorkspace(string cmsId)
    {
        if (_ccs.IsGitEnabled(cmsId) && !WorkspaceExists(DeployWorkspacePath(cmsId)))
        {
            throw new CmsGitWorkspaceNotFoundException(cmsId, DeployWorkspacePath(cmsId));
        }
    }

    /// <summary>
    /// Has to be called after the files of a workspace changed outside the CMSManager (checkout, merge, pull...)
    /// </summary>
    public void Invalidate(string workspacePath)
        => _cache.TryRemove(NormalizeKey(workspacePath), out _);

    #region Helper

    private CMSManager GetGitCmsManager(string cmsId, string workspacePath, CmsItemTransistantInjectionServicePack servicePack)
    {
        if (!WorkspaceExists(workspacePath))
        {
            // never let CMSManager create an empty tree here => the workspace has to be cloned first
            throw new CmsGitWorkspaceNotFoundException(cmsId, workspacePath);
        }

        return _cache.GetOrAdd(
            NormalizeKey(workspacePath),
            _ => _ccs.CreateCmsManager(_ccs.GetCmsItem(cmsId), workspacePath, servicePack));
    }

    private CmsConfig.GitConfig GitConfig(string cmsId)
        => _ccs.GetCmsItem(cmsId)?.Git
           ?? throw new InvalidOperationException($"Git is not configured for cms-item {cmsId}");

    private static string NormalizeKey(string path)
        => Path.GetFullPath(path).TrimEnd('\\', '/');

    public static string SafeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(new[] { '\\', '/', ':', ' ' }).ToHashSet();

        var safe = new string(name.Trim().ToLowerInvariant().Select(c => invalid.Contains(c) ? '_' : c).ToArray());

        if (String.IsNullOrEmpty(safe.Trim('.', '_')))
        {
            throw new ArgumentException($"Invalid name: {name}");
        }

        return safe;
    }

    #endregion
}

public class CmsGitWorkspaceNotFoundException : Exception
{
    public CmsGitWorkspaceNotFoundException(string cmsId, string workspacePath)
        : base($"No git workspace found for cms-item '{cmsId}'. The workspace has to be fetched first.")
    {
        CmsId = cmsId;
        WorkspacePath = workspacePath;
    }

    public string CmsId { get; }
    public string WorkspacePath { get; }
}

public class CmsGitMergeInProgressException : Exception
{
    public CmsGitMergeInProgressException(string cmsId)
        : base($"A merge is in progress for cms-item '{cmsId}'. Resolve the conflicts or abort the merge before editing.")
    {
        CmsId = cmsId;
    }

    public string CmsId { get; }
}
