using System.Threading.Tasks;

namespace E.Standard.Security.App.KeyVault;

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
