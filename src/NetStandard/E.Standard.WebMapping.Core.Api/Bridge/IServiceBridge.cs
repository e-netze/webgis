namespace E.Standard.WebMapping.Core.Api.Bridge;

public interface IServiceBridge : IApiObjectBridge
{
    string Name { get; }
    string Id { get; }

    ILayerBridge[] Layers { get; }

    ILayerBridge FindLayer(string id);
}
