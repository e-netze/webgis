# Logging

WebGIS has two, mostly independent logging layers:

1. **GeoService performance/request logging** — controlled by the `Api:logging-type`
   setting in `_api.config` (`files` or `microsoft`). This decides which
   `IGeoServicePerformanceLogger` / `IGeoServiceRequestLogger` implementation is used to
   record how long GeoService requests (`GetMap`, `GetSelection`, `GetLegend`,
   `GetPrintImage`, print jobs, ...) take. `files` writes the classic CSV log files under
   `Log_Path` (`webgis_performance.csv`, ...); `microsoft` routes the same events through
   `Microsoft.Extensions.Logging` (`ILogger`), which is the entry point into everything
   described below.
2. **Application/host logging** — the standard `Microsoft.Extensions.Logging` pipeline
   used by ASP.NET Core itself and by the WebGIS internals when `logging-type` is set to
   `microsoft`. This is what this document is about: how an administrator can choose
   *where* those log events end up.

The `Api`, `Cms` and `Portal` hosts all share the same setup, so everything below applies
to all three (adjust the file names accordingly, e.g. `Cms/appsettings.json`).

> **Note:** `MicrosoftGeoServiceRequestLogger` (the `microsoft` `IGeoServiceRequestLogger`,
> i.e. the per-request "audit" log, as opposed to the performance timings) logs at
> `LogLevel.Trace` — the most verbose level, disabled by default. `Logging:LogLevel:Default`
> is normally `Information`, so `_logger.IsEnabled(LogLevel.Trace)` is `false` and nothing is
> written; this is expected filtering, not a bug. To see these entries, add an explicit
> category override set to `Trace` (raising only `Default` is not enough, since `Trace` is
> more verbose than `Information`):
> ```json
> "Logging": {
>   "LogLevel": {
>     "Default": "Information",
>     "Api.Core.AppCode.Services.Logging.MicrosoftGeoServiceRequestLogger": "Trace"
>   }
> }
> ```
> This can go in `appsettings.json`, `_config/logging.json`, or as
> `Logging__LogLevel__Api.Core.AppCode.Services.Logging.MicrosoftGeoServiceRequestLogger=Trace`.
> If a specific provider (e.g. `Console`) has its own narrower `Logging:<Provider>:LogLevel`
> section, that provider needs the same override too. Under the hood, `AddLoggingEngine()`
> (`E.Standard.WebApp`) always routes logging through Serilog, which normally only obeys its
> own `Serilog:MinimumLevel` section and ignores `Logging:LogLevel` entirely; WebGIS bridges
> `Logging:LogLevel` into Serilog's `MinimumLevel` automatically so this standard ASP.NET Core
> section keeps working as documented. An explicit `Serilog:MinimumLevel`/`Override` entry for
> the same category always takes precedence over the bridged `Logging:LogLevel` value.

## Configuring via `_config` (recommended for production/Kubernetes)

Most customers only touch the `_config` directory next to the application binaries (it also
holds `api.config`/`cms.config`/`portal.config`) - in a Kubernetes deployment this is typically
the one directory mounted from a ConfigMap/Secret volume, while `appsettings.json` and the pod's
environment variables are not (easily) editable per instance. Everything below can therefore
also be configured by dropping one or both of these optional files into `_config`, with no
rebuild and no pod-spec changes required:

- **`_config/logging.json`** - a regular JSON file merged into the standard
  `Microsoft.Extensions.Configuration` configuration, i.e. anything you could otherwise only
  configure via `appsettings.json` or environment variables (`Logging`, `Serilog`, `OTEL_*`
  keys, ...) can be placed here instead. All JSON examples in this document work unchanged as
  the content of `_config/logging.json`.
- **`_config/logging.env`** - a simple `KEY=VALUE` per line file (like a Docker `--env-file`,
  `#` starts a comment line), loaded as real process environment variables before the app
  starts. Useful for the standard OpenTelemetry `OTEL_*` variables shown below, since they are
  normally set as environment variables rather than nested JSON. An environment variable that
  is already set on the process/pod always wins over the file.

Example `_config/logging.env`:

```
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```

Both files are optional and take effect for `Api`, `Cms` and `Portal` alike; a missing file is
a no-op. `_config/logging.json` is loaded after `appsettings.json`/`appsettings.<Environment>
.json`, so it overrides them (matching how `_config/*.config` already overrides other defaults
today). The shared loading logic lives in `E.Standard.Configuration`
(`ConfigDirectory`, `EnvFileLoader`, `AddConfigDirectoryJsonFile()`); all three hosts wire it up
identically via a single `builder.AddLoggingEngine()` call (`E.Standard.WebApp`), which is also
the one place that would need to change if the logging engine/framework is ever swapped out.

## Console (default)

Out of the box, logs go to the console (stdout), formatted as plain text — this is the
ASP.NET Core default and requires no configuration. Log levels are controlled the usual
way via the `Logging:LogLevel` section in `appsettings.json` or environment variables
(`Logging__LogLevel__Default=Information`, ...).

For structured/JSON console output (useful when a container log collector like
Filebeat/Fluent Bit/Promtail parses stdout), switch the console formatter:

```json
"Logging": {
  "Console": {
    "FormatterName": "json"
  }
}
```

## OpenTelemetry (OTLP) — recommended for most backends

The `Api`, `Cms` and `Portal` hosts export logs, metrics and traces via the OpenTelemetry
Protocol (OTLP) whenever an OTLP endpoint is configured. This is controlled purely by the
standard OpenTelemetry environment variables — no other configuration or rebuild is
needed:

