namespace E.Standard.WebMapping.Core.Api.Bridge;

public enum LayerGeometryType
{
    unknown = 0,
    point = 1,
    line = 2,
    polygon = 3,
    image = 4,
    annotation = 5,
    network = 6
}

public interface ILayerBridge : IApiObjectBridge
{
    string Id { get; }
    string Name { get; }

    LayerGeometryType GeometryType { get; }

    double MinScale { get; }
    double MaxScale { get; }

    string IdFieldname { get; }
}
