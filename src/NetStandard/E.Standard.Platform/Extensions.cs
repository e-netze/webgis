using System.IO;

namespace E.Standard.Platform;

static public class Extensions
{
    static public string ToPlatformPath(this string path)
    {
        if (path.StartsWith(@"\\")) // UNC ??? do not touch
        {
            return path;
        }

        return SystemInfo.IsWindows 
            ? 
            (
                path.Contains("/") 
                    ? path.ReplaceFolderSeparator("/", @"\")
                    : path
            )
            : 
            (
                  path.Contains("\\") 
                    ? path.ReplaceFolderSeparator(@"\", "/")
                    : path
            );
    }

    static private string ReplaceFolderSeparator(this string path, string from, string to)
        => path.Replace(from, to);

    static public string RemoveDoubleSlashes(this string path)
    {
        if (path != null)
        {
            while (path.Contains("//"))
            {
                path = path.Replace("//", "/");
            }
        }

        return path;
    }
}
