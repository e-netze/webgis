using System;

namespace E.Standard.Api.App.Exceptions;

public class ReportWarningException : Exception
{
    public ReportWarningException(string server, string service, string cmd, string message, string requestId)
        : base(message)
    {
        this.Server = server;
        this.Service = service;
        this.Command = cmd;
        this.RequestId = requestId;
    }

    public string Server { get; }
    public string Service { get; }
    public string Command { get; }
    public string RequestId { get; }
}
