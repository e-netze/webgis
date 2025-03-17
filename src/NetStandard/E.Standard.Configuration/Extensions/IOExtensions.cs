using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace E.Standard.Configuration.Extensions;

static public class IOExtensions
{
    static public void CreateTargetFileRedirections(this DirectoryInfo configPath, DirectoryInfo targetPath, IEnumerable<string> filenames = null)
    {
        if (filenames == null)
        {
            filenames = new List<string>(targetPath.GetFiles("*.config").Select(f => f.Name));
        }
        foreach (var filename in filenames)
        {
            var targetFile = new FileInfo(Path.Combine(targetPath.FullName, filename));
            if (!targetFile.Exists)
            {
                continue;
            }

            var configFile = new FileInfo(Path.Combine(configPath.FullName, filename));
            if (configFile.Exists)
            {
                configFile.Delete();
            }

            if (!configFile.Directory.Exists)
            {
                configFile.Directory.Create();
            }

            Console.WriteLine($"Write file {configFile.FullName}");
            Console.WriteLine($"{ConfigFileInfo.TargetFilePrefix}{targetFile.FullName}");

            ConfigFileInfo.AllowConfigRedirection = true;

            File.WriteAllText(configFile.FullName, $"{ConfigFileInfo.TargetFilePrefix}{targetFile.FullName}");
        }
    }
}
