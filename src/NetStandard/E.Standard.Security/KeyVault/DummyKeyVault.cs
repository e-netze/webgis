using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Security.KeyVault
{
    [Obsolete("Use E.Standard.Security.App assembly")]
    public class DummyKeyVault : IKeyVault
    {
        public Task<string> SecretAsync(string uri)
        {
            return Task.FromResult(uri);
        }

        public string Secret(string uri)
        {
            return uri;
        }
    }
}
