using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.Cryptography.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Custom.Core.Extensions;

static public class CustomStartupServiceExtensions
{
    static public void ConfigureServices(this IEnumerable<ICustomStartupService> customStartupServices, IServiceCollection services, IConfiguration configuration)
    {
        if (customStartupServices != null)
        {
            foreach (var customStartupService in customStartupServices)
            {
                customStartupService.ConfigureServices(services, configuration);
            }
        }
    }

    static public bool ImplementsCryptographyService(this IEnumerable<ICustomStartupService> customStartupServices, IConfiguration configuration)
    {
        if (customStartupServices != null)
        {
            if (customStartupServices.Where(c => c.ImplementsCryptoService(configuration) == true).Count() > 0)
            {
                return true;
            }
        }

        return false;
    }

    static public CryptoServiceOptions GetCryproServiceOptions(this IEnumerable<ICustomStartupService> customStartupServices, IConfiguration configuration)
    {
        if (customStartupServices == null)
        {
            return null;
        }

        foreach (var customStartupService in customStartupServices)
        {
            if (customStartupService.ImplementsCryptoService(configuration))
            {
                var cryptoOptions = customStartupService.GetCryptoServiceOptions();
                if (cryptoOptions != null)
                {
                    return cryptoOptions;
                }
            }
        }

        return null;
    }
}
