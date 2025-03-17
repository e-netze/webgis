using Newtonsoft.Json;
using System;

namespace E.Standard.Security.App.Json;

public class ApplicationSecurityConfig
{
    [JsonProperty(PropertyName = "users")]
    [System.Text.Json.Serialization.JsonPropertyName("users")]
    public User[] Users { get; set; }

    [JsonProperty(PropertyName = "mapUsers")]
    [System.Text.Json.Serialization.JsonPropertyName("mapUsers")]
    public User[] MapUsers { get; set; }

    [JsonProperty("identityType")]
    [System.Text.Json.Serialization.JsonPropertyName("identityType")]
    public string IdentityType { get; set; }

    [JsonProperty(PropertyName = "oidc")]
    [System.Text.Json.Serialization.JsonPropertyName("oidc")]
    public OpenIdConnect OpenIdConnectConfiguration
    {
        get; set;
    }

    [JsonProperty(PropertyName = "azure-ad")]
    [System.Text.Json.Serialization.JsonPropertyName("azure-ad")]
    public AzureAD AzureADConfiguration { get; set; }

    public bool UseApplicationSecurity { get; set; }

    public void MemberwiseCopy(ApplicationSecurityConfig config)
    {
        Console.WriteLine("ApplicationSecurityConfig:");
        Console.WriteLine("MemberwiseCopy form existing opject");

        if (config != null)
        {
            foreach (var propertyInfo in typeof(ApplicationSecurityConfig).GetProperties())
            {
                propertyInfo.SetValue(this, propertyInfo.GetValue(config));
            }
        }
    }

    #region Classes

    public class User
    {
        [JsonProperty(PropertyName = "name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "password", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("password")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string Password { get; set; }

        [JsonProperty(PropertyName = "userid", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("userid")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "mapUsernames", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("mapUsernames")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string[] MapUsernames { get; set; }
    }

    public class OpenIdConnect
    {
        [JsonProperty(PropertyName = "authority")]
        [System.Text.Json.Serialization.JsonPropertyName("authority")]
        public string Authority { get; set; }

        [JsonProperty(PropertyName = "metadataAddress")]
        [System.Text.Json.Serialization.JsonPropertyName("metadataAddress")]
        public string MetadataAddress { get; set; }

        [JsonProperty(PropertyName = "clientId")]
        [System.Text.Json.Serialization.JsonPropertyName("clientId")]
        public string ClientId { get; set; }

        [JsonProperty(PropertyName = "clientSecret")]
        [System.Text.Json.Serialization.JsonPropertyName("clientSecret")]
        public string ClientSecret { get; set; }

        [JsonProperty(PropertyName = "requiredRole")]
        [System.Text.Json.Serialization.JsonPropertyName("requiredRole")]
        public string RequiredRole { get; set; }

        [JsonProperty(PropertyName = "extended-roles-from")]
        [System.Text.Json.Serialization.JsonPropertyName("extended-roles-from")]
        public string ExtendedRolesFrom { get; set; }

        [JsonProperty("scopes")]
        [System.Text.Json.Serialization.JsonPropertyName("scopes")]
        public string[] Scopes { get; set; }

        [JsonProperty("claimsFromUserInfoEndpoint")]
        [System.Text.Json.Serialization.JsonPropertyName("claimsFromUserInfoEndpoint")]
        public bool ClaimsFromUserInfoEndpoint { get; set; }

        [JsonProperty("nameClaimType")]
        [System.Text.Json.Serialization.JsonPropertyName("nameClaimType")]
        public string NameClaimType { get; set; }

        [JsonProperty("roleClaimType")]
        [System.Text.Json.Serialization.JsonPropertyName("roleClaimType")]
        public string RoleClaimType { get; set; }
    }

    public class AzureAD
    {
        public string Instance { get; set; }
        public string Domain { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string CallbackPath { get; set; }
        public string SignedOutCallbackPath { get; set; }

        [JsonProperty(PropertyName = "extended-roles-from")]
        [System.Text.Json.Serialization.JsonPropertyName("extended-roles-from")]
        public string ExtendedRolesFrom { get; set; }
    }

    #endregion
}
