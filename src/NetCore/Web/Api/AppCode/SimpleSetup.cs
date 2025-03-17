using E.Standard.Configuration.Extensions;
using E.Standard.Extensions.ErrorHandling;
using E.Standard.Platform;
using E.Standard.WebGIS.Core.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Api.Core.AppCode;

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

            var fiConfig = new FileInfo(Path.Combine("_config", "api.config"));

            bool initalializeRepository =
                fiConfig.Exists == false  // first startup => create all new
                || RespositoryRequiresInitialization();

            if (!fiConfig.Exists)
            {
                var fi = new FileInfo(Path.Combine("_setup", "proto", "_api.config"));
                if (fi.Exists)
                {
                    string configContent = String.Empty;

                    Console.WriteLine("#############################################################################################################");
                    Console.WriteLine("Setup:");
                    Console.WriteLine("First start: creating simple _config/api.config. You can modify this file for your production settings...");
                    Console.WriteLine("#############################################################################################################");

                    if (SystemInfo.IsWindows)
                    {
                        configContent = WindowsSetup(fi.FullName, args);
                    }
                    else if (SystemInfo.IsLinux)
                    {
                        configContent = LinuxSetup(fi.FullName, args);
                    }

                    if (!String.IsNullOrEmpty(configContent))
                    {
                        Console.WriteLine(configContent);

                        var configTargetes = new List<string>(new string[] { $"{fiConfig.Directory.FullName}/api.config" });
#if DEBUG || DEBUG_INTERNAL
                        configTargetes.Add($"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/_config/api.config");
#endif

                        foreach (var configTarget in configTargetes)
                        {
                            fiConfig = new FileInfo(configTarget);

                            if (!fiConfig.Directory.Exists)
                            {
                                fiConfig.Directory.Create();
                            }

                            File.WriteAllText(fiConfig.FullName, configContent);
                        }
                    }
                }
            }

            if (initalializeRepository)
            {
                var protoRepository = new DirectoryInfo(Path.Combine("_setup", "proto", "_webgis-repository"));

                string repositoryPath = RepositoryPath(new FileInfo(Path.Combine("_setup", "proto", "_api.config")).FullName);

                Console.WriteLine($"Initialize Repository: {repositoryPath}");
                DirectoryCopy(
                    protoRepository.FullName,
                    repositoryPath,
                    true);

                string outputDirectory = Path.Combine(repositoryPath, "output");
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Warning: can't intialize configuration for first start:");
            Console.WriteLine(ex.SecureMessage());

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

#if DEBUG
        string apiHost = "https://localhost:44341";
#else
        string apiHost = "http://localhost:5001";
#endif

        if (!String.IsNullOrEmpty(args.GetArgumentValue("-expose-https")))
        {
            apiHost = $"https://localhost:{args.GetArgumentValue("-expose-https")}";
        }
        else if (!String.IsNullOrEmpty(args.GetArgumentValue("-expose-http")))
        {
            apiHost = $"http://localhost:{args.GetArgumentValue("-expose-http")}";
        }

#if DEBUG || DEBUG_INTERNAL
        string portalHost = args.GetArgumentValue("-portal-url") ?? "https://localhost:44320";
#else
        string portalHost = args.GetArgumentValue("-portal-url") ?? "http://localhost:5002";
#endif

#if DEBUG || DEBUG_INTERNAL
        string company = "dev";
#else
        string company = "my-company";
#endif

        var configText = File.ReadAllText(fi.FullName);
        configText = configText.Replace("{api-repository-path}", RepositoryPath(configTemplateFile))
                               .Replace("{api-onlineresource}", apiHost)
                               .Replace("{portal-onlineresource}", portalHost)
                               .Replace("{portal-internal-url}", portalHost)
                               .Replace("{company}", company);

        return configText;
    }

    #endregion

    #region Linux

    private const string EnvKey_ApiRespositoryPath = "API_REPOSITORY_PATH";
    private const string EnvKey_InitializeRepository = "INITALIZE_REPOSITORY";
    private const string EnvKey_ApiOnlineResourceUrl = "API_ONLINERESOURCE_URL";
    private const string EnvKey_PortalOnlineResourceUrl = "PORTAL_ONLINERESOURCE_URL";
    private const string EnvKey_PortalInternalUrl = "PORTAL_INTERNAL_URL";
    private const string EnvKey_ConfigRootPath = "API_CONFIG_ROOT_PATH";
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
        string apiOnlineResource = GetEnvironmentVariable(EnvKey_ApiOnlineResourceUrl) ?? "http://localhost:5001";
        string portalOnlineResource = GetEnvironmentVariable(EnvKey_PortalOnlineResourceUrl) ?? "http://localhost:5002";
        string portalInternalUrl = GetEnvironmentVariable(EnvKey_PortalInternalUrl) ?? "http://webgis-portal:8080";
        string company = GetEnvironmentVariable(EnvKey_Company)
    ??
#if DEBUG || DEBUG_INTERNAL
            "dev";
#else
            "my-company";
#endif

        var fi = new FileInfo(configTemplateFile);

        var configText = File.ReadAllText(fi.FullName);
        configText = configText.Replace("{api-repository-path}", apiRepositoryPath)
                               .Replace("{api-onlineresource}", apiOnlineResource)
                               .Replace("{portal-onlineresource}", portalOnlineResource)
                               .Replace("{portal-internal-url}", portalInternalUrl)
                               .Replace("{company}", company);

        return configText;
    }

    #endregion

    #region Helper

    private bool RespositoryRequiresInitialization()
    {
        var repoPath = GetEnvironmentVariable(EnvKey_ApiRespositoryPath);

        if (String.IsNullOrEmpty(repoPath) || GetEnvironmentVariable(EnvKey_InitializeRepository) != "true")  // Environment variable must be set (in kubernetes YAML)
        {
            return false;
        }

        if (!File.Exists(Path.Combine(repoPath, "api.initialized")))
        {
            Directory.CreateDirectory(repoPath);
            File.WriteAllText(Path.Combine(repoPath, "api.initialized"), "done");
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

    private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        if (!dir.Exists)
        {
            return;
        }

        Console.WriteLine($"Copy directory: {sourceDirName} => {destDirName}");

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
                string tempPath = Path.Combine(destDirName, subdir.Name);
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
