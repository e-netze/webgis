namespace E.Standard.Api.App.Exceptions;

public class ReportExceptionException : ReportWarningException
{
    public ReportExceptionException(string server, string service, string cmd, string message, string requestId)
        : base(server, service, cmd, message, requestId)
    {

    }
}
