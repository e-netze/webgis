using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using E.Standard.Cms.Abstraction;
using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Extensions;
using E.Standard.CMS.Core.Abstractions;
using E.Standard.CMS.Core.Branches;
using E.Standard.Extensions.Security;
using E.Standard.Security.Cryptography.Services;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Models;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

namespace E.Standard.Cms.Services;

/// <summary>
/// Lists and removes branch deploys (deployments with allowBranchDeploy)
/// File target: {target-dir}/branches/{encoded-branch}/...
/// Url target: api endpoints cache/uploadbranches, cache/deletebranch (derived from the upload url)
/// </summary>
public class BranchDeployService
{
    private readonly CmsConfigurationService _ccs;
    private readonly IHttpService _http;
    private readonly IServiceProvider _serviceProvider;
    private readonly DeployService _deployService;
    private readonly ICmsLogger _cmsLogger;

    public BranchDeployService(
                CmsConfigurationService ccs,
                IHttpService http,
                IServiceProvider serviceProvider,
                DeployService deployService,
                ICmsLogger cmsLogger)
    {
        _ccs = ccs;
        _http = http;
        _serviceProvider = serviceProvider;
        _deployService = deployService;
        _cmsLogger = cmsLogger;
    }

    public IEnumerable<CmsConfig.DeployItem> BranchDeployments(string cmsId)
        => _ccs.Instance.CmsItems?
                .FirstOrDefault(i => i.Id == cmsId)?
                .Deployments?
                .Where(d => d.AllowBranchDeploy)
                .ToArray()
           ?? Array.Empty<CmsConfig.DeployItem>();

    public CmsConfig.DeployItem BranchDeployment(string cmsId, string deployName)
        => BranchDeployments(cmsId).FirstOrDefault(d => d.Name == deployName)
           ?? throw new Exception($"Branch deploy not allowed: {cmsId}/{deployName}");

    async public Task<IEnumerable<CmsBranchDeployInfo>> GetBranchDeploysAsync(string cmsId, string deployName)
    {
        var deploy = BranchDeployment(cmsId, deployName);

        if (deploy.Target.IsUrl())
        {
            var json = await _http.GetStringAsync(
                deploy.Target.ToCacheApiUrl("uploadbranches"),
                Authorization(cmsId, deploy),
                timeOutSeconds: 60);

            return JsonConvert.DeserializeObject<CmsBranchDeployInfo[]>(json) ?? Array.Empty<CmsBranchDeployInfo>();
        }

        return CmsBranches.ReadDeployInfos(deploy.Target);
    }

    async public Task RemoveBranchDeployAsync(string cmsId, string deployName, string encodedBranch, string username, IConsoleOutputStream? console = null)
    {
        if (!CmsBranches.IsValidEncoded(encodedBranch))
        {
            throw new Exception($"Invalid branch: {encodedBranch}");
        }

        var deploy = BranchDeployment(cmsId, deployName);

        if (deploy.Target.IsUrl())
        {
            await _http.PostValues(
                deploy.Target.ToCacheApiUrl("deletebranch", ("branch", encodedBranch)),
                Array.Empty<KeyValuePair<string, string>>(),
                Authorization(cmsId, deploy),
                timeOutSeconds: 60);
        }
        else if (!CmsBranches.DeleteBranch(deploy.Target, encodedBranch))
        {
            return;  // nothing deployed
        }

        _cmsLogger.Log(username, "Deploy", "RemoveBranch", cmsId, deploy.Name, encodedBranch);

        // e.g. cache/clear?id=...&branch={branch} => api removes the branch from the cache
        _deployService.RunPostEvents(deploy, encodedBranch, false, console);
    }

    /// <summary>
    /// Branch was deleted in the CMS => remove its deploys from all deployments (errors are only logged)
    /// </summary>
    async public Task RemoveBranchFromAllDeploymentsAsync(string cmsId, string branchName, string username)
    {
        if (String.IsNullOrWhiteSpace(branchName))
        {
            return;
        }

        var encodedBranch = CmsBranches.Encode(branchName);

        foreach (var deploy in BranchDeployments(cmsId))
        {
            try
            {
                if (deploy.Target.IsUrl() ||
                    CmsBranches.FindBranches(deploy.Target).Contains(encodedBranch))
                {
                    await RemoveBranchDeployAsync(cmsId, deploy.Name, encodedBranch, username);
                }
            }
            catch (Exception ex)
            {
                _cmsLogger.Log(username, "Deploy", "RemoveBranchFailed", cmsId, deploy.Name, encodedBranch, ex.Message);
            }
        }
    }

    private RequestAuthorization Authorization(string cmsId, CmsConfig.DeployItem deploy)
    {
        var jwtTokenService = _serviceProvider.GetRequiredKeyedService<JwtAccessTokenService>($"cms-upload-{cmsId}-{deploy.Name}");

        return new RequestAuthorization()
        {
            AuthType = "Bearer",
            AccessToken = jwtTokenService.GenerateToken(deploy.Client, 1)
        };
    }
}
