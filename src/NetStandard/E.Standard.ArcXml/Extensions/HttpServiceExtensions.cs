using E.Standard.ArcXml.Models;
using E.Standard.Extensions.Compare;
using E.Standard.Platform;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;
using E.Standard.Web.Models;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace E.Standard.ArcXml.Extensions;

static public class HttpServiceExtensions
{
    async public static Task<string> SendAxlRequestAsync(this IHttpService httpService,
                                                         ArcAxlConnectionProperties connectionProperties,
                                                         string axl,
                                                         string serverName,
                                                         string serviceName,
                                                         string customService = "",
                                                         char commaFormat = ',')
    {
        string theURL = serverName;
        if (serverName.IndexOf("http") != 0)
        {
            theURL = "http://" + theURL;
        }

        if (theURL.NeedsServletExecPath())
        {
            theURL += (serverName.EndsWith("/") ? "" : "/") + serverName.ServerUrl();
        }

        if (theURL.IndexOf("?") == -1)
        {
            theURL += "?";
        }

        if (theURL.IndexOf("?") != (theURL.Length - 1))
        {
            theURL += "&";
        }

        if (serviceName != "")
        {
            theURL += "ServiceName=" + serviceName + "&ClientVersion=4.0";
        }
        else
        {
            theURL += "ClientVersion=4.0";
        }

        if (customService != "")
        {
            theURL += "&CustomService=" + customService;
        }

        theURL = AppendToken(theURL, connectionProperties?.Token);

        axl.ReplaceComma(commaFormat);

        var bytes = await httpService.PostDataAsync(theURL,
                                                    Encoding.UTF8.GetBytes(axl),
                                                    new RequestAuthorization()
                                                    {
                                                        Username = connectionProperties?.AuthUsername ?? "",
                                                        Password = connectionProperties?.AuthPassword ?? ""
                                                    },
                                                    connectionProperties?.Timeout.OrTake(20) ?? 20);

        return Encoding.UTF8.GetString(bytes.data);
    }

