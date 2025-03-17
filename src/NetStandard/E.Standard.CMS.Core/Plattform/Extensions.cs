using System;

namespace E.Standard.CMS.Core.Plattform;

public static class Extensions
{
    static public string ToPlattformPath(this string path/*, bool untouchUNC = false*/)
    {
        if (String.IsNullOrWhiteSpace(path))
        {
            return String.Empty;
        }

        //if(untouchUNC && path.IsUncPath())
        //{
        //    return path;
        //}

        if (path.Contains("://"))
        {
            path = path.Replace("\\", "/");
        }
        else
        {
            path = path.Replace("\\", "/");
            while (path.Contains("//"))
            {
                path = path.Replace("//", "/");
            }
        }
        return path;
    }

    static public bool EqualPath(this string path, string canditate)
    {
        return path?.ToPlattformPath() == canditate?.ToPlattformPath();
    }

    static public bool IsUncPath(this string path)
    {
        return Uri.TryCreate(path, UriKind.Absolute, out Uri uri) && uri.IsUnc;
    }
}
