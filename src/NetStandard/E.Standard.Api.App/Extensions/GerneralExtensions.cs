using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace E.Standard.Api.App.Extensions;

static public class GerneralExtensions
{
    static public NameValueCollection ToCollection(this IEnumerable<KeyValuePair<string, StringValues>> collection)
    {
        NameValueCollection result = new NameValueCollection();

        if (collection != null)
        {
            foreach (var pairs in collection)
            {
                result[pairs.Key] = (string)pairs.Value;
            }
        }

        return result;
    }

    static public bool CheckBoxRes(this IEnumerable<KeyValuePair<string, StringValues>> form, string name)
    {
        return CheckBoxRes(form.ToCollection(), name);
    }

    static public bool CheckBoxRes(this NameValueCollection form, string name)
    {
        if (form[name] == null)
        {
            return false;
        }

        string res = form[name].ToString().Split(',')[0].Trim();

        return res == "on" || res == "true" || res == "checked";
    }

    static public string[] Split2(this string str, char separator, int minLength)
    {
        var splits = str.Split(separator);

        if (splits.Length < minLength)
        {
            var list = new string[minLength];
            for (int i = 0; i < minLength; i++)
            {
                list[i] = i < splits.Length ? splits[i] : String.Empty;
            }

            splits = list;
        }

        return splits;
    }

    static public string ExpandoToCsv(this object[] objects, string separator = ";", bool header = true)
    {
        StringBuilder sb = new StringBuilder();
        if (objects != null)
        {
            foreach (var rec in objects)
            {
                if (rec is IDictionary<string, object>)
                {
                    IDictionary<string, object> record = rec as IDictionary<string, object>;
                    if (record.ContainsKey("_location"))
                    {
                        record.Remove("_location");
                    }
                    //if (record.ContainsKey("location_Latitude"))
                    //    record.Remove("location_Latitude");
                    //if (record.ContainsKey("location_Longitude"))
                    //    record.Remove("location_Longitude");
                    if (record.ContainsKey("oid"))
                    {
                        record.Remove("oid");
                    }

                    if (header)
                    {
                        sb.Append(String.Join(separator, record.Keys) + Environment.NewLine);
                        header = false;
                    }
                    sb.Append(String.Join(separator, record.Values.Select(v => "\"" + v.ToString().Replace("\"", "\"\"") + "\"")) + Environment.NewLine);
                }
            }
        }
        return sb.ToString();
    }
}
