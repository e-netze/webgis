using E.Standard.Extensions.Compare;
using E.Standard.GeoJson;
using E.Standard.GeoJson.Extensions;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.Web.Models;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace E.Standard.WebMapping.GeoServices.OGC.WMS;

class OgcWmsLayer : Layer, ILayer2
{
    public OgcWmsLayer(string name, string id, IMapService service, bool queryable)
        : base(name, id, service, queryable: queryable)
    {
    }
    public OgcWmsLayer(string name, string id, LayerType type, IMapService service, bool queryable)
        : base(name, id, type, service, queryable: queryable)
    {
    }

    async public override Task<bool> GetFeaturesAsync(QueryFilter filter, FeatureCollection features, IRequestContext requestContext) // , bool isCSVExport = false
    {
        if (filter is SpatialFilter &&
            ((SpatialFilter)filter).QueryShape != null &&
            _service is WmsService)
        {
            WmsService service = (WmsService)_service;

            Point point = ((SpatialFilter)filter).QueryShape.ShapeEnvelope.CenterPoint, transformedPoint = null;
            Point imagePoint = service._map.WorldToImage(point);

            StringBuilder req = new StringBuilder();
            req.Append("REQUEST=GetFeatureInfo");

            if (service.GetFeatureInfoOnlineResouce.IndexOf("?service=wms", StringComparison.InvariantCultureIgnoreCase) < 0 &&
                service.GetFeatureInfoOnlineResouce.IndexOf("&service=wms", StringComparison.InvariantCultureIgnoreCase) < 0)
            {
                req.Append("&SERVICE=WMS");
            }

            #region Version

            switch (service._version)
            {
                case WMS_Version.version_1_1_1:
                    req.Append("&VERSION=1.1.1");
                    break;
                case WMS_Version.version_1_3_0:
                    req.Append("&VERSION=1.3.0");
                    break;
            }

            #endregion

            #region QueryLayer
            string layerId = this.ID, styles = String.Empty;
            if (layerId.Contains("|"))
            {
                int pos = layerId.LastIndexOf("|");
                styles = layerId.Substring(pos + 1, layerId.Length - pos - 1);
                layerId = layerId.Substring(0, pos);
            }

            req.Append("&LAYERS=" + layerId);
            req.Append("&QUERY_LAYERS=" + layerId);
            // Disy ;-)
            req.Append("&SELECTED_LAYERS=" + layerId);
            if (!String.IsNullOrEmpty(styles))
            {
                req.Append("&STYLES=" + styles);
            }
            #endregion

            #region SRS
            if (service._map.SpatialReference != null)
            {
                if (service._version == WMS_Version.version_1_3_0)
                {
                    req.Append("&CRS=EPSG:" + service._map.SpatialReference.Id);
                }
                else
                {
                    req.Append("&SRS=EPSG:" + service._map.SpatialReference.Id);
                }
            }
            #endregion

            #region BBOX
            req.Append("&BBOX=");
            if (service._version == WMS_Version.version_1_3_0 && service._map.SpatialReference != null)
            {
                switch (service._map.SpatialReference.AxisX)
                {
                    case AxisDirection.North:
                    case AxisDirection.South: req.Append(service._map.Extent.MinY.ToPlatformNumberString() + ","); break;
                    case AxisDirection.East:
                    case AxisDirection.West: req.Append(service._map.Extent.MinX.ToPlatformNumberString() + ","); break;
                }
                switch (service._map.SpatialReference.AxisY)
                {
                    case AxisDirection.North:
                    case AxisDirection.South: req.Append(service._map.Extent.MinY.ToPlatformNumberString() + ","); break;
                    case AxisDirection.East:
                    case AxisDirection.West: req.Append(service._map.Extent.MinX.ToPlatformNumberString() + ","); break;
                }
                switch (service._map.SpatialReference.AxisX)
                {
                    case AxisDirection.North:
                    case AxisDirection.South: req.Append(service._map.Extent.MaxY.ToPlatformNumberString() + ","); break;
                    case AxisDirection.East:
                    case AxisDirection.West: req.Append(service._map.Extent.MaxX.ToPlatformNumberString() + ","); break;
                }
                switch (service._map.SpatialReference.AxisY)
                {
                    case AxisDirection.North:
                    case AxisDirection.South: req.Append(service._map.Extent.MaxY.ToPlatformNumberString()); break;
                    case AxisDirection.East:
                    case AxisDirection.West: req.Append(service._map.Extent.MaxX.ToPlatformNumberString()); break;
                }
            }
            else
            {
                req.Append(service._map.Extent.MinX.ToPlatformNumberString() + ",");
                req.Append(service._map.Extent.MinY.ToPlatformNumberString() + ",");
                req.Append(service._map.Extent.MaxX.ToPlatformNumberString() + ",");
                req.Append(service._map.Extent.MaxY.ToPlatformNumberString());
            }
            #endregion

            #region ImageSize
            req.Append("&WIDTH=" + service._map.ImageWidth);
            req.Append("&HEIGHT=" + service._map.ImageHeight);
            #endregion

            #region X,Y
            if (service._version == WMS_Version.version_1_3_0 && service._map.SpatialReference != null)
            {
                //switch (service._map.SpatialReference.AxisX)
                //{
                //    case AxisDirection.North:
                //    case AxisDirection.South:
                //        req.Append("&X=" + (int)qPoint.Y);
                //        req.Append("&Y=" + (int)qPoint.X);
                //        break;
                //    case AxisDirection.East:
                //    case AxisDirection.West:
                //        req.Append("&X=" + (int)qPoint.X);
                //        req.Append("&Y=" + (int)qPoint.Y);
                //        break;
                //}

                // Achenrichtung hier nicht berücksichtigen! Wurde mit Kagis Wasser Profile Dienst getestet und funktioniert!!
                req.Append("&X=" + (int)imagePoint.X);
                req.Append("&Y=" + (int)imagePoint.Y);
            }
            else
            {
                req.Append("&X=" + (int)imagePoint.X);
                req.Append("&Y=" + (int)imagePoint.Y);
            }
            #endregion

            #region Feature Count

            req.Append("&FEATURE_COUNT=" + service.GetFeatureInfoFeatureCount);

            #endregion

            #region InfoFormat

            var infoFormat = service.GetFeatureInfoFormat ?? String.Empty;

            if (infoFormat.Contains("+"))
            {
                infoFormat = infoFormat.Replace("+", HttpUtility.UrlEncode("+"));  // Esri: application/geo+json, ...
            }

            req.Append("&INFO_FORMAT=" + infoFormat);

            #endregion

            string url = service.AppendToUrl(service.GetFeatureInfoOnlineResouce, req.ToString().Replace(" ", "%20"));
            if (this.Service != null && this.Service.Map != null)
            {
                url = url.Replace("[webgis-username]", this.Service.Map.Environment.UserString(webgisConst.UserName));
            }

            var httpService = requestContext.Http;
            //string resp = await WebHelper.DownloadStringAsync(url, service._conn.GetProxy(url), null, service.X509Certificate, service.AuthUsername, service.AuthPassword);
            string resp = await httpService.GetStringAsync(url, new RequestAuthorization() { ClientCerticate = service.X509Certificate, Username = service.AuthUsername, Password = service.AuthPassword });

            //Console.WriteLine("WMS GetFeatures:");
            //Console.WriteLine(url);
            //Console.WriteLine(resp);
            //Console.WriteLine("--------------");

            if (requestContext.Trace && _service != null)
            {
                var requestLogger = requestContext.GetRequiredService<IGeoServiceRequestLogger>();

                requestLogger.LogString(_service.Server, _service.Service, "GetFeatures", "WMS Request: " + url);
                requestLogger.LogString(_service.Server, _service.Service, "GetFeatures", "WMS Response: " + resp);
            }

            string respLower = resp.ToLower();

            if ("application/json".Equals(service.GetFeatureInfoFormat, StringComparison.InvariantCultureIgnoreCase) ||
                "application/geojson".Equals(service.GetFeatureInfoFormat, StringComparison.InvariantCultureIgnoreCase) ||
                "application/geo+json".Equals(service.GetFeatureInfoFormat, StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    var geoJsonFeatures = JSerializer.Deserialize<GeoJsonFeatures>(resp);
                    int geoJsonEpsg = geoJsonFeatures?.Crs.TryGetEpsg().OrTake(4326) ?? 4326;
                    var geoJsonSRef = Globals.SpatialReferences.ById(geoJsonEpsg);

                    if (geoJsonFeatures.Features != null)
                    {
                        foreach (var geoJsonFeature in geoJsonFeatures.Features)
                        {
                            var feature = new Feature();
                            feature.Shape = geoJsonFeature.ToShape();

                            #region Use Click Point ...

                            if (feature.Shape == null)
                            {
                                if (transformedPoint == null)
                                {
                                    transformedPoint = new Point(point);
                                    using (var transformer = new GeometricTransformerPro(((SpatialFilter)filter).FilterSpatialReference, filter.FeatureSpatialReference))
                                    {
                                        transformer.Transform(transformedPoint);
                                    }
                                }

                                feature.Shape = new Point(transformedPoint);
                            }
                            else
                            {
                                if (filter.FeatureSpatialReference?.Id != geoJsonEpsg)
                                {
                                    using (var transfromer = new GeometricTransformerPro(geoJsonSRef, ((SpatialFilter)filter).FeatureSpatialReference))
                                    {
                                        transfromer.Transform(feature.Shape);
                                    }
                                }
                            }

                            #endregion

                            geoJsonFeature.PropertiesToDict();
                            var properties = (IDictionary<string, object>)geoJsonFeature.Properties;

                            if (properties != null)
                            {
                                foreach (var key in properties.Keys)
                                {
                                    var value = properties[key]?.ToString();
                                    if (!String.IsNullOrEmpty(value))
                                    {
                                        feature.Attributes.Add(new Core.Attribute(key, value));
                                    }
                                }
                            }

                            features.Add(feature);
                        }
                    }
                }
                catch (Exception ex)
                {
                    resp = "Parse GeoJson: " + ex.Message;
                }
            }
            else
            {
                if (respLower.Trim().StartsWith("<?xml ") || resp.StartsWith("<FeatureCollection"))
                {
                    #region Parse Xml

                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(resp);
                        XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
                        ns.AddNamespace("gml", "http://www.opengis.net/gml");
                        ns.AddNamespace("wfs", "http://www.opengis.net/wfs");
                        string featureNameSpace = String.Empty;
                        foreach (XmlNode nsNode in doc.SelectNodes(@"//namespace::*[not(. = ../../namespace::*)]"))
                        {
                            if (nsNode.Name == "xmlns:feature")  // disy ;-)
                            {
                                if (!String.IsNullOrEmpty(featureNameSpace))
                                {
                                    ns.RemoveNamespace("feature", featureNameSpace);
                                }

                                featureNameSpace = nsNode.InnerText;
                                ns.AddNamespace("feature", featureNameSpace);
                            }
                            else if (nsNode.Name == "xmlns" && String.IsNullOrEmpty(featureNameSpace))
                            {
                                featureNameSpace = nsNode.InnerText;
                                ns.AddNamespace("feature", featureNameSpace);
                            }
                            else if (nsNode.Name.Contains(":") && !ns.HasNamespace(nsNode.Name.Split(':')[1]))
                            {
                                ns.AddNamespace(nsNode.Name.Split(':')[1], nsNode.InnerText);
                            }
                        }
                        XmlNodeList xmlFeatures = doc.SelectNodes("//FeatureCollection[@themeName='" + layerId + "']/featureMember", ns);
                        if (xmlFeatures == null || xmlFeatures.Count == 0)
                        {
                            if (!String.IsNullOrEmpty(featureNameSpace))
                            {
                                xmlFeatures = doc.SelectNodes("//wfs:FeatureCollection/gml:featureMember/feature:" + layerId, ns);
                                if (xmlFeatures == null || xmlFeatures.Count == 0)
                                {
                                    xmlFeatures = doc.SelectNodes("//wfs:FeatureCollection/gml:featureMember/" + layerId, ns);
                                }

                                if (xmlFeatures == null || xmlFeatures.Count == 0)  // Wien (Geoland) ist anders!!!!
                                {
                                    xmlFeatures = doc.SelectNodes("//feature:FeatureCollection/gml:featureMember/" + layerId, ns);
                                }

                                if (xmlFeatures == null || xmlFeatures.Count == 0)
                                {
                                    xmlFeatures = doc.SelectNodes("//feature:FeatureCollection/gml:featureMember/feature:" + layerId, ns);
                                }
                            }
                            else
                            {
                                xmlFeatures = doc.SelectNodes("//wfs:FeatureCollection/gml:featureMember/" + layerId, ns);
                            }
                        }
                        if (xmlFeatures == null || xmlFeatures.Count == 0)
                        {
                            xmlFeatures = doc.SelectNodes("//" + layerId + "_layer/" + layerId + "_feature");
                        }

                        if (xmlFeatures == null || xmlFeatures.Count == 0)  // für Disy
                        {
                            try
                            {
                                xmlFeatures = doc.SelectNodes("//wfs:FeatureCollection/gml:featureMember/feature:*", ns);
                            }
                            catch { }
                        }

                        if (xmlFeatures != null && xmlFeatures.Count > 0)
                        {
                            #region Spalten auslesen
                            UniList<string> columns = new UniList<string>();
                            XmlNodeList fieldNodes = xmlFeatures[0].SelectNodes("field[@name and @value]");
                            if (fieldNodes != null && fieldNodes.Count > 0)
                            {
                                foreach (XmlNode fieldNode in fieldNodes)
                                {
                                    columns.Add(fieldNode.Attributes["name"].Value);
                                }
                            }
                            else
                            {
                                foreach (XmlNode fieldNode in xmlFeatures[0].ChildNodes)
                                {
                                    if (fieldNode.Name.StartsWith("gml:") || fieldNode.Name.StartsWith("wfs:") ||
                                        fieldNode.SelectSingleNode("gml:*", ns) != null)
                                    {
                                        if (fieldNode.Name == "gml:boundedBy") // Nur boundedBox -> Schauen obs auch andere Geometrie gibt
                                        {
                                            bool hasRealGeometry = false;
                                            foreach (XmlNode gmlNode in xmlFeatures[0].SelectNodes("*/gml:*", ns))
                                            {
                                                if (gmlNode.Name != "gml:Box")
                                                {
                                                    hasRealGeometry = true;
                                                    break;
                                                }
                                            }
                                            if (hasRealGeometry)
                                            {
                                                continue;
                                            }
                                        }
                                        columns.Insert(0, fieldNode.Name);
                                    }
                                    else if (fieldNode.Name == "feature:geometry")  // disy ;-)
                                    {
                                        columns.Insert(0, fieldNode.ChildNodes[0].Name);
                                    }
                                    else
                                    {
                                        columns.Add(fieldNode.Name);
                                    }
                                }
                            }
                            #endregion

                            string srsName;

                            #region Features auslesen // Geoland (hier muss im CMS application/vnd.ogc.gml->features angegeben werden... ->features händisch!!)

                            if (service.GetFeatureInfoTarget.ToLower() == "features")
                            {
                                foreach (XmlNode xmlFeature in xmlFeatures)
                                {
                                    Feature feature = new Feature();
                                    foreach (string column in columns)
                                    {
                                        if (fieldNodes != null && fieldNodes.Count > 0)
                                        {
                                            XmlNode fieldNode = xmlFeature.SelectSingleNode("field[@name='" + column + "' and @value]", ns);
                                            if (fieldNode != null)
                                            {
                                                feature.Attributes.Add(new Core.Attribute(fieldNode.Attributes["name"].Value, fieldNode.Attributes["value"].Value));
                                            }
                                        }
                                        else
                                        {
                                            XmlNode fieldNode = xmlFeature.SelectSingleNode(column, ns);
                                            if (fieldNode == null && !String.IsNullOrEmpty(featureNameSpace))  // Wien ist anders!!
                                            {
                                                fieldNode = xmlFeature.SelectSingleNode("feature:" + column, ns);
                                            }

                                            string val = String.Empty;

                                            if (fieldNode != null)
                                            {
                                                if (fieldNode.Name.StartsWith("gml:"))
                                                {
                                                    feature.Shape = GML.GeometryTranslator.GML2Geometry(fieldNode.SelectSingleNode("gml:*", ns).OuterXml, GML.GmlVersion.v1, out srsName);

                                                }
                                                else if (fieldNode.Name.StartsWith("wfs:") || fieldNode.SelectSingleNode("gml:*", ns) != null)
                                                {
                                                    feature.Shape = GML.GeometryTranslator.GML2Geometry(fieldNode.SelectSingleNode("gml:*", ns).OuterXml, GML.GmlVersion.v1, out srsName);
                                                }
                                                else
                                                {
                                                    val = fieldNode.InnerText;
                                                }
                                            }
                                            feature.Attributes.Add(new Core.Attribute(column, val));
                                        }
                                    }

                                    features.Add(feature);
                                }
                                return true;
                            }

                            #endregion

                            StringBuilder tab = new StringBuilder();

                            foreach (XmlNode xmlFeature in xmlFeatures)
                            {
                                tab.Append("<table callpadding=1 cellspacing=1 width=100% class=webgis-result-table>");

                                foreach (string column in columns)
                                {
                                    #region Title

                                    string title = column.Contains(":") ? Globals.ShortName(column.Split(':')[1]) : Globals.ShortName(column);
                                    if (column.StartsWith("feature:") && column != "feature:geometry") // disy ;-)
                                    {
                                        title = column.Split(':')[1];
                                    }

                                    if (title == "boundedBy")
                                    {
                                        continue;  // Kein ZoomTo im webGIS 5
                                    }

                                    #endregion

                                    #region Value

                                    string val = null;
                                    if (fieldNodes != null && fieldNodes.Count > 0)
                                    {
                                        XmlNode fieldNode = xmlFeature.SelectSingleNode("field[@name='" + column + "' and @value]", ns);
                                        if (fieldNode != null)
                                        {
                                            val = fieldNode.Attributes["value"].Value;
                                        }
                                    }
                                    else
                                    {
                                        XmlNode fieldNode = xmlFeature.SelectSingleNode(column, ns);
                                        if (fieldNode != null)
                                        {
                                            //if (fieldNode.Name.StartsWith("gml:"))
                                            //{
                                            //    Shape shape = GML.GeometryTranslator.GML2Geometry(fieldNode.SelectSingleNode("gml:*", ns).OuterXml, GML.GmlVersion.v1, out srsName);
                                            //    val = AppendZoomToButton(shape, shapes.Count);
                                            //    shapes.Add(shape);
                                            //}
                                            //else if (fieldNode.Name.StartsWith("wfs:") || fieldNode.SelectSingleNode("gml:*", ns) != null)
                                            //{
                                            //    Shape shape = GML.GeometryTranslator.GML2Geometry(fieldNode.SelectSingleNode("gml:*", ns).OuterXml, GML.GmlVersion.v1, out srsName);
                                            //    val = AppendZoomToButton(shape, shapes.Count);
                                            //    shapes.Add(shape);
                                            //}
                                            //else
                                            {
                                                val = fieldNode.InnerText;
                                            }
                                        }
                                    }

                                    #endregion

                                    if (!String.IsNullOrWhiteSpace(val))
                                    {
                                        tab.Append("<tr>");
                                        tab.Append("<td class=webgis-result-table-header>" + title + "</td>");
                                        tab.Append("<td class=webgis-result-table-cell>" + val + "</td>");
                                        tab.Append("</tr>");
                                    }
                                }

                                tab.Append("</table>");
                            }


                            resp = tab.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        resp = "Exception: " + ex.Message/* + "<br/>" + ex.StackTrace + "<br/><br/>" + resp*/;
                    }

                    #endregion
                }
                else if (respLower.Contains("<body>") && respLower.Contains("</body>"))
                {
                    int pos1 = respLower.IndexOf("<body>");
                    int pos2 = respLower.IndexOf("</body>");

                    resp = resp.Substring(pos1 + 6, pos2 - pos1 - 6).Replace("\n", "").Replace("\n", "");
                    while (resp.Contains("> "))
                    {
                        resp = resp.Replace("> ", ">");
                    }
                }
                features.ResultText = resp;
            }

            return true;
        }

        return false;
    }

