namespace E.Standard.WebMapping.Core.Logging.Abstraction;

/// <summary>
/// Controls how (or whether) an end-user's username is written into performance/exception log
/// entries - configurable globally via the <c>Api:logging-username-mode</c> setting, honored by
/// every <see cref="IGeoServicePerformanceLogger"/>/<see cref="IExceptionLogger"/> implementation
/// (CSV/Microsoft/database), so customers can choose the right trade-off between traceability
/// and privacy for their deployment. See <see cref="UsernameLogging"/> for the shared
/// implementation.
/// </summary>
public enum UsernameLoggingMode
{
    /// <summary>The username is not logged at all.</summary>
    None,

    /// <summary>The username is logged as-is (backward-compatible default).</summary>
    PlainText,

    /// <summary>
    /// Only a one-way SHA-256 hash of the (trimmed, lowercased) username is logged - lets an
    /// administrator recognize "the same user" across log rows without persisting the actual
    /// username.
    /// </summary>
    Hash,
}
