using E.Standard.WebMapping.Core.Logging;
using E.Standard.WebMapping.Core.Logging.Abstraction;

namespace Api.Core.AppCode.Services.Logging;

public class NullUsagePerformanceLogger : GenericUsagePerformanceLogger<NullLogger>, IDatalinqPerformanceLogger
{
    public NullUsagePerformanceLogger()
         : base(new NullLogger())
    {

    }
}
