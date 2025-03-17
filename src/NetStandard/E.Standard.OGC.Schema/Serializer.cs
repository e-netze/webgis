using E.Standard.Web.Abstractions;
using E.Standard.Web.Models;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace E.Standard.OGC.Schema;

public class Serializer<T>
{
    private Dictionary<string, string> _replaceNamespaces = new Dictionary<string, string>();

    public Serializer()
    {
    }


    public void AddReplaceNamespace(string from, string to)
    {
        _replaceNamespaces[from] = to;
    }

    public T Deserialize(Stream stream)
    {
        // avoid errors => DTD parsing is not allowed per default:
        // https://stackoverflow.com/questions/13854068/dtd-prohibited-in-xml-document-exception

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Ignore;
        settings.MaxCharactersFromEntities = 1024;

        using (XmlReader xmlReader = XmlReader.Create(stream, settings))
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            return (T)ser.Deserialize(xmlReader);
        }
    }

    public T FromString(string xml, System.Text.Encoding encoding)
    {

        foreach (var key in _replaceNamespaces.Keys)
        {
            xml = xml.Replace($@"=""{key}""", $@"=""{_replaceNamespaces[key]}""");
        }

        MemoryStream ms = new MemoryStream();
        byte[] buffer = encoding.GetBytes(xml);
        ms.Write(buffer, 0, buffer.Length);
        ms.Position = 0;

        return Deserialize(ms);
    }

    async public Task<T> FromUrlAsync(string url, IHttpService http, RequestAuthorization authorization)
    {
        var bytes = await http.GetDataAsync(url, authorization);

        #region Xml Encoding

        Encoding encoding = Encoding.UTF8;
        string xml = encoding.GetString(bytes);

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
                                xml = encoding.GetString(bytes);
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

        #endregion

        return FromString(xml, encoding);
    }

    public string Serialize(T t)
    {
        XmlSerializer ser = new XmlSerializer(typeof(T));

        MemoryStream ms = new MemoryStream();
        UTF8Encoding utf8e = new UTF8Encoding();
        XmlTextWriter xmlSink = new XmlTextWriter(ms, utf8e);
        xmlSink.Formatting = Formatting.Indented;
        ser.Serialize(xmlSink, t);
        byte[] utf8EncodedData = ms.ToArray();
        return utf8e.GetString(utf8EncodedData);
    }
}
