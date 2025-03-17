using E.Standard.WebMapping.Core.Api.Bridge;

namespace E.Standard.Api.App.DTOs;

public sealed class PrintFormatDTO : IPrintFormatBridge
{
    public PageSize Size { get; set; }
    public PageOrientation Orientation { get; set; }
}