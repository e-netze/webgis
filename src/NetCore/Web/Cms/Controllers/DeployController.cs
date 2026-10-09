using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Cms.AppCode;
using Cms.AppCode.Mvc;
using Cms.AppCode.Services;
using Cms.Models;

using E.Standard.Cms.Abstraction;
using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Git.Exceptions;
using E.Standard.Cms.Git.Models;
using E.Standard.Cms.Git.Services;
using E.Standard.Cms.Services;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Branches;
using E.Standard.CMS.Core.Extensions;
using E.Standard.CMS.Core.IO;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Localization.Abstractions;
using E.Standard.Security.App.Reflection;
using E.Standard.Security.App.Services;
using E.Standard.Security.Cryptography.Abstractions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Cms.Controllers;

[ApplicationSecurity]
public class DeployController : ApplicationSecurityController
{
    private readonly CmsConfigurationService _ccs;
    private readonly string _applicationContentRootPath;
    private readonly ICmsLogger _cmsLogger;
    private readonly DeployService _deployService;
    private readonly SolveWaringsService _solveWarningsService;
    private readonly CmsManagerResolver _cmsResolver;
    private readonly CmsGitService _git;
    private readonly ILocalizer _gitLocalizer;
    private readonly BranchDeployService _branchDeployService;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public DeployController(
            CmsConfigurationService ccs,
            UrlHelperService urlHelperService,
            ApplicationSecurityUserManager applicationSecurityUserManager,
            IWebHostEnvironment environment,
            ICryptoService crypto,
            CmsItemInjectionPackService instanceService,
            ICmsLogger cmsLogger,
            DeployService deployService,
            SolveWaringsService solveWarningsService,
            CmsManagerResolver cmsResolver,
            CmsGitService git,
            BranchDeployService branchDeployService,
            IStringLocalizerFactory stringLocalizerFactory,
            IEnumerable<ICustomCmsPageSecurityService> customSecurity = null)
        : base(ccs, urlHelperService, applicationSecurityUserManager, customSecurity, crypto, instanceService)
    {
        _git = git;
        _branchDeployService = branchDeployService;
        _gitLocalizer = stringLocalizerFactory.CreateCmsLocalizer(typeof(GitController));
        _ccs = ccs;
        _applicationContentRootPath = environment.ContentRootPath;
        _cmsLogger = cmsLogger;
        _deployService = deployService;
        _solveWarningsService = solveWarningsService;
        _cmsResolver = cmsResolver;

        _servicePack = instanceService.ServicePack;
    }

