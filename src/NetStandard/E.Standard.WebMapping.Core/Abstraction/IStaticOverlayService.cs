namespace E.Standard.WebMapping.Core.Abstraction;

public interface IStaticOverlayService : IMapService
{
    double[] TopLeftLngLat { get; }
    double[] TopRightLngLat { get; }
    double[] BottomLeftLngLat { get; }

    string OverlayImageUrl { get; }

    float WidthHeightRatio { get; }
}
