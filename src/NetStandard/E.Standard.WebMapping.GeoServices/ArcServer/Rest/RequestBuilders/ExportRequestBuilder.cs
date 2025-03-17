#nullable enable

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.DynamicLayers;
using System.Collections.Generic;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.RequestBuilders;

public class ExportRequestBuilder : BaseRequestBuilder<ExportRequestBuilder>
{
    public ExportRequestBuilder()
    {
        _self = this;
    }

    new public ExportRequestBuilder WithFormat(string format) => base.WithFormat(format);

    new public ExportRequestBuilder WithBBox(Envelope bbox) => base.WithBBox(bbox);

    new public ExportRequestBuilder WithTransparency() => base.WithTransparency();

    new public ExportRequestBuilder WithLayers(IEnumerable<int>? layerIds, string prefix = "show")
        => base.WithLayers(layerIds, prefix);

    new public ExportRequestBuilder WithLayers(IEnumerable<string>? layerIds, string prefix = "show")
        => base.WithLayers(layerIds, prefix);

    new public ExportRequestBuilder WithLayerDefintions(IDictionary<string, string>? layerDefs)
        => base.WithLayerDefintions(layerDefs);

    new public ExportRequestBuilder WithMapRotation(IMap map) => base.WithMapRotation(map);

    new public ExportRequestBuilder WithImageFormat(string imageFormat) => base.WithImageFormat(imageFormat);

    new public ExportRequestBuilder WithImageSizeAndDpi(int imageWidth, int imageHeight, int dpi)
        => base.WithImageSizeAndDpi(imageWidth, imageHeight, dpi);

    new public ExportRequestBuilder WithImageSRef(int sRefId) => base.WithImageSRef(sRefId);

    new public ExportRequestBuilder WithBBoxSRef(int sRefId) => base.WithBBoxSRef(sRefId);

    public ExportRequestBuilder WithImageAndBBoxSRef(int sRefId)
        => WithImageSRef(sRefId).WithBBoxSRef(sRefId);

    new public ExportRequestBuilder WithDatumTransformations(int[] datumTransformations)
        => base.WithDatumTransformations(datumTransformations);

    new public ExportRequestBuilder WithDynamicLayers(List<DynamicLayer>? dynamicLayers)
        => base.WithDynamicLayers(dynamicLayers);
}
