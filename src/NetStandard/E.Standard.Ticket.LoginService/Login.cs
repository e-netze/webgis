using E.Standard.Json;
using E.Standard.Web.Abstractions;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Ticket.LoginService;

public class Login
{
    public Login(string url)
    {
        this.Url = url;
    }

    public string Url { get; set; }

    //public string GetToken(string user, string password, WebProxy webProxy = null, int minimunSecondsValid = 600)   // mind 10min gültig
    //{
    //    using (WebClient client = new WebClient())
    //    {
    //        if (webProxy != null)
    //        {
    //            client.Proxy = webProxy;
    //        }

    //        System.Collections.Specialized.NameValueCollection reqParameters = new System.Collections.Specialized.NameValueCollection();
    //        reqParameters.Add("username", user);
    //        reqParameters.Add("password", password);
    //        reqParameters.Add("miniumSecondsValid", minimunSecondsValid.ToString());

    //        TokenResponse response = TokenResponse.Deserialize(client.UploadValues(Url + "/login/gettoken", "POST", reqParameters));

    //        if (response.success)
    //        {
    //            return response.token;
    //        }

    //        throw new Exception(response.exception);
    //    }
    //}

    async public Task<TokenType> GetTokenType(
        IHttpService http,
        string user, string password,
        WebProxy webProxy = null, int minimunSecondsValid = 600)   // mind 10min gültig) 
    {
        var formData = new StringBuilder();
        formData.Append($"username={user}");
        formData.Append($"&password={password}");
        formData.Append($"&minimunSecondsValid={minimunSecondsValid}");

        var response = TokenResponse.Deserialize(
            Encoding.UTF8.GetBytes(
                await http.PostFormUrlEncodedStringAsync($"{Url}/login/gettoken",
                formData.ToString())
            )
        );

        if (response.success)
        {
            return new TokenType()
            {
                Token = response.token,
                Expires = new DateTime(response.token_expires.Ticks, DateTimeKind.Utc)
            };
        }

        throw new Exception(response.exception);

        //using (WebClient client = new WebClient())
        //{
        //    if (webProxy != null)
        //    {
        //        client.Proxy = webProxy;
        //    }

        //    System.Collections.Specialized.NameValueCollection reqparm = new System.Collections.Specialized.NameValueCollection();
        //    reqparm.Add("username", user);
        //    reqparm.Add("password", password);
        //    reqparm.Add("miniumSecondsValid", minimunSecondsValid.ToString());

        //    TokenResponse response = TokenResponse.Deserialize(client.UploadValues(Url + "/login/gettoken", "POST", reqparm));

        //    if (response.success)
        //    {
        //        return new TokenType()
        //        {
        //            Token = response.token,
        //            Expires = new DateTime(response.token_expires.Ticks, DateTimeKind.Utc)
        //        };
        //    }

        //    throw new Exception(response.exception);
        //}
    }

    #region Sub Classes

    private class TokenResponse
    {
        public bool success { get; set; }

        [JsonProperty(PropertyName = "token", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("token")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string token { get; set; }

        [JsonProperty(PropertyName = "token_expires", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("token_expires")]
        //[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DateTime token_expires { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string exception { get; set; }

        static public TokenResponse Deserialize(string json)
        {
            return JSerializer.Deserialize<TokenResponse>(json);
        }
        static public TokenResponse Deserialize(byte[] bytes)
        {
            return Deserialize(Encoding.UTF8.GetString(bytes));
        }
    }

    #endregion
}
