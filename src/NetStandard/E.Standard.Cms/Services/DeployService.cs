using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

using E.Standard.Cms.Abstraction;
using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Extensions;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Abstractions;
using E.Standard.CMS.Core.Branches;
using E.Standard.CMS.Core.Extensions;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.Extensions.Compare;
using E.Standard.Extensions.ErrorHandling;
using E.Standard.Extensions.Security;
using E.Standard.Platform;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Services;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Models;

using Microsoft.Extensions.DependencyInjection;

namespace E.Standard.Cms.Services;

public class DeployService : ICmsTool
{
    private readonly CmsConfigurationService _ccs;
    private readonly ICmsLogger _cmsLogger;
    private readonly IHttpService _http;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICryptoService _cryptoService;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public DeployService(
                CmsConfigurationService ccs,
                ICmsLogger cmsLogger,
                IHttpService http,
                IServiceProvider serviceProvider,
                ICryptoService cryptoService,
                CmsItemInjectionPackService instanceService
            )
    {
        _ccs = ccs;
        _cmsLogger = cmsLogger;
        _http = http;
        _serviceProvider = serviceProvider;
        _cryptoService = cryptoService;

        _servicePack = instanceService.ServicePack;
    }

    private JwtAccessTokenService? _jwtTokenService = null;
    public void Init(CmsToolContext context)
    {
        CmsConfig.CmsItem? cmsItem = _ccs.Instance.CmsItems.FirstOrDefault(i => i.Id == context.CmsId); ;
        var deploy = cmsItem?.Deployments.FirstOrDefault(d => d.Name == context.Deployment.ToString());

        if (deploy?.Target.IsUrl() == true)
        {
            _jwtTokenService = _serviceProvider.GetRequiredKeyedService<JwtAccessTokenService>($"cms-upload-{cmsItem!.Id}-{deploy.Name}");
        }
    }