```
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc        # or "http/protobuf" for port 4318
OTEL_SERVICE_NAME=webgis-api            # optional, defaults to the host name
```

If `OTEL_EXPORTER_OTLP_ENDPOINT` is not set, no OTLP exporter is created and there is no
overhead.

These can be set as regular process/pod environment variables, or - if only the `_config`
directory is available - via `_config/logging.env` (see above).

This is the lowest-effort way to plug WebGIS into most modern observability backends,
because they all speak OTLP natively (no WebGIS-specific sink needed): an OpenTelemetry
Collector (which can then fan out anywhere), Grafana stack (Loki/Tempo/Mimir), Elastic
(8.x+ / Elastic Cloud), Seq, Jaeger, Zipkin, Honeycomb, Datadog, New Relic, Azure Monitor,
AWS X-Ray/ADOT, etc.

> Previously this was only wired up for local development (Aspire). It is now available
> in Release builds of `Api`, `Cms` and `Portal` as well.

### Request log entries on the trace

GeoService requests (`GetMap`, `GetSelection`, ..., print jobs) each run inside their own
`geoservice:{command} {service}` trace span (`WebGIS.GeoServices` `ActivitySource`, tagged with
`webgis.category`/`command`/`server`/`service`/`map`/`success`). While that span is active, every
`IGeoServiceRequestLogger.LogString(...)` call (the per-request "audit" trail - e.g. the
underlying HTTP request/response to the real GIS server) is *also* attached to it as an
OpenTelemetry span event (`webgis.geoservice.request`, with `webgis.server`/`service`/`command`
tags) - see `GeoServiceTelemetry.RecordRequestEvent`. In a trace viewer (e.g. the Aspire
dashboard's trace detail view, or the "View Logs" action there for the correlated
`Microsoft.Extensions.Logging` entries), this means the request-audit trail for a call shows up
directly next to/on its span, not just as a separately-filtered log level. Recording the span
event does not depend on the `microsoft` `IGeoServiceRequestLogger`'s own `Logging:LogLevel`
(`LogLevel.Trace`, see above) - it is skipped only when nothing is sampling/listening to the
trace (`Activity.IsAllDataRequested`).

The request and response are kept as two separate tags instead of being concatenated into one
string:

- `webgis.requestResult` - the response (or, for call sites that only log a single value, that
  value) - typically JSON, and still recognizable as such by tooling since nothing is prefixed
  in front of it.
- `webgis.requestBody` - only present when a request body was actually logged separately (e.g.
  the outgoing request sent to the upstream GIS server).

The same split applies to the structured `Microsoft.Extensions.Logging` entry itself: with a
request body, `MicrosoftGeoServiceRequestLogger` logs `requestBody` and `message` as distinct
named placeholders (rather than one pre-joined string), so a JSON response is still parsed as
JSON by log backends that recognize structured fields.

## Serilog sinks (SQL Server / PostgreSQL)

For deployments that need logs written directly into a relational database — without
standing up an OpenTelemetry Collector — `Api`, `Cms` and `Portal` additionally run
[Serilog](https://serilog.net/) alongside the default `ILogger` pipeline (wired up via
`builder.AddLoggingEngine()`, `E.Standard.WebApp`, internally `Host.UseSerilog(...,
writeToProviders: true)`), so it augments rather than replaces OTLP/console logging. Serilog is
purely config-driven: with no `Serilog` section present, nothing changes from today's behavior
(console output only).

To add a sink, add a `Serilog` section to `appsettings.json` (or
`appsettings.Production.json`, or environment variables) — or, equivalently, to
`_config/logging.json` (see above).

### SQL Server

```json
"ConnectionStrings": {
  "LogsDb": "Server=sql-host;Database=WebGisLogs;User Id=webgis;Password=***;TrustServerCertificate=True"
},
"Serilog": {
  "WriteTo": [
    {
      "Name": "MSSqlServer",
      "Args": {
        "connectionString": "LogsDb",
        "sinkOptions": {
          "tableName": "Logs",
          "autoCreateSqlTable": true
        }
      }
    }
  ]
}
```

### PostgreSQL

The PostgreSQL sink needs its configuration companion package to be loaded explicitly via
`Using`:

```json
"ConnectionStrings": {
  "LogsDb": "Host=pg-host;Port=5432;Database=webgis_logs;Username=webgis;Password=***"
},
"Serilog": {
  "Using": [ "Serilog.Sinks.PostgreSQL.Configuration" ],
  "WriteTo": [
    {
      "Name": "PostgreSQL",
      "Args": {
        "connectionString": "LogsDb",
        "tableName": "logs",
        "needAutoCreateTable": true
      }
    }
  ]
}
```

Both sinks are batching sinks (periodic bulk insert), so they have negligible impact on
request latency.

### Oracle

There is currently no well-maintained Serilog sink for Oracle, so a direct DB sink is not
offered for Oracle. If your log backend is Oracle-only, use the OTLP path instead (e.g.
an OpenTelemetry Collector with an Oracle/JDBC exporter, or export to an OTLP-native
backend and query it there).

## Notes

- All of the above concerns *where application log events go*; it is independent of the
  `Api:logging-type` GeoService performance-logging switch described at the top.
- Multiple sinks can be active at once (e.g. console + OTLP + SQL Server) — enable only
  what you need.
