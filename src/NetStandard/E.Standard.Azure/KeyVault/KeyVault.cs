using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using E.Standard.Security.App.KeyVault;
using System;
using System.Threading.Tasks;

namespace E.Standard.Azure.KeyVault;

//public class KeyVault : IKeyVault
//{
//    public KeyVault(string clientId, string clientSecret)
//    {
//        var authentication = new AD.Authentication(clientId, clientSecret);
//        this.Client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(authentication.GetToken));
//    }

//    private KeyVaultClient Client { get; set; }

//    async public Task<string> SecretAsync(string uri)
//    {
//        var sec = await this.Client.GetSecretAsync(uri);
//        return sec.Value;
//    }

//    public string Secret(string uri)
//    {
//        return SecretAsync(uri).Result;
//    }
//}


public class KeyVault : IKeyVault
{
    private readonly SecretClient _client;

    public KeyVault(string keyVaultUri, string tenantId, string clientId, string clientSecret)
    {
        Console.WriteLine("KeyValue:");
        Console.WriteLine($"KeyVaultUri: {keyVaultUri}");
        Console.WriteLine($"TenantId: {tenantId}");
        Console.WriteLine($"ClientId: {clientId}");

        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        _client = new SecretClient(new Uri(keyVaultUri), credential);

        var value = _client.GetSecret("secret-wgc-adminaccountpw-prod");
    }

    public async Task<string> SecretAsync(string secretName)
    {
        string version = null;

        if (secretName.Contains("/secrets/", StringComparison.OrdinalIgnoreCase))  // remove url from secreted (kompatiblity)
        {
            var kvUrl = secretName.Substring(0, secretName.IndexOf("/secrets/", StringComparison.OrdinalIgnoreCase) + "/secrets/".Length);
            Console.WriteLine($"Warining: secretsname contains full url {kvUrl}...");
            secretName = secretName.Substring(kvUrl.Length);
        }

        if (secretName.Contains("/"))
        {
            (secretName, version) = (secretName.Split('/')[0], secretName.Split("/")[1]);
        }

        KeyVaultSecret secret = await _client.GetSecretAsync(secretName, version);
        return secret.Value;
    }

    public string Secret(string secretName)
    {
        return SecretAsync(secretName).GetAwaiter().GetResult();
    }

}
