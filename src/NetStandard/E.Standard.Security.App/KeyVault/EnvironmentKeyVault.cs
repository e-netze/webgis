using System;
using System.Threading.Tasks;

namespace E.Standard.Security.App.KeyVault;

public class EnvironmentKeyVault : IKeyVault
{
    public string Secret(string uri)
    {
        return ConfigSecurityHandler.GetGlobalConfigString(uri, String.Empty);
    }

    public Task<string> SecretAsync(string uri)
    {
        return Task.FromResult<string>(Secret(uri));
    }
}
