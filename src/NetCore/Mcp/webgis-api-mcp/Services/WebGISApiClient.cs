namespace WebGIS.API.MCP.Services;

public class WebGISApiClient
{
    private readonly HttpClient _httpClient;

    public WebGISApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("WebGISApiClient");
    }

    public async Task<T?> GetAsync<T>(string endpoint) where T : class
    {
        endpoint = endpoint.Contains("?")
            ? $"{endpoint}&f=json"
            : $"{endpoint}?f=json";

        var response = await _httpClient.GetAsync($"{endpoint}");
        response.EnsureSuccessStatusCode();

        if(typeof(T) == typeof(string))
        {
            var str = await response.Content.ReadAsStringAsync();
            return str as T;
        }

        return await response.Content.ReadFromJsonAsync<T>();
    }
}
