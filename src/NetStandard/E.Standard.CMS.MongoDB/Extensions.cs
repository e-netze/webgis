using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.CMS.MongoDB;

static class Extensions
{
    public static string ToItemPath(this string path)
    {
        path = path.ToLower().Trim().Replace("\\", "/").Trim('/');
        while (path.Contains("//"))
        {
            path = path.Replace("//", "/");
        }

        return path;
    }

    public static string PathToParent(this string path)
    {
        path = path.ToItemPath();
        if (path.Contains("/"))
        {
            return path.Substring(0, path.LastIndexOf("/"));
        }
        else
        {
            return String.Empty;
        }
    }

    public static string PathToCmsId(this string path)
    {
        path = path.ToItemPath();
        if (path.Contains("/"))
        {
            return path.Substring(0, path.IndexOf("/"));
        }
        else
        {
            return path;
        }
    }

    public static byte[] ToUTF8Bytes(this string text)
    {
        if (String.IsNullOrEmpty(text))
        {
            return null;
        }

        return Encoding.UTF8.GetBytes(text);
    }

    public static string ToUTF8String(this byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return String.Empty;
        }

        return Encoding.UTF8.GetString(bytes);
    }

    public static string GetConnetion(this string path)
    {
        return path.GetPathValue("connection");
    }

    public static string GetDatabase(this string path)
    {
        return path.GetPathValue("db");
    }

    public static string GetCollectionName(this string path)
    {
        return path.GetPathValue("collection");
    }

    public static string GetNodePath(this string path)
    {
        return path.GetPathValue("path").ToItemPath();
    }

    public static string GetPathValue(this string path, string parameter)
    {
        foreach (var p in path.Split(';'))
        {
            if (!p.Contains("="))
            {
                continue;
            }

            int pos = p.IndexOf("=");
            var param = p.Substring(0, pos).Trim();
            if (param == parameter)
            {
                return p.Substring(pos + 1).Trim();
            }
        }

        return String.Empty;
    }

    static public string PathString(string connection, string db, string collection, string path)
    {
        return "connection=" + connection + ";db=" + db + ";collection=" + collection + ";path=" + path;
    }

    static public IEnumerable<MongoCmsItem> GetAll(this IMongoQueryable<MongoCmsItem> query)
    {
        var cursor = query.ToCursor();

        List<MongoCmsItem> items = new List<MongoCmsItem>();
        while (cursor.MoveNext())
        {
            items.AddRange(cursor.Current);
        }
        return items;
    }

    static public IEnumerable<MongoCmsItem> GetAll(this IAsyncCursor<MongoCmsItem> cursor)
    {
        List<MongoCmsItem> items = new List<MongoCmsItem>();
        while (cursor.MoveNext())
        {
            items.AddRange(cursor.Current);
        }
        return items;
    }
}
