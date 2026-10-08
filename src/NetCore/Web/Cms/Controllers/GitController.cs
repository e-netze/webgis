using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using Cms.AppCode.Mvc;
using Cms.AppCode.Services;

using E.Standard.Cms.Abstraction;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Git.Exceptions;
using E.Standard.Cms.Git.Models;
using E.Standard.Cms.Git.Services;
using E.Standard.Cms.Services;
using E.Standard.CMS.Core.Extensions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Localization.Abstractions;
using E.Standard.Security.App.Reflection;
using E.Standard.Security.App.Services;
using E.Standard.Security.Cryptography.Abstractions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Cms.Controllers;

/// <summary>
/// Git operations on the user's working copy of a cms-item (only if git is configured for the cms-item)
/// </summary>
[ApplicationSecurity]
public class GitController : ApplicationSecurityController
{
    private const string FallbackEmailDomain = "cms.local";

    /// <summary>
    /// Localized strings used by wwwroot/js/site-cms-git.js
    /// </summary>
    public static readonly string[] ClientL10nKeys =
    [
        "title", "please-wait",
        "workspace-missing-title", "workspace-missing-text", "init-workspace",
        "branch", "no-changes", "changes", "ahead", "behind", "up-to-date", "no-upstream", "stale", "check-status", "last-checked",
        "pull", "commit", "push", "branches",
        "pull-success", "push-success", "commit-success",
        "commit-title", "commit-message", "commit-message-placeholder", "commit-button",
        "change-added", "change-modified", "change-deleted", "change-conflicted",
        "branches-title", "branch-new", "branch-new-placeholder", "branch-create", "branch-switch", "branch-delete",
        "branch-delete-confirm", "branch-current", "branch-default", "branch-remote-only", "branch-created-offline",
        "close",
        "error-commit-first", "error-message-required",
        "merge-from-default", "merge-from-default-success",
        "merge-into-default", "merge-into-default-title", "merge-into-default-text", "merge-delete-branch",
        "merge-into-default-button", "merge-into-default-success",
        "merging-banner", "merging-banner-resolved", "resolve-conflicts",
        "merge-complete", "merge-complete-success", "merge-abort", "merge-abort-confirm",
        "conflicts-title", "conflicts-none", "conflict-mine", "conflict-theirs",
        "conflict-version", "conflicts-intro", "diff-unchanged-lines",
        "conflict-use-mine", "conflict-use-theirs", "conflict-all-mine", "conflict-all-theirs", "conflict-deleted",
        "conflict-kind-modified", "conflict-kind-added", "conflict-kind-deleted-by-me", "conflict-kind-deleted-by-them",
        "discard", "discard-confirm", "discard-all", "discard-all-confirm", "node-changed",
        "workspaces", "workspaces-title", "workspaces-intro", "workspaces-none",
        "workspace-user", "workspace-branch", "workspace-state", "workspace-last-modified", "workspace-current-user",
        "workspace-clean", "workspace-merging", "workspace-delete", "workspace-delete-confirm", "workspace-delete-own-confirm",
        "workspace-deleted", "deploy-workspace-reset", "deploy-workspace-reset-confirm", "deploy-workspace-reset-success",
        "deploy-git-running",
        "history", "history-title", "history-all-branches", "history-load-more", "history-none",
        "history-head", "history-deployed", "history-unpushed", "history-select-commit",
        "history-author", "history-date", "history-commit", "history-parents", "history-merge",
        "history-changes", "history-no-changes", "history-before", "history-after",
        "commit-sub", "push-sub", "pull-sub", "merge-from-default-sub", "merge-into-default-sub", "discard-all-sub",
        "branches-sub", "history-sub", "workspaces-sub", "check-status-sub", "merge-complete-sub", "merge-abort-sub",
        "resolve-conflicts-sub",
        "commit-tip", "push-tip", "pull-tip", "merge-from-default-tip", "merge-into-default-tip", "discard-all-tip",
        "branches-tip", "history-tip", "workspaces-tip", "check-status-tip", "resolve-conflicts-tip",
        "merge-complete-tip", "merge-abort-tip",
        "disabled-no-changes", "disabled-commit-first", "disabled-nothing-to-push",
        "disabled-commit-or-discard-first", "disabled-conflicts-remaining",
        "count-changes", "count-ahead", "count-behind", "stale-short", "panel-collapse", "panel-expand",
        "diff-view-table", "diff-view-xml", "diff-view-tip", "diff-show-all", "diff-property", "diff-before", "diff-after",
        "diff-no-property-changes", "diff-more", "diff-working", "diff-show", "order-changed",
        "changes-title", "changes-tip", "changes-none", "changes-goto", "changes-files",
        "commit-message-modified", "commit-message-added", "commit-message-deleted", "commit-message-more", "commit-ctrl-enter",
        "node-history", "node-history-title", "node-history-only-node", "node-history-uncommitted", "node-history-uncommitted-text",
        "restore", "restore-tip", "restore-confirm", "restore-counts", "restore-overwrite-warning", "restore-nothing",
        "main-diff-title", "main-diff-tip", "main-diff-none", "main-diff-state", "main-diff-fetched", "main-diff-unknown",
        "main-diff-refresh", "main-diff-take", "main-diff-take-tip", "main-diff-current", "compare-with", "compare-head", "compare-main",
        "diff-no-differences"
    ];

