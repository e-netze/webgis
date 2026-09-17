#nullable enable

using System;

namespace Api.Core.AppCode.Services.Logging.Db;

/// <summary>
/// One buffered <c>webgis_performance</c> row - see <see cref="BatchedDbLogBuffer{TRow}"/>.
/// </summary>
internal readonly record struct PerformanceLogRow(
    DateTime TimestampUtc,
    string Server,
    string Service,
    string Command,
    string? Map,
    bool Success,
    double DurationMs,
    string Message,
    string? SessionId,
    string? MapRequestId,
    string? ClientIp,
    string? User,
    double? CenterX,
    double? CenterY,
    double? Scale);
