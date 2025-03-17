using System;
using System.Text;

namespace E.Standard.Extensions.Credentials;

static public class CredentialExtensions
{
    static public string PureUsername(this string username)
    {
        if (String.IsNullOrWhiteSpace(username))
        {
            return String.Empty;
        }

        if (username.Contains("::"))
        {
            username = username.Substring(username.LastIndexOf("::") + 2);
        }

        if (username.Contains(@"\"))
        {
            username = username.Substring(username.LastIndexOf(@"\") + 1);
        }

        return username;
    }

    static public string ShortPureUsername(this string username, int length)
    {
        username = username.PureUsername();

        if (username.Length == 0)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder(length);

        for (var i = 0; i < Math.Min(length, username.Length); i++)
        {
            sb.Append(i == 0 ? username[i].ToString().ToUpper() :
                               username[i].ToString().ToLower());
        }

        return sb.ToString();
    }
}
