using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Exceptions;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using System;
using System.Linq;
using System.Text;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;

static class AgsErrorResponseExtensions
{
    public static string UrlEncodePassword(this string password)
    {
        if (password != null && password.IndexOfAny("+/=&".ToCharArray()) >= 0)
        {
            password = System.Web.HttpUtility.UrlEncode(password);
        }

        return password;
    }

    public static void ThrowIfTokenRequiredOrError(this byte[] data)
    {
        if (data?.FirstOrDefault() == '{')
        {
            string jsonString = "";
            try
            {
                jsonString = Encoding.UTF8.GetString(data);
                if (!jsonString.Contains("\"error\":"))
                {
                    return;
                }
            }
            catch { }

            if (jsonString.TryParseJsonError(out JsonError jsonError))
            {
                jsonError.ThrowIfTokenRequiredOrError();
            }
        }
    }

    public static bool TryParseJsonError(this string responseString, out JsonError jsonError)
    {
        try
        {
            jsonError = JSerializer.Deserialize<JsonError>(responseString);
        }
        catch
        {
            jsonError = null;
        }

        return jsonError is not null;
    }

    public static void ThrowIfTokenRequiredOrError(this JsonError jsonError)
    {
        var error = jsonError?.error
                        ?? jsonError?.addResults?.Where(r => r?.error is not null).FirstOrDefault()?.error
                        ?? jsonError?.updateResults?.Where(r => r?.error is not null).FirstOrDefault()?.error
                        ?? jsonError?.deleteResults?.Where(r => r?.error is not null).FirstOrDefault()?.error;

        if (error is null)
        {
            throw new Exception("Unknown error");
        }

        if (error.code == 499 || error.code == 498 || error.code == 403) // Token Required (499), Invalid Token (498), No user Persmissions (403)
        {
            throw new TokenRequiredException();
        }

        throw new Exception($"Error:{error.code}\n{error.message.OrTake(error.description)}");
    }

    public static void ThrowIfTokenRequiredOrError(this string responseString)
    {
        if (responseString.Contains("\"error\":") && responseString.TryParseJsonError(out JsonError jsonError))
        {
            jsonError.ThrowIfTokenRequiredOrError();
        }
    }
}
