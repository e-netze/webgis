using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Security.KeyVault
{
    [Obsolete("Use E.Standard.Security.App assembly")]
    public interface IKeyVault
    {
        Task<string> SecretAsync(string uri);

        string Secret(string uri);
    }
}
