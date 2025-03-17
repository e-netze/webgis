using E.Standard.WebMapping.Core.Abstraction;
using System.Collections.Concurrent;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Services;

internal class AgsTokenStore
{
    private ConcurrentDictionary<string, string> _tokens = new();

    public string GetToken(IMapServiceAuthentication mapService)
        => GetToken(ServiceKey(mapService));

    public string GetToken(string serviceKey)
        => _tokens.TryGetValue(serviceKey, out var token)
            ? token
            : null;

    public bool ContainsKey(string serviceKey) => _tokens.ContainsKey(serviceKey);

    public void SetToken(IMapServiceAuthentication mapService, string token)
        => _tokens[ServiceKey(mapService)] = token;

    public string ServiceKey(IMapServiceAuthentication mapService)
        => $"{mapService.Server}:{mapService.Username}";
}
