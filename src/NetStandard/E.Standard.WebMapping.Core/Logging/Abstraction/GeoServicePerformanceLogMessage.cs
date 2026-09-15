using System.Runtime.CompilerServices;
using System.Text;

namespace E.Standard.WebMapping.Core.Logging.Abstraction;

/// <summary>
/// Interpolated string handler used by <see cref="IGeoServicePerformanceLogger"/> to build the
/// performance-log message lazily. When the target logger is not enabled (e.g. a
/// null/no-op logger), <see cref="AppendLiteral"/>/<see cref="AppendFormatted{T}"/> are never
/// invoked by the compiler-generated code, so no string concatenation/allocation happens at all.
/// </summary>
[InterpolatedStringHandler]
public struct GeoServicePerformanceLogMessage
{
    private readonly StringBuilder _builder;

    public GeoServicePerformanceLogMessage(int literalLength, int formattedCount, IGeoServicePerformanceLogger logger, out bool shouldAppend)
    {
        shouldAppend = logger is not null && logger.IsEnabled;
        _builder = shouldAppend ? new StringBuilder(literalLength) : null;
    }

    public void AppendLiteral(string value)
        => _builder?.Append(value);

    public void AppendFormatted<T>(T value)
        => _builder?.Append(value);

    public void AppendFormatted(string value)
        => _builder?.Append(value);

    public override string ToString()
        => _builder?.ToString() ?? string.Empty;
}
