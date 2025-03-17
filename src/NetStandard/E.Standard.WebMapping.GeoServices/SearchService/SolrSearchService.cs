using E.Standard.CMS.Core;
using E.Standard.Platform;
using E.Standard.Web.Abstractions;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.SearchService;

public class SolrSearchService : ISearchService
{
    protected string _name, _id;
    protected string _serviceUrl, _suggestedTextField, _thumbnailField, _geometryField, _subtextField, _link;
    protected int _rows = 5;
    protected SpatialReference _sRef = null, _sRef4326 = null;
    protected ISpatialReferenceStore _sRefStore = null;

    public SolrSearchService(CmsNode node)
    {
        this.Id = node.Url;
        this.Name = node.Name;

        _serviceUrl = node.LoadString("serviceUrl");
        _suggestedTextField = node.LoadString("suggestedtext");
        _thumbnailField = node.LoadString("thumbnail");
        _geometryField = node.LoadString("geo");
        _subtextField = node.LoadString("subtext");
        _link = node.LoadString("link");
        _rows = (int)node.Load("rows", 5);

        int projId = (int)node.Load("projid", -1);
        if (projId > 0 /* && projId != 4326*/)
        {
            _sRef = CoreApiGlobals.SRefStore.SpatialReferences.ById(projId);
            _sRef4326 = CoreApiGlobals.SRefStore.SpatialReferences.ById(4326);
        }

        _sRefStore = CoreApiGlobals.SRefStore;
    }

    #region ISearchService Member

    public string Name
    {
        get { return _name; }
        set { _name = value; }
    }

    public string Id
    {
        get { return _id; }
        set { _id = value; }
    }

    public string CopyrightId { get; set; }

    async public Task<SearchServiceItems> QueryAsync(IHttpService httpService, string term, int rows, int targetProjId = 4326)
    {
        if (this is ISearchService2)
        {
            return await ((ISearchService2)this).Query2Async(httpService, term, rows, null, targetProjId);
        }

        string url = String.Format(_serviceUrl, term).Replace("{term}", term).Replace("[term]", term);
        url += "&rows=" + (rows > 0 ? rows : _rows);

        string json = await httpService.GetStringAsync(url, encoding: Encoding.UTF8);
        var lucType = JsonConvert.DeserializeObject<LucType>(json);

        List<SearchServiceItem> items = new List<SearchServiceItem>();

        SpatialReference targetSRef = _sRef4326;
        if (targetProjId > 0 && targetProjId != 4326 && _sRefStore != null)
        {
            targetSRef = _sRefStore.SpatialReferences.ById(targetProjId);
        }

        if (lucType.response != null &&
            lucType.response.docs is Newtonsoft.Json.Linq.JArray &&
            ((Newtonsoft.Json.Linq.JArray)lucType.response.docs).Count > 0)
        {
            using (var transformer = new GeometricTransformer())
            {
                if (_sRef != null && targetSRef != null && _sRef.Id != targetSRef.Id)
                {
                    transformer.FromSpatialReference(_sRef.Proj4, !_sRef.IsProjective);
                    transformer.ToSpatialReference(targetSRef.Proj4, !targetSRef.IsProjective);
                }
                foreach (Newtonsoft.Json.Linq.JObject jObject in (Newtonsoft.Json.Linq.JArray)lucType.response.docs)
                {
                    var item = new SearchServiceItem(this)
                    {
                        SuggestText = GetJsonValue(jObject, _suggestedTextField),
                        ThumbnailUrl = GetJsonValue(jObject, _thumbnailField),
                        Subtext = GetJsonValue(jObject, _subtextField),
                        Link = GetJsonValue(jObject, _link),
                        Score = 0D
                    };

                    string geo = GetJsonValue(jObject, _geometryField);
                    Shape shape = GetGeometry(geo);

                    if (shape != null && transformer.CanTransform)
                    {
                        transformer.Transform2D(shape);
                    }

                    item.Geometry = shape;

                    #region BBox

                    double? minx = GetJsonDouble(jObject, "minx"),
                            miny = GetJsonDouble(jObject, "miny"),
                            maxx = GetJsonDouble(jObject, "maxx"),
                            maxy = GetJsonDouble(jObject, "maxy");

                    if (minx != null && miny != null && maxx != null && maxy != null)
                    {
                        double[] x = new double[] { (double)minx, (double)maxx };
                        double[] y = new double[] { (double)miny, (double)maxy };

                        if (transformer.CanTransform)
                        {
                            transformer.Transform2D(x, y);
                        }

                        item.BBox = new double[] { x[0], y[0], x[1], y[1] };
                    }

                    #endregion

                    items.Add(item);
                }
            }
        }

        return new SearchServiceItems()
        {
            Items = items.ToArray()
        };
    }

    #endregion

    #region Helper

    protected string GetJsonValue(Newtonsoft.Json.Linq.JObject jObject, string propertyName)
    {
        if (String.IsNullOrWhiteSpace(propertyName))
        {
            return String.Empty;
        }

        try
        {
            return GetJsonValue(jObject[propertyName]);
        }
        catch
        {
            return String.Empty;
        }
    }

    private string GetJsonValue(Newtonsoft.Json.Linq.JToken token)
    {
        if (token is Newtonsoft.Json.Linq.JArray && ((Newtonsoft.Json.Linq.JArray)token).Count > 0)
        {
            return GetJsonValue(((Newtonsoft.Json.Linq.JArray)token)[0]);
        }
        if (token is Newtonsoft.Json.Linq.JValue)
        {
            return token.ToString();
        }

        return String.Empty;
    }

    public double? GetJsonDouble(Newtonsoft.Json.Linq.JObject jObject, string propertyName, double? defaultValue = null)
    {
        var val = GetJsonValue(jObject, propertyName);

        if (String.IsNullOrWhiteSpace(val))
        {
            return defaultValue;
        }

        double res;
        if (val.TryToPlatformDouble(out res))
        {
            return res;
        }

        return defaultValue;
    }

    protected Shape GetGeometry(string geom)
    {
        geom = geom.ToLower();

        while (geom.Contains(" ("))
        {
            geom = geom.Replace(" (", "(");
        }

        if (geom.StartsWith("point("))
        {
            string coordsString = geom.Substring(6, geom.Length - 7).Trim();
            while (coordsString.Contains("  "))
            {
                coordsString = coordsString.Replace("  ", " ");
            }

            string[] coords = coordsString.Split(' ');
            return new Point(
                coords[0].ToPlatformDouble(),
                coords[1].ToPlatformDouble());

        }
        else if (geom.Contains(",") && geom.Split(',').Length == 2)
        {
            string[] coords = geom.Trim().Split(',');
            return new Point(
                coords[0].ToPlatformDouble(),
                coords[1].ToPlatformDouble());
        }
        return null;
    }

    #endregion

    #region Json Classes

    internal class LucType
    {
        public ResponseType response { get; set; }

        public class ResponseType
        {
            public int numFound { get; set; }

            public int start { get; set; }

            //public DocType[] docs { get; set; }
            public object docs { get; set; }

            public class DocType
            {
                public string subtext { get; set; }

                public string id { get; set; }

                public string[] title { get; set; }

                public string[] geo { get; set; }
            }
        }
    }

    #endregion
}
