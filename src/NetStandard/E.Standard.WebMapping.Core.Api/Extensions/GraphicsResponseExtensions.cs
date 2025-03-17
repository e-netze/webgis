using E.Standard.WebMapping.Core.Api.EventResponse;

namespace E.Standard.WebMapping.Core.Api.Extensions;

static public class GraphicsResponseExtensions
{
    static public GraphicsResponse SetActiveGraphicsTool(this GraphicsResponse response, GraphicsTool tool)
    {
        if (response != null)
        {
            response.ActiveGraphicsTool = tool;
        }

        return response;
    }

    static public GraphicsResponse DoReplaceElements(this GraphicsResponse response, bool replaceElements)
    {
        if (response != null)
        {
            response.ReplaceElements = replaceElements;
        }

        return response;
    }

    static public GraphicsResponse DoSuppressZoom(this GraphicsResponse response, bool suppressZoom)
    {
        if (response != null)
        {
            response.SuppressZoom = suppressZoom == true ? true : null;
        }

        return response;
    }
}
