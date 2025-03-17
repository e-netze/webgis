using E.Standard.Json;
using Microsoft.AspNetCore.Http;

namespace Portal.Core.AppCode.Extensions;

static internal class HttpFormExtensions
{
    static public string FormatGraphicsForHtmlTemplate(this IFormCollection form)
    {
        if (!string.IsNullOrEmpty(form["graphics"]))
        {
            var graphicsObject = JSerializer.Deserialize(form["graphics"], typeof(object));
            var formattedGraphics = JSerializer.Serialize(graphicsObject, pretty: true)
                .Replace("\r", "")
                .Replace("\n", "\n        ");

            return formattedGraphics;
        }

        return string.Empty;
    }
}
