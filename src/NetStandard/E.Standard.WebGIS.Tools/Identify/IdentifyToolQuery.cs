using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Identify;

internal class IdentifyToolQuery : IQueryBridge
{
    public IdentifyToolQuery(IBridge bridge, IApiTool tool, string toolId, string name, string toolParameters)
    {
        this.Bridge = bridge;
        this.Name = String.IsNullOrWhiteSpace(name) ? tool.Name : name;
        this.Url = "~~" + toolId + "~" + toolParameters;
        this.Image = tool.Image;
    }

    public int CountResults { get; set; }

    public IBridge Bridge { get; private set; }

    public string Url
    {
        get;
    }

    public string Image
    {
        get;
    }

    public string Name
    {
        get;
    }

    public string QueryGlobalId { get; }

    public bool IsSelectable => false;

    public bool Distinct => false;

    public bool ApplyZoomLimits => false;
    public int MaxFeatures => 0;

    public bool Union => false;

    public Task<WebMapping.Core.Feature> FirstFeatureAsync(IRequestContext requestContext, ApiQueryFilter filter, string appendFilterClause = "", double mapScale = 0D)
    {
        return Task.FromResult<WebMapping.Core.Feature>(null);
    }

    public Task<Shape> FirstFeatureGeometryAsync(IRequestContext requestContext, ApiQueryFilter filter, string appendFilterClause = "", double mapScale = 0D)
    {
        return Task.FromResult<Shape>(null);
    }

    public LayerType GetLayerType()
    {
        return LayerType.unknown;
    }

    public string GetServiceId() => null;
    public Guid? GetServiceGuid() => null;

    public string GetLayerId() => null;

    public Dictionary<string, string> GetSimpleTableFields()
    {
        return new Dictionary<string, string>();
    }

    public Task<int> HasFeaturesAsync(IRequestContext requestContext, ApiQueryFilter filter, string appendFilterClause = "", double mapScale = 0D)
    {
        return Task.FromResult<int>(0);
    }

    public Task<string> LegendItemImageUrlAsync(IRequestContext requestContext, ApiQueryFilter filter)
    {
        return Task.FromResult<string>(String.Empty);
    }

    public Task<string> LegendItemImageUrlAsync(Feature feature, out string legendValue)
    {
        legendValue = String.Empty;

        return Task.FromResult<string>(String.Empty);
    }

    public Task<WebMapping.Core.Collections.FeatureCollection> PerformAsync(IRequestContext requestContext, ApiQueryFilter filter, string appendFilterClause = "", int limit = 0, double mapScale = 0D)
    {
        return Task.FromResult<WebMapping.Core.Collections.FeatureCollection>(new WebMapping.Core.Collections.FeatureCollection());
    }

    public void SetMapProperties(SpatialReference sRef, Envelope mapBox4326, int mapImageWidth, int mapImageHeight)
    {

    }
}
