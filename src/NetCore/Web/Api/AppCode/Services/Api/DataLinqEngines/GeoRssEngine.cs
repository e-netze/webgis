using Api.Core.Models.DataLinq;
using E.DataLinq.Core.Engines.Abstraction;
using E.DataLinq.Core.Models;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Web;
using E.Standard.ThreadsafeClasses;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.Api.DataLinqEngines;

public class GeoRssEngine : IDataLinqSelectEngine, IDataLinqEngineCache
{
    private readonly IHttpService _http;
    private readonly IConfiguration _config;

    public GeoRssEngine(IHttpService http,
                        IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    #region IDataLinqSelectEngine

    public int EndpointType => (int)WebGISCustomEndPointTypes.GeoRss;

    async public Task<(object[] records, bool isOrdered)> SelectAsync(DataLinqEndPoint endPoint, DataLinqEndPointQuery query, NameValueCollection arguments)
    {
        bool isOrdered = false;
        var webConnectionString = new WebConnectionString(endPoint.ConnectionString);

        string serviceUrl = _config.DataLinqApiEngineConnectionReplace(webConnectionString.Service);
        string url = $"{serviceUrl}{query.Statement.ParseStatementPreCompilerDirectives(arguments, StatementType.Url)}";

        foreach (var parameterName in arguments.AllKeys)
        {
            if (String.IsNullOrWhiteSpace(parameterName))
            {
                continue;
            }

            if (url.Contains("{{" + parameterName + "}}"))
            {
                url = url.Replace("{{" + parameterName + "}}", arguments[parameterName]);
            }
        }

        if (webConnectionString.CacheExpires > 0)
        {
            var cachedResult = _selectApiTemporaryCache[url];
            if (cachedResult != null)
            {
                return (records: cachedResult.ToArray(), isOrdered: isOrdered);
            }
        }

        List<object> result = new List<object>();

        var requestAuthorization = !String.IsNullOrEmpty(webConnectionString.User) ?
            new RequestAuthorization("Basic")
            {
                Username = webConnectionString.User,
                Password = webConnectionString.Password
            } : null;

        string response = await _http.GetStringAsync(url, requestAuthorization);

        var features = E.Standard.Api.App.DTOs.FeaturesDTO.FromGeoRSS(response, rawAttributes: true);

        if (features?.features != null)
        {
            foreach (var feature in features.features)
            {
                if (feature.properties == null)
                {
                    continue;
                }

                ExpandoObject expando = new ExpandoObject();
                IDictionary<string, object> expandoDict = expando;

                expandoDict.Add("oid", feature.oid);
                if (feature.geometry != null && feature.geometry.type?.ToLower() == "point" &&
                    feature.geometry.coordinates is double[] coords && coords.Length == 2)
                {
                    expandoDict.Add("_location", new RecordLocation()
                    {
                        Longitude = Convert.ToDouble(coords[0]),
                        Latitude = Convert.ToDouble(coords[1]),

                        BBox = feature.bounds != null && feature.bounds.Length == 4 ? feature.bounds : null
                    });
                }

                if (feature.properties is IDictionary<string, object> properties)
                {
                    foreach (var key in properties.Keys)
                    {
                        if (key.StartsWith("_"))
                        {
                            continue;
                        }

                        expandoDict.Add(key, properties[key]);
                    }
                }

                result.Add(expando);
            }
        }

        if (webConnectionString.CacheExpires > 0)
        {
            _selectApiTemporaryCache.Add(url, result, webConnectionString.CacheExpires);
        }

        return (records: result.ToArray(), isOrdered: isOrdered);
    }

    async public Task<bool> TestConnection(DataLinqEndPoint endPoint)
    {
        var webConnectionString = new WebConnectionString(endPoint.ConnectionString);
        var url = _config.DataLinqApiEngineConnectionReplace(webConnectionString.Service);

        var requestAuthorization = !String.IsNullOrEmpty(webConnectionString.User) ?
        new RequestAuthorization("Basic")
        {
            Username = webConnectionString.User,
            Password = webConnectionString.Password
        } : null;

        await _http.GetStringAsync(url, requestAuthorization);

        return true;
    }

    #endregion

    #region IDataLinqEngineCache

    private static readonly TemporaryCache<List<object>> _selectApiTemporaryCache = new TemporaryCache<List<object>>(3600);

    public bool ClearCache()
    {
        _selectApiTemporaryCache.Clear();
        return true;
    }

    #endregion
}