    async public static Task<(string layerId, char commaFormat)> GetAxlServiceLayerIdAsync(this IHttpService httpService,
                                                                                           ArcAxlConnectionProperties connectionProperties,
                                                                                           string server,
                                                                                           string service,
                                                                                           string theme)
    {
        string axl = "<ARCXML version=\"1.1\"><REQUEST><GET_SERVICE_INFO fields=\"false\" envelope=\"false\" renderer=\"false\" extensions=\"false\" /></REQUEST></ARCXML>", resp = String.Empty;

        resp = await httpService.SendAxlRequestAsync(connectionProperties, axl, server, service);
        char commaFormat = ',';

        XmlDocument doc = new XmlDocument();
        try
        {
            doc.LoadXml(resp);

            var seperators = doc.SelectSingleNode("ARCXML/RESPONSE/SERVICEINFO/ENVIRONMENT/SEPARATORS[@dec]");
            if (seperators != null && seperators.Attributes["dec"].Value.Length == 1)
            {
                commaFormat = seperators.Attributes["dec"].Value[0];
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Invalid AXL Response: {resp}", ex);
        }

        var errorNode = doc.SelectSingleNode("ARCXML/RESPONSE/ERROR");
        if (errorNode != null)
        {
            throw new Exception(errorNode.Attributes["message"]?.Value ?? errorNode.InnerText);
        }

        foreach (XmlNode layer in doc.SelectNodes("//LAYERINFO"))
        {
            if (layer.Attributes["name"] == null || layer.Attributes["id"] == null)
            {
                continue;
            }

            if (layer.Attributes["id"].Value == theme)
            {
                return (theme, commaFormat);
            }

            if (layer.Attributes["name"].Value == theme)
            {
                return (layer.Attributes["id"].Value, commaFormat);
            }
        }

        return ("", commaFormat);
    }

    async public static Task<double[]> GetAxlServiceRasterInfoAsync(this IHttpService httpService,
                                                                    ArcAxlConnectionProperties connectionProperties,
                                                                    string server,
                                                                    string service,
                                                                    string layerId,
                                                                    double x,
                                                                    double y,
                                                                    char commaFormat)
    {
        try
        {
            StringBuilder axl = new StringBuilder();
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xWriter = new XmlTextWriter(ms, System.Text.Encoding.UTF8);

            xWriter.WriteStartDocument();
            xWriter.WriteStartElement("ARCXML");
            xWriter.WriteAttributeString("version", "1.1");
            xWriter.WriteStartElement("REQUEST");
            xWriter.WriteStartElement("GET_RASTER_INFO");
            xWriter.WriteAttributeString("x", x.ToPlatformNumberString().Replace(".", commaFormat.ToString()));
            xWriter.WriteAttributeString("y", y.ToPlatformNumberString().Replace(".", commaFormat.ToString()));
            xWriter.WriteAttributeString("layerid", layerId);
            xWriter.WriteEndElement(); // GET_RASTER_INFO

            xWriter.WriteEndDocument();
            xWriter.Flush();

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            axl.Append(sr.ReadToEnd());
            sr.Close();
            ms.Close();
            xWriter.Close();

            string req = axl.ToString().Replace("&amp;", "&");
            // REQUEST verschicken

            string axlResponse = await httpService.SendAxlRequestAsync(connectionProperties, axl.ToString(), server, service);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(axlResponse);

            XmlNodeList bands = doc.SelectNodes("//RASTER_INFO/BANDS/BAND[@value]");
            if (bands.Count == 0)
            {
                return null;
            }

            double[] res = new double[bands.Count];
            for (int i = 0; i < bands.Count; i++)
            {
                res[i] = bands[i].Attributes["value"].Value.ToPlatformDouble();
            }

            return res;
        }
        catch
        {
            return null;
        }
    }
    async public static Task<bool> GetAxlServiceRasterInfoProAsync(this IHttpService httpService,
                                                                   ArcAxlConnectionProperties connectionProperties,
                                                                   string server,
                                                                   string service,
                                                                   string id,
                                                                   List<ArcXmlPoint> points,
                                                                   char commaFormat)
    {
        try
        {
            if (points == null)
            {
                return false;
            }

            StringBuilder axl = new StringBuilder();
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xWriter = new XmlTextWriter(ms, System.Text.Encoding.UTF8);

            xWriter.WriteStartDocument();
            xWriter.WriteStartElement("ARCXML");
            xWriter.WriteAttributeString("version", "1.1");
            xWriter.WriteStartElement("REQUEST");
            xWriter.WriteStartElement("GET_RASTER_INFO");
            xWriter.WriteAttributeString("layerid", id);
            foreach (ArcXmlPoint point in points)
            {
                point.Write(xWriter, commaFormat);
            }

            xWriter.WriteEndElement(); // GET_RASTER_INFO

            xWriter.WriteEndDocument();
            xWriter.Flush();

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            axl.Append(sr.ReadToEnd());
            sr.Close();
            ms.Close();
            xWriter.Close();

            string req = axl.ToString().Replace("&amp;", "&");
            // REQUEST verschicken

            string axlResponse = await httpService.SendAxlRequestAsync(connectionProperties, axl.ToString(), server, service);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(axlResponse);

            XmlNodeList raster_infos = doc.SelectNodes("//RASTER_INFO[@x]");
            if (raster_infos.Count == 0)
            {
                return false;
            }

            foreach (XmlNode raster_info in raster_infos)
            {
                double x = raster_info.Attributes["x"].Value.ToPlatformDouble();
                double y = raster_info.Attributes["y"].Value.ToPlatformDouble();

                foreach (ArcXmlPoint p in points)
                {
                    if (p.Z.Equals(double.NaN) &&
                        Math.Abs(p.X - x) < 1e-5 &&
                        Math.Abs(p.Y - y) < 1e-5)
                    {
                        XmlNode band = raster_info.SelectSingleNode("BANDS/BAND[@number='0']");
                        if (band != null && band.Attributes["value"] != null)
                        {
                            p.Z = band.Attributes["value"].Value.ToPlatformDouble();
                        }
                    }
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    async static public Task<IBitmap> GetAxlServiceImageAsync(this IHttpService httpService,
                                                             ArcAxlConnectionProperties connectionProperties,
                                                             XmlNode outputNode,
                                                             string outputPath)
    {
        string filename;
        string imageUrl = GetImageUrl(outputNode);

        try
        {
            if (!String.IsNullOrEmpty(outputPath))
            {
                filename = outputPath + @"\" + FileTitle(imageUrl);
                FileInfo fi = new FileInfo(filename);
                if (fi.Exists)
                {
                    return await fi.FullName.ImageFromUri(httpService);
                }
            }
            if (outputNode.Attributes["file"] != null)
            {
                filename = outputNode.Attributes["file"].Value;
                if (!httpService.Legacy_AlwaysDownloadFrom(filename))
                {
                    FileInfo fi = new FileInfo(filename);
                    if (fi.Exists)
                    {
                        return await fi.FullName.ImageFromUri(httpService);
                    }
                }
            }
            if (imageUrl != "")
            {
                return await httpService.GetImageAsync(imageUrl, new RequestAuthorization()
                {
                    Username = connectionProperties.AuthUsername,
                    Password = connectionProperties.AuthPassword
                });
            }
        }
        catch
        {
        }
        return null;
    }



    async static public Task<IEnumerable<string>> GetServiceNamesAsync(this IHttpService httpService,
                                                                       ArcAxlConnectionProperties connectionProperties,
                                                                       string server,
                                                                       bool onlyPublic = false)
    {
        string resp = await SendAxlRequestAsync(httpService,
                                                connectionProperties,
                                                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><GETCLIENTSERVICES/>",
                                                server,
                                                "catalog");

        var serviceNames = new List<string>();

        XmlDocument doc = new XmlDocument();
        doc.LoadXml(resp);
        XmlNodeList services = doc.SelectNodes("//SERVICE");

        foreach (XmlNode service in services)
        {
            if (onlyPublic)
            {
                // früher wahren die Attribute kleingeschreiben
                // jetzt sind sie groß
                // Danke ESRI...!!
                if (service.Attributes["access"] != null)
                {
                    if (service.Attributes["access"].Value.ToString().ToLower() != "public")
                    {
                        continue;
                    }
                }
                else if (service.Attributes["ACCESS"] != null)
                {
                    if (service.Attributes["ACCESS"].Value.ToString().ToLower() != "public")
                    {
                        continue;
                    }
                }
            }
            if ("folder".Equals(service.Attributes["servicetype"]?.Value, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (service.Attributes["name"] != null)
            {
                serviceNames.Add(service.Attributes["name"].Value);
            }
            else if (service.Attributes["NAME"] != null)
            {
                serviceNames.Add(service.Attributes["NAME"].Value);
            }
        }

        return serviceNames;
    }

    #region Helper

    static private bool NeedsServletExecPath(this string url)
    {
        if (url.Contains("servlet/com.esri.esrimap.Esrimap"))
        {
            return false;
        }

        if (url.EndsWith("/"))  // Damit kann man das erzwingen, praktisch wenn man über einen Proxy (Karko) die Dienste anspricht
        {
            return true;
        }

        if (url.IndexOf("/", 10) > 0)
        {
            return false;
        }

        return true;
    }

    static private string ReplaceComma(this string axl, char commaFormat)
    {
        if (commaFormat != ',')
        {
            axl = ReplaceComma(axl, "minx", ',', commaFormat);
            axl = ReplaceComma(axl, "miny", ',', commaFormat);
            axl = ReplaceComma(axl, "maxx", ',', commaFormat);
            axl = ReplaceComma(axl, "maxy", ',', commaFormat);

            axl = ReplaceComma(axl, "x", ',', commaFormat);
            axl = ReplaceComma(axl, "y", ',', commaFormat);

            axl = ReplaceComma(axl, "width", ',', commaFormat);
            axl = ReplaceComma(axl, "fontsize", ',', commaFormat);

            axl = ReplaceComma(axl, "transparency", ',', commaFormat);
            axl = ReplaceComma(axl, "filltransparency", ',', commaFormat);
        }

        return axl;
    }

    static private string ReplaceComma(this string axl, string attribute, char from, char to)
    {
        int pos = axl.IndexOf(attribute + "=\"");
        StringBuilder sb = new StringBuilder();
        sb.Append(axl);
        while (pos != -1)
        {
            int pos2 = axl.IndexOf("\"", pos + attribute.Length + 2);
            if (pos2 == -1)
            {
                return sb.ToString();
            }

            int posC = 0;
            while (true)
            {
                posC = axl.IndexOf(from, (posC == 0) ? pos + attribute.Length + 2 : posC + 1);
                if (posC != -1 && posC < pos2)
                {
                    sb[posC] = to;
                }
                else
                {
                    break;
                }
            }
            pos = axl.IndexOf(attribute + "=\"", pos2);
        }
        return sb.ToString();
    }

    static private string GetImageUrl(XmlNode output)
    {
        if (output.Attributes["url"] != null)
        {
            return output.Attributes["url"].Value;
        }

        return "";
    }

    static private string FileTitle(string uri)
    {
        uri = uri.Replace("\\", "/").Replace("//", "/");

        int index = 0;
        while ((index = uri.IndexOf("/")) > -1)
        {
            uri = uri.Substring(index + 1, uri.Length - (index + 1));
        }

        uri = uri.Replace("&", "_").Replace("?", "_").Replace(":", "_");
        if (uri.Length > 150)
        {
            int pos = uri.LastIndexOf(".");
            string ext = uri.Substring(pos, uri.Length - pos);
            uri = "ims_" + System.Guid.NewGuid().ToString("N") + ext;
        }
        return uri;
    }

    #region Legacy (dotNetConnector)

    //private static ConcurrentBag<string> _serverUrls = null; //new ConcurrentBag<string>();
    static private string ServerUrl(this string serverName)
    {
        return "servlet/com.esri.esrimap.Esrimap"; ;

        //if (_serverUrls == null)
        //{
        //    return "servlet/com.esri.esrimap.Esrimap";
        //}

        //if (!_serverUrls.Contains(serverName))
        //{
        //    return "servlet/com.esri.esrimap.Esrimap";
        //}

        //return _serverUrls[serverName].ToString();
    }

    static private string AppendToken(string url, string token)
    {
        if (!String.IsNullOrWhiteSpace(token))
        {
            string tokenParameter = token.Contains("=") ? token : "token=" + token;
            url += (url.Contains("?") ? "&" : "?") + tokenParameter;
        }

        return url;
    }

    #endregion

    #endregion
}
