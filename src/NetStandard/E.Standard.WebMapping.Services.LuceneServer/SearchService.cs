using E.Standard.CMS.Core;
using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.Web.Abstractions;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Models;
using E.Standard.WebMapping.GeoServices.LuceneServer.Extentions;
using E.Standard.WebMapping.GeoServices.LuceneServer.Models;
using LuceneServerNET.Client;
using LuceneServerNET.Core;
using LuceneServerNET.Core.Language;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.LuceneServer;

public class SearchService : ISearchService3, IDisposable
{
    private readonly string
        _serviceUrl,
        _indexName,
        _suggestedTextField,
        _thumbnailField,
        _geometryField,
        _subtextField,
        _categoryField = "category",
        _linkField;
    private readonly int _rows = 5;
    private readonly SpatialReference _sRef = null, _sRef4326 = null;
    private readonly bool _usePhoneticSearch;

    public SearchService(CmsNode node, bool usePhoneticSearch)
    {
        this.Id = node.Url;
        this.Name = node.Name;

        _serviceUrl = node.LoadString("serviceUrl");
        _indexName = node.LoadString("indexname");

        _suggestedTextField = node.LoadString("suggestedtext").OrTake("suggested_text");
        _thumbnailField = node.LoadString("thumbnail").OrTake("thumbnail_url");
        _geometryField = node.LoadString("geo").OrTake("geo");
        _subtextField = node.LoadString("subtext").OrTake("subtext");
        _linkField = node.LoadString("link").OrTake("link");
        _rows = (int)node.Load("rows", 5);

        int projId = (int)node.Load("projid", -1);
        if (projId > 0 /*&& projId != 4326*/)
        {
            _sRef = CoreApiGlobals.SRefStore.SpatialReferences.ById(projId);
            _sRef4326 = CoreApiGlobals.SRefStore.SpatialReferences.ById(4326);
        }

        _usePhoneticSearch = usePhoneticSearch;
    }

    public string Name { get; set; }
    public string Id { get; set; }
    public string CopyrightId { get; set; }

    public Task<SearchServiceItems> QueryAsync(IHttpService httpService, string term, int rows, int targetProjId = 4326)
    {
        return Query2Async(httpService, term, rows, null, targetProjId);
    }

    public Task<SearchServiceItems> Query2Async(IHttpService httpService, string term, int rows, IEnumerable<string> categories, int targetProjId = 4326)
    {
        return Query3Async(httpService, term, rows, categories, null, targetProjId);
    }

    async public Task<SearchServiceItems> Query3Async(IHttpService httpService, string term, int rows, IEnumerable<string> categories, Envelope queryBBox, int targetProjId = 4326)
    {
        List<SearchServiceItem> searchItems = new List<SearchServiceItem>();

        var client = ReusableLuceneServerClient(httpService);

        bool hasCategories = categories != null && categories.Any();
        var termParser = new QueryBuilder(Languages.German);
        string query = termParser.ParseTerm(term.ToAsciiEncoding()).AppendCategories(categories);

        bool isPhonetic = false;
        var luceneSearchResult = await client.SearchAsync(query, size: rows > 0 ? rows : _rows);

        if (_usePhoneticSearch == true && luceneSearchResult.Hits.Count() == 0)
        {
            isPhonetic = true;
            luceneSearchResult = await client.SearchPhoneticAsync(term,
                size: (rows > 0 ? rows : _rows) * (hasCategories ? 10 : 1));  // ask for more if using categories and filter hits 
        }

        foreach (var hit in luceneSearchResult.Hits)
        {
            string bboxString = hit["bbox"]?.ToString();
            var geo = hit[_geometryField]?.ToString().Split(' ').Select(b => double.Parse(b, CultureInfo.InvariantCulture)).ToArray();

            if (hasCategories &&
                _usePhoneticSearch &&
                hit.ContainsKey("category") &&
                categories.Contains(hit["category"]?.ToString(), StringComparer.OrdinalIgnoreCase) == false)
            {
                continue;
            }

            searchItems.Add(new SearchServiceItem(this)
            {
                Score = hit.ContainsKey("_score") ?
                   Convert.ToDouble(hit["_score"], CultureInfo.InvariantCulture) :
                   0,

                Id = hit.ContainsKey("id") ?
                   hit["id"]?.ToString() :
                   String.Empty,

                SuggestText = !String.IsNullOrEmpty(_suggestedTextField) && hit.ContainsKey(_suggestedTextField) ?
                    hit[_suggestedTextField]?.ToString() :
                    String.Empty,

                Subtext = !String.IsNullOrEmpty(_subtextField) && hit.ContainsKey(_subtextField) ?
                    hit[_subtextField]?.ToString() :
                    String.Empty,

                Category = !String.IsNullOrEmpty(_categoryField) && hit.ContainsKey(_categoryField) ?
                    hit[_categoryField]?.ToString() :
                    String.Empty,

                Link = !String.IsNullOrEmpty(_linkField) && hit.ContainsKey(_linkField) ?
                    hit[_linkField]?.ToString() :
                    String.Empty,

                ThumbnailUrl = !String.IsNullOrEmpty(_thumbnailField) && hit.ContainsKey(_thumbnailField) ?
                    hit[_thumbnailField]?.ToString() :
                    String.Empty,

                Geometry = geo != null && geo.Length == 2 ?
                    new Point(geo[0], geo[1]) :
                    null,

                BBox = String.IsNullOrEmpty(bboxString) ?
                    null :
                    bboxString.Split(',').Select(b => double.Parse(b, CultureInfo.InvariantCulture)).ToArray(),

                DoYouMean = isPhonetic
            });
        }

        return new SearchServiceItems()
        {
            Items = searchItems
        };
    }

