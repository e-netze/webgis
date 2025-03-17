using E.Standard.Api.App.Models.Abstractions;
using Newtonsoft.Json;
using System;
using System.Text;

namespace E.Standard.Api.App.DTOs;

public enum CmsItemStatus
{
    Ok = 0,
    Corrupt = 1
}

public sealed class CmsItemDTO : IHtml
{
    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty("status")]
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public CmsItemStatus Status { get; set; }

    [JsonProperty("errormessage")]
    [System.Text.Json.Serialization.JsonPropertyName("errormessage")]
    public string ErrorMessage { get; set; }

    [JsonProperty("servicescount")]
    [System.Text.Json.Serialization.JsonPropertyName("servicescount")]
    public int ServicesCount { get; set; }

    [JsonProperty("initializationtiome")]
    [System.Text.Json.Serialization.JsonPropertyName("initializationtiome")]
    public DateTime InitializationTime { get; set; }

    #region IHtml Member

    public string ToHtmlString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(HtmlHelper.ToHeader(this.Name, HtmlHelper.HeaderType.h4));
        sb.Append(HtmlHelper.Text("Status: " + this.Status.ToString()));
        sb.Append(HtmlHelper.Text($"InitTime: {this.InitializationTime.ToShortDateString()} {this.InitializationTime.ToLongTimeString()}"));
        if (!String.IsNullOrWhiteSpace(this.ErrorMessage))
        {
            sb.Append(HtmlHelper.Text("Error: " + this.ErrorMessage));
        }
        sb.Append(HtmlHelper.Text("Services: " + this.ServicesCount));

        return sb.ToString();
    }

    #endregion
}
