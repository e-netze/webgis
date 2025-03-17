using System.Threading.Tasks;

namespace E.Standard.Security.App.KeyVault;

public interface IKeyVault
{
    Task<string> SecretAsync(string uri);

    string Secret(string uri);
}
