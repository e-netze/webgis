using Newtonsoft.Json;
using System.Collections.Generic;

namespace Cms.Models.Elastic;

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
    public SouceClass Source { get; set; }

    public class SouceClass
    {
        [JsonProperty(PropertyName = "id")]
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "suggested_text")]
        [System.Text.Json.Serialization.JsonPropertyName("suggested_text")]
        public string SuggestedText { get; set; }

        [JsonProperty(PropertyName = "subtext")]
        [System.Text.Json.Serialization.JsonPropertyName("subtext")]
        public string SubText { get; set; }

        [JsonProperty(PropertyName = "path")]
        [System.Text.Json.Serialization.JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonProperty(PropertyName = "category")]
        [System.Text.Json.Serialization.JsonPropertyName("category")]
        public string Category
        {
            get; set;
        }
    }
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
