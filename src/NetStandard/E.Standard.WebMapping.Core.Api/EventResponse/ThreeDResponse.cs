namespace E.Standard.WebMapping.Core.Api.EventResponse;

public class ThreeDResponse : ApiEventResponse
{
    public double[] BoundingBox { get; set; }
    public int BoundBoxEpsg { get; set; }
    public int[] ArraySize { get; set; }
    public float[] Values { get; set; }

    public ThreeDTexture Texture { get; set; }

    public string TextureOrthoService { get; set; }
    public string TextureStreetsOverlayService { get; set; }
}

public enum ThreeDTexture
{
    Monochrome = 0,
    Map = 1,
    Ortho = 2,
    OrthoStreets = 3
}
