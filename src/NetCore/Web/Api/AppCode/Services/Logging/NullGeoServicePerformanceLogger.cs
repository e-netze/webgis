using E.Standard.WebMapping.Core.Logging;
using E.Standard.WebMapping.Core.Logging.Abstraction;

namespace Api.Core.AppCode.Services.Logging;

public class NullGeoServicePerformanceLogger : GenericGeoServicePerformanceLogger<NullLogger>, IOgcPerformanceLogger
{
    public NullGeoServicePerformanceLogger()
        : base(new NullLogger())
    {

    }
}
