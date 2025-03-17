using System;
using System.Xml;

namespace E.Standard.CMS.Core.Extensions;

public static class CmsExtensions
{
    public static bool UseWith(this CmsNodeType nodeTypes, CmsNodeType nodeType)
    {
        if (nodeTypes == CmsNodeType.Any || nodeType == CmsNodeType.Any)
        {
            return true;
        }

        return nodeTypes.HasFlag(nodeType);
    }

    public static bool IsLinkTargetValid(this XmlNode schemaNode, string path, string target, string linkName = "*")
    {
        if (string.IsNullOrEmpty(target))
        {
            return false;
        }

        var schemaLinkNode = schemaNode?.SelectSingleNode($"schema-link[@name='{linkName}' and @urifilterpath]");
        if (schemaLinkNode == null)
        {
            return true;
        }

        var uriFilterPath = schemaLinkNode.Attributes["urifilterpath"].Value?.ToLower().Replace("\\", "/");
        path = path.ToLower();

        while (uriFilterPath.StartsWith("../"))
        {
            uriFilterPath = uriFilterPath.Substring(3);
            path = path.ParentPath();
        }

        return target.ToLower().StartsWith($"{path}/{uriFilterPath}/");
    }

    public static string ParentPath(this string path)
    {
        path = path.Replace("\\", "/");
        while (path.EndsWith("/"))
        {
            path = path.Substring(0, path.Length - 1);
        }

        while (path.Contains("//"))
        {
            path = path.Replace("//", "/");
        }

        var pos = path.LastIndexOf("/");
        if (pos <= 0)
        {
            return string.Empty;
        }

        return path.Substring(0, pos);
    }

    static public bool IsSecretPlaceholder(this string str) => !String.IsNullOrEmpty(str) && str.Trim().StartsWith("{{secret-") && str.Trim().EndsWith("}}");

    static public bool ContainsSecretPlaceholders(this string str)
    {
        int pos = str.IndexOf("{{secret-");
        if (pos < 0)
        {
            return false;
        }

        return str.IndexOf("}}", pos) >= 0;
    }
}
