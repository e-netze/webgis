using System;
using System.Linq;

using E.Standard.CMS.Core.Branches;
using E.Standard.Security.Cryptography.Abstractions;

using Newtonsoft.Json;

namespace E.Standard.WebGIS.Core.Models;

// deployed cms branch, returned by api rest/branches; Encoded == "" => main
public class ApiBranchDTO
{
    [JsonProperty(PropertyName = "encoded")]
    [System.Text.Json.Serialization.JsonPropertyName("encoded")]
    public string Encoded { get; set; }

    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "user")]
    [System.Text.Json.Serialization.JsonPropertyName("user")]
    public string User { get; set; }

    [JsonProperty(PropertyName = "commit")]
    [System.Text.Json.Serialization.JsonPropertyName("commit")]
    public string Commit { get; set; }

    [JsonProperty(PropertyName = "date")]
    [System.Text.Json.Serialization.JsonPropertyName("date")]
    public DateTime? Date { get; set; }

    [JsonProperty(PropertyName = "cms_count")]
    [System.Text.Json.Serialization.JsonPropertyName("cms_count")]
    public int CmsCount { get; set; }

    // encrypted branch token (CmsBranchTokens), filled by the portal for map authors; sent to the api as hmac_br
    [JsonProperty(PropertyName = "token", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("token")]
    public string Token { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsMain => String.IsNullOrEmpty(Encoded);

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string Details
    {
        get
        {
            var commit = String.IsNullOrEmpty(Commit) ? null : Commit.Length > 7 ? Commit.Substring(0, 7) : Commit;
            return String.Join(" · ", new[] { User, Date?.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), commit }
                                          .Where(s => !String.IsNullOrEmpty(s)));
        }
    }
}

static public class ApiBranchDTOExtensions
{
    // adds a branch token without expiration (map authors)
    static public ApiBranchDTO[] WithBranchTokens(this ApiBranchDTO[] branches, ICryptoService crypto)
    {
        foreach (var branch in branches ?? Array.Empty<ApiBranchDTO>())
        {
            branch.Token = branch.IsMain || !CmsBranches.IsValidEncoded(branch.Encoded)
                ? null
                : CmsBranchTokens.Create(crypto, branch.Encoded);
        }

        return branches ?? Array.Empty<ApiBranchDTO>();
    }
}
