using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace E.Standard.WebGIS.Core.Extensions;

static public class IoExtensions
{
    static public bool TryCreateDirectoryIfNotExistes(this IConfiguration configuration, string configKey)
    {
        var directoryPath = configuration[configKey];

        if (!String.IsNullOrEmpty(directoryPath))
        {
            if (directoryPath.StartsWith("fs:", StringComparison.InvariantCultureIgnoreCase))
            {
                directoryPath = directoryPath.Substring(3);
            }

            if (IsValidPath(directoryPath))
            {
                try
                {
                    var di = new DirectoryInfo(directoryPath);
                    if (!di.Exists)
                    {
                        di.Create();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warnung: Can't create directory {directoryPath}");
                    Console.WriteLine(ex.Message);

                    return false;
                }
            }
        }

        return true;
    }

    static public bool IsValidPath(string path, bool allowRelativePaths = false)
    {
        bool isValid = true;

        try
        {
            string fullPath = Path.GetFullPath(path);

            if (allowRelativePaths)
            {
                isValid = Path.IsPathRooted(path);
            }
            else
            {
                string root = Path.GetPathRoot(path);
                isValid = string.IsNullOrEmpty(root.Trim(new char[] { '\\', '/' })) == false;
            }
        }
        catch /*(Exception ex)*/
        {
            isValid = false;
        }

        return isValid;
    }
}
