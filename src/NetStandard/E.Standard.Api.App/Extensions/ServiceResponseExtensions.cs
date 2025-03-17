using E.Standard.Api.App.Exceptions;
using E.Standard.WebMapping.Core.ServiceResponses;

namespace E.Standard.Api.App.Extensions;

static public class ServiceResponseExtensions
{
    static public void ThrowException(this ErrorResponse errorResponse, string server, string service, string cmd, string requestId, bool forceWarningException = true)
    {
        if (errorResponse is ExceptionResponse)
        {
            if (forceWarningException)
            {
                throw new ReportWarningException(
                    server,
                    service,
                    cmd,
                    $"{errorResponse.ErrorMessage}\n{errorResponse.ErrorMessage2}",
                    requestId);
            }

            throw new ReportExceptionException(
                server,
                service,
                cmd,
                $"{errorResponse.ErrorMessage}\n{errorResponse.ErrorMessage2}",
                requestId);
        }

        throw new ReportWarningException(
                server,
                service,
                cmd,
                errorResponse.ErrorMessage,
                requestId);
    }
}
