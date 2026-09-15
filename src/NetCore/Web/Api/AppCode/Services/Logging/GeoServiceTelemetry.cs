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
}
