namespace E.Standard.WebMapping.GeoServices.Extensions;

/// <summary>
/// Implemented by request builders (see <c>BaseRequestBuilder&lt;T&gt;</c>) whose <see cref="Build"/>
/// result is expensive/awkward to compute twice - lets <see cref="RequestContextExtensions.LogRequest{T}"/>
/// build the request body exactly once and reuse it for both the actual call and the audit log,
/// without the caller having to call <see cref="Build"/> itself at the call site.
/// </summary>
internal interface IRequestBuilder
{
    string Build();
}
