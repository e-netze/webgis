using System.Net;

namespace E.Standard.GeoRSS20;

public class WebFunctions
{
    public static WebProxy Proxy(string host, int port)
    {
        return Proxy(host, port, "", "", "");
    }

    public static WebProxy Proxy(string server, int port, string user, string password, string domain)
    {
        WebProxy proxy = new WebProxy(server, port);

        if (user != "" && user != null && password != null)
        {
            NetworkCredential credential = new NetworkCredential(user, password);
            if (domain != "" && domain != null)
            {
                credential.Domain = domain;
            }

            proxy.Credentials = credential;
        }
        return proxy;
    }

    //public static string DownloadXml(string url, WebProxy proxy, Encoding encoding)
    //{
    //    try
    //    {
    //        HttpWebRequest wReq = (HttpWebRequest)HttpWebRequest.Create(url);

    //        if (proxy != null)
    //        {
    //            wReq.Proxy = proxy;
    //        }

    //        HttpWebResponse wresp = (HttpWebResponse)wReq.GetResponse();

    //        int Bytes2Read = 3500000;
    //        Byte[] b = new Byte[Bytes2Read];

    //        DateTime t1 = DateTime.Now;
    //        Stream stream = wresp.GetResponseStream();

    //        MemoryStream memStream = new MemoryStream();

    //        while (Bytes2Read > 0)
    //        {
    //            int len = stream.Read(b, 0, Bytes2Read);
    //            if (len == 0)
    //            {
    //                break;
    //            }

    //            memStream.Write(b, 0, len);
    //        }
    //        memStream.Position = 0;
    //        string ret = encoding.GetString(memStream.GetBuffer()).Trim(' ', '\0'); ;
    //        memStream.Close();
    //        memStream.Dispose();

    //        return ret.Trim();
    //    }
    //    catch (Exception ex)
    //    {
    //        return "<Exception>" + ex.Message + "</Exception>";
    //    }
    //}

    public static string RemoveDOCTYPE(string xml)
    {
        int pos = xml.IndexOf("<!DOCTYPE");
        if (pos != -1)
        {
            int o = 1, i;
            for (i = pos + 1; i < xml.Length; i++)
            {
                if (xml[i] == '<')
                {
                    o++;
                }
                else if (xml[i] == '>')
                {
                    o--;
                    if (o == 0)
                    {
                        break;
                    }
                }
            }

            string s1 = xml.Substring(0, pos - 1);
            string s2 = xml.Substring(i + 1, xml.Length - i - 1);

            return s1 + s2;
        }

        return xml;
    }
}
