namespace E.Standard.Security.Cryptography.Models;

public class KeysModel
{
    public int Saltsize { get; set; }
    public int Iterations { get; set; }
    public string DefaultPassword { get; set; }
    public string CookiePassword { get; set; }
    public byte[] HashBytesSalt { get; set; }

    public string Custom_ApiStoragePassword { get; set; }
    public string Custom_PortalProxyRequests { get; set; }
    public string Custom_ApiBridgeUserCryptoPassword { get; set; }
    public string Custom_LicensePassword { get; set; }
    public string Custom_ApiAdminQueryPassword { get; set; }

    // For legacy reasons (old password to encrypt serverside connectionstrings ans statements)
    // should be null or emtpy => DefaultPassword will be used
    public string DefaultPasswordDataLinq { get; set; }
}
