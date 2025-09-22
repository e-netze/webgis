using ModelContextProtocol.Server;
using System.ComponentModel;
using WebGIS.API.MCP.Services;

namespace WebGIS.API.MCP.Tools;

public class WebGISApiTools
{
    private readonly WebGISApiClient _apiClient;

    public WebGISApiTools(WebGISApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [McpServerTool]
    [Description("""
        Returns the current version of the WebGIS API.
        The major number represents the main product version.
        The minor number corresponds to the last two digits of the year when the version was published.
        The patch number indicates the calendar week of release, with an additional two digit sequential number if there are multiple releases in the same week.
        """)]
    async public Task<string> GetWebGISVersion() => await _apiClient.GetAsync<string>("rest/version") ?? "Unknown version";

    [McpServerTool]
    [Description("Fetches and returns a list of available map services from the WebGIS API. Every map service as an id, wich is needfull for other tools.")]
    async public Task<string> GetMapServices() =>
        await _apiClient.GetAsync<string>("rest/services") ?? "No services found";

    [McpServerTool]
    [Description("Get information about a map service. Infos are layers, queries, edit themes that are exposed by a map service.")]
    async public Task<string> GetMapServiceInfo(string[] serviceIds) =>
        await _apiClient.GetAsync<string>($"rest/serviceinfo?ids={String.Join(",", serviceIds)}") ?? "No service info available";

    [McpServerTool]
    [Description("""
        Constructs and returns a URL to access a map in the WebGIS portal.
        You can specify one or more map service IDs to include specific services in the map.
        Additionally, you can provide a query ID and associated query parameters to customize the map view.
        Possible query parameters depend on the selected query ID (items with id). 
        Query, query parameters and their values can be found in the map service info.
        The generated URL can be used to directly access the map with the specified configurations.
        """)]
    async public Task<string> GetMapUrl(
        string[]? serviceIds = null,
        string? queryId = null,
        //[Description("Query parameter IDs (items with id) that are defined in the map service info. The syntax for a single paramater is {query-item-id}={value}")]
        //string[]? queryParameters = null,
        [Description("Required query parameters to customize the query execution. Possible query parameters depend on the selected query ID (items with id). Query, query parameters and their values can be found in the map service info.")]
        Dictionary<string, string>? queryParameters = null)
    {
        string portalUrl = "https://localhost:44320/";

        portalUrl += "default/map/Allgemein/Basemap.at?";  // basemap

        if (serviceIds?.Any() == true)
        {
            portalUrl += $"append-services={String.Join(",", serviceIds)}&";  // map services
        }

        if (!String.IsNullOrWhiteSpace(queryId))
        {
            portalUrl += $"query={queryId}&";  // query id
        }

        //foreach(var queryParam in queryParameters ?? Array.Empty<string>())
        //{
        //    portalUrl += $"{queryParam}&";  // query parameters
        //}

        if (queryParameters?.Any() == true)
        {
            foreach (var (key, value) in queryParameters)
            {
                portalUrl += $"{key}={value}&";  // query parameters
            }
        }

        return portalUrl;
    }

    [McpServerTool]
    [Description("""
        Executes a predefined query on a specified map service and returns the results.
        You need to provide the service ID and the query ID to identify the specific query to be executed.
        If query specific objects, query parameters are required and must be included as query parameters to customize the query execution.
        Possible query parameters depend on the selected query ID (items with id). 
        Query, query parameters and their values can be found in the map service info.
        The resultGeometryType parameter allows you to specify the type of geometry to be included in the results:
        - "full": Returns the complete geometry data.
        - "simple": Returns a simplified representation of the geometry, typically as a single point.
        - "none": Excludes geometry data from the results.
        The renderFields parameter determines whether to return rendered fields (including expressions and hotlinks) or only the original database values.
        """)]
    async public Task<string> PerformServiceQuery(
        string serviceId,
        string queryId,
        [Description("Required query parameters parameters to customize the query execution. Possible query parameters depend on the selected query ID (items with id). Query, query parameters and their values can be found in the map service info.")]
        Dictionary<string, string>? queryParameters = null,
        [Description("Determines, if the result contains the full geometry (full), only a single point represents the geometry (simple) or no geometry (none).")] 
        string resultGeometryType = "none",
        [Description("Return rendered fields: returns all fields including expressions and hotlinks, that are set in them WebGIS CMS. If false, only the original fields from the datebase values will returned.")]
        bool renderFields = true 
        )
    {
        string endpoint = $"rest/services/{serviceId}/queries/{queryId}?";

        endpoint += renderFields switch
        {
            false => "c=datalinq_query&",
            _ => "c=query&"
        };

        endpoint += resultGeometryType?.ToLower() switch
        {
            "simple" => "geomtry=simple&",
            "full" => "geometry=full&",
            _ => "geometry=none&"
        };

        if (queryParameters?.Any() == true)
        {
            foreach (var (key, value) in queryParameters)
            {
                endpoint += $"{key}={value}&";  // query parameters
            }
        }

        return await _apiClient.GetAsync<string>(endpoint) ?? "No query result";
    }
}