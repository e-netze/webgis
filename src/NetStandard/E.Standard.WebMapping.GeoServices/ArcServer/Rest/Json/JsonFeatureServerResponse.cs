using E.Standard.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

class JsonFeatureServerResponse
{
    [JsonProperty(PropertyName = "addResults", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("addResults")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public JsonResponse[] AddResults { get; set; }

    [JsonProperty(PropertyName = "updateResults", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("updateResults")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public JsonResponse[] UpdateResults { get; set; }

    [JsonProperty(PropertyName = "deleteResults", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("deleteResults")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public JsonResponse[] DeleteResults { get; set; }

    #region Methods

    public bool CheckSuccess(string response)
    {
        if (this.AddResults != null && this.AddResults.Where(r => r.Success == false).Count() > 0)
        {
            return false;
        }

        if (this.UpdateResults != null && this.UpdateResults.Where(r => r.Success == false).Count() > 0)
        {
            return false;
        }

        if (this.DeleteResults != null && this.DeleteResults.Where(r => r.Success == false).Count() > 0)
        {
            return false;
        }

        if (this.AddResults == null &&
            this.UpdateResults == null &&
            this.DeleteResults == null &&
            !String.IsNullOrEmpty(response))
        {
            var jsonError = JSerializer.Deserialize<Json.JsonError>(response);
            if (jsonError?.error != null && jsonError.error.code > 0)
            {
                return false;
            }
        }

        return true;
    }

    public IEnumerable<int> ObjectIds
    {
        get
        {
            List<int> objectIds = new List<int>();

            if (this.AddResults != null)
            {
                objectIds.AddRange(this.AddResults.Where(r => r.ObjectId.HasValue).Select(r => r.ObjectId.Value));
            }

            if (this.UpdateResults != null)
            {
                objectIds.AddRange(this.UpdateResults.Where(r => r.ObjectId.HasValue).Select(r => r.ObjectId.Value));
            }

            if (this.DeleteResults != null)
            {
                objectIds.AddRange(this.DeleteResults.Where(r => r.ObjectId.HasValue).Select(r => r.ObjectId.Value));
            }

            return objectIds;
        }
    }

    public string GetErrorMessage()
    {
        StringBuilder sb = new StringBuilder();

        foreach (var response in new JsonResponse[][] { this.AddResults, this.UpdateResults, this.DeleteResults })
        {
            if (response != null)
            {
                response
                    .Where(r => r.Error != null)
                    .Select(r => r.Error)
                    .ToList()
                    .ForEach(e => sb
                    .Append(e.Code + " " + e.Description + "\n"));
            }
        }

        return sb.ToString();
    }

    #endregion

    #region Classses

    public class JsonResponse
    {
        [JsonProperty(PropertyName = "success")]
        [System.Text.Json.Serialization.JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonProperty(PropertyName = "objectId", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("objectId")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public int? ObjectId { get; set; }

        [JsonProperty(PropertyName = "error", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("error")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public JsonError Error { get; set; }
    }

    public class JsonError
    {
        [JsonProperty(PropertyName = "code")]
        [System.Text.Json.Serialization.JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonProperty(PropertyName = "description")]
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string Description { get; set; }
    }

    #endregion
}
