using E.WebGIS.SDK;
using Microsoft.Extensions.Options;

namespace E.Standard.WebGIS.SDK.Services;

public class SDKPluginManagerService
{
    private readonly SDKPluginManager _manager;

    public SDKPluginManagerService(IOptionsMonitor<SDKPluginManagerServiceOptions> options)
    {
        try
        {
            _manager = new SDKPluginManager(options.CurrentValue.RootPath);
        }
        catch
        {
            _manager = new SDKPluginManager();
        }
    }

    public SDKPluginManager Manager => _manager;
}
