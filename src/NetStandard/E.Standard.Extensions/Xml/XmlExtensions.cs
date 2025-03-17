using System.Text;

namespace E.Standard.Extensions.Xml;

static public class XmlExtensions
{
    public static string EscapeXml(this string s)
    {
        string toxml = s;

        if (!string.IsNullOrEmpty(toxml))
        {
            // replace literal values with entities
            toxml = toxml.Replace("&", "&amp;");
            toxml = toxml.Replace("'", "&apos;");
            toxml = toxml.Replace("\"", "&quot;");
            toxml = toxml.Replace(">", "&gt;");
            toxml = toxml.Replace("<", "&lt;");
        }
        return toxml;
    }

    static public string ToXmlString(this byte[] buffer, out Encoding encoding)
    {
        encoding = Encoding.UTF8;
        string xml = encoding.GetString(buffer);

        try
        {
            if (xml.StartsWith("<?xml "))
            {
                int index = xml.IndexOf(" encoding=");

                if (index != -1)
                {
                    int index2 = xml.IndexOf(xml[index + 10], index + 11);

                    if (index2 != -1)
                    {

                        string encodingString = xml.Substring(index + 11, index2 - index - 11);

                        if (encodingString.ToLower() != "utf-8" && encodingString.ToLower() != "utf8")
                        {
                            encoding = Encoding.GetEncoding(encodingString);

                            if (encoding != null)
                            {
                                xml = encoding.GetString(buffer);
                            }
                            else
                            {
                                encoding = Encoding.UTF8;
                            }
                        }

                    }
                }
            }
        }
        catch { encoding = Encoding.UTF8; }

        return xml;
    }
}