    public bool Run(CmsToolContext context, IConsoleOutputStream console)
    {
        IPathInfo2? rootPathInfo2 = null;

        try
        {
            CmsConfig.CmsItem? cmsItem = null;
            bool isDynamicCms = false;

            if (_ccs.IsCustomCms(context.CmsId))
            {
                cmsItem = context.CmsId.ToDynamicCmsItem(_ccs, _servicePack);
                isDynamicCms = true;
            }
            else
            {
                cmsItem = _ccs.Instance.CmsItems.FirstOrDefault(i => i.Id == context.CmsId);
            }
            if (cmsItem == null)
            {
                throw new Exception("Unknown Cms-Item-Id: " + context.CmsId);
            }

            string cmsTreePath = context.CmsTreePath.OrTake(cmsItem.Path);

            var deploy = cmsItem.Deployments.FirstOrDefault(d => d.Name == context.Deployment.ToString());
            if (deploy == null)
            {
                throw new Exception($"Unknown deploy: {context.CmsId}/{context.Deployment}");
            }

            bool isBranchDeploy = !String.IsNullOrEmpty(context.Branch);
            if (isBranchDeploy)
            {
                if (!deploy.AllowBranchDeploy || isDynamicCms)
                {
                    throw new Exception($"Branch deploy not allowed: {context.CmsId}/{context.Deployment}");
                }
                if (!CmsBranches.IsValidEncoded(context.Branch!))
                {
                    throw new Exception($"Invalid branch: {context.Branch}");
                }

                console.WriteLine($"Branch: {context.BranchName} ({context.Branch})");
                if (!String.IsNullOrEmpty(context.Commit))
                {
                    console.WriteLine($"Commit: {context.Commit}");
                }
                if (context.Uncommitted)
                {
                    console.WriteLine($"Contains uncommitted changes (base commit: {context.Commit})");
                }
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(context.ContentRootPath, "schemes", cmsItem.Scheme, "schema.xml"));

            var cms = new CMSManager(doc);
            cms.SetConnectionString(_servicePack, cmsTreePath);

            rootPathInfo2 = DocumentFactory.PathInfo(cmsTreePath) as IPathInfo2;
            if (rootPathInfo2 != null)
            {
                console.WriteLine($"Loaded {rootPathInfo2.CacheAllRecursive()} nodes...");
            }

            int counter = 0;
            int stepWidth = SystemInfo.IsLinux ? 100 : 1000;
            DateTime currentTime = DateTime.Now;

            void ThrowIfCanceled()
            {
                if (console.IsCanceled)
                {
                    throw new OperationCanceledException();
                }
            }

            cms.OnExportNode += (object? sender, EventArgs e) =>
            {
                ThrowIfCanceled();
                counter++;
                if (counter % stepWidth == 0)
                {
                    if (e is CMSManager.ParseEventArgs)
                    {
                        CMSManager.ParseEventArgs pe = (CMSManager.ParseEventArgs)e;

                        console.WriteLine($"Processed {counter} nodes in {(int)(DateTime.Now - currentTime).TotalMilliseconds}ms");
                        currentTime = DateTime.Now;
                    }
                }
            };
            cms.OnMessage += (object? sender, EventArgs e) =>
            {
                if (e is CMSManager.ParseEventArgs)
                {
                    CMSManager.ParseEventArgs pe = (CMSManager.ParseEventArgs)e;

                    console.WriteLine($"Info {pe.NodeName}: {pe.FileName}");
                }
            };

            console.WriteLine($"Environment: {deploy.Environment}");
            console.WriteLine("============================================================");

            #region Init Replace Files and Secrets

            var replace = new CmsReplace();

            List<Action> replaceActions = new List<Action>()
                {
                    () => replace.AddCmsSecrets(cmsTreePath, deploy)
                };

            // add Relplacement File
            if (!String.IsNullOrEmpty(deploy.ReplacementFile))
            {
                replaceActions.Insert(deploy.ReplceSecretsFirst == true ? 1 : 0,
                    () => replace.AddReplacementFile(deploy.ReplacementFile));
            }

            foreach (var replaceAction in replaceActions)
            {
                replaceAction();
            }

            #endregion

            counter = 0;
            console.WriteLine("Export (incl. link check)");

            if (deploy.Services?.Any() == true)
            {
                console.WriteLine($"Service filter active: only the following services will be included: {String.Join(", ", deploy.Services)}");
            }

            ThrowIfCanceled();

            // single pass: link warnings are collected while exporting (no separate warnings scan)
            var warnings = new List<CMSManager.Warning>();
            var exportStatistics = new CMSManager.ExportStatistics();
            var exportCache = isBranchDeploy ? PrepareExportCache(context, cmsTreePath, console) : null;

            var document = cms.Export(_servicePack, deploy.IgnoreAuthentification, (ref string valueToEncrypt) =>
            {
                if (valueToEncrypt.ContainsSecretPlaceholders())
                {
                    //process.WriteLine("beforeEncryptValue " + valueToEncrypt);
                    valueToEncrypt = replace.ReplaceSecrets(valueToEncrypt);
                }
            }, deploy.Services, warnings, exportStatistics, fileCache: exportCache).GetAwaiter().GetResult();

            console.WriteLine($"Exported {exportStatistics}");

            if (exportCache != null)
            {
                SaveExportCache(context, exportCache, console);
            }

            ThrowIfCanceled();

            #region Warnings

            // branch deploys don't touch the warnings file of the production target (own file per user)
            var fiWarnings = isBranchDeploy
                ? deploy.Target.BranchWarningsFileInfo(context.Username)
                : deploy.Target.WarningsFileInfo();
            if (fiWarnings.Exists)
            {
                fiWarnings.Delete();
            }

            if (warnings.Count > 0)
            {
                bool hasCriticalWarnings = false;
                StringBuilder sbWarnings = new StringBuilder();
                foreach (var warning in warnings)
                {
                    //string line = warning.Message + ": " + warning.Filename.Substring(cms.ConnectionString.Length + 1).Replace(@"\", "/");
                    var path = warning.Path;
                    if (path.ToLower().StartsWith(cms.ConnectionString.ToLower()) ||
                        path.ToLower().Replace(@"\", "/").StartsWith(cms.ConnectionString.ToLower().Replace(@"\", "/")))
                    {
                        path = path.Substring(cms.ConnectionString.Length + 1);
                    }

                    string line = $"{warning.Level} - {warning.Message} : {path.Replace(@"\", "/")}";
                    console.WriteLine(line);

                    if (warning.Level == CMSManager.Warning.WaringLevel.Critical)
                    {
                        hasCriticalWarnings = true;
                        sbWarnings.AppendLine(line);
                    }
                }

                if (hasCriticalWarnings)
                {
                    if (isBranchDeploy)
                    {
                        sbWarnings.Insert(0, $"{E.Standard.Cms.Extensions.StringExtensions.BranchWarningsHeaderPrefix}{context.BranchName}{Environment.NewLine}");
                    }
                    System.IO.File.WriteAllText(fiWarnings.FullName, sbWarnings.ToString());
                    throw new Exception("Unsolved warnings found! Nothing was deployed.");
                }
            }

            #endregion

            #region Perform Replace

            if (replace.HasItems)
            {
                var replaceWatch = Stopwatch.StartNew();
                console.WriteLine("Replace...");

                var replaceItems = replace.ToCollection();
                foreach (string key in replaceItems.Keys)
                {
                    console.WriteLine($"  {key} => {(key.StartsWith("{{secret-") ? "***********" : replaceItems[key])}");
                }

                replace.ReplaceInXmlDocument(document);
                console.WriteLine($"Replaced in {replaceWatch.ElapsedMilliseconds}ms");
            }

            #endregion

            // last chance to cancel: once the target is written/uploaded, the deploy (incl. post events) is completed
            ThrowIfCanceled();

            var writeWatch = Stopwatch.StartNew();

            if (deploy.Target.IsUrl())
            {
                #region Upload Xml

                var token = _jwtTokenService?.GenerateToken(deploy.Client, 1);
                var uploadUrl = isBranchDeploy
                    ? deploy.Target.AppendBranchUploadParameters(context)
                    : deploy.Target;

                console.WriteLine($"Upload to {uploadUrl}");

                using (var memoryStream = new MemoryStream())
                {
                    document.Save(memoryStream);
                    memoryStream.Position = 0;

                    var base64 = Convert.ToBase64String(memoryStream.ToArray());
                    var encryptedBytes = Encoding.UTF8.GetBytes(_cryptoService.StaticEncrypt_Aes(
                                base64,
                                deploy.Secret,
                                Security.Cryptography.CryptoResultStringType.Hex));

                    if (!_http.UploadFileAsync(
                            uploadUrl,
                            encryptedBytes,
                        "cms.xml",
                        authorization: new RequestAuthorization()
                        {
                            AuthType = "Bearer",
                            AccessToken = token ?? "",
                        },
                        timeOutSeconds: 300).Result)
                    {
                        throw new Exception("Can't upload cms file");
                    }
                }

                console.WriteLine("Upload Succeeded (cache/clear included)");

                #endregion
            }
            else if (isBranchDeploy)
            {
                #region Save Branch Xml

                // {target-dir}/branches/{encoded-branch}/{target-filename} + deploy.json, no archive
                var branchFilePath = CmsBranches.BranchFilePath(deploy.Target, context.Branch!);

                console.WriteLine($"Write {branchFilePath}");
                CmsBranches.WriteBranch(deploy.Target, new CmsBranchDeployInfo()
                {
                    Branch = context.BranchName.OrTake(CmsBranches.TryDecode(context.Branch!)),
                    EncodedBranch = context.Branch,
                    User = context.Username,
                    Commit = context.Commit,
                    Uncommitted = context.Uncommitted,
                    Date = DateTime.UtcNow
                }, path => document.Save(path));

                #endregion

                _cmsLogger.Log(context.Username,
                               "Deploy", "SaveBranchXml", context.CmsId, deploy.Name, branchFilePath);
            }
            else
            {
                #region Save Xml

                FileInfo fi = new FileInfo(deploy.Target);

                #region Archive

                if (isDynamicCms == false)
                {
                    DirectoryInfo archiveDirectory = new DirectoryInfo(fi.Directory!.FullName + "/_archive");
                    if (!archiveDirectory.Exists)
                    {
                        archiveDirectory.Create();
                    }

                    if (fi.Exists)
                    {
                        string archiveFilename = archiveDirectory.FullName + "/" +
                            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + fi.Name;

                        console.WriteLine($"Archive {archiveFilename}");
                        fi.CopyTo(archiveFilename);
                    }
                }

                #endregion Archive

                console.WriteLine($"Write {(isDynamicCms ? context.CmsId + "/" + fi.Name : fi.FullName)}");
                document.Save(fi.FullName);

                #endregion

                _cmsLogger.Log(context.Username,
                               "Deploy", "SaveXml", context.CmsId, deploy.Name, fi.FullName);
            }

            console.WriteLine($"Written/uploaded in {writeWatch.ElapsedMilliseconds}ms");

            RunPostEvents(deploy, context.Branch ?? String.Empty, isDynamicCms, console);

            _cmsLogger.Log(context.Username,
                           "Deploy", "Succeeded", context.CmsId, deploy.Name, context.Branch ?? String.Empty);

            console.WriteLine("Succeeded");

            return true;
        }
        catch (OperationCanceledException) when (console.IsCanceled)
        {
            console.WriteLine("---------------------------------------------------------------------------");
            console.WriteLine("Canceled: nothing was deployed");
            console.WriteLine("---------------------------------------------------------------------------");

            _cmsLogger.Log(context.Username,
                           "Deploy", "Canceled", context.CmsId, context.Deployment.ToString() ?? String.Empty);

            return false;
        }
        catch (Exception ex)
        {
            console.WriteLine("---------------------------------------------------------------------------");
            console.WriteLines("EXCEPTION:");
            console.WriteLines(ex.FullMessage());
#if !DEBUG
            if (ex is NullReferenceException)
#endif
            {
                console.WriteLine("Stacktrace:");
                foreach (string stacktrace in ex.StackTrace?.Replace("\r", "").Split("\n") ?? [])
                {
                    console.WriteLine(stacktrace);
                }
            }
            console.WriteLine("---------------------------------------------------------------------------");

            _cmsLogger.Log(context.Username,
                           "Deploy", "Exception", context.CmsId, ex.Message);

            return false;
        }
        finally
        {
            if (rootPathInfo2 != null)
            {
                console.WriteLine("Release Cache");
                rootPathInfo2.ReleaseCacheRecursive();
            }
        }
    }

    #region Fast Deploy (export file snapshot)

    // returns the snapshot for the export: a valid (invalidated) snapshot for a fast deploy,
    // an empty one to be filled for a full read, or null if fast deploy is not available
    private static ExportFileCache? PrepareExportCache(CmsToolContext context, string cmsTreePath, IConsoleOutputStream console)
    {
        if (String.IsNullOrEmpty(context.ExportCacheFile) || context.ChangedPathsSince == null)
        {
            return null;
        }

        if (context.ExportFull)
        {
            console.WriteLine("Fast deploy: full read requested, snapshot will be rebuilt");
            return new ExportFileCache();
        }

        if (!File.Exists(context.ExportCacheFile))
        {
            console.WriteLine("Fast deploy: no snapshot yet, full read (snapshot will be created)");
            return new ExportFileCache();
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var cache = ExportFileCache.Load(context.ExportCacheFile);

            var changed = String.IsNullOrEmpty(cache.Commit) ? null : context.ChangedPathsSince(cache.Commit);
            if (changed == null)
            {
                console.WriteLine($"Fast deploy: snapshot commit {cache.Commit} not found, full read");
                return new ExportFileCache();
            }

            var changedPaths = new HashSet<string>(changed);
            changedPaths.UnionWith(context.UncommittedFiles ?? Array.Empty<string>());
            changedPaths.UnionWith(cache.UncommittedFiles);

            int removed = cache.Invalidate(cmsTreePath, changedPaths);

            console.WriteLine($"Fast deploy: snapshot from {cache.CreatedUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss} (commit {ShortSha(cache.Commit)}), {cache.FileCount} files, {cache.FolderCount} folders");
            console.WriteLine($"Fast deploy: {changedPaths.Count} changed paths, {removed} snapshot entries invalidated ({stopwatch.ElapsedMilliseconds}ms)");

            return cache;
        }
        catch (Exception ex)
        {
            console.WriteLine($"Fast deploy: snapshot not usable ({ex.Message}), full read");
            return new ExportFileCache();
        }
    }

    // the snapshot is written right after the export (also if the deploy is canceled later because of warnings)
    private static void SaveExportCache(CmsToolContext context, ExportFileCache cache, IConsoleOutputStream console)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            cache.Commit = context.Commit;
            cache.UncommittedFiles = context.UncommittedFiles ?? Array.Empty<string>();
            cache.CreatedUtc = DateTime.UtcNow;
            cache.Save(context.ExportCacheFile!);

            console.WriteLine($"Fast deploy: snapshot saved, {cache.FileCount} files, {cache.FolderCount} folders ({stopwatch.ElapsedMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            console.WriteLine($"Fast deploy: saving snapshot failed: {ex.Message}");
        }
    }

    private static string ShortSha(string? sha)
        => String.IsNullOrEmpty(sha) ? String.Empty : sha.Length > 8 ? sha.Substring(0, 8) : sha;

    #endregion

    // {branch} placeholder in commands/urls => encoded branch name (empty for production)
    public void RunPostEvents(CmsConfig.DeployItem deploy, string encodedBranch, bool isDynamicCms, IConsoleOutputStream? console)
    {
        #region PostEvents

        if (deploy.PostEvents != null)
        {
            if (deploy.PostEvents.Commands != null)
            {
                #region Console Commands

                foreach (string command in deploy.PostEvents.Commands.Select(c => c.Replace("{branch}", encodedBranch)))
                {
                    string fileName = command.CommandFileName();
                    string? arguments = command.CommandLineArguments();

                    Process cmdProcess = new Process();
                    cmdProcess.StartInfo = new ProcessStartInfo(fileName);
                    cmdProcess.StartInfo.UseShellExecute = false;
                    cmdProcess.StartInfo.RedirectStandardInput = true;
                    cmdProcess.StartInfo.RedirectStandardOutput = true;
                    cmdProcess.StartInfo.RedirectStandardError = true;
                    cmdProcess.StartInfo.Arguments = arguments;

                    console?.WriteLine($"Run: {command}");
                    cmdProcess.Start();
                    while (!cmdProcess.StandardOutput.EndOfStream)
                    {
                        console?.WriteLine(cmdProcess.StandardOutput.ReadLine());
                    }
                    cmdProcess.WaitForExit();

                    if (cmdProcess.ExitCode > 0)
                    {
                        throw new Exception(cmdProcess.StandardError.ReadToEnd());
                    }
                }

                #endregion Console Commands
            }
            if (deploy.PostEvents.HttpGet != null)
            {
                #region HttpGet

                foreach (string httpGetUrl in deploy.PostEvents.HttpGet.Select(u => u.Replace("{branch}", Uri.EscapeDataString(encodedBranch))))
                {
                    console?.WriteLine($"Http-Get: {httpGetUrl.ToPrintableUrl(isDynamicCms)}");

                    var response = _http.GetStringAsync(httpGetUrl,
                        new RequestAuthorization()
                        {
                            UseDefaultCredentials = true
                        },
                        timeOutSeconds: 300).Result;

                    if (response.Contains("<") && response.Contains(">"))
                    {
                        response = "Html Response...";
                    }
                    console?.WriteLine($"        -> {response}");
                }

                #endregion HttpGet
            }
        }

        #endregion PostEvents
    }
}
