using E.Standard.WebMapping.Core.Logging;

namespace Api.Core.AppCode.Services.Logging;

public class NullWarningsLogger : GenericWarningsLogger<NullLogger>
{
    public NullWarningsLogger()
        : base(new NullLogger())
    {

    }
}
