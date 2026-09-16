using System;

namespace E.Standard.WebMapping.Core.Logging.Abstraction;

public interface IGeoServiceRequestLogger : IWebGISLogger
{
    /// <summary>
    /// Logs a GeoService request together with its <paramref name="requestBody"/> kept separate
    /// from <paramref name="message"/> (typically the raw response, often JSON, coming back from
    /// the upstream GIS server), instead of the caller having to concatenate both into a single
    /// string up front. Concatenating defeats automatic JSON-recognition of <paramref
    /// name="message"/> by log/trace tooling once a "request body" prefix is glued in front of it.
    /// Loggers that only support a single message stream (e.g. the CSV file logger) don't need to
    /// implement this - the default here falls back to the previous concatenated behavior.
    /// </summary>
    void LogString(string server, string service, string cmd, string message, string requestBody, int performaceMilliseconds = 0, bool success = true)
        => LogString(
            server, service, cmd,
            string.IsNullOrEmpty(requestBody) ? message : $"{requestBody}{Environment.NewLine}=>{Environment.NewLine}{message}",
            performaceMilliseconds, success);
}
