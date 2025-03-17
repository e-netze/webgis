using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Security.KeyVault
{
    [Obsolete("Use E.Standard.Security.App assembly")]
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
}
