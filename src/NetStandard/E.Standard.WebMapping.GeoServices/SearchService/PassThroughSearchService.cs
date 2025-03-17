using E.Standard.CMS.Core;
using E.Standard.Json;
using E.Standard.Web.Abstractions;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Models;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.SearchService;

public class PassThroughSearchService : ISearchService
{
    private readonly string _serviceUrl;

    public PassThroughSearchService(CmsNode node)
    {
        this.Id = node.Url;
        this.Name = node.Name;

        _serviceUrl = node.LoadString("serviceUrl");
    }

    #region ISearchService

    public string Name
    {
        get; set;
    }

    public string Id
    {
        get; set;
    }

    public string CopyrightId { get; set; }

    async public Task<SearchServiceItems> QueryAsync(IHttpService httpService, string term, int rows, int targetProjId = 4326)
    {

        string json = await httpService.GetStringAsync(String.Format(_serviceUrl, term));

        var autoCompleteItems = JSerializer.Deserialize<AutocompleteItem[]>(json);

        return new SearchServiceItems()
        {
            Items = autoCompleteItems.Select(item =>
            new SearchServiceItem(this)
            {
                Id = item.Id,
                Geometry = item.Coords != null && item.Coords.Length == 2 ? new Point(item.Coords[0], item.Coords[1]) : null,
                SuggestText = item.Value,
                Subtext = item.SubText,
                Link = item.Link,
                ThumbnailUrl = item.Thumbnail,
                BBox = item.BBox
            })
        };
    }

    #endregion

    #region Classes

    class AutocompleteItem
    {
        public AutocompleteItem()
        {

        }

        [JsonProperty(PropertyName = "id")]
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "label")]
        [System.Text.Json.Serialization.JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonProperty(PropertyName = "value")]
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonProperty(PropertyName = "link")]
        [System.Text.Json.Serialization.JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonProperty(PropertyName = "subtext", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("subtext")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string SubText { get; set; }

        [JsonProperty(PropertyName = "thumbnail", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("thumbnail")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string Thumbnail { get; set; }

        [JsonProperty(PropertyName = "coords", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("coords")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public double[] Coords { get; set; }

        [JsonProperty(PropertyName = "bbox", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("bbox")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public double[] BBox
        { get; set; }
    }

    #endregion
}
