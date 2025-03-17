using E.Standard.Web.Models;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.OGC.Extensions;
using E.Standard.WebMapping.GeoServices.OGC.GML;
using E.Standard.WebMapping.GeoServices.OGC.WFS.Helper;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace E.Standard.WebMapping.GeoServices.OGC.WFS;

public class OgcWfsLayer : Layer, ILayer2
{
    public const string DefaultIdFieldName = "GML:ID";
    private string _targetNamespace = String.Empty;

    public OgcWfsLayer(string name, string id, IMapService service, bool queryable)
        : base(name, id, service, queryable: queryable)
    {
    }
    public OgcWfsLayer(string name, string id, LayerType type, IMapService service, bool queryable)
        : base(name, id, type, service, queryable: queryable)
    {
    }

    internal string TargetNamespace
    {
        get { return _targetNamespace; }
        set { _targetNamespace = value; }
    }

    async public override Task<bool> GetFeaturesAsync(QueryFilter query, FeatureCollection result, IRequestContext requestContext)
    {
        return await GetFeaturesProAsync(query, result, query.FeatureLimit, requestContext);
    }

    async private Task<bool> GetFeaturesProAsync(QueryFilter query, FeatureCollection result, int maxFeatures, IRequestContext requestContext)
    {
        WfsService service = _service as WfsService;

        if (service == null /*|| _service.Map.SpatialReference == null*/)
        {
            return false;
        }

        var httpService = requestContext.Http;
        // GML:ID darf nicht als Feld übergeben werden
        query.SubFields = query.SubFields.Replace(DefaultIdFieldName, "").Trim().Replace("  ", " ");

        bool interpretSrsAxis = service.InterpretSrsAxix;

        string response = String.Empty;
        if (query.FeatureSpatialReference == null)
        {
            query.FeatureSpatialReference = service.Map.SpatialReference;
        }

        if (maxFeatures <= 0)
        {
            maxFeatures = 100;
        }

        if (String.IsNullOrEmpty(service._GF_HttpPost) &&
            !String.IsNullOrEmpty(service._GF_HttpGet))
        {
            string param = String.Empty;

            if (service._version == WFS_Version.version_1_0_0)
            {
                param = $"SERVICE=WFS&VERSION=1.0.0&REQUEST=GetFeature&TYPENAME={this.ID}&MAXFEATURES={maxFeatures}&FILTER=";
            }
            else if (service._version == WFS_Version.version_1_1_0)
            {
                param = $"SERVICE=WFS&VERSION=1.1.0&REQUEST=GetFeature&TYPENAME={this.ID}&MAXFEATURES={maxFeatures}&FILTER=";
            }

            string wfsFilter = Helper.FilterHelper.ToWFS(this, query, new Helper.Filter_Capabilities(), service._gmlVersion);
            param += wfsFilter;

            string url = service._GF_HttpGet;
            httpService.AppendParametersToUrl(url, param);

            //response = await WebHelper.DownloadStringAsync(url, service._conn.GetProxy(url),null, null, service.AuthUsername, service.AuthPassword);
            response = await httpService.GetStringAsync(url, new RequestAuthorization() { Username = service.AuthUsername, Password = service.AuthPassword });
        }
        else if (!String.IsNullOrEmpty(service._GF_HttpPost))
        {
            string param = $"SERVICE=WFS&MAXFEATURES={maxFeatures}";//&VERSION=1.0.0&REQUEST=GetFeature&TYPENAME=" + this.Name + "&MAXFEATURES=10&FILTER=";

            string url = service._GF_HttpPost;
            url = httpService.AppendParametersToUrl(url, param);

            string wfsFilter = String.Empty;
            if (service._version == WFS_Version.version_1_0_0)
            {
                wfsFilter = this.GetFeature1_0_0(
                    query,
                    query.FeatureSpatialReference != null ? "EPSG:" + query.FeatureSpatialReference.EPSG : String.Empty,
                    new Filter_Capabilities(),
                    maxFeatures: maxFeatures);
            }
            else if (service._version == WFS_Version.version_1_1_0)
            {
                if (query is SpatialFilter && ((SpatialFilter)query).FilterSpatialReference == null)
                {
                    ((SpatialFilter)query).FilterSpatialReference = _service.Map.SpatialReference;
                }

                wfsFilter = this.GetFeature1_1_0(
                    query,
                    query.FeatureSpatialReference != null ? "EPSG:" + query.FeatureSpatialReference.EPSG : String.Empty,
                    new Filter_Capabilities(),
                    maxFeatures: maxFeatures,
                    ignoreAxis: service is WfsService ? !service.InterpretSrsAxix : true);
            }
            //response = await WebHelper.HttpSendRequestAsync(url, "POST",
            //    Encoding.UTF8.GetBytes(wfsFilter), service._conn.GetProxy(url), service.AuthUsername, service.AuthPassword, Encoding.UTF8);
            response = await httpService.PostXmlAsync(url,
                                                      wfsFilter,
                                                      new RequestAuthorization(service.AuthUsername, service.AuthPassword));
        }

        try
        {
            StringReader stringReader = new StringReader(response);
            XmlTextReader xmlReader = new XmlTextReader(stringReader);

            XmlDocument doc = new XmlDocument();
            //doc.LoadXml(response);
            XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("GML", "http://www.opengis.net/gml");
            ns.AddNamespace("WFS", "http://www.opengis.net/wfs");
            ns.AddNamespace("OGC", "http://www.opengis.net/ogc");
            ns.AddNamespace("myns", _targetNamespace);

            //XmlNode featureCollection = doc.SelectSingleNode("WFS:FeatureCollection", ns);
            //if (featureCollection == null)
            //    featureCollection = doc.SelectSingleNode("GML:FeatureCollection", ns);
            //if (featureCollection == null) return null;

            using (FeatureCursor2 cursor = new FeatureCursor2(this, xmlReader, ns, query, service._gmlVersion, null, interpretSrsAxis))
            {
                Feature feature;

                while ((feature = cursor.NextFeature) != null)
                {
                    result.Add(feature);
                    if (result.Count >= maxFeatures)
                    {
                        break;
                    }
                }
            }

            int maxId = Math.Max(1, result.Min(f => f.Oid));

            result.ForEach(f =>
            {
                if (f.Oid < 0)
                {
                    f.Oid = maxId++;
                }
            });

            result.Layer = this;
            result.Query = query;

            return true;
        }
        catch
        {
            return false;
        }
    }

