using E.Standard.Configuration.Extensions;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Extensions;
using E.Standard.WebGIS.Core.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Cms.AppCode;

public class SimpleSetup
{
    public bool TrySetup(string[] args)
    {
        try
        {
            if (SystemInfo.IsWindows)
            {
                WindowPreSetup();
            }
            else if (SystemInfo.IsLinux)
            {
                LinuxPreSetup();
            }

            var fiCmsConfig = new FileInfo(Path.Combine("_config", "cms.config"));

            bool initalializeRepository =
                fiCmsConfig.Exists == false  // first startup => create all new
                || RespositoryRequiresInitialization();

            if (!fiCmsConfig.Exists)
            {
                var fiProtoCmsConfig = new FileInfo(Path.Combine("_setup", "proto", "__cms.config"));

                if (fiProtoCmsConfig.Exists)
                {
                    string configContent = String.Empty;

                    Console.WriteLine("#############################################################################################################");
                    Console.WriteLine("Setup:");
                    Console.WriteLine("First start: creating simple _config/cms.config. You can modify this file for your production settings...");
                    Console.WriteLine("#############################################################################################################");

                    if (SystemInfo.IsWindows)
                    {
                        configContent = WindowsSetup(fiProtoCmsConfig.FullName, args);
                    }
                    else if (SystemInfo.IsLinux)
                    {
                        configContent = LinuxSetup(fiProtoCmsConfig.FullName, args);
                    }

                    WriteConfigFiles(fiCmsConfig, configContent);
                }
            }

            if (initalializeRepository)
            {
                var protoRepository = new DirectoryInfo(Path.Combine("_setup", "proto", "_webgis-repository"));
                var targetRepository = new DirectoryInfo(RepositoryPath(new FileInfo(Path.Combine("_setup", "proto", "__cms.config")).FullName));

                //if (protoRepository.Exists)
                {
                    Console.WriteLine("Initialize Repository");
                    DirectoryCopy(protoRepository.FullName, targetRepository.FullName, true);
                }
            }

#if DEBUG || DEBUG_INTERNAL
            #region Integrate datalinq.config

            var fiDatalinqConfig = new FileInfo(Path.Combine("_config", "datalinq.config"));

            if (!fiDatalinqConfig.Exists)
            {
                var fiProtoDataLinqConfig = new FileInfo(Path.Combine("_setup", "proto", "__datalinq.config"));

                if (fiProtoDataLinqConfig.Exists)
                {
                    string configContent = String.Empty;

                    Console.WriteLine("#############################################################################################################");
                    Console.WriteLine("creating simple _config/datalinq.config. You can modify this file for your production settings...");
                    Console.WriteLine("#############################################################################################################");


                    if (SystemInfo.IsWindows)
                    {
                        configContent = WindowsSetup(fiProtoDataLinqConfig.FullName, args);
                    }
                    else if (SystemInfo.IsLinux)
                    {
                        configContent = LinuxSetup(fiProtoDataLinqConfig.FullName, args);
                    }

                    WriteConfigFiles(fiDatalinqConfig, configContent);
                }
            }

            #endregion
#endif

        }
        catch (Exception ex)
        {
            Console.WriteLine("Warning: can't intialize configuration for first start:");
            Console.WriteLine(ex.Message);

            return false;
        }

        return true;
    }

    #region Windows

    private void WindowPreSetup()
    {

    }

