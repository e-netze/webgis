using E.Standard.WebMapping.Core.Extensions;
using System;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIUploadFile : UIElement
{
    public UIUploadFile(Type toolType, string servercommand)
        : base("upload-file")
    {
        this.servercommand = servercommand;
        this.toolid = toolType.ToToolId();
    }

    public string servercommand { get; set; }
    public string toolid { get; set; }
}
