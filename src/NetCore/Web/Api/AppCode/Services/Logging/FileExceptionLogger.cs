using E.Standard.Api.App.Configuration;
using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging;
using Microsoft.Extensions.Configuration;
using System;

namespace Api.Core.AppCode.Services.Logging;

public class FileExceptionLogger : GenericExceptionLogger<SimpleExceptionLogger>
{
    public FileExceptionLogger(IConfiguration configuration)
        : base(new SimpleExceptionLogger(
            $"{configuration[ApiConfigKeys.LogPath]}/webgis_exceptions.log",
            50, null))
    {

    }

    public override void LogException(CmsDocument.UserIdentification ui, string server, string service, string command, Exception ex)
        => ((SimpleExceptionLogger)_logger.Clone(ui))
                .LogException(server, service, command, ex);

    public override void LogException(IMap map, string server, string service, string command, Exception ex)
        => ((SimpleExceptionLogger)_logger.Clone(map))
                .LogException(server, service, command, ex);
}
