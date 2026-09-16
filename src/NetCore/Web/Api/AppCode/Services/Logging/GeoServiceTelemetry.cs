using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Api.Core.AppCode.Services.Logging;

/// <summary>
/// Central OpenTelemetry instrumentation (metrics + distributed tracing) for the WebGIS
/// performance-logger family (GeoService/OGC/Usage/DataLinq), shared by <see cref="MicrosoftLog"/>.
/// Recording is effectively free when nothing listens (no OTel exporter configured, no
/// dotnet-counters/dotnet-trace attached): the metrics API is designed so <see cref="Histogram{T}.Record(T)"/>/
/// <see cref="Counter{T}.Add(T)"/> are cheap no-ops without a subscriber, and
/// <see cref="ActivitySource.StartActivity(string)"/> simply returns <see langword="null"/>.
/// See webgis.ServiceDefaults, which registers both the meter and the activity source by
/// <see cref="Name"/> for every Aspire-hosted service, so dashboards/traces work as soon as an
/// OTLP exporter (or any other listener) is configured - independent of the configured
/// Microsoft.Extensions.Logging level.
/// </summary>
internal static class GeoServiceTelemetry
{
    public const string Name = "WebGIS.GeoServices";

    private static readonly Meter _meter = new(Name);

    public static readonly ActivitySource ActivitySource = new(Name);

    private static readonly Histogram<double> _requestDuration = _meter.CreateHistogram<double>(
        "webgis.geoservice.request.duration",
        unit: "ms",
        description: "Duration of GeoService/OGC/Usage/DataLinq performance-logged requests.");

    private static readonly Counter<long> _requestCount = _meter.CreateCounter<long>(
        "webgis.geoservice.requests",
        description: "Number of GeoService/OGC/Usage/DataLinq performance-logged requests.");

    public static void RecordRequest(string category, string command, string server, string service, bool success, double durationMs)
    {
        var tags = new TagList
        {
            { "category", category },
            { "command", command },
            { "server", server ?? string.Empty },
            { "service", service ?? string.Empty },
            { "success", success },
        };

        _requestDuration.Record(durationMs, tags);
        _requestCount.Add(1, tags);
    }

    /// <summary>
    /// Attaches a WebGIS request-audit entry (<c>IGeoServiceRequestLogger.LogString</c>) as
    /// an OpenTelemetry span event on the currently active Activity - typically the
    /// "geoservice:{command} {service}" span started by <c>MicrosoftLog</c> around the whole
    /// request. This makes the entry show up directly on that span in a trace viewer (e.g. the
    /// Aspire dashboard's trace detail view), in addition to (and independent of) whatever the
    /// "microsoft" <c>IGeoServiceRequestLogger</c> implementation separately writes via
    /// <c>Microsoft.Extensions.Logging</c> at <c>LogLevel.Trace</c> - so it is unaffected by that
    /// logger's minimum level. <see cref="Activity.IsAllDataRequested"/> is checked first so tags
    /// aren't allocated when nothing is listening/sampling. <paramref name="requestBody"/> and
    /// <paramref name="message"/> (the response) are kept as separate tags - not concatenated -
    /// so a JSON response is still recognizable as JSON by tooling that inspects the tag value.
    /// </summary>
    public static void RecordRequestEvent(string server, string service, string command, string message, string requestBody = null)
    {
        var activity = Activity.Current;
        if (activity is null || !activity.IsAllDataRequested)
        {
            return;
        }

        var tags = new ActivityTagsCollection
        {
            { "webgis.server", server ?? string.Empty },
            { "webgis.service", service ?? string.Empty },
            { "webgis.command", command ?? string.Empty },
            { "webgis.requestResult", message ?? string.Empty },
        };

        if (!string.IsNullOrEmpty(requestBody))
        {
            tags["webgis.requestBody"] = requestBody;
        }

        activity.AddEvent(new ActivityEvent("webgis.geoservice.request", tags: tags));
    }
}
