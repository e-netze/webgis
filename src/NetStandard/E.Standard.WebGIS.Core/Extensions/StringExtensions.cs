namespace E.Standard.WebGIS.Core.Extensions;

static public class StringExtensions
{
    static public string Username2StorageDirectory(this string username)
    {
        return username.Replace(":", "~").Replace(@"\", "$");
    }

    static public string StorageDirectory2Username(this string directoryName)
    {
        return directoryName.Replace("~", ":").Replace("$", @"\");
    }
}