    public IActionResult Index(string id = "")
    {
        try
        {
            if (_ccs.IsCustomCms(id))
            {
                return View(new DeployModel()
                {
                    CmsItem = DynamicCmsItem(id),
                    IsIFramed = Request.Query["iframe"] == "true"
                });
            }
            else
            {
                var cmsItem = _ccs.Instance.CmsItems.Where(i => i.Id == id).FirstOrDefault();
                if (cmsItem == null)
                {
                    throw new Exception("Unknown Cms-Item-Id: " + id);
                }

                var gitEnabled = _git.IsEnabled(id);
                var gitDeployInfo = gitEnabled ? _git.GetDeployInfo(id, this.GetCurrentUsername()) : null;

                return View(new DeployModel()
                {
                    CmsItem = cmsItem,
                    IsIFramed = Request.Query["iframe"] == "true",
                    GitDeployInfo = gitDeployInfo,
                    GitBranchDeployName = gitEnabled ? _git.BranchDeployName(this.GetCurrentUsername(), gitDeployInfo?.UserStatus) : null,
                    GitDeployRunningBy = gitEnabled ? _git.RunningDeployUser(id) : null,
                    GitLocalizer = gitEnabled ? _gitLocalizer : null,
                    ExportCacheInfo = gitEnabled ? ExportFileCache.ReadInfo(_cmsResolver.ExportCachePath(id, this.GetCurrentUsername())) : null,
                    Username = this.GetCurrentUsername()
                });
            }
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    public IActionResult Deploy(string id, string name, bool branch = false, bool full = false)
    {
        if (branch)
        {
            return DeployBranch(id, name, full);
        }

        var deployLocked = false;

        try
        {
            _cmsLogger.Log(this.GetCurrentUsername(),
                           "Deploy", "Start", id, name);

            if (_git.IsEnabled(id))
            {
                // only one deployment per cms-item: all deployments read from the same deploy clone
                _git.BeginDeploy(id, this.GetCurrentUsername());
                deployLocked = true;

                // deploy always the latest state of the remote default branch
                var sha = _git.UpdateDeployWorkspace(id);
                _cmsLogger.Log(this.GetCurrentUsername(),
                               "Deploy", "GitCommit", id, sha);
            }

            _cmsResolver.EnsureDeployWorkspace(id);

            var backgroundProcess = new BackgroundProcess(id, this.GetCurrentUsername(), DeployCms, name);
            deployLocked = false;  // released by the background process

            return OpenConsole(backgroundProcess, $"Deploying: {name}", id);
        }
        catch (CmsGitException ex)
        {
            var message = _gitLocalizer.Localize(ex.L10nKey);

            return Json(new
            {
                success = false,
                exception = String.IsNullOrWhiteSpace(ex.Details) ? message : $"{message}\n{ex.Details}"
            });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
        finally
        {
            if (deployLocked)
            {
                _git.EndDeploy(id);
            }
        }
    }

    /// <summary>
    /// Deploys the current state (including uncommitted changes) of the user's working copy (current branch) to
    /// {target-dir}/branches/{encoded-branch}/... (file target) or uploads it with a branch parameter (url target)
    /// </summary>
    private IActionResult DeployBranch(string id, string name, bool full)
    {
        CmsGitBranchDeploy branchDeploy = null;

        try
        {
            if (!_git.IsEnabled(id))
            {
                throw new Exception("Branch deploy requires git");
            }

            _branchDeployService.BranchDeployment(id, name);  // throws, if not allowed

            branchDeploy = _git.BeginBranchDeploy(id, this.GetCurrentUsername());

            _cmsLogger.Log(this.GetCurrentUsername(),
                           "Deploy", "StartBranch", id, name, branchDeploy.Branch, branchDeploy.Commit ?? String.Empty, branchDeploy.Uncommitted ? "uncommitted" : String.Empty);

            var job = new BranchDeployJob(name, branchDeploy,
                _cmsResolver.TreePath(id, this.GetCurrentUsername()),
                _cmsResolver.ExportCachePath(id, this.GetCurrentUsername()),
                full);
            var backgroundProcess = new BackgroundProcess(id, this.GetCurrentUsername(), DeployCmsBranch, job);
            branchDeploy = null;  // released by the background process

            return OpenConsole(backgroundProcess, $"Deploying: {name} ({job.Handle.Branch})", id);
        }
        catch (CmsGitException ex)
        {
            var message = _gitLocalizer.Localize(ex.L10nKey);

            return Json(new
            {
                success = false,
                exception = String.IsNullOrWhiteSpace(ex.Details) ? message : $"{message}\n{ex.Details}"
            });
        }
        catch (CmsGitWorkspaceNotFoundException)
        {
            return Json(new { success = false, exception = _gitLocalizer.Localize("error-no-workspace") });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
        finally
        {
            branchDeploy?.Dispose();
        }
    }

    /// <summary>
    /// Deployed branches of a deployment
    /// </summary>
    async public Task<IActionResult> BranchDeploys(string id, string name)
    {
        try
        {
            var deploys = await _branchDeployService.GetBranchDeploysAsync(id, name);

            return Json(new
            {
                success = true,
                deploys = deploys.OrderByDescending(d => d.Date).ToArray()
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, exception = ex.Message });
        }
    }

    [HttpPost]
    async public Task<IActionResult> RemoveBranchDeploy(string id, string name, string branch)
    {
        try
        {
            await _branchDeployService.RemoveBranchDeployAsync(id, name, branch, this.GetCurrentUsername());

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, exception = ex.Message });
        }
    }

    #region Background Process

    private record BranchDeployJob(string Name, CmsGitBranchDeploy Handle, string TreePath, string ExportCacheFile, bool Full)
    {
        public override string ToString() => Name;
    }

    private void DeployCmsBranch(object arg)
    {
        BackgroundProcess process = (BackgroundProcess)arg;
        var job = (BranchDeployJob)process.UserData;

        try
        {
            var context = new CmsToolContext()
            {
                CmsId = process.CmsId,
                Deployment = job.Name,
                ContentRootPath = _applicationContentRootPath,
                Username = process.UserName,
                CmsTreePath = job.TreePath,
                Branch = CmsBranches.Encode(job.Handle.Branch),
                BranchName = job.Handle.Branch,
                Commit = job.Handle.Commit,
                Uncommitted = job.Handle.Uncommitted,
                ExportCacheFile = job.ExportCacheFile,
                ExportFull = job.Full,
                UncommittedFiles = job.Handle.UncommittedFiles,
                ChangedPathsSince = job.Handle.ChangedPathsSince
            };

            _deployService.Init(context);
            _deployService.Run(context, process);
        }
        finally
        {
            job.Handle.Dispose();
        }
    }

    private void DeployCms(object arg)
    {
        BackgroundProcess process = (BackgroundProcess)arg;

        try
        {
            var context = new CmsToolContext()
            {
                CmsId = process.CmsId,
                Deployment = process.UserData,
                ContentRootPath = _applicationContentRootPath,
                Username = process.UserName,
                CmsTreePath = _cmsResolver.DeployTreePath(process.CmsId)
            };

            _deployService.Init(context);
            _deployService.Run(context, process);
        }
        finally
        {
            if (_git.IsEnabled(process.CmsId))
            {
                _git.EndDeploy(process.CmsId);
            }
        }
    }

    #endregion Background Process

    public IActionResult SolveWarnings(string id, string name, bool branch = false)
    {
        try
        {
            _cmsLogger.Log(this.GetCurrentUsername(),
                           "Warnings", "Solve_Start", id, name, branch ? "branch" : String.Empty);

            _cmsResolver.EnsureEditableUserWorkspace(id, this.GetCurrentUsername());

            var backgroundProcess = new BackgroundProcess(id, this.GetCurrentUsername(), SolveCmsWarnings, new SolveWarningsJob(name, branch));

            return OpenConsole(backgroundProcess, "Solving: " + name, id);
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    #region Background Process

    private record SolveWarningsJob(string Name, bool Branch)
    {
        public override string ToString() => Name;
    }

    private void SolveCmsWarnings(object arg)
    {
        BackgroundProcess process = (BackgroundProcess)arg;
        var job = (SolveWarningsJob)process.UserData;

        var context = new CmsToolContext()
        {
            CmsId = process.CmsId,
            Deployment = job.Name,
            ContentRootPath = _applicationContentRootPath,
            Username = process.UserName,
            CmsTreePath = _cmsResolver.TreePath(process.CmsId, process.UserName),
            BranchWarnings = job.Branch
        };

        _solveWarningsService.Run(context, process);
    }

    #endregion Background Process

    #region Helper

    private CmsConfig.CmsItem DynamicCmsItem(string id)
    {
        _ccs.InitCustomCms(_servicePack, id);

        var cmsItem = new CmsConfig.CmsItem()
        {
            Id = id,
            Name = _ccs.CMS[id].CmsDisplayName,
            Scheme = _ccs.Instance.CustomCms.Scheme,
            Path = _ccs.CMS[id].ConnectionString,
            Deployments = new CmsConfig.DeployItem[]
            {
                            new CmsConfig.DeployItem()
                            {
                                Name=_ccs.CMS[id].CmsDisplayName,
                                Target=_ccs.Instance.CustomCms.RootUrl+"/"+id+"/cms.xml",
                                PostEvents=new CmsConfig.Events()
                                {
                                    HttpGet= new string[]
                                    {
                                        _ccs.Instance.CustomCms.RootTemplate
                                    }
                                }
                            }
            }
        };

        if (_ccs.Instance?.CustomCms?.HttpPostEvents != null)
        {
            var deployment = cmsItem.Deployments.First();
            deployment.PostEvents = new CmsConfig.Events()
            {
                HttpGet = _ccs.Instance.CustomCms.HttpPostEvents
                                .Select(h => h.Replace("{cmsid}", id))
                                .ToArray()
            };
        }

        return cmsItem;
    }

    private string PrintableUrl(string url, bool isDynamic)
    {
        try
        {
            if (isDynamic)
            {
                var uri = new Uri(url);
                url = uri.PathAndQuery;
            }
        }
        catch { }

        return url;
    }

    #endregion Helper
}