    private readonly CmsConfigurationService _ccs;
    private readonly CmsGitService _git;
    private readonly ICmsLogger _cmsLogger;
    private readonly ILocalizer _localizer;
    private readonly BranchDeployService _branchDeployService;

    public GitController(
            CmsConfigurationService ccs,
            UrlHelperService urlHelperService,
            ApplicationSecurityUserManager applicationSecurityUserManager,
            ICryptoService crypto,
            CmsItemInjectionPackService instanceService,
            CmsGitService git,
            ICmsLogger cmsLogger,
            IStringLocalizerFactory stringLocalizerFactory,
            BranchDeployService branchDeployService,
            IEnumerable<ICustomCmsPageSecurityService> customSecurity = null)
        : base(ccs, urlHelperService, applicationSecurityUserManager, customSecurity, crypto, instanceService)
    {
        _ccs = ccs;
        _git = git;
        _branchDeployService = branchDeployService;
        _cmsLogger = cmsLogger;
        _localizer = stringLocalizerFactory.CreateCmsLocalizer(typeof(GitController));
    }

    public IActionResult Status(string id, bool fetch = false)
        => Execute(id, null, () => StatusResult(_git.GetStatus(id, Username, fetch)));

    public IActionResult InitWorkspace(string id)
        => Execute(id, "InitWorkspace", () => StatusResult(
            _git.CreateWorkspace(id, Username, GitUser, _localizer.Localize("initial-commit-message"))));

    public IActionResult Pull(string id)
        => Execute(id, "Pull", () => StatusResult(_git.Pull(id, Username, GitUser)));

    public IActionResult Commit(string id, string message)
        => Execute(id, "Commit", () => StatusResult(_git.Commit(id, Username, GitUser, message)));

    public IActionResult Push(string id)
        => Execute(id, "Push", () => StatusResult(_git.Push(id, Username, GitUser)));

    public IActionResult Branches(string id)
        => Execute(id, null, () => Json(new
        {
            success = true,
            enabled = true,
            branches = _git.GetBranches(id, Username).ToArray()
        }));

    public IActionResult CreateBranch(string id, string name)
        => Execute(id, "CreateBranch", () => StatusResult(_git.CreateBranch(id, Username, name)), name);

    public IActionResult Checkout(string id, string name)
        => Execute(id, "Checkout", () => StatusResult(_git.Checkout(id, Username, name)), name);

    public IActionResult DeleteBranch(string id, string name, bool deleteRemote = true)
        => Execute(id, "DeleteBranch", () =>
        {
            var status = _git.DeleteBranch(id, Username, name, deleteRemote);
            RemoveBranchDeploys(id, name);

            return StatusResult(status);
        }, name);

    public IActionResult MergeFromDefault(string id)
        => Execute(id, "MergeFromDefault", () => StatusResult(_git.MergeFromDefault(id, Username, GitUser)));

    public IActionResult MergeIntoDefault(string id, bool deleteBranch = true)
        => Execute(id, "MergeIntoDefault", () =>
        {
            var mergedBranch = deleteBranch ? _git.GetStatus(id, Username, false).Branch : null;
            var status = _git.MergeIntoDefault(id, Username, GitUser, deleteBranch);

            if (!String.IsNullOrEmpty(mergedBranch) &&
                !_git.GetBranches(id, Username).Any(b => b.IsLocal && b.Name == mergedBranch))
            {
                RemoveBranchDeploys(id, mergedBranch);
            }

            return StatusResult(status);
        }, deleteBranch.ToString());

    public IActionResult Conflicts(string id)
        => Execute(id, null, () => Json(new
        {
            success = true,
            enabled = true,
            conflicts = _git.GetConflicts(id, Username).ToArray()
        }));

    public IActionResult ResolveConflict(string id, string node, string choice)
        => Execute(id, "ResolveConflict", () => StatusResult(_git.ResolveConflict(id, Username, node, choice)), $"{node}: {choice}");

