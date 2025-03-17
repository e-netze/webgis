using Api.Core.Models.DataLinq;
using E.DataLinq.Core.Engines.Abstraction;
using E.DataLinq.Core.Models;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Web;
using E.Standard.Json;
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

public class GeoJsonEngine : IDataLinqSelectEngine, IDataLinqEngineCache
{
    private readonly IHttpService _http;
    private readonly IConfiguration _config;

    public GeoJsonEngine(IHttpService http,
                         IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    #region IDataLinqSelectEngine

    public int EndpointType => (int)WebGISCustomEndPointTypes.GeoJson;

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

        var featureCollection = JSerializer.Deserialize<ApiFeatureCollection>(response);

        if (featureCollection.Success == false && !String.IsNullOrWhiteSpace(featureCollection.Exception))
        {
            throw new Exception(featureCollection.Exception);
        }

        if (featureCollection.Features != null)
        {
            foreach (var feature in featureCollection.Features)
            {
                if (feature.Properties == null)
                {
                    continue;
                }

                ExpandoObject expando = new ExpandoObject();
                IDictionary<string, object> expandoDict = expando;

                expandoDict.Add("oid", feature.Oid);
                if (feature.Geo != null && feature.Geo.TypeName?.ToLower() == "point" && feature.Geo.Coordinates != null && feature.Geo.Coordinates.Length == 2)
                {
                    expandoDict.Add("_location", new RecordLocation()
                    {
                        Longitude = feature.Geo.Coordinates[0],
                        Latitude = feature.Geo.Coordinates[1],

                        BBox = feature.Bounds != null && feature.Bounds.Length == 4 ? feature.Bounds : null
                    });
                }
                foreach (var key in feature.Properties.Keys)
                {
                    if (key.StartsWith("_"))
                    {
                        continue;
                    }

                    expandoDict.Add(key, feature.Properties[key]);
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
