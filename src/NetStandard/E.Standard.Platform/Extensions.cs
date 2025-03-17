namespace E.Standard.Platform;

static public class Extensions
{
    static public string ToPlatformPath(this string path)
    {
        if (path.StartsWith(@"\\")) // UNC ???
        {
            return path;
        }

        return path.Replace(@"\", "/");
    }

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