    public IActionResult CompleteMerge(string id)
        => Execute(id, "CompleteMerge", () => StatusResult(_git.CompleteMerge(id, Username, GitUser)));

    public IActionResult AbortMerge(string id)
        => Execute(id, "AbortMerge", () => StatusResult(_git.AbortMerge(id, Username)));

    public IActionResult Discard(string id, string node)
        => Execute(id, "Discard", () => StatusResult(_git.Discard(id, Username, node)), node);

    public IActionResult ChangedNodes(string id)
        => Execute(id, null, () =>
        {
            var nodes = _git.GetChangedNodes(id, Username).ToArray();
            ResolveDisplayNames(id, nodes);

            return Json(new { success = true, enabled = true, nodes });
        });

    public IActionResult WorkingFileDiff(string id, string path)
        => Execute(id, null, () => Json(new
        {
            success = true,
            enabled = true,
            diff = _git.GetWorkingFileDiff(id, Username, path)
        }));

    /// <summary>
    /// Display names like in the CMS tree (deleted nodes keep the last part of the path)
    /// </summary>
    private void ResolveDisplayNames(string id, IEnumerable<CmsGitChangedNode> nodes)
    {
        foreach (var parentGroup in nodes
                    .Where(n => n.State != CmsGitChangeStates.Deleted && !String.IsNullOrEmpty(n.Node))
                    .GroupBy(n => n.Node.Contains('/') ? n.Node.Substring(0, n.Node.LastIndexOf('/')) : String.Empty))
        {
            try
            {
                var cms = Cms(id);
                var doc = String.IsNullOrEmpty(parentGroup.Key)
                    ? cms.ToXml(ServicePack, false, false)
                    : cms.ToXml(ServicePack, cms.ConnectionString + "/" + parentGroup.Key, false, false);

                foreach (var node in parentGroup)
                {
                    try
                    {
                        var name = node.Node.Substring(node.Node.LastIndexOf('/') + 1);
                        var itemNode = doc?.SelectSingleNode("CMS/item[@name=" + XPathLiteral(name) + "]");
                        if (itemNode == null)
                        {
                            continue;
                        }

                        var displayName = _ccs.Translate(id, (itemNode.Attributes["displayname"]?.Value ?? String.Empty).Replace("_", " "));

                        if (String.IsNullOrWhiteSpace(displayName) && itemNode.Attributes["type"]?.Value == "link")
                        {
                            var targetInstance = cms.SchemaNodeInstance(ServicePack, itemNode.Attributes["target"]?.Value ?? String.Empty, true, true);
                            if (targetInstance is IDisplayName displayNameInstance && !String.IsNullOrWhiteSpace(displayNameInstance.DisplayName))
                            {
                                displayName = displayNameInstance.DisplayName;
                            }
                            else if (targetInstance is NameUrl nameUrl)
                            {
                                displayName = nameUrl.Name;
                            }
                        }

                        if (!String.IsNullOrWhiteSpace(displayName))
                        {
                            node.Name = displayName;
                        }
                    }
                    catch { /* keep the fallback name */ }
                }
            }
            catch { /* keep the fallback names */ }
        }
    }

    private static string XPathLiteral(string value)
        => !value.Contains('\'') ? "'" + value + "'"
         : !value.Contains('"') ? "\"" + value + "\""
         : "concat('" + value.Replace("'", "', \"'\", '") + "')";

    #region History

    public IActionResult History(string id, bool fetch = false, bool allBranches = true, int limit = 100, string node = null, bool nodeOnly = false)
        => Execute(id, null, () => Json(new
        {
            success = true,
            enabled = true,
            history = _git.GetHistory(id, Username, fetch, allBranches, limit, node, nodeOnly)
        }));

    public IActionResult CommitDetails(string id, string sha, string node = null, bool nodeOnly = false)
        => Execute(id, null, () => Json(new
        {
            success = true,
            enabled = true,
            commit = _git.GetCommitDetails(id, Username, sha, node, nodeOnly)
        }));

    /// <summary>
    /// Restore a node from a commit (sha) or the default branch (source="default")
    /// </summary>
    public IActionResult RestorePreview(string id, string source, string node, bool nodeOnly = false)
        => Execute(id, null, () => Json(new
        {
            success = true,
            enabled = true,
            restore = _git.Restore(id, Username, source, node, nodeOnly, true)
        }));

    public IActionResult Restore(string id, string source, string node, bool nodeOnly = false)
        => Execute(id, "Restore", () =>
        {
            var restore = _git.Restore(id, Username, source, node, nodeOnly, false);

            return Json(new
            {
                success = true,
                enabled = true,
                restore,
                status = _git.GetStatus(id, Username, false)
            });
        }, $"{node} <= {source}{(nodeOnly ? " (node only)" : "")}");

