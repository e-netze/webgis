using E.Standard.Configuration.Extensions;
using E.Standard.Platform;
using E.Standard.WebGIS.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Reflection;

namespace Portal.Core.AppCode;

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

            var fiConfig = new FileInfo("_config/portal.config");
            if (!fiConfig.Exists)
            {
                var fi = new FileInfo("_setup/proto/_portal.config");
                if (fi.Exists)
                {
                    string configContent = String.Empty;

                    Console.WriteLine("#############################################################################################################");
                    Console.WriteLine("Setup:");
                    Console.WriteLine("First start: creating simple _config/portal.config. You can modify this file for your production settings...");
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

                        var configTargetes = new List<string>(new string[] { $"{fiConfig.Directory.FullName}/portal.config" });
#if DEBUG || DEBUG_INTERNAL
                        configTargetes.Add($"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/_config/portal.config");
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

#if DEBUG
        string portalHost = "https://localhost:44320";
#else
        string portalHost = "http://localhost:5002";
#endif
        if (!String.IsNullOrEmpty(args.GetArgumentValue("-expose-https")))
        {
            portalHost = $"https://localhost:{args.GetArgumentValue("-expose-https")}";
        }
        else if (!String.IsNullOrEmpty(args.GetArgumentValue("-expose-http")))
        {
            portalHost = $"http://localhost:{args.GetArgumentValue("-expose-http")}";
        }

#if DEBUG || DEBUG_INTERNAL
        string apiHost = args.GetArgumentValue("-api-url") ?? "https://localhost:44341";
#else
        string apiHost = args.GetArgumentValue("-api-url") ?? "http://localhost:5001";
#endif

#if DEBUG || DEBUG_INTERNAL
        string company = "dev";
#else
        string company = "my-company";
#endif

        var configText = File.ReadAllText(fi.FullName);
        configText = configText.Replace("{api-repository-path}", RepositoryPath(new FileInfo(Path.Combine("_setup", "proto", "_portal.config")).FullName))
                               .Replace("{api-onlineresource}", apiHost)
                               .Replace("{api-internal-url}", apiHost)
                               .Replace("{portal-onlineresource}", portalHost)
                               .Replace("{company}", company);

        return configText;
    }

    #endregion

    #region Linux

    private const string EnvKey_ApiRespositoryPath = "API_REPOSITORY_PATH";
    private const string EnvKey_ApiOnlineResourceUrl = "API_ONLINERESOURCE_URL";
    private const string EnvKey_ApiInternalUrl = "API_INTERNAL_URL";
    private const string EnvKey_PortalOnlineResourceUrl = "PORTAL_ONLINERESOURCE_URL";
    private const string EnvKey_ConfigRootPath = "PORTAL_CONFIG_ROOT_PATH";
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
        string apiRepositoryPath = GetEnvironmentVariable(EnvKey_ApiRespositoryPath) ?? "/etc/webgis";
        string apiOnlineResource = GetEnvironmentVariable(EnvKey_ApiOnlineResourceUrl) ?? "http://localhost:5001";
        string apiInternalUrl = GetEnvironmentVariable(EnvKey_ApiInternalUrl) ?? "http://webgis-api:8080";
        string portalOnlineResource = GetEnvironmentVariable(EnvKey_PortalOnlineResourceUrl) ?? "http://localhost:5002";
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
                               .Replace("{api-internal-url}", apiInternalUrl)
                               .Replace("{portal-onlineresource}", portalOnlineResource)
                               .Replace("{company}", company);

        return configText;
    }

#endregion

    #region Helper

    private string GetEnvironmentVariable(string name)
    {
        var environmentVariables = Environment.GetEnvironmentVariables();

        if (environmentVariables.Contains(name) && !String.IsNullOrWhiteSpace(environmentVariables[name]?.ToString()))
        {
            return environmentVariables[name]?.ToString();
        }

        return null;
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
