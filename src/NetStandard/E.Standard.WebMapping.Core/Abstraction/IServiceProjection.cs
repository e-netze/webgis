namespace E.Standard.WebMapping.Core.Abstraction;

public interface IServiceProjection
{
    ServiceProjectionMethode ProjectionMethode { get; set; }
    int ProjectionId { get; set; }

    void RefreshSpatialReference();
}
