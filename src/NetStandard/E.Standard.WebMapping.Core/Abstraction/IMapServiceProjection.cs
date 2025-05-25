namespace E.Standard.WebMapping.Core.Abstraction;

public interface IMapServiceProjection
{
    ServiceProjectionMethode ProjectionMethode { get; set; }
    int ProjectionId { get; set; }

    void RefreshSpatialReference();
}
