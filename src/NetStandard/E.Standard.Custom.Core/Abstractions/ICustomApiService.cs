using E.Standard.Configuration.Services;
using E.Standard.WebGIS.Core.Models.Abstraction;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomApiService
{
    void InitGlobals(ConfigurationService configuration);

    Task HandleApiResultObject(IWatchable watchable, byte[] data, string username);
    Task HandleApiResultObject(IWatchable watchable, string json, string username);
    Task HandleApiResultObject(IWatchable watchable, int contentLength, string typeName, string username);

    Task HandleApiClientAction(string clientId, string action, string username);

    Task LogToolRequest(string id, string category, string map, string toolId, string username);

    Dictionary<string, string> CustomSearchServices();
}
