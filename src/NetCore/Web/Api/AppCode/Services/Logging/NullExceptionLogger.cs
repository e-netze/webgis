using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging;
using System;

namespace Api.Core.AppCode.Services.Logging;

public class NullExceptionLogger : GenericExceptionLogger<NullLogger>
{
    public NullExceptionLogger()
        : base(new NullLogger())
    {

    }

    public override void LogException(CmsDocument.UserIdentification ui, string server, string service, string command, Exception ex)
    {

    }

    public override void LogException(IMap map, string server, string service, string command, Exception ex)
    {

    }
}
