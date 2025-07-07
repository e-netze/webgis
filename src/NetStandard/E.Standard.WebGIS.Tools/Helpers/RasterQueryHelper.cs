using E.Standard.ArcXml;
using E.Standard.ArcXml.Extensions;
using E.Standard.ArcXml.Models;
using E.Standard.Drawing.Models;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using gView.GraphicsEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace E.Standard.WebGIS.Tools.Helpers;

class RasterQueryHelper
{
    async public Task<IEnumerable<HeightQueryResult>> PerformHeightQueryAsync(IBridge bridge, Point clickPoint4326, string xmlFilename)
    {
        List<HeightQueryResult> results = new List<HeightQueryResult>();

        if (new FileInfo(xmlFilename).Exists)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFilename);

            int index = 0;
            foreach (XmlNode hadNode in doc.SelectNodes($"//heightabovedatum[@type and @server and @service and @rastertheme and @srs]"))
            {
                var point = new Point(clickPoint4326);  // Neue Instanz erzeugen-> Wird immer wieder neue projeziert, weil in jedem hadNode ein anderes SRS stehen kann!!
                int srsId = Convert.ToInt32(hadNode.Attributes["srs"].Value);
                using (var transformer = bridge.GeometryTransformer(4326, srsId))
                {
                    transformer.Transform(point);
                }

                var heightQueryResult = await PerformHeightQueryAsync(bridge, bridge.TemporaryMapObject(), hadNode, point);
                if (String.IsNullOrEmpty(heightQueryResult.Name))
                {
                    heightQueryResult.Name = DefaultHeightName(index);
                }

                results.Add(heightQueryResult);

                index++;
            }
        }

        return results;
    }

    async public Task<IEnumerable<HeightQueryResult>> PerformHeightQueryAsync(IBridge bridge, List<Point> points, string xmlFilename)
    {
        List<HeightQueryResult> results = new List<HeightQueryResult>();

        if (new FileInfo(xmlFilename).Exists)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFilename);

            foreach (XmlNode hadNode in doc.SelectNodes("//heightabovedatum[@type and @server and @service and @rastertheme and @srs]"))
            {
                int srsId = Convert.ToInt32(hadNode.Attributes["srs"].Value);
                using (var transformer = bridge.GeometryTransformer(4326, srsId))
                {
                    foreach (var point in points)
                    {
                        transformer.Transform(point);
                    }
                }

                await PerformHeightQueryAsync(bridge, hadNode, points);

                break;
            }
        }

        return results;
    }

    async public Task<EnvelopeQueryResult> PerformHeightQueryAsync(IBridge bridge, Envelope bbox, Dimension size, double resolution, string xmlFilename, string hadName)
    {
        if (new FileInfo(xmlFilename).Exists)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFilename);

            if (bbox.SrsId == 0)
            {
                bbox.SrsId = 4326;
            }

            foreach (XmlNode hadNode in doc.SelectNodes($"//heightabovedatum[@type and @server and @service and @rastertheme and @srs and @name='{hadName}']"))
            {
                int srsId = Convert.ToInt32(hadNode.Attributes["srs"].Value);
                if (srsId != bbox.SrsId)
                {
                    using (var transformer = bridge.GeometryTransformer(bbox.SrsId, srsId))
                    {
                        transformer.Transform(bbox);
                        bbox.SrsId = srsId;
                    }
                }

                return await PerformHeightQueryAsync(hadNode, bbox, size, resolution);
            }
        }

        return null;
    }

    public IEnumerable<string> GetHadNodeNames(IBridge bridge, string xmlFilename, string[] types = null)
    {
        List<string> names = new List<string>();

        if (new FileInfo(xmlFilename).Exists)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFilename);

            foreach (XmlNode hadNode in doc.SelectNodes("//heightabovedatum[@type and @name and @server and @service and @rastertheme and @srs]"))
            {
                string type = hadNode.Attributes["type"].Value, name = hadNode.Attributes["name"].Value;
                if (types != null && !types.Contains(type))
                {
                    continue;
                }

                names.Add(name);
            }
        }

        return names;
    }

    public IEnumerable<string> HeightNameNodes(string xmlFilename)
    {
        List<string> names = new List<string>();

        if (new FileInfo(xmlFilename).Exists)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFilename);

            int index = 0;
            foreach (XmlNode hadNode in doc.SelectNodes("//heightabovedatum[@type and @server and @service and @rastertheme and @srs]"))
            {
                names.Add(String.IsNullOrEmpty(hadNode.Attributes["name"]?.Value) ? DefaultHeightName(index) : hadNode.Attributes["name"].Value);
                index++;
            }
        }

        return names;
    }

    async private Task<HeightQueryResult> PerformHeightQueryAsync(IBridge bridge, WebMapping.Core.Abstraction.IMap map, XmlNode hadNode, Point point)
    {
        string type = hadNode.Attributes["type"].Value;
        string server = hadNode.Attributes["server"].Value;
        string service = hadNode.Attributes["service"]?.Value;
        string theme = hadNode.Attributes["rastertheme"]?.Value;
        string name = hadNode.Attributes["name"]?.Value;

        try
        {
            StringBuilder text = new StringBuilder();

            double[] res = null;
            if (type.ToLower() == "ims")
            {
                var connectionProperties = new ArcAxlConnectionProperties()
                {
                    AuthUsername = hadNode.Attributes["user"]?.Value,
                    AuthPassword = hadNode.Attributes["pwd"]?.Value,
                    Timeout = 25
                };

                //string layerId = await query.GetLayerIdAsync(query.server, query.service, theme);
                var layerProps = await bridge.HttpService.GetAxlServiceLayerIdAsync(connectionProperties,
                                                                                    server,
                                                                                    service,
                                                                                    theme);

                if (String.IsNullOrEmpty(layerProps.layerId))
                {
                    throw new Exception($"PerformHeightQueryAsync: Unknown layer {theme}");
                }

                //res = await query.GetRasterInfoAsync(layerID, point.X, point.Y);
                res = await bridge.HttpService.GetAxlServiceRasterInfoAsync(connectionProperties,
                                                                            server,
                                                                            service,
                                                                            layerProps.layerId,
                                                                            point.X, point.Y,
                                                                            layerProps.commaFormat);
                if (res == null || res.Length == 0)
                {
                    throw new Exception("no data");
                }

                string m = res[0].ToString();
                if (hadNode.Attributes["expression"] != null)
                {
                    try
                    {
                        double dm = res[0]; //double.Parse(res[0].ToString().Replace(",", "."), ApiToolGlobals.Nhi);
                        m = string.Format(hadNode.Attributes["expression"].Value, dm);
                    }
                    catch { }
                }
                if (text.Length > 0)
                {
                    text.Append("\n");
                }

                text.Append(m);
            }
            if (type.ToLower() == "ags" || type.ToLower() == "ags-mosaic")
            {
                string user = hadNode.Attributes["user"] != null ? hadNode.Attributes["user"].Value : String.Empty;
                string pwd = hadNode.Attributes["pwd"] != null ? hadNode.Attributes["pwd"].Value : String.Empty;
                string token = hadNode.Attributes["token"] != null ? hadNode.Attributes["token"].Value : String.Empty;
                int tokenExpiration = hadNode.Attributes["tokenExpiration"] != null ? int.Parse(hadNode.Attributes["tokenExpiration"].Value) : 60;

                var agsService = new E.Standard.WebMapping.GeoServices.ArcServer.Rest.MapService()
                {
                    TokenExpiration = tokenExpiration
                };


                agsService.PreInit(String.Empty, server, service, user, pwd, token, map.Environment.UserString(webgisConst.AppConfigPath), null);
                await agsService.InitAsync(map, bridge.RequestContext);

                if (hadNode.Attributes["projectionMethod"] != null && hadNode.Attributes["projectionMethod"].Value.ToLower() == "map")
                {
                    agsService.ProjectionMethode = WebMapping.Core.ServiceProjectionMethode.Map;
                }
                else if (hadNode.Attributes["projectionId"] != null)
                {
                    agsService.ProjectionId = int.Parse(hadNode.Attributes["projectionId"].Value);
                    agsService.ProjectionMethode = WebMapping.Core.ServiceProjectionMethode.Userdefined;
                }
                else if (hadNode.Attributes["srs"] != null)
                {
                    agsService.ProjectionId = int.Parse(hadNode.Attributes["srs"].Value);
                    agsService.ProjectionMethode = WebMapping.Core.ServiceProjectionMethode.Userdefined;
                }

                foreach (var layer in agsService.Layers)
                {
                    if (layer.Name.ToLower() == theme.ToLower())
                    {
                        var features = new WebMapping.Core.Collections.FeatureCollection();
                        var filter = new WebMapping.Core.Filters.SpatialFilter(layer.IdFieldName, point, 1, 1);
                        await layer.GetFeaturesAsync(filter, features, bridge.RequestContext);

                        var rasterFeatures = features.ToArray();

                        if (type.ToLower() == "ags-mosaic")
                        {
                            // Bei Mosaic Layers nur die Features mit "Pixel Value" nehmen
                            rasterFeatures = features
                                .Where(f => f.Attributes != null && f.Attributes.Where(a => a.Name.Equals("pixel value", StringComparison.InvariantCultureIgnoreCase)).Count() > 0)
                                .Select(f => new E.Standard.WebMapping.Core.Feature(f.Attributes.Where(a => a.Name.Equals("pixel value", StringComparison.InvariantCultureIgnoreCase))))
                                .ToArray();
                        }

                        if (rasterFeatures.Length == 1 && hadNode.Attributes["expression"] != null)
                        {
                            try
                            {
                                string m = String.Empty;
                                switch (rasterFeatures[0].Attributes.Count)
                                {
                                    case 0:
                                        break;
                                    case 1:
                                        res = new double[] { ToDouble(rasterFeatures[0].Attributes[0].Value) };
                                        m = string.Format(hadNode.Attributes["expression"].Value, res[0]);
                                        break;
                                    case 2:
                                        res = new double[] { ToDouble(rasterFeatures[0].Attributes[0].Value), ToDouble(rasterFeatures[0].Attributes[1].Value) };
                                        m = string.Format(hadNode.Attributes["expression"].Value, res[0], res[1]);
                                        break;
                                    default:
                                        res = new double[] { ToDouble(rasterFeatures[0].Attributes[0].Value), ToDouble(rasterFeatures[0].Attributes[1].Value), ToDouble(rasterFeatures[0].Attributes[2].Value) };
                                        m = string.Format(hadNode.Attributes["expression"].Value, res[0], res[1], res[2]);
                                        break;
                                }
                                if (text.Length > 0)
                                {
                                    text.Append("\n");
                                }

                                text.Append(m);
                            }
                            catch { }
                        }
                    }
                }
            }
            if (type.ToLower() == "ags-imageserver")
            {
                string user = hadNode.Attributes["user"] != null ? hadNode.Attributes["user"].Value : String.Empty;
                string pwd = hadNode.Attributes["pwd"] != null ? hadNode.Attributes["pwd"].Value : String.Empty;
                string token = hadNode.Attributes["token"] != null ? hadNode.Attributes["token"].Value : String.Empty;
                int tokenExpiration = hadNode.Attributes["tokenExpiration"] != null ? int.Parse(hadNode.Attributes["tokenExpiration"].Value) : 60;

                var imageService = new WebMapping.GeoServices.ArcServer.Rest.ImageServerService()
                {
                    ServiceUrl = service,
                    TokenExpiration = tokenExpiration
                };

                imageService.PreInit(String.Empty, server, service, user, pwd, token, map.Environment.UserString(webgisConst.AppConfigPath), null);
                await imageService.InitAsync(map, bridge.RequestContext);

                var layer = imageService?.Layers.FirstOrDefault();

                if (layer is not null)
                {
                    var features = new WebMapping.Core.Collections.FeatureCollection();
                    var filter = new WebMapping.Core.Filters.SpatialFilter(layer.IdFieldName, point, 1, 1);
                    await layer.GetFeaturesAsync(filter, features, bridge.RequestContext);

                    if (features.Count == 1 && hadNode.Attributes["expression"] != null)
                    {
                        var pixelValue = features[0].Attributes?
                            .Where(a => "pixel".Equals(a.Name, StringComparison.OrdinalIgnoreCase))
                            .FirstOrDefault()?
                            .Value;

                        if(pixelValue is not null)
                        {
                            res = new double[] { ToDouble(pixelValue) };
                            text.Append(string.Format(hadNode.Attributes["expression"].Value, res[0]));
                        }
                    }
                }
            }
            if (type.ToLower() == "voibos")
            {
                // http://voibos.rechenraum.com/voibos/voibos?name=hoehenservice&Koordinate=15,47&CRS=4326
                //                      {
                //                      "abfragestatus": "erfolgreich",
                //	                    "abfragekoordinaten":
                //	                    {
                //                            "rechtswert": 15.000000,
                //		                      "hochwert": 47.000000,
                //		                      "CRS": 4326
                //                      },
                //	                    "hoeheDTM": 895.9,
                //	                    "hoeheDSM": 920.2,
                //	                    "einheit": "Meter über Adria",
                //	                    "datengrundlage": "Laserscanning Höhenmodell 2018 - geoland.at",
                //	                    "flugjahr": "2011",
                //	                    "voibos": "v2019.7-voibos1-build-Jul 22 2019-15:55:22"
                //                      }
                var url = $"{server}?name={service}&Koordinate={point.X.ToString(ApiToolGlobals.Nhi)},{point.Y.ToString(ApiToolGlobals.Nhi)}&CRS={point.SrsId}";

                var json = await bridge.HttpService.GetStringAsync(url);

                JObject jsonObject = (JObject)JsonConvert.DeserializeObject(json);

                List<object> values = new List<object>();
                var expression = hadNode.Attributes["expression"].Value;

                var status = jsonObject["abfragestatus"] as JValue;
                if (status?.Value?.ToString() == "erfolgreich")
                {
                    foreach (var attribute in theme.Replace(";", ",").Split(','))
                    {
                        var jsonToken = jsonObject[attribute] as JValue;
                        if (jsonToken != null && jsonToken.Value != null)
                        {
                            try
                            {
                                values.Add(Convert.ToDouble(jsonToken.Value));
                            }
                            catch
                            {
                                values.Add(jsonToken.Value);
                            }
                        }
                        else
                        {
                            values.Add(0D);
                        }
                    }

                    var placeholders = E.Standard.WebGIS.CMS.Globals.KeyParameters(expression) ?? new string[0];
                    foreach (var placeholder in placeholders)
                    {
                        var val = (jsonObject[placeholder] as JValue).Value?.ToString() ?? String.Empty;
                        expression = expression.Replace($"[{placeholder}]", val);
                    }
                }
                else
                {
                    text.Append($"Status: {status?.Value?.ToString() ?? String.Empty}");
                }

                res = values.Select(v => v is double ? (double)v : 0D).ToArray();
                text.Append(String.Format(expression, values.ToArray()));
            }

            return new HeightQueryResult()
            {
                Name = name,
                Values = res,
                ResultString = text.ToString()
            };
        }
        catch (Exception ex)
        {
            return new HeightQueryResult(false)
            {
                Name = name,
                ResultString = ex.Message
            };
        }
        ;
    }

    async private Task PerformHeightQueryAsync(IBridge bridge, XmlNode hadNode, List<Point> points)
    {

        string type = hadNode.Attributes["type"].Value;
        string server = hadNode.Attributes["server"].Value;
        string service = hadNode.Attributes["service"]?.Value;
        string theme = hadNode.Attributes["rastertheme"]?.Value;
        string name = hadNode.Attributes["name"]?.Value;

        try
        {
            StringBuilder text = new StringBuilder();

            if (type.ToLower() == "ims")
            {
                var connectionProperties = new ArcAxlConnectionProperties()
                {
                    AuthUsername = hadNode.Attributes["user"]?.Value,
                    AuthPassword = hadNode.Attributes["pwd"]?.Value,
                    Timeout = 25
                };

                //string layerId = await query.GetLayerIdAsync(query.server, query.service, theme);
                var layerProps = await bridge.HttpService.GetAxlServiceLayerIdAsync(connectionProperties,
                                                                                    server,
                                                                                    service,
                                                                                    theme);

                int queryMax = 1000;
                for (var interation = 0; interation < points.Count; interation += queryMax)
                {
                    var queryPoints = new List<ArcXmlPoint>();
                    foreach (var point in points.Skip(interation).Take(queryMax))
                    {
                        queryPoints.Add(new ArcXmlPoint(point.X, point.Y, double.NaN));
                    }

                    //if (await query.GetRasterInfoProAsync(layerId, queryPoints))
                    if (await bridge.HttpService.GetAxlServiceRasterInfoProAsync(connectionProperties,
                                                                                server,
                                                                                service,
                                                                                layerProps.layerId,
                                                                                queryPoints,
                                                                                layerProps.commaFormat))
                    {
                        for (int i = 0; i < queryPoints.Count; i++)
                        {
                            points[interation + i].Z = queryPoints[i].Z;
                        }
                    }
                }
            }
            else
            {
                throw new Exception($"Quering multiple points is not implemented for '{type}'");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    async private Task<EnvelopeQueryResult> PerformHeightQueryAsync(XmlNode hadNode, Envelope envelope, Dimension size, double resolution)
    {

        string type = hadNode.Attributes["type"].Value;
        string srs = hadNode.Attributes["srs"].Value;
        string server = hadNode.Attributes["server"].Value;
        string service = hadNode.Attributes["service"]?.Value;
        string theme = hadNode.Attributes["rastertheme"]?.Value;

        float[] dataArray = null;

        try
        {
            StringBuilder text = new StringBuilder();

            if (type.ToLower() == "gview-image")
            {
                string layerID = theme;

                float dpi = (float)(96.0 / resolution);
                var url = $"{service}/export?bbox={envelope.ToBBox()}&layers=show:{theme}&bboxSR={srs}&size={size.Width},{size.Height}&transparent=true&dpi={dpi}&format=png&f=json";

                using (HttpClient httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(url);
                    var imageResultJson = await response.Content.ReadAsStringAsync();

                    var imageResult = JSerializer.Deserialize<JsonExportResponse>(imageResultJson);
                    if (String.IsNullOrEmpty(imageResult.Href))
                    {
                        throw new Exception("Unable to query map image with raster data");
                    }

                    var imageBytes = await httpClient.GetAsync(imageResult.Href);
                    using (var bitmap = Current.Engine.CreateBitmap(await imageBytes.Content.ReadAsStreamAsync()))
                    {
                        var bitmapData = bitmap.LockBitmapPixelData(
                                        BitmapLockMode.ReadWrite,
                                        PixelFormat.Rgba32);

                        try
                        {
                            dataArray = new float[bitmap.Width * bitmap.Height];
                            int dataArrayIndex = 0;
                            unsafe
                            {
                                int pixelSpace = 4;
                                byte* ptr = (byte*)(bitmapData.Scan0);

                                for (int y = 0; y < bitmap.Height; y++)
                                {
                                    for (int x = 0; x < bitmap.Width; x++)
                                    {
                                        float h = BitConverter.ToInt32(new byte[] { ptr[0], ptr[1], ptr[2], 0 }, 0) / 100f;

                                        dataArray[dataArrayIndex++] = h;

                                        ptr += pixelSpace;
                                    }

                                    ptr += bitmapData.Stride - bitmapData.Width * pixelSpace;
                                }

                            }
                        }
                        finally
                        {
                            if (bitmapData != null)
                            {
                                bitmap.UnlockBitmapPixelData(bitmapData);
                            }
                        }
                    }

                    return new EnvelopeQueryResult()
                    {
                        BoundingBox = new double[] { imageResult.Extent.Xmin, imageResult.Extent.Ymin, imageResult.Extent.Xmax, imageResult.Extent.Ymax },
                        BoundingBoxEpsg = envelope.SrsId,
                        ArraySize = new int[] { imageResult.Width, imageResult.Height },
                        Data = dataArray
                    };
                }
            }
            else
            {
                throw new Exception($"Quering multiple points is not implemented for '{type}'");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    #region Helper

    private double ToDouble(object val)
    {
        if (val == null)
        {
            return 0D;
        }

        try
        {
            if (val is double)
            {
                return (double)val;
            }

            return val.ToString().ToPlatformDouble(); //double.Parse(val.ToString().Replace(",", "."), ApiToolGlobals.Nhi);
        }
        catch
        {
            return 0D;
        }
    }

    private string DefaultHeightName(int index) => $"H{index + 1}";

    #endregion

    #region Resul Classes

    public class HeightQueryResult
    {
        public HeightQueryResult(bool isValid = true)
        {
            this.IsValid = isValid;
        }

        public string Name { get; set; }
        public double[] Values { get; set; }
        public string ResultString { get; set; }

        public bool IsValid { get; }
    }

    public class EnvelopeQueryResult
    {
        public double[] BoundingBox { get; set; }
        public int BoundingBoxEpsg { get; set; }
        public int[] ArraySize { get; set; }
        public float[] Data { get; set; }
    }

    #endregion
}