    private T2 IDictionary<T1, T2>()
    {
        throw new NotImplementedException();
    }

    override public ILayer Clone(IMapService parent)
    {
        if (parent is null)
        {
            return null;
        }

        OgcWmsLayer clone = new OgcWmsLayer(this.Name, this.ID, this.Type,
            parent, queryable: this.Queryable);
        clone.ClonePropertiesFrom(this);
        return clone;
    }

    #region Helper

    private string AppendZoomToButton(Shape shape, int id)
    {
        return String.Empty;
    }

    #endregion

    #region ILayer2 Member

    async public Task<int> HasFeaturesAsync(QueryFilter filter, IRequestContext requestContext)
    {
        if (_service is WmsService && ((WmsService)_service).GetFeatureInfoFormat.ToLower() == "application/vnd.ogc.gml")
        {
            FeatureCollection features = new FeatureCollection();

            await GetFeaturesAsync(filter, features, requestContext);

            if (features.Count > 0)
            {
                return features.Count;
            }

            if (!String.IsNullOrEmpty(features.ResultText) &&
                features.ResultText.StartsWith("<table "))
            {
                return 1;
            }
        }
        if (_service is WmsService && ((WmsService)_service).GetFeatureInfoFormat.ToLower() == "text/html")
        {
            FeatureCollection features = new FeatureCollection();

            await GetFeaturesAsync(filter, features, requestContext);

            if (features.Count > 0)
            {
                return features.Count;
            }

            if (!String.IsNullOrEmpty(features.ResultText) &&
                (features.ResultText.Contains("<table ") || features.ResultText.Contains("<table>")))
            {
                return 1;
            }
        }
        if ("application/json".Equals(((WmsService)_service).GetFeatureInfoFormat, StringComparison.InvariantCultureIgnoreCase) ||
            "application/geojson".Equals(((WmsService)_service).GetFeatureInfoFormat, StringComparison.InvariantCultureIgnoreCase))
        {
            FeatureCollection features = new FeatureCollection();

            await GetFeaturesAsync(filter, features, requestContext);

            return features.Count;
        }

        return 0;
    }

    public Task<Shape> FirstFeatureGeometryAsync(QueryFilter filter, IRequestContext requestContext)
    {
        if (_service is WmsService && ((WmsService)_service).GetFeatureInfoFormat.ToLower() == "application/vnd.ogc.gml")
        {
        }

        return Task.FromResult<Shape>(null);
    }

    public Task<Feature> FirstFeatureAsync(QueryFilter filter, IRequestContext requestContext)
    {
        if (_service is WmsService && ((WmsService)_service).GetFeatureInfoFormat.ToLower() == "application/vnd.ogc.gml")
        {
        }

        return Task.FromResult<Feature>(null);
    }

    #endregion
}