    private string WindowsSetup(string configTemplateFile, string[] args)
    {
        var fi = new FileInfo(configTemplateFile);

#if DEBUG || DEBUG_INTERNAL
        string apiHost = args.GetArgumentValue("-api-url") ?? "https://localhost:44341";
        string portalHost = args.GetArgumentValue("-portal-url") ?? "https://localhost:44320";
#else
        string apiHost = args.GetArgumentValue("-api-url") ?? "http://localhost:5001";
        string portalHost = args.GetArgumentValue("-portal-url") ?? "http://localhost:5002";
#endif

#if DEBUG || DEBUG_INTERNAL
        string company = "dev";
#else
        string company = "my-company";
#endif

        var configText = File.ReadAllText(fi.FullName);
        configText = configText.Replace("{api-repository-path}", RepositoryPath(new FileInfo(Path.Combine("_setup", "proto", "__cms.config")).FullName).Replace(@"\", @"\\"))
                               .Replace("{api-internal-url}", apiHost.Replace(@"\", @"\\"))
                               .Replace("{api-onlineresource}", apiHost.Replace(@"\", @"\\"))
                               .Replace("{portal-internal-url}", portalHost.Replace(@"\", @"\\"))
                               .Replace("{company}", company);

        return configText;
    }

    #endregion

    #region Linux

    private const string EnvKey_ApiRespositoryPath = "API_REPOSITORY_PATH";
    private const string EnvKey_ApiOnlineResourceUrl = "API_INTERNAL_URL";
    private const string EnvKey_InitializeRepository = "INITALIZE_REPOSITORY";
    private const string EnvKey_PortalOnlineResourceUrl = "PORTAL_INTERNAL_URL";
    private const string EnvKey_Username = "CMS_USERNAME";
    private const string EnvKey_Password = "CMS_PASSWORD";
    private const string EnvKey_ConfigRootPath = "CMS_CONFIG_ROOT_PATH";
    private const string EnvKey_Company = "COMPANY";

    private void LinuxPreSetup()
    {
        string configRootPath = GetEnvironmentVariable(EnvKey_ConfigRootPath);

        if (!String.IsNullOrEmpty(configRootPath))
        {
            var configPath = new DirectoryInfo("_config");
            var targetConfigPath = new DirectoryInfo(configRootPath);

            configPath.CreateTargetFileRedirections(targetConfigPath);
        }
    }

    private string LinuxSetup(string configTemplateFile, string[] args)
    {
        string apiRepositoryPath = RepositoryPath(configTemplateFile);
        string apiInternalUrl = GetEnvironmentVariable(EnvKey_ApiOnlineResourceUrl) ?? "http://webgis-api";
        string apiOnlineResource = GetEnvironmentVariable(EnvKey_ApiOnlineResourceUrl) ?? "http://localhost:5001";
        string portalInternalUrl = GetEnvironmentVariable(EnvKey_PortalOnlineResourceUrl) ?? "http://webgis-portal";
        string company = GetEnvironmentVariable(EnvKey_Company)
            ??
#if DEBUG || DEBUG_INTERNAL
            "dev";
#else
            "my-company";
#endif
        string username = GetEnvironmentVariable(EnvKey_Username);
        string password = GetEnvironmentVariable(EnvKey_Password);

        if (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password))
        {
            var appSecurityConfig = new ApplicationSecurityConfig()
            {
                Users = new ApplicationSecurityConfig.User[]
                {
                    new ApplicationSecurityConfig.User()
                    {
                        Name = username,
                        Password = password.Hash64()
                    }
                }
            };

            var appSecurityConfigFileInfo = new FileInfo("_config/application-security.config");
            File.WriteAllText(appSecurityConfigFileInfo.FullName, JSerializer.Serialize(appSecurityConfig));
        }

        var fi = new FileInfo(configTemplateFile);

        var configText = File.ReadAllText(fi.FullName);
        configText = configText.Replace("{api-repository-path}", apiRepositoryPath.Replace(@"\", @"\\"))
                               .Replace("{api-internal-url}", apiInternalUrl.Replace(@"\", @"\\"))
                               .Replace("{api-onlineresource}", apiOnlineResource.Replace(@"\", @"\\"))
                               .Replace("{portal-internal-url}", portalInternalUrl.Replace(@"\", @"\\"))
                               .Replace("{company}", company);

        return configText;
    }

    #endregion

    #region Helper

    private bool RespositoryRequiresInitialization()
    {
        var repoPath = GetEnvironmentVariable(EnvKey_ApiRespositoryPath);

        if (String.IsNullOrEmpty(repoPath) || GetEnvironmentVariable(EnvKey_InitializeRepository) == "true")  // Environment variable must be set (in kubernetes YAML)
        {
            return false;
        }

        if (!File.Exists(Path.Combine(repoPath, "cms.initialized")))
        {
            Directory.CreateDirectory(repoPath);
            File.WriteAllText(Path.Combine(repoPath, "cms.initialized"), "done");
            return true;
        }
        ;

        return false;
    }

    private string GetEnvironmentVariable(string name)
    {
        var environmentVariables = Environment.GetEnvironmentVariables();

        if (environmentVariables.Contains(name) && !String.IsNullOrWhiteSpace(environmentVariables[name]?.ToString()))
        {
            return environmentVariables[name]?.ToString();
        }

        return null;
    }

    private void WriteConfigFiles(
            FileInfo fi,
            string configContent)
    {
        if (!String.IsNullOrEmpty(configContent))
        {
            Console.WriteLine(configContent);

            var fileName = fi.Name;
            var configTargetes = new List<string>(new string[] { $"{fi.Directory.FullName}/{fileName}" });
#if DEBUG || DEBUG_INTERNAL
            configTargetes.Add($"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/_config/{fileName}");
#endif

            foreach (var configTarget in configTargetes)
            {
                var targetFi = new FileInfo(configTarget);

                if (!targetFi.Directory.Exists)
                {
                    targetFi.Directory.Create();
                }

                File.WriteAllText(targetFi.FullName, configContent);
            }
        }
    }

    private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        if (!dir.Exists)
        {
            return;
        }

        Console.WriteLine($"Copy Directory: {sourceDirName} => {destDirName}");

        DirectoryInfo[] dirs = dir.GetDirectories();

        if (new DirectoryInfo(destDirName).Exists == false)
        {
            Directory.CreateDirectory(destDirName);
        }

        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destDirName, file.Name);
            if (new FileInfo(tempPath).Exists == false)
            {
                file.CopyTo(tempPath, false);
            }
        }

        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string subdirName = SystemInfo.IsLinux ? subdir.Name.ToLower() : subdir.Name;

                string tempPath = Path.Combine(destDirName, subdirName);
                DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
            }
        }
    }

    private string RepositoryPath(string configTemplateFile)
    {
        if (SystemInfo.IsWindows)
        {
            var fi = new FileInfo(configTemplateFile);
#if DEBUG || DEBUG_INTERNAL
            var srcDirectory = new DirectoryInfo(fi.Directory.FullName.Substring(0, fi.Directory.FullName.IndexOf(@"\src\")));
            return Path.Combine(srcDirectory.Parent.FullName, "webgis-repository");
#else
            return Path.Combine(fi.Directory.Parent.Parent.Parent.FullName, "webgis-repository");
#endif
        }
        else if (SystemInfo.IsLinux)
        {
            return GetEnvironmentVariable(EnvKey_ApiRespositoryPath) ?? "/etc/webgis";
        }
        throw new Exception("SimpleSetup: Unsupported OS!");
    }

    #endregion
}
