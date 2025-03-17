using E.Standard.WebMapping.Core.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace E.Standard.WebMapping.Core.Extensions;

public static class GeneralExtensions
{
    static public string[] SplitQuotedString(this string input, bool replaceSingleQuotes = true)
    {
        if (replaceSingleQuotes)
        {
            input = input.Replace("'", "\"");
        }

        var parts = Regex.Matches(input, @"[\""].+?[\""]|[^ ]+")   //  "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"
            .Cast<Match>()
            .Select(m => m.Value.StartsWith("\"") && m.Value.EndsWith("\"") ? m.Value.Substring(1, m.Value.Length - 2).Trim() : m.Value)
            .ToArray();

        return parts;
    }

    static public IEnumerable<T> MakeUnique<T>(this IEnumerable<T> list)
    {
        if (list == null)
        {
            return null;
        }

        List<T> unique = new List<T>();
        foreach (T item in list)
        {
            if (!unique.Contains(item))
            {
                unique.Add(item);
            }
        }

        return unique;
    }

    static public string ToProjectUserId(this string userId)
    {
        return userId.Replace(":", "_-colon-_").Replace(";", "_-semicolon-_").Replace("?", "_-qmark-_").Replace("/", "_-slash-_")
            .Replace(">", "_-gt-_").Replace("<", "_-lt-_").Replace("|", "_-pipe-_").Replace("\"", "_-quote-_")
            .Replace("*", "_-asterisk-_");
    }

    public static FileInfo FirstExistingFile(this IEnumerable<string> fileNames)
    {
        foreach (var fileName in fileNames)
        {
            FileInfo fi = new FileInfo(fileName);
            if (fi.Exists)
            {
                return fi;
            }
        }

        return null;
    }

    public static string FirstExistingFileContent(this IEnumerable<string> fileNames)
    {
        var fileInfo = fileNames.FirstExistingFile();
        if (fileInfo != null)
        {
            return System.IO.File.ReadAllText(fileInfo.FullName);
        }

        return String.Empty;
    }

    public static DirectoryInfo FirstExistingPath(this IEnumerable<string> directoryNames)
    {
        foreach (var directoryName in directoryNames)
        {
            DirectoryInfo di = new DirectoryInfo(directoryName);
            if (di.Exists)
            {
                return di;
            }
        }

        return null;
    }

    static public string ToToolId(this Type button)
    {
        var toolIdAttribute = button.GetCustomAttribute<ToolIdAttribute>();
        if (!String.IsNullOrEmpty(toolIdAttribute?.ToolId))
        {
            return toolIdAttribute.ToolId;
        }

        string typeName = button.ToString().ToLower();

        if (typeName.StartsWith("e.standard."))
        {
            typeName = typeName.Substring("e.standard.".Length);
        }

        return typeName;
    }

    static public string ToNewToolId(this string toolId)
    {
        if (toolId.ToLower().StartsWith("webmapping.tools.api."))
        {
            toolId = "webgis.tools." + toolId.Substring("webmapping.tools.api.".Length);
        }

        return toolId;
    }

    static public string ToUniqueFilterId(this string filterId, string serviceId)
    {
        return $"{serviceId}~{filterId}";
    }


    static public string ToSimpleRequestId(this string requestId)
    {
        if (!String.IsNullOrEmpty(requestId) && requestId.Contains("."))
        {
            return requestId.Split('.')[0];
        }

        return requestId;
    }
}
