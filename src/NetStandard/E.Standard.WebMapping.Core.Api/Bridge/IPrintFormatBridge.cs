namespace E.Standard.WebMapping.Core.Api.Bridge;

public enum PageSize
{
    A4 = 1,
    A3 = 2,
    A2 = 3,
    A1 = 4,
    A0 = 5,
    A4_A3 = 6,
    A4_A2 = 7,
    A4_A1 = 8,
    A4_A0 = 9,
    A3_A2 = 10,
    A3_A1 = 11,
    A2_A1 = 12
}
public enum PageOrientation
{
    Fixed = 0,
    Portrait = 1,
    Landscape = 2
}

public interface IPrintFormatBridge
{
    PageSize Size { get; set; }
    PageOrientation Orientation { get; set; }
}
