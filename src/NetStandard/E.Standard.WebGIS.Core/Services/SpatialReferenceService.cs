using E.Standard.Security.App.Services.Abstraction;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Geometry;
using Microsoft.Extensions.Options;
using System;

namespace E.Standard.WebGIS.Core.Services;

public class SpatialReferenceService
{
    private readonly SpatialReferenceStore _store;

    public SpatialReferenceService(ISecurityConfigurationService config, IOptionsMonitor<SpatialReferenceServiceOptions> optionsMonitor)
    {
        var options = optionsMonitor.CurrentValue;

        string configPath = String.Empty, etcPath = String.Empty;

        if (!String.IsNullOrWhiteSpace(options.ServerSideConfigurationPathConfigKey) && !String.IsNullOrEmpty(config[options.ServerSideConfigurationPathConfigKey]))
        {
            configPath = $"{config[options.ServerSideConfigurationPathConfigKey]}/config";
            etcPath = $"{config[options.ServerSideConfigurationPathConfigKey]}/etc";
        }

        _store = new SpatialReferenceStore(etcPath);
        if (!String.IsNullOrEmpty(etcPath))
        {
            _store.SpatialReferences.RootPath = System.IO.Path.Combine(etcPath, "coordinates", "system", "proj");
        }
        SpatialReferenceCollection.p4DatabaseConnection =
            !String.IsNullOrEmpty(config[options.Proj4DatabaseConnectionStringConfigKey]) ?
            config[options.Proj4DatabaseConnectionStringConfigKey] :
            "#";
    }

    public SpatialReference GetSpatialReference(int id)
    {
        return _store.SpatialReferences.ById(id);
    }

    public SpatialReferenceCollection Collection => _store.SpatialReferences;
}
