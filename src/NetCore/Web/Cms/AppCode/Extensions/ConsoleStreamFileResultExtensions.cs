using E.Standard.CMS.Core.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cms.AppCode.Extensions;

static public class ConsoleStreamFileResultExtensions
{
    static public IActionResult Result(this ConsoleStreamFileResult file, Controller controller)
    {
        var cd = new System.Net.Mime.ContentDisposition
        {
            FileName = file.FileName,
            Inline = false
        };

        controller.Response.Headers.Append("Content-Disposition", cd.ToString());
        return controller.File(file.Data, file.ContentType);
    }
}
