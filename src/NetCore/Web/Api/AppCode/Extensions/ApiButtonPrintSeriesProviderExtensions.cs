#nullable enable

using Api.Core.AppCode.Services;
using E.Standard.Api.App.DTOs;
using E.Standard.Web.Abstractions;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Print;

namespace Api.Core.AppCode.Extensions;

static internal class ApiButtonPrintSeriesProviderExtensions
{
    static public IGraphicsContainer GetPrintSericiesGraphicsElements(
        this IApiButtonPrintSeriesProvider? provider,
        IMap map,
        IHttpService httpService,
        UrlHelperService _urlHelper,
        PrintLayoutDTO printLayout,
        PageSize pageSize,
        PageOrientation pageOrientation,
        double scale,
        Shape toolSketch)
    {
        if(provider is null) return new GraphicsContainer();

        var layoutBuilder = new LayoutBuilder(
                        map.Clone(null),
                        httpService,
                        System.IO.Path.Combine(_urlHelper.AppEtcPath(), "layouts", printLayout.LayoutFile),
                        pageSize,
                        pageOrientation,
                        96.0,
                        System.IO.Path.Combine(_urlHelper.AppEtcPath(), "layouts", "data"));
        layoutBuilder.Scale = scale;
        var envelope = layoutBuilder.MapEnvelope;

        return provider.GetPrintSeriesGraphicElements(
            toolSketch,
            envelope.Width,
            envelope.Height);
    }
}
