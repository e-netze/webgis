using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace E.Standard.Configuration.Extensions;

static public class StringExtensions
{
    static public IEnumerable<string> AppendToDefaultConfigPaths(this string path)
    {
        if (Platform.SystemInfo.IsWindows)
        {
            return new[]
            {
                $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/_config",
                path
            };
        }

        return new[] { path };
    }
}
