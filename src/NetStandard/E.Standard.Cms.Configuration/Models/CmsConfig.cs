using E.Standard.CMS.Schema;
using E.Standard.Configuration;
using E.Standard.Security.App.Services.Abstraction;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.Cms.Configuration.Models;

public class CmsConfig : PropertiesParserBaseClass
{
    [JsonProperty(PropertyName = "shared-crypto-keys-path")]
    [System.Text.Json.Serialization.JsonPropertyName("shared-crypto-keys-path")]
    public string SharedCrptoKeysPath { get; set; }

    [JsonProperty(PropertyName = "company")]
    [System.Text.Json.Serialization.JsonPropertyName("company")]
    public string Company { get; set; }

    [JsonProperty(PropertyName = "elasticsearch-endpoint")]
    [System.Text.Json.Serialization.JsonPropertyName("elasticsearch-endpoint")]
    public string ElasticSearchEndpoint { get; set; }

    [JsonProperty(PropertyName = "force-https")]
    [System.Text.Json.Serialization.JsonPropertyName("force-https")]
    public bool ForceHttps { get; set; }

    [JsonProperty(PropertyName = "services-default-url-scheme")]
    [System.Text.Json.Serialization.JsonPropertyName("services-default-url-scheme")]
    public string ServicesDefaultUrlScheme { get; set; }

    [JsonProperty(PropertyName = "webgis-portal-instance")]
    [System.Text.Json.Serialization.JsonPropertyName("webgis-portal-instance")]
    public string WebGisPortalInstance { get; set; }

    [JsonProperty(PropertyName = "cms-display-url")]
    [System.Text.Json.Serialization.JsonPropertyName("cms-display-url")]
    public string CmsDisplayUrl { get; set; }

    [JsonProperty(PropertyName = "custom-cms-behavior")]
    [System.Text.Json.Serialization.JsonPropertyName("custom-cms-behavior")]
    public CustomCmsBehavior CustomCms { get; set; }

    [JsonProperty(PropertyName = "cms-items")]
    [System.Text.Json.Serialization.JsonPropertyName("cms-items")]
    public IEnumerable<CmsItem> CmsItems { get; set; }



    public override void Parse(IConfigValueParser parser)
    {
        base.Parse(parser);

        if (CustomCms != null)
        {
            CustomCms.Parse(parser);
        }

        if (CmsItems != null)
        {
            foreach (var cmsItem in CmsItems)
            {
                cmsItem.Parse(parser);
            }
        }
    }

    #region Classes

    public class CmsItem : PropertiesParserBaseClass
    {
        [JsonProperty(PropertyName = "id")]
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "path")]
        [System.Text.Json.Serialization.JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonProperty(PropertyName = "scheme")]
        [System.Text.Json.Serialization.JsonPropertyName("scheme")]
        public string Scheme { get; set; }

        [JsonProperty(PropertyName = "secrets-password")]
        [System.Text.Json.Serialization.JsonPropertyName("secrets-password")]
        public string SecretsPassword { get; set; }

        [JsonProperty(PropertyName = "deployments")]
        [System.Text.Json.Serialization.JsonPropertyName("deployments")]
        public IEnumerable<DeployItem> Deployments { get; set; }

        public override void Parse(IConfigValueParser parser)
        {
            base.Parse(parser);

            if (Deployments != null)
            {
                foreach (var deployment in Deployments)
                {
                    deployment.Parse(parser);
                }
            }
        }
    }

    public class DeployItem : PropertiesParserBaseClass
    {
        [JsonProperty(PropertyName = "name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "target")]
        [System.Text.Json.Serialization.JsonPropertyName("target")]
        public string Target { get; set; }

        [JsonProperty(PropertyName = "client")]
        [System.Text.Json.Serialization.JsonPropertyName("client")]
        public string Client { get; set; }

        [JsonProperty(PropertyName = "secret")]
        [System.Text.Json.Serialization.JsonPropertyName("secret")]
        public string Secret { get; set; }

        [JsonProperty(PropertyName = "replacement-file")]
        [System.Text.Json.Serialization.JsonPropertyName("replacement-file")]
        public string ReplacementFile { get; set; }

        [JsonProperty(PropertyName = "replace-secrets-first")]
        [System.Text.Json.Serialization.JsonPropertyName("replace-secrets-first")]
        public bool ReplceSecretsFirst { get; set; } = false;

        [JsonProperty(PropertyName = "ignoreAuthentification")]
        [System.Text.Json.Serialization.JsonPropertyName("ignoreAuthentification")]
        public bool IgnoreAuthentification { get; set; }

        [JsonProperty(PropertyName = "postEvents")]
        [System.Text.Json.Serialization.JsonPropertyName("postEvents")]
        public Events PostEvents { get; set; }

        [JsonProperty(PropertyName = "environment")]
        [System.Text.Json.Serialization.JsonPropertyName("environment")]
        public DeployEnvironment Environment { get; set; }

        public override void Parse(IConfigValueParser parser)
        {
            base.Parse(parser);

            if (PostEvents != null)
            {
                PostEvents.Parse(parser);
            }
        }
    }

    public class Events : PropertiesParserBaseClass
    {
        [JsonProperty("commands")]
        [System.Text.Json.Serialization.JsonPropertyName("commands")]
        public string[] Commands { get; set; }
        [JsonProperty("http-get")]
        [System.Text.Json.Serialization.JsonPropertyName("http-get")]
        public string[] HttpGet { get; set; }
    }

    public class CustomCmsBehavior : PropertiesParserBaseClass
    {
        [JsonProperty("allow")]
        [System.Text.Json.Serialization.JsonPropertyName("allow")]
        public bool Allow { get; set; }

        [JsonProperty("root-url")]
        [System.Text.Json.Serialization.JsonPropertyName("root-url")]
        public string RootUrl { get; set; }

        [JsonProperty("root-template")]
        [System.Text.Json.Serialization.JsonPropertyName("root-template")]
        public string RootTemplate { get; set; }

        [JsonProperty("scheme")]
        [System.Text.Json.Serialization.JsonPropertyName("scheme")]
        public string Scheme { get; set; }

        [JsonProperty("token-verification-url")]
        [System.Text.Json.Serialization.JsonPropertyName("token-verification-url")]
        public string TokenVerificationUrl { get; set; }

        [JsonProperty("http-post-events")]
        [System.Text.Json.Serialization.JsonPropertyName("http-post-events")]
        public string[] HttpPostEvents { get; set; }

        [JsonProperty("access-token-authority")]
        [System.Text.Json.Serialization.JsonPropertyName("access-token-authority")]
        public string AccessTokenAuthority { get; set; }
    }

    #endregion
}
