using E.Standard.Cms.Abstraction;
using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Extensions;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Abstractions;
using E.Standard.CMS.Core.Extensions;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.Extensions.Compare;
using E.Standard.Extensions.ErrorHandling;
using E.Standard.Extensions.Security;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Services;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

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
        CmsConfig.CmsItem? cmsItem = _ccs.Instance.CmsItems.Where(i => i.Id == context.CmsId).FirstOrDefault(); ;
        var deploy = cmsItem?.Deployments.Where(d => d.Name == context.Deployment.ToString()).FirstOrDefault();

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
                cmsItem = _ccs.Instance.CmsItems.Where(i => i.Id == context.CmsId).FirstOrDefault();
            }
            if (cmsItem == null)
            {
                throw new Exception("Unknown Cms-Item-Id: " + context.CmsId);
            }

            string cmsTreePath = context.CmsTreePath.OrTake(cmsItem.Path);

            var deploy = cmsItem.Deployments.Where(d => d.Name == context.Deployment.ToString()).FirstOrDefault();
            if (deploy == null)
            {
                throw new Exception($"Unknown deploy: {context.CmsId}/{context.Deployment}");
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
            cms.OnParseWaring += (object? sender, EventArgs e) =>
            {
                counter++;
                if (counter % 1000 == 0)
                {
                    if (e is CMSManager.ParseEventArgs)
                    {
                        CMSManager.ParseEventArgs pe = (CMSManager.ParseEventArgs)e;

                        console.WriteLine("scanned " + counter + " nodes");
                    }
                }
            };
            cms.OnExportNode += (object? sender, EventArgs e) =>
            {
                counter++;
                if (counter % 1000 == 0)
                {
                    if (e is CMSManager.ParseEventArgs)
                    {
                        CMSManager.ParseEventArgs pe = (CMSManager.ParseEventArgs)e;

                        console.WriteLine("Processed " + counter + " nodes");
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

            console.WriteLine("Scann for warnings");
            var warnings = cms.Warnings();

            var fiWarnings = deploy.Target.WarningsFileInfo();
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
                    System.IO.File.WriteAllText(fiWarnings.FullName, sbWarnings.ToString());
                    throw new Exception("Unsolved warnings found!");
                }
            }

            #region Init Replace Files and Secrets

            var replace = new CmsReplace();

            List<Action> replaceActions = new List<Action>()
                {
                    () => replace.AddCmsSecrets(cmsItem,deploy)
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
            console.WriteLine("Export");
            var document = cms.Export(_servicePack, deploy.IgnoreAuthentification, (ref string valueToEncrypt) =>
            {
                if (valueToEncrypt.ContainsSecretPlaceholders())
                {
                    //process.WriteLine("beforeEncryptValue " + valueToEncrypt);
                    valueToEncrypt = replace.ReplaceSecrets(valueToEncrypt);
                }
            }).Result;

            #region Perform Replace

            if (replace.HasItems)
            {
                console.WriteLine("Replace...");

                var replaceItems = replace.ToCollection();
                foreach (string key in replaceItems.Keys)
                {
                    console.WriteLine($"  {key} => {(key.StartsWith("{{secret-") ? "***********" : replaceItems[key])}");
                }

                replace.ReplaceInXmlDocument(document);
            }

            #endregion

            if (deploy.Target.IsUrl())
            {
                #region Upload Xml

                var token = _jwtTokenService?.GenerateToken(deploy.Client, 1);

                console.WriteLine($"Upload to {deploy.Target}");

                using (var memoryStream = new MemoryStream())
                {
                    document.Save(memoryStream);
                    memoryStream.Position = 0;

                    var base64 = Convert.ToBase64String(memoryStream.ToArray());
                    var encryptedBytes = Encoding.UTF8.GetBytes(_cryptoService.StaticEncrypt(
                                base64,
                                deploy.Secret,
                                Security.Cryptography.CryptoResultStringType.Hex));

                    if (!_http.UploadFileAsync(
                            deploy.Target,
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
            #region PostEvents

            if (deploy.PostEvents != null)
            {
                if (deploy.PostEvents.Commands != null)
                {
                    #region Console Commands

                    foreach (string command in deploy.PostEvents.Commands)
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

                        console.WriteLine($"Run: {command}");
                        cmdProcess.Start();
                        while (!cmdProcess.StandardOutput.EndOfStream)
                        {
                            console.WriteLine(cmdProcess.StandardOutput.ReadLine());
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

                    foreach (string httpGetUrl in deploy.PostEvents.HttpGet)
                    {
                        console.WriteLine($"Http-Get: {httpGetUrl.ToPrintableUrl(isDynamicCms)}");

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
                        console.WriteLine($"        -> {response}");
                    }

                    #endregion HttpGet
                }
            }

            #endregion PostEvents

            _cmsLogger.Log(context.Username,
                           "Deploy", "Succeeded", context.CmsId, deploy.Name);

            console.WriteLine("Succeeded");

            return true;
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
}
