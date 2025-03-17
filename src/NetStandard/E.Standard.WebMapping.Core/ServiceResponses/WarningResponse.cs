using E.Standard.WebMapping.Core.Abstraction;
using System;

namespace E.Standard.WebMapping.Core.ServiceResponses;

public class WarningResponse : ErrorResponse
{
    public WarningResponse(IMapService service)
        : this(service.Map.Services.IndexOf(service), service.ID)
    {
    }

    public WarningResponse(int index, string serviceID)
        : this(index, serviceID, String.Empty, String.Empty)
    {
    }

    public WarningResponse(int index, string serviceID, string message, string message2)
        : base(index, serviceID)
    {
    }
}
