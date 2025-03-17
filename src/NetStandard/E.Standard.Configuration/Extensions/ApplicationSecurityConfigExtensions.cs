using E.Standard.Security.App.Json;

namespace E.Standard.Configuration.Extensions;

static public class ApplicationSecurityConfigExtensions
{
    static public ApplicationSecurityConfig LoadFromJsonFile(this ApplicationSecurityConfig applcationSecurityConfig)
    {
        var appConfig = new JsonAppConfiguration("application-security.config");

        if (appConfig.Exists)
        {
            applcationSecurityConfig.MemberwiseCopy(appConfig.Deserialize<ApplicationSecurityConfig>());
            applcationSecurityConfig.UseApplicationSecurity = true;
        }
        else
        {
            applcationSecurityConfig.UseApplicationSecurity = false;
        }

        return applcationSecurityConfig;
    }
}
