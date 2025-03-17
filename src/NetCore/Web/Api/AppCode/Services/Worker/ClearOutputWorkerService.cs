using E.Standard.Custom.Core.Abstractions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.Worker;

public class ClearOutputWorkerService : IWorkerService
{
    private readonly ApiConfigurationService _apiConfig;

    public ClearOutputWorkerService(ApiConfigurationService apiConfig)
    {
        _apiConfig = apiConfig;
    }

    public int DurationSeconds => 600;

    public Task<bool> DoWork()
    {
        #region Clear Output

        try
        {
            if (_apiConfig.UseClearOuputBackgroundTask)
            {
                var outputPath = _apiConfig.OutputPath;

                if (!outputPath.ToLower().StartsWith("http://") && !outputPath.ToLower().StartsWith("https://"))
                {
                    foreach (string extension in new string[] { "*.json", "*pdf", "*.zip", "*.png", "*.jpg", "*.jpeg", "*.csv" })
                    {
                        foreach (var file in new DirectoryInfo(outputPath).GetFiles(extension))
                        {
                            if ((DateTime.UtcNow - file.CreationTimeUtc).TotalMinutes >= 10)
                            {
                                try
                                {
                                    file.Delete();
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
        }
        catch { }

        return Task.FromResult(true);

        #endregion
    }

    public Task<bool> Init()
    {
        return Task.FromResult(true);
    }
}
