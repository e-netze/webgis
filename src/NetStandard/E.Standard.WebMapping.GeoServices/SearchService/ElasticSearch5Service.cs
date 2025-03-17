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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.SearchService;

public class ElasticSearch5Service : ISearchService2
{
    private string _name, _id;
    private readonly string _serviceUrl, _suggestedTextField, _thumbnailField, _geometryField, _subtextField, _typename = "category", _categoryField = null, _link;
    private readonly int _rows = 5;
    private readonly SpatialReference _sRef = null, _sRef4326 = null;
    readonly ISpatialReferenceStore _sRefStore = null;

    public ElasticSearch5Service(CmsNode node)
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
        if (projId > 0 /*&& projId != 4326*/)
        {
            _sRef = CoreApiGlobals.SRefStore.SpatialReferences.ById(projId);
            _sRef4326 = CoreApiGlobals.SRefStore.SpatialReferences.ById(4326);
        }

        _sRefStore = CoreApiGlobals.SRefStore;
    }

    #region ISearchService

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
        return await Query2Async(httpService, term, rows, null, targetProjId);
    }

    #endregion

    #region  ISearchService2

    async public Task<SearchServiceItems> Query2Async(IHttpService httpService, string term, int rows, IEnumerable<string> categories, int targetProjId = 4326)
    {
        string analizedTerm = AnalyseTerm(term, categories);
        string url = String.Format(_serviceUrl + "/item/_search?q={0}", analizedTerm).Replace("{term}", analizedTerm).Replace("[term]", analizedTerm);
        url += "&size=" + (rows > 0 ? rows : _rows);

        ElasticSearchResponse response = null;
        try
        {
            string json = await httpService.GetStringAsync(url, encoding: Encoding.UTF8);
            response = JsonConvert.DeserializeObject<ElasticSearchResponse>(json);
        }
        catch { }
        if (response == null)
        {
            return null;
        }

        if (response.Hits != null && response.Hits.Items != null)
        {
            List<SearchServiceItem> searchItems = new List<SearchServiceItem>();

            SpatialReference targetSRef = _sRef4326;
            if (targetProjId > 0 && targetProjId != 4326 && _sRefStore != null)
            {
                targetSRef = _sRefStore.SpatialReferences.ById(targetProjId);
            }

            using (var transformer = new GeometricTransformer())
            {
                if (_sRef != null && targetSRef != null && _sRef.Id != targetSRef.Id)
                {
                    transformer.FromSpatialReference(_sRef.Proj4, !_sRef.IsProjective);
                    transformer.ToSpatialReference(targetSRef.Proj4, !targetSRef.IsProjective);
                }

                foreach (var item in response.Hits.Items)
                {
                    var jObject = (Newtonsoft.Json.Linq.JObject)item.Source;

                    var searchItem = new SearchServiceItem(this)
                    {
                        Id = item.Id,
                        SuggestText = AnalyseResponse(GetJsonValue(jObject, _suggestedTextField)),
                        ThumbnailUrl = AnalyseResponse(GetJsonValue(jObject, _thumbnailField)),
                        Subtext = AnalyseResponse(GetJsonValue(jObject, _subtextField)),
                        Link = AnalyseResponse(GetJsonValue(jObject, _link)),
                        Category = AnalyseResponse(GetJsonValue(jObject, _categoryField ?? _typename)),
                        Score = item.Score
                    };

                    #region Location

                    Newtonsoft.Json.Linq.JToken geoJObject;
                    if (jObject.TryGetValue(_geometryField, out geoJObject))
                    {
                        if (geoJObject is Newtonsoft.Json.Linq.JObject)
                        {
                            double? lat = GetJsonDouble((Newtonsoft.Json.Linq.JObject)geoJObject, "lat");
                            double? lon = GetJsonDouble((Newtonsoft.Json.Linq.JObject)geoJObject, "lon");

                            if (lat != null && lon != null)
                            {
                                Point location = new Point((double)lon, (double)lat);
                                if (transformer.CanTransform)
                                {
                                    transformer.Transform(location);
                                }

                                searchItem.Geometry = location;

                                searchItem.BBox = new double[] { location.X, location.Y, location.X, location.Y };
                            }
                        }
                    }

                    #endregion

                    #region BBOX

                    string bbox = GetJsonValue(jObject, "bbox");
                    if (!String.IsNullOrWhiteSpace(bbox))
                    {
                        string[] env = bbox.Split(',');
                        if (env.Length == 4)
                        {
                            double minx = env[0].ToPlatformDouble();
                            double miny = env[1].ToPlatformDouble();
                            double maxx = env[2].ToPlatformDouble();
                            double maxy = env[3].ToPlatformDouble();

                            double[] x = new double[] { minx, maxx };
                            double[] y = new double[] { miny, maxy };

                            if (transformer.CanTransform)
                            {
                                transformer.Transform2D(x, y);
                            }

                            searchItem.BBox = new double[] { x[0], y[0], x[1], y[1] };
                        }
                    }

                    #endregion

                    searchItems.Add(searchItem);
                }
            }

            return new SearchServiceItems()
            {
                Items = searchItems
            };
        }

        return null;
    }

    async public Task<IEnumerable<SearchServiceAggregationBucket>> TypesAsync(IHttpService httpService)
    {
        List<SearchServiceAggregationBucket> aggBuckets = new List<SearchServiceAggregationBucket>();

        //string json = Encoding.UTF8.GetString(await client.DownloadDataTaskAsync(this._serviceUrl + "/meta/_search?pretty=true&size=1000"));
        string json = await httpService.GetStringAsync(this._serviceUrl + "/meta/_search?pretty=true&size=1000", encoding: Encoding.UTF8);

        var response = JsonConvert.DeserializeObject<ElasticSearchResponse>(json);

        if (response != null && response.Hits != null && response.Hits.Items != null)
        {
            foreach (var item in response.Hits.Items)
            {
                var jObject = (Newtonsoft.Json.Linq.JObject)item.Source;

                var category = GetJsonValue(jObject, "category");

                var aggBucket = aggBuckets.Where(a => a.Key == category).FirstOrDefault();
                if (aggBucket != null)
                {
                    aggBucket.Count++;
                }
                else
                {
                    aggBuckets.Add(new SearchServiceAggregationBucket()
                    {
                        Key = category,
                        Count = 1
                    });
                }
            }
        }

        return aggBuckets;
    }

    async public Task<SearchTypeMetadata> GetTypeMetadataAsync(IHttpService httpService, string metaId)
    {
        try
        {
            string url = _serviceUrl + "/meta/" + metaId;
            string json = await httpService.GetStringAsync(url, encoding: Encoding.UTF8);

            var item = JsonConvert.DeserializeObject<Item>(json);
            if (item.Id.ToLower() == metaId.ToLower() && item.Source != null)
            {
                var jObject = (Newtonsoft.Json.Linq.JObject)item.Source;

                return new SearchTypeMetadata()
                {
                    Id = item.Id,
                    Sample = GetJsonValue(jObject, "sample"),
                    Description = GetJsonValue(jObject, "description"),
                    ServiceId = GetJsonValue(jObject, "service"),
                    QueryId = GetJsonValue(jObject, "query")
                };
            }
        }
        catch { }

        return null;
    }
    async public Task<IEnumerable<SearchTypeMetadata>> GetTypesMetadataAsync(IHttpService httpService)
    {
        List<SearchTypeMetadata> metas = new List<SearchTypeMetadata>();

        try
        {
            string url = _serviceUrl + "/meta/_search?size=1000";
            string json = await httpService.GetStringAsync(url, encoding: Encoding.UTF8);

            var elasticResponse = JsonConvert.DeserializeObject<ElasticSearchResponse>(json);
            if (elasticResponse.Hits != null && elasticResponse.Hits.Items != null)
            {
                foreach (var item in elasticResponse.Hits.Items)
                {
                    if (item.Source != null)
                    {
                        var jObject = (Newtonsoft.Json.Linq.JObject)item.Source;

                        metas.Add(new SearchTypeMetadata()
                        {
                            Id = item.Id,
                            Sample = GetJsonValue(jObject, "sample"),
                            Description = GetJsonValue(jObject, "description"),
                            ServiceId = GetJsonValue(jObject, "service"),
                            QueryId = GetJsonValue(jObject, "query"),
                            TypeName = GetJsonValue(jObject, "category")
                        });
                    }
                }
            }
        }
        catch { }

        return metas;
    }

    #endregion

    #region Helper

    public string ReplaceTerm(string term)
    {
        term = term.Trim().Replace("-", "__minus__").Replace("/", "__slash__");
        return term;
    }

    private string AnalyseTerm(string term, IEnumerable<string> categories)
    {
        if (String.IsNullOrWhiteSpace(term))
        {
            return String.Empty;
        }

        term = term.Trim().Replace("-", "__minus__").Replace("/", "__slash__");

        while (term.Contains("  "))
        {
            term = term.Replace("  ", " ");
        }

        List<string> singleTerms = new List<string>(term.Split(' '));


        StringBuilder sb = new StringBuilder();
        foreach (var suffix in new string[] { "", "*" })
        {
            if (sb.Length > 0)
            {
                sb.Append(" OR ");
            }

            if (singleTerms.Count > 0)
            {
                sb.Append("(");
            }

            for (int i = 0; i < singleTerms.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(" AND ");
                }

                sb.Append(AnalyseGerman(singleTerms[i], suffix));
            }
            if (singleTerms.Count > 0)
            {
                sb.Append(")");
            }
        }

        StringBuilder cat = new StringBuilder();
        if (categories != null && categories.Count() > 0)
        {

            foreach (var category in categories)
            {
                if (cat.Length > 0)
                {
                    cat.Append(" OR ");
                }

                cat.Append(_typename + ":\"" + category + "\"");  // "category" ... mit hochkomma -> exakte suche
            }
        }

        if (cat.Length > 0)
        {
            return "(" + sb.ToString() + ") AND (" + cat.ToString() + ")";
        }

        return sb.ToString();
    }

    private string AnalyseGerman(string word, string suffix)
    {
        List<string> words = new List<string>(new string[] { word });

        if (word.Contains("ss"))
        {
            words.Add(word.Replace("ss", "ß"));
        }
        if (word.Contains("ß"))
        {
            words.Add(word.Replace("ß", "ss"));
        }
        if (word.Contains("__slash__"))
        {
            string original = word.Replace("__slash__", "/");
            if (!IsGstnr(original))
            {
                words.AddRange(original.Split('/'));
            }
        }
        if (word.Contains("__minus__"))
        {
            string original = word.Replace("__minus__", "-");
            // Falls es sich um einen Technischen Platz, etc. handelt, soll nicht geteilt werden. Sonst passt Score nicht
            if (!IsTechnical(original))
            {
                words.AddRange(original.Split('-'));
            }
        }

        if (words.Count == 1)
        {
            return word + suffix;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("(");
        for (int i = 0; i < words.Count; i++)
        {
            if (String.IsNullOrWhiteSpace(words[i]))
            {
                continue;
            }

            if (i > 0)
            {
                sb.Append(" OR ");
            }

            sb.Append(words[i] + suffix);
        }
        sb.Append(")");

        return sb.ToString();
    }

    private string AnalyseResponse(string str)
    {
        return str.Replace("__minus__", "-").Replace("__slash__", "/");
    }

    private string GetJsonValue(Newtonsoft.Json.Linq.JObject jObject, string propertyName)
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

    private Shape GetGeometry(string geom)
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

    private bool IsGstnr(string gnr)
    {
        return Regex.IsMatch(gnr, @"^([.]{0,1})([0-9]{1,5}|[0-9]{1,5}\/{1}[0-9]{1,5})$");
    }

    private bool IsTechnical(string str)
    {
        // Wenn ein Teil nur aus Buchstaben besteht = > kein TP
        foreach (var part in str.Split('-'))
        {
            if (Regex.IsMatch(part, @"^\p{L}+$"))
            {
                return false;
            }
        }
        return true;
    }

    #endregion

    #region Classes

    public class ElasticSearchResponse
    {
        [JsonProperty(PropertyName = "took")]
        [System.Text.Json.Serialization.JsonPropertyName("took")]
        public int Took { get; set; }

        [JsonProperty(PropertyName = "timed_out")]
        [System.Text.Json.Serialization.JsonPropertyName("timed_out")]
        public bool TimedOut { get; set; }

        [JsonProperty(PropertyName = "hits")]
        [System.Text.Json.Serialization.JsonPropertyName("hits")]
        public Hits Hits { get; set; }

        [JsonProperty(PropertyName = "aggregations")]
        [System.Text.Json.Serialization.JsonPropertyName("aggregations")]
        public Aggregations Aggs { get; set; }
    }

    public class Hits
    {
        [JsonProperty(PropertyName = "total")]
        [System.Text.Json.Serialization.JsonPropertyName("total")]
        public long Total { get; set; }

        [JsonProperty(PropertyName = "max_score")]
        [System.Text.Json.Serialization.JsonPropertyName("max_score")]
        public double? MaxScore { get; set; }

        [JsonProperty(PropertyName = "hits")]
        [System.Text.Json.Serialization.JsonPropertyName("hits")]
        public Item[] Items { get; set; }
    }

    public class Item
    {
        [JsonProperty(PropertyName = "_index")]
        [System.Text.Json.Serialization.JsonPropertyName("_index")]
        public string Index { get; set; }

        [JsonProperty(PropertyName = "_type")]
        [System.Text.Json.Serialization.JsonPropertyName("_type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "_id")]
        [System.Text.Json.Serialization.JsonPropertyName("_id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "_score")]
        [System.Text.Json.Serialization.JsonPropertyName("_score")]
        public double Score { get; set; }

        [JsonProperty(PropertyName = "_source")]
        [System.Text.Json.Serialization.JsonPropertyName("_source")]
        public object Source { get; set; }
    }

    public class Aggregations
    {
        [JsonProperty(PropertyName = "categories")]
        [System.Text.Json.Serialization.JsonPropertyName("categories")]
        public TermsCategory Categories
        {
            get; set;
        }

        public class TermsCategory
        {
            [JsonProperty(PropertyName = "buckets")]
            [System.Text.Json.Serialization.JsonPropertyName("buckets")]
            public IEnumerable<Bucket> Buckets { get; set; }
        }

        public class Bucket
        {
            [JsonProperty(PropertyName = "key")]
            [System.Text.Json.Serialization.JsonPropertyName("key")]
            public string Key { get; set; }

            [JsonProperty(PropertyName = "doc_count")]
            [System.Text.Json.Serialization.JsonPropertyName("doc_count")]
            public int DocCount { get; set; }
        }
    }

    #endregion

}
