using System;
using System.Linq;
using System.Xml;

namespace E.Standard.WebGIS.Tools.Extensions;

static public class EditingExtensions
{
    static public bool IsRequiredField(this XmlNode fieldNode)
    {
        return fieldNode.Attributes["required"]?.Value == "true" ||
               (!String.IsNullOrEmpty(fieldNode.Attributes["minlen"]?.Value) && int.Parse(fieldNode.Attributes["minlen"].Value) > 0) ||
               !String.IsNullOrEmpty(fieldNode.Attributes["regex"]?.Value);
    }

    static public bool ApplyField(this string checkVal)
    {
        if (String.IsNullOrEmpty(checkVal))
        {
            return false;
        }

        return new[]
        {
            "checked",
            "true",
            "yes",
            "use"
        }.Contains(checkVal.ToLower());
    }

    static public T ParameterValue<T>(this string[] roleParameters, string roleParameterName)
    {
        if (roleParameters != null)
        {
            string val = String.Empty;

            foreach (string roleParameter in roleParameters)
            {
                if (roleParameter.StartsWith($"{roleParameterName}="))
                {
                    val = roleParameter.Substring(roleParameterName.Length + 1, roleParameter.Length - roleParameterName.Length - 1);
                    break;
                }
            }

            if (!String.IsNullOrEmpty(val))
            {
                return (T)Convert.ChangeType(val, typeof(T));
            }
        }

        return default(T);
    }
}
