using System;
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

    static public void CheckIfFileInDirectory(
        this FileInfo fileInfo, 
        DirectoryInfo directoryInfo)
    {
        var fileDirectory = fileInfo.Directory;

        if (fileDirectory == null ||
            !fileDirectory.FullName.Equals(directoryInfo.FullName, 
                SystemInfo.IsWindows ? 
                    System.StringComparison.OrdinalIgnoreCase :
                    System.StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("The file is not located in the specified directory.");
        }
    }

    static public void CheckIfFileInDirectoryOrSubDirectory(
        this FileInfo fileInfo,
        DirectoryInfo directoryInfo)
    {
        var fileDirectory = fileInfo.Directory;

        // GetFullPath removes all ..\ ../ etc from the path
        if (Path.GetFullPath(fileInfo.FullName).IndexOf(directoryInfo.FullName,
                SystemInfo.IsWindows ?
                    System.StringComparison.OrdinalIgnoreCase :
                    System.StringComparison.Ordinal) != 0)
        {
            throw new UnauthorizedAccessException("The file is not located in the specified directory or sub directory.");
        }
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
