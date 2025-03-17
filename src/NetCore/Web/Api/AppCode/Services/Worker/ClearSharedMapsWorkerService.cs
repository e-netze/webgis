using E.Standard.Custom.Core.Abstractions;
using E.Standard.Json;
using E.Standard.WebGIS.Core.Serialization;
using System.IO;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.Worker;

public class ClearSharedMapsWorkerService : IWorkerService
{
    private readonly ApiConfigurationService _apiConfig;

    public ClearSharedMapsWorkerService(ApiConfigurationService config)
    {
        _apiConfig = config;
    }

    public int DurationSeconds => 30 * 60;

    async public Task<bool> DoWork()
    {
        try
        {
            if (_apiConfig.UseClearSharedMapsBackgroundTask)
            {
                #region Clear expired shared maps 

                string storagePath = _apiConfig.StorageRootPath;

                if (!storagePath.ToLower().StartsWith("http://") && !storagePath.ToLower().StartsWith("https://"))
                {
                    foreach (var portalDirectory in new DirectoryInfo($"{storagePath}/webgis.tools.portal.maps").GetDirectories())
                    {
                        foreach (var sharedMapsDirectory in portalDirectory.GetDirectories("_sharedmaps"))
                        {
                            foreach (var metafile in sharedMapsDirectory.GetFiles("*.mta"))
                            {
                                try
                                {
                                    SharedMapMeta meta = JSerializer.Deserialize<SharedMapMeta>(await File.ReadAllTextAsync(metafile.FullName));

                                    if (meta.IsExpired)
                                    {
                                        string name = metafile.Name.Split('.')[0];

                                        foreach (var file in metafile.Directory.GetFiles($"{name}.*"))
                                        {
                                            file.Delete();
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }

                }

                #endregion
            }
        }
        catch { }

        return true;
    }

    public Task<bool> Init()
    {
        return Task.FromResult(true);
    }
}
