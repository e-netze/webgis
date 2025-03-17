namespace E.Standard.WebMapping.Core.Api.Bridge;

public interface IUserAgent
{
    bool IsInternetExplorer { get; }
    bool IsChrome { get; }
    bool IsEdge { get; }
    bool IsEdgeChromium { get; }
    bool IsFirefox { get; }
    bool IsSafari { get; }
    bool IsOpera { get; }
}
