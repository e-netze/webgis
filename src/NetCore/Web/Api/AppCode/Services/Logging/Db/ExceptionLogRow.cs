#nullable enable

using System;

namespace Api.Core.AppCode.Services.Logging.Db;

/// <summary>
/// One buffered <c>webgis_exceptions</c> row - see <see cref="BatchedDbLogBuffer{TRow}"/>.
/// </summary>
internal readonly record struct ExceptionLogRow(
    DateTime TimestampUtc,
    string Server,
    string Service,
    string Command,
    string? Map,
    string? User,
    string? ExceptionType,
    string Message,
    string? StackTrace,
    string? SessionId,
    string? MapRequestId,
    string? ClientIp,
    double? CenterX,
    double? CenterY,
    double? Scale);
