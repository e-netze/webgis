using System;

namespace E.Standard.WebMapping.Core.ServiceResponses;

public class ExceptionResponse : ErrorResponse
{
    public ExceptionResponse(int index, string serviceID, Exception ex, string preMessage = "")
        : base(index, serviceID,
        preMessage + ((ex != null) ? ex.Message : ""),
        (ex is NullReferenceException) ? ex.StackTrace : "")
    {
    }
}
