using Api.Core.Models.DataLinq;
using E.DataLinq.Core.Engines.Abstraction;
using E.DataLinq.Core.Models;
using E.Standard.WebGIS.SDK.DataLinq;
using E.Standard.WebGIS.SDK.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.Api.DataLinqEngines;

public class SdkPluginEngine : IDataLinqSelectEngine
{
    private readonly SDKPluginManagerService _sdkPlugins;

    public SdkPluginEngine(SDKPluginManagerService sdkPlugins)
    {
        _sdkPlugins = sdkPlugins;
    }

    public int EndpointType => (int)WebGISCustomEndPointTypes.WebGIS_Api;

    async public Task<(object[] records, bool isOrdered)> SelectAsync(DataLinqEndPoint endPoint, DataLinqEndPointQuery query, NameValueCollection arguments)
    {
        IDataLinqEndpoint plugin = _sdkPlugins.Manager.CreatePluginInstance<IDataLinqEndpoint>(endPoint.Plugin);
        if (plugin == null)
        {
            throw new Exception("Unknown plugin: " + endPoint.Plugin);
        }

        string statement = query.Statement;

        foreach (var parameterName in arguments.AllKeys)
        {
            if (String.IsNullOrWhiteSpace(parameterName))
            {
                continue;
            }

            statement = statement.Replace("{{" + parameterName + "}}", arguments[parameterName]);
        }

        while (statement.Contains("{{"))
        {
            int start = statement.IndexOf("{{"), end = statement.IndexOf("}}", start);
            statement = statement.Substring(0, start) + statement.Substring(end + 2);
        }

        // ToDo: Plugins sollten InitAsync und SelectAsync implementieren
        plugin.Init(endPoint.ConnectionString);
        var records = await plugin.Select(statement);

        if (arguments["_original"] == "true")
        {
            return (records: records, isOrdered: false);
        }

        List<object> ret = new List<object>();
        if (records != null)
        {
            foreach (var record in records)
            {
                if (record is IDictionary<string, object>)
                {
                    var recordDict = (IDictionary<string, object>)record;
                    ExpandoObject flatRecord = new ExpandoObject();
                    var flatRecordDict = (IDictionary<string, object>)flatRecord;

                    CopyProperties(recordDict, flatRecordDict);

                    ret.Add(flatRecordDict);
                }
            }
        }

        return (records: ret.ToArray(), isOrdered: false);
    }

    public Task<bool> TestConnection(DataLinqEndPoint endPoint)
    {
        throw new NotImplementedException();
    }

    #region Helper

    private void CopyProperties(IDictionary<string, object> from, IDictionary<string, object> to, string prefix = "")
    {
        while (prefix.EndsWith("__"))
        {
            prefix = prefix.Substring(0, prefix.Length - 1);
        }

        foreach (var property in from.Keys)
        {
            var val = from[property];
            if (val is IDictionary<string, object>)
            {
                CopyProperties((IDictionary<string, object>)val, to, (String.IsNullOrEmpty(prefix) ? "" : (prefix.EndsWith("_") ? prefix : prefix + "_")) + property + "_");
            }
            else if (val is object[])
            {
                var array = (object[])val;

                int count = 0;
                foreach (var item in array)
                {
                    if (item is IDictionary<string, object>)
                    {
                        CopyProperties((IDictionary<string, object>)item, to, (String.IsNullOrEmpty(prefix) ? "" : (prefix.EndsWith("_") ? prefix : prefix + "_")) + property + "_item" + (count++) + "_");
                    }
                }

                to[prefix + property + "_array_count"] = count;
            }
            else
            {
                to[prefix + property] = val;
            }
        }
    }

    #endregion
}
