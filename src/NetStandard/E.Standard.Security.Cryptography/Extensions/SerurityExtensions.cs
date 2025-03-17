namespace E.Standard.Security.Cryptography.Extensions;

static public class SerurityExtensions
{
    static public string HashHex(this string password)
    {
        return new CryptoImpl(null).HashHex(password);
    }

    static public string HashHex(this string password, string salt)
    {
        return new CryptoImpl(null).HashHex(password, salt);
    }

    static public string Hash64(this string password)
    {
        return new CryptoImpl(null).Hash64(password);
    }

    static public string Hash64(this string password, string salt)
    {
        return new CryptoImpl(null).Hash64(password, salt);
    }
}
