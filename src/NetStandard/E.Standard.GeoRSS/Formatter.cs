using System;
using System.Text;
using System.Xml;

namespace E.Standard.GeoRSS20;

public class Formatter
{
    internal static IFormatProvider nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

    public static XmlNode[] FromString(string str)
    {
        XmlDocument doc = new XmlDocument();
        XmlNode node = doc.CreateNode(XmlNodeType.Text, "string", "");
        node.Value = str;

        XmlNode[] nodes = new XmlNode[1];
        nodes[0] = node;

        return nodes;
    }
    public static string ToString(object obj)
    {
        if (obj == null)
        {
            return null;
        }

        if (obj is System.String)
        {
            return (string)obj;
        }

        if (obj is XmlNode[])
        {
            XmlNode[] nodes = (XmlNode[])obj;

            StringBuilder sb = new StringBuilder();
            foreach (XmlNode node in nodes)
            {
                sb.Append(node.Value);
            }
            return sb.ToString();
        }
        return null;
    }
}