    override public ILayer Clone(IMapService parent)
    {
        if (parent is null)
        {
            return null;
        }

        OgcWfsLayer clone = new OgcWfsLayer(this.Name, this.ID, this.Type,
            parent, queryable: this.Queryable);
        clone.ClonePropertiesFrom(this);
        clone.TargetNamespace = _targetNamespace;
        return clone;
    }

    #region ILayer2

    async public Task<int> HasFeaturesAsync(QueryFilter filter, IRequestContext requestContext)
    {
        FeatureCollection features = new FeatureCollection();
        await GetFeaturesAsync(filter, features, requestContext);

        int count = features.Count;
        return count;
    }

    async public Task<Shape> FirstFeatureGeometryAsync(QueryFilter filter, IRequestContext requestContext)
    {
        FeatureCollection features = new FeatureCollection();
        await GetFeaturesProAsync(filter, features, 1, requestContext);

        if (features.Count > 0)
        {
            return features[0].Shape;
        }

        return null;
    }

    async public Task<Feature> FirstFeatureAsync(QueryFilter filter, IRequestContext requestContext)
    {
        FeatureCollection features = new FeatureCollection();
        await GetFeaturesProAsync(filter, features, 1, requestContext);

        if (features.Count > 0)
        {
            return features[0];
        }

        return null;
    }

    #endregion

    public override string IdFieldName
    {
        get
        {
            return DefaultIdFieldName;
        }
    }

    #region Static Members

    static public bool CheckCondition(Feature feature, string where)
    {
        if (String.IsNullOrWhiteSpace(where))
        {
            return true;
        }

        if (!where.ToLower().Contains(" and ") && !where.ToLower().Contains(" or "))
        {
            if (where.StartsWith(DefaultIdFieldName + "="))
            {
                int id = int.Parse(where.Substring((DefaultIdFieldName + "=").Length));
                return feature.Oid == id;
            }
            if (where.StartsWith(DefaultIdFieldName + " in ("))
            {
                int[] ids = where.Substring((DefaultIdFieldName + " in (").Length, where.Length - (DefaultIdFieldName + " in (").Length - 1)
                                .Split(',')
                                .Select(i => int.Parse(i))
                                .ToArray();

                return ids.Contains(feature.Oid);
            }
        }

        return true;  // Im Zweifelsfall ist das Feature dabei ;)   .... Damit selection auch noch funktioniert, wenn über eine Detailsuche gesucht wurde. Diese Funktion sollte eigentlich lösen, dass nicht alle Features gehighligtet werden, wenn man in der Ergebnisliste auf die Lupe klickt
    }

    #endregion
}
