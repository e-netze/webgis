using E.Standard.Api.App.Configuration;
using E.Standard.WebMapping.Core.Logging;
using Microsoft.Extensions.Configuration;

namespace Api.Core.AppCode.Services.Logging;

public class FileWarningsLogger : GenericWarningsLogger<SimpleFileLogger>
{
    public FileWarningsLogger(IConfiguration configuration)
        : base(new SimpleFileLogger(
            $"{configuration[ApiConfigKeys.LogPath]}/webgis_warnings.log", 50))
    {

    }
}