    async public Task<SearchTypeMetadata> GetTypeMetadataAsync(IHttpService httpService, string metaId)
    {
        SearchTypeMetadata result = null;

        var client = ReusableLuceneServerClient(httpService);

        var metadataResult = await client.GetCustomMetadataAsync(metaId);
        if (!String.IsNullOrEmpty(metadataResult?.Metadata))
        {
            var meta = JSerializer.Deserialize<Meta>(metadataResult.Metadata);
            result = new SearchTypeMetadata()
            {
                Id = meta.Id,
                Sample = meta.Sample,
                Description = meta.Descrption,
                ServiceId = meta.Service,
                QueryId = meta.Query
            };
        }

        return result;
    }

    async public Task<IEnumerable<SearchTypeMetadata>> GetTypesMetadataAsync(IHttpService httpService)
    {
        List<SearchTypeMetadata> result = new List<SearchTypeMetadata>();

        try
        {
            var client = ReusableLuceneServerClient(httpService);

            var metadata = await client.GetCustomMetadatasAsync();
            if (metadata != null)
            {
                foreach (var key in metadata.Keys)
                {
                    if (!String.IsNullOrEmpty(metadata[key]))
                    {
                        var meta = JSerializer.Deserialize<Meta>(metadata[key]);
                        result.Add(new SearchTypeMetadata()
                        {
                            Id = meta.Id,
                            TypeName = meta.Category,
                            Sample = meta.Sample,
                            Description = meta.Descrption,
                            ServiceId = meta.Service,
                            QueryId = meta.Query
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Add(new SearchTypeMetadata() { Id = "", TypeName = "Error", Description = ex.Message });
        }
        return result;
    }

    async public Task<IEnumerable<SearchServiceAggregationBucket>> TypesAsync(IHttpService httpService)
    {
        List<SearchServiceAggregationBucket> aggBuckets = new List<SearchServiceAggregationBucket>();

        var client = ReusableLuceneServerClient(httpService);

        var metadata = await client.GetCustomMetadatasAsync();
        if (metadata != null)
        {
            foreach (var key in metadata.Keys)
            {
                if (!String.IsNullOrEmpty(metadata[key]))
                {
                    var meta = JSerializer.Deserialize<Meta>(metadata[key]);
                    if (!String.IsNullOrEmpty(meta.Category))
                    {
                        aggBuckets.Add(new SearchServiceAggregationBucket()
                        {
                            Key = meta.Category,
                            Count = 1
                        });
                    }
                }
            }
        }

        return aggBuckets;
    }

    #region IDisposable

    public void Dispose()
    {
        if (_luceneClient != null)
        {
            _luceneClient.Dispose();
            _luceneClient = null;
        }
    }

    #endregion

    #region Helper

    private LuceneServerClient _luceneClient = null;
    private LuceneServerClient ReusableLuceneServerClient(IHttpService httpService)
    {
        if (_luceneClient == null)
        {
            _luceneClient = new LuceneServerClient(_serviceUrl, _indexName, httpService.Create(_serviceUrl));
        }

        return _luceneClient;
    }

    #endregion
}
