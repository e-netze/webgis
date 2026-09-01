using System;
using System.IO;
using System.Threading.Tasks;

using E.Standard.Api.App;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Extensions.Security;
using E.Standard.Web.Extensions;

namespace Api.Core.AppCode.Services.Worker;

public class ClearOutputWorkerService : IWorkerService
{
    private readonly ApiConfigurationService _apiConfig;
    private readonly string[] _extensionFilters = new string[] { "*.json", "*.pdf", "*.zip", "*.png", "*.jpg", "*.jpeg", "*.csv", "*.txt", "*.dat", $"*{ApiGlobals.DownloadFileExtension}" };

    public ClearOutputWorkerService(ApiConfigurationService apiConfig)
    {
        _apiConfig = apiConfig;
    }

    public int DurationSeconds => 600;

    public Task<bool> DoWork()
    {
        if(!_apiConfig.UseClearOuputBackgroundTask)
        {
            return Task.FromResult(true);
        }

        var outputPath = _apiConfig.OutputPath;
        if (outputPath.IsUrl()) // if output is an Url (https://....) => Service, dont run cleanup
        {
            return Task.FromResult(true);
        }

        try
        { 
            #region Clear Output

            foreach (string extensionFilter in _extensionFilters)
            {
                foreach (var file in new DirectoryInfo(outputPath).GetFiles(extensionFilter))
                {
                    int totalSeconds = extensionFilter switch
                    {
                        "*.pdf" when file.Name.StartsWith(ApiGlobals.PrintOutputPrefix) => 3600,  // Print PDF
                        "*.zip" when file.Name.StartsWith(ApiGlobals.PrintOutputPrefix) => 3600,  // Print Zips
                        "*.jpg" when (file.Name.StartsWith(ApiGlobals.PrintOutputPrefix) && file.Name.Contains("_preview")) => 3600,  // Print previews
                        "*.pdf" when file.Name.StartsWith(ApiGlobals.ProfileOutputPrefix) => 3600,  // Print PDF
                        "*.png" when(file.Name.StartsWith(ApiGlobals.ProfileOutputPrefix) && file.Name.Contains("_preview")) => 3600,  // Print previews
                        _ => 60
                    };

                    if ((DateTime.UtcNow - file.CreationTimeUtc).TotalSeconds >= totalSeconds)
                    {
                        file.FullName.TryDelete();
                    }
                }
            }
            
            #endregion
        }
        catch { }

        return Task.FromResult(true);
    }

    public Task<bool> Init()
    {
        return Task.FromResult(true);
    }
}
