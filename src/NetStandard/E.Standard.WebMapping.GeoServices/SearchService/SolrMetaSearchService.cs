using E.Standard.CMS.Core;
using E.Standard.Json;
using E.Standard.Web.Abstractions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.SearchService;

public class SolrMetaSearchService : SolrSearchService, ISearchService2
{
    private readonly string _metaJsonFile = String.Empty;

    public SolrMetaSearchService(IMap map, CmsNode node)
        : base(node)
    {
        _metaJsonFile = System.IO.Path.Combine(map.Environment.UserString(webgisConst.EtcPath), "search", $"{this.Id}.json");
    }

    #region ISearchService2

    public Task<SearchTypeMetadata> GetTypeMetadataAsync(IHttpService httpService, string metaId)
    {
        try
        {
            if (String.IsNullOrWhiteSpace(_metaJsonFile))
            {
                return Task.FromResult<SearchTypeMetadata>(null);
            }

            var metaItem = JSerializer.Deserialize<MetaItem[]>(System.IO.File.ReadAllText(_metaJsonFile)).Where(m => m.MetaId == metaId).FirstOrDefault();

            if (metaItem == null)
            {
                return Task.FromResult<SearchTypeMetadata>(null);
            }

            return Task.FromResult<SearchTypeMetadata>(new SearchTypeMetadata()
            {
                Id = metaItem.MetaId,
                Sample = metaItem.Sample,
                Description = metaItem.Description,
                ServiceId = metaItem.ServiceId,
                QueryId = metaItem.QueryId,
                TypeName = metaItem.Category
            });
        }
        catch (Exception ex)
        {
            throw new Exception("Json Parse error: " + ex.Message);
        }
    }

    public Task<IEnumerable<SearchTypeMetadata>> GetTypesMetadataAsync(IHttpService httpService)
    {
        List<SearchTypeMetadata> metas = new List<SearchTypeMetadata>();

        try
        {
            if (!String.IsNullOrWhiteSpace(_metaJsonFile))
            {
                return Task.FromResult<IEnumerable<SearchTypeMetadata>>(JSerializer.Deserialize<MetaItem[]>(System.IO.File.ReadAllText(_metaJsonFile))
                    .Select(metaItem => new SearchTypeMetadata()
                    {
                        Id = metaItem.MetaId,
                        Sample = metaItem.Sample,
                        Description = metaItem.Description,
                        ServiceId = metaItem.ServiceId,
                        QueryId = metaItem.QueryId,
                        TypeName = metaItem.Category
                    }));
            }
        }
        catch { }

        return Task.FromResult<IEnumerable<SearchTypeMetadata>>(metas);
    }

    async public Task<SearchServiceItems> Query2Async(IHttpService httpService, string term, int rows, IEnumerable<string> categories, int targetProjId = 4326)
    {
        string url = String.Format(_serviceUrl, term).Replace("{term}", term).Replace("[term]", term);
        url += "&rows=" + (rows > 0 ? rows : _rows);

        //if (queryBBox != null)
        //{
        //    url += $"&fq=geowgs:[{ queryBBox.MinY },{ queryBBox.MinX } TO { queryBBox.MaxY },{ queryBBox.MaxX }]";
        //}

        //string json = await dotNETConnector.DownloadXmlAsync(url, _connector, null);
        string json = await httpService.GetStringAsync(url, encoding: Encoding.UTF8);
        var lucType = JsonConvert.DeserializeObject<LucType>(json);

        List<SearchServiceItem> items = new List<SearchServiceItem>();

        if (categories != null && categories.Count() > 0)
        {
            if ((await this.TypesAsync(httpService)).Select(t => t.Key).Intersect(categories).Count() == 0)
            {
                return new SearchServiceItems()
                {
                    Items = items.ToArray()
                };
            }
        }

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
                        Id = GetJsonValue(jObject, "id"),
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
                    item.BBox = shape?.ShapeEnvelope?.ToArray();

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

    public Task<IEnumerable<SearchServiceAggregationBucket>> TypesAsync(IHttpService httpService)
    {
        List<SearchServiceAggregationBucket> aggBuckets = new List<SearchServiceAggregationBucket>();

        try
        {
            if (!String.IsNullOrWhiteSpace(_metaJsonFile))
            {
                foreach (var metaItem in JSerializer.Deserialize<MetaItem[]>(System.IO.File.ReadAllText(_metaJsonFile)))
                {
                    if (String.IsNullOrWhiteSpace(metaItem.Category))
                    {
                        continue;
                    }

                    var aggBucket = aggBuckets.Where(a => a.Key == metaItem.Category).FirstOrDefault();
                    if (aggBucket != null)
                    {
                        aggBucket.Count++;
                    }
                    else
                    {
                        aggBuckets.Add(new SearchServiceAggregationBucket()
                        {
                            Key = metaItem.Category,
                            Count = 1
                        });
                    }
                }
            }
        }
        catch { }

        return Task.FromResult<IEnumerable<SearchServiceAggregationBucket>>(aggBuckets);
    }

    #endregion

    #region Classes

    private class MetaItem
    {
        [JsonProperty(PropertyName = "json_featuretype")]
        [System.Text.Json.Serialization.JsonPropertyName("json_featuretype")]
        public string JsonFeaturetype { get; set; }

        [JsonProperty(PropertyName = "meta_id")]
        [System.Text.Json.Serialization.JsonPropertyName("meta_id")]
        public string MetaId { get; set; }

        [JsonProperty(PropertyName = "service_id")]
        [System.Text.Json.Serialization.JsonPropertyName("service_id")]
        public string ServiceId { get; set; }

        [JsonProperty(PropertyName = "query_id")]
        [System.Text.Json.Serialization.JsonPropertyName("query_id")]
        public string QueryId { get; set; }

        [JsonProperty(PropertyName = "sample")]
        [System.Text.Json.Serialization.JsonPropertyName("sample")]
        public string Sample { get; set; }

        [JsonProperty(PropertyName = "description")]
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "category")]
        [System.Text.Json.Serialization.JsonPropertyName("category")]
        public string Category { get; set; }
    }

    #endregion
}
