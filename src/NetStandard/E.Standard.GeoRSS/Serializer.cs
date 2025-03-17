using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace E.Standard.GeoRSS20;

public class Serializer
{
    #region Deserialize
    static public rss Deserialize(Stream stream)
    {
        // avoid errors => DTD parsing is not allowed per default:
        // https://stackoverflow.com/questions/13854068/dtd-prohibited-in-xml-document-exception

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Ignore;
        settings.MaxCharactersFromEntities = 1024;

        using (XmlReader xmlReader = XmlReader.Create(stream, settings))
        {
            XmlSerializer ser = new XmlSerializer(typeof(rss));
            return (rss)ser.Deserialize(xmlReader);
        }
    }
    static public rss FromFile(string filename)
    {
        StreamReader sr = new StreamReader(filename);
        rss rss = Deserialize(sr.BaseStream);
        sr.Close();

        return rss;
    }
    static public rss FromString(string xml, System.Text.Encoding encoding)
    {
        MemoryStream ms = new MemoryStream();
        byte[] buffer = encoding.GetBytes(xml);
        ms.Write(buffer, 0, buffer.Length);
        ms.Position = 0;

        return Deserialize(ms);
    }
    //static public rss FromUrl(string url)
    //{
    //    return FromUrl(url, null);
    //}
    //static public rss FromUrl(string url, WebProxy proxy)
    //{
    //    HttpWebRequest wReq = (HttpWebRequest)HttpWebRequest.Create(url);

    //    if (proxy != null)
    //    {
    //        wReq.Proxy = proxy;
    //    }

    //    wReq.Timeout = 360000;
    //    HttpWebResponse wresp = (HttpWebResponse)wReq.GetResponse();

    //    int Bytes2Read = 3500000;
    //    Byte[] b = new Byte[Bytes2Read];

    //    DateTime t1 = DateTime.Now;
    //    Stream stream = wresp.GetResponseStream();

    //    MemoryStream memStream = new MemoryStream();

    //    while (Bytes2Read > 0)
    //    {
    //        int len = stream.Read(b, 0, Bytes2Read);
    //        if (len == 0)
    //        {
    //            break;
    //        }

    //        memStream.Write(b, 0, len);
    //    }
    //    memStream.Position = 0;
    //    string xml = Encoding.UTF8.GetString(memStream.GetBuffer()).Trim(' ', '\0').Trim();
    //    xml = xml.Replace("< ", "&lt;  ").Replace(" >", " &gt;");
    //    memStream.Close();
    //    memStream.Dispose();

    //    return FromString(xml, System.Text.Encoding.UTF8);
    //}

    #endregion

    #region Serialize
    static public string Serialize(rss rss)
    {
        return Serialize(rss, Encoding.UTF8);
    }
    static public string Serialize(rss rss, Encoding encoding)
    {
        System.Xml.Serialization.XmlSerializerNamespaces namespaces = new System.Xml.Serialization.XmlSerializerNamespaces();

        //namespaces.Add("xs", "http://www.w3.org/2001/XMLSchema");
        //namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        namespaces.Add("geo", "http://www.w3.org/2003/01/geo/wgs84_pos#");
        namespaces.Add("georss", "http://www.georss.org/georss");

        MemoryStream ms = new MemoryStream();
        XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(rss));
        ser.Serialize(ms, rss, namespaces);

        return encoding.GetString(ms.GetBuffer());
    }
    static public bool SerializeToFile(string filename, rss rss, Encoding encoding)
    {
        try
        {
            StreamWriter sw = new StreamWriter(filename, false, encoding);
            sw.Write(Serialize(rss, encoding));
            sw.Close();
            return true;
        }
        catch
        {
            return false;
        }
    }
    #endregion
}
