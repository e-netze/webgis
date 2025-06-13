using E.Standard.Api.App.Configuration;
using E.Standard.Configuration.Services;
using E.Standard.Extensions.IO;
using E.Standard.Platform;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Api.Core.AppCode.Services;

// Singleton Service
// => to inialize often used keys only once => Performance
public class ApiConfigurationService
{
    public ApiConfigurationService(ConfigurationService config)
    {
        this.InstanceRoles = GetInstanceRoles(config).ToArray();
        this.AllowBranches = GetAllowBranches(config);
        this.ServiceSideConfigurationPath = GetServiceSideConfigurationPath(config).ToPlatformPath();

        this.StorageRootPath = GetStorageRootPath(config).HasHttpUrlSchema()
            ? GetStorageRootPath(config)
            : GetStorageRootPath(config).ToPlatformPath();
        this.StorageRootPath2 = GetStorageRootPath2(config).HasHttpUrlSchema()
            ? GetStorageRootPath2(config) 
            : GetStorageRootPath2(config).ToPlatformPath();

        this.UseClearOuputBackgroundTask = GetUseClearOuputBackgroundTask(config);
        this.UseClearSharedMapsBackgroundTask = GetUseClearSharedMapsBackgroundTask(config);

        this.OutputPath = GetOutputPath(config).HasHttpUrlSchema()
            ? GetOutputPath(config)
            : GetOutputPath(config).ToPlatformPath();
    }

    public string[] InstanceRoles { get; private set; }
    public bool AllowBranches { get; private set; }
    public string ServiceSideConfigurationPath { get; private set; }

    #region Storage

    public string StorageRootPath { get; private set; }

    public string StorageRootPath2 { get; private set; }

    #endregion

    #region Background Tasks

    public bool UseClearOuputBackgroundTask { get; private set; }

    public bool UseClearSharedMapsBackgroundTask { get; private set; }

    #endregion

    #region Paths 

    public string OutputPath { get; private set; }

    #endregion

    #region Initialize Properties

    private IEnumerable<string> GetInstanceRoles(ConfigurationService config)
    {
        string instaceRolesString = config[ApiConfigKeys.InstanceRoles];

        if (String.IsNullOrWhiteSpace(instaceRolesString))
        {
            return new string[0];
        }

        return instaceRolesString
                    .Split(',')
                    .Select(a => a.Trim())
                    .Where(a => !String.IsNullOrEmpty(a));
    }

    public bool GetAllowBranches(ConfigurationService config)
    {
        if (bool.TryParse(config[$"{ApiConfigKeys.AllowBranches}"], out bool allow))
        {
            return allow;
        }

        return false;
    }

    public string GetServiceSideConfigurationPath(ConfigurationService config)
        => config[ApiConfigKeys.ServerSideConfigurationPath];

    #region Storage

    private string GetStorageRootPath(ConfigurationService config)
    {
        return config[ApiConfigKeys.StorageRootPath];
    }

    private string GetStorageRootPath2(ConfigurationService config)
    {
        return config[ApiConfigKeys.StorageRootPath2];
    }

    #endregion

    #region Background Tasks

    public bool GetUseClearOuputBackgroundTask(ConfigurationService config)
    {
        return config[ApiConfigKeys.BackgroundTaskClearOuput] != "false";
    }

    public bool GetUseClearSharedMapsBackgroundTask(ConfigurationService config)
    {
        return config[ApiConfigKeys.BackgroundTaskClearSharedMaps] != "false";
    }

    #endregion

    #region Paths 

    public string GetOutputPath(ConfigurationService config)
    {
        return config[ApiConfigKeys.OutputPath];
    }

    #endregion

    #endregion
}