    /// <summary>
    /// Differences between the working copy and the default branch (last fetched state)
    /// </summary>
    public IActionResult MainDiff(string id, bool fetch = false)
        => Execute(id, null, () =>
        {
            var diff = _git.GetDefaultBranchDiff(id, Username, fetch);
            ResolveDisplayNames(id, diff.Nodes);

            return Json(new { success = true, enabled = true, diff });
        });

    public IActionResult MainFileDiff(string id, string path)
        => Execute(id, null, () => Json(new
        {
            success = true,
            enabled = true,
            diff = _git.GetDefaultBranchFileDiff(id, Username, path)
        }));

    public IActionResult CommitFileDiff(string id, string sha, string path)
        => Execute(id, null, () => Json(new
        {
            success = true,
            enabled = true,
            diff = _git.GetCommitFileDiff(id, Username, sha, path)
        }));

    #endregion

    #region Admin

    // All CMS editors are admins (no separate admin role) => every authorized CMS user may manage the workspaces

    public IActionResult Workspaces(string id)
        => Execute(id, null, () => Json(new
        {
            success = true,
            enabled = true,
            deploy_running_by = _git.RunningDeployUser(id),
            workspaces = _git.GetWorkspaces(id, Username).ToArray()
        }));

    public IActionResult DeleteWorkspace(string id, string name)
        => Execute(id, "DeleteWorkspace", () =>
        {
            _git.DeleteWorkspace(id, name);
            return Json(new { success = true, enabled = true });
        }, name);

    public IActionResult ResetDeployWorkspace(string id)
        => Execute(id, "ResetDeployWorkspace", () =>
        {
            _git.BeginDeploy(id, Username);
            try
            {
                _git.ResetDeployWorkspace(id);
            }
            finally
            {
                _git.EndDeploy(id);
            }

            return Json(new { success = true, enabled = true });
        });

    #endregion

    #region Helper

    private string Username => GetCurrentUsername();

    private string CmsId => RouteData.Values["id"]?.ToString() ?? Request.Query["id"].ToString();

    /// <summary>
    /// A branch was deleted in the CMS => remove its branch deploys (errors are logged only)
    /// </summary>
    private void RemoveBranchDeploys(string id, string branchName)
        => _branchDeployService.RemoveBranchFromAllDeploymentsAsync(id, branchName, Username).GetAwaiter().GetResult();

    private IActionResult Execute(string id, string logAction, Func<IActionResult> action, string logArg = "")
    {
        try
        {
            if (!_git.IsEnabled(id))
            {
                return Json(new { success = true, enabled = false });
            }

            if (!String.IsNullOrEmpty(logAction))
            {
                _cmsLogger.Log(Username, "Git", logAction, id, logArg ?? String.Empty);
            }

            return action();
        }
        catch (CmsGitException ex)
        {
            var message = _localizer.Localize(ex.L10nKey);

            return Json(new
            {
                success = false,
                error_key = ex.L10nKey,
                exception = String.IsNullOrWhiteSpace(ex.Details) ? message : $"{message}\n{ex.Details}"
            });
        }
        catch (CmsGitWorkspaceNotFoundException)
        {
            return Json(new
            {
                success = false,
                error_key = "error-no-workspace",
                exception = _localizer.Localize("error-no-workspace")
            });
        }
        catch (Exception ex)
        {
            return ExceptionResult(ex);
        }
    }

    private IActionResult StatusResult(CmsGitStatus status)
        => Json(new { success = true, enabled = true, status });

    /// <summary>
    /// Commit author: the CMS user. E-Mail from the identity claims, otherwise a generated address.
    /// </summary>
    private CmsGitUser GitUser
    {
        get
        {
            var username = Username;

            var email = new[] { "email", System.Security.Claims.ClaimTypes.Email, "preferred_username", "upn", System.Security.Claims.ClaimTypes.Upn }
                .Select(type => User?.FindFirst(type)?.Value)
                .FirstOrDefault(value => !String.IsNullOrWhiteSpace(value) && value.Contains('@'));

            if (String.IsNullOrWhiteSpace(email))
            {
                var domain = _ccs.GetCmsItem(CmsId)?.Git?.AuthorEmailDomain;
                var name = username?.Split('\\', '/')[^1];
                email = $"{CmsManagerResolver.SafeName(String.IsNullOrWhiteSpace(name) ? "cms-user" : name)}@{(String.IsNullOrWhiteSpace(domain) ? FallbackEmailDomain : domain.Trim().TrimStart('@'))}";
            }

            return new CmsGitUser(String.IsNullOrWhiteSpace(username) ? "cms-user" : username, email);
        }
    }

    #endregion
}
