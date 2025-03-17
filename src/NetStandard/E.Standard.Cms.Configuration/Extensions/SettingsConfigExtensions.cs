using E.Standard.Cms.Configuration.Models;
using System;
using System.Linq;
using System.Net;

namespace E.Standard.Cms.Configuration.Extensions;

static public class SettingsConfigExtensions
{
    static public WebProxy GetWebProxy(this SettingsConfig settingsConfig)
    {
        try
        {
            if (settingsConfig.Proxy != null && settingsConfig.Proxy.Use)
            {
                var webProxy = new WebProxy(settingsConfig.Proxy.Server, settingsConfig.Proxy.Port);

                if (!String.IsNullOrEmpty(settingsConfig.Proxy.User))
                {
                    System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(
                        settingsConfig.Proxy.User,
                        settingsConfig.Proxy.Password,
                        settingsConfig.Proxy.Domain);

                    webProxy.Credentials = credentials;
                }

                return webProxy;
            }
        }
        catch
        {

        }

        return null;
    }

    static public string[] WebProxyIgnores(this SettingsConfig settingsConfig)
    {
        if (!String.IsNullOrEmpty(settingsConfig?.Proxy?.Ignore))
        {
            return settingsConfig.Proxy.Ignore.Replace(",", ";")
                                        .Split(';')
                                        .Select(s => s.Trim())
                                        .ToArray();
        }

        return null;
    }
}
