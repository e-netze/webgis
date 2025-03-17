using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using gView.GraphicsEngine;
using System;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IMap : IDisplay, IClone<IMap, object>
{
    //event OnExtentChangedEventHandler OnExtentChanged;

    string Name { get; set; }
    ServiceCollection Services { get; }

    // MapScale without WebMercator Corrections
    // The Paramized Scale...
    double MapScale { get; set; }

    // Experimental Feature
    // Use this eg for VisInScale to solve problems in WebMercator!?
    double ServiceMapScale { get; }

    double RefScale { get; set; }
    double Resolution { get; }
    //double MinScale { get; }

    Envelope InitialExtent { get; set; }

    bool IsDirty { get; set; }

    Task<ServiceResponse> GetMapAsync(IRequestContext requestContext, bool makeTransparent = false, ArgbColor? transparentColor = null, string format = null, bool throwException = false);
    Task<ServiceResponse> GetLegendAsync(IRequestContext requestContext);

    void SetScale(double mapScale, int iWidth, int iHeight);
    void SetScale(double mapScale, int iWidth, int iHeight, double cx, double cy);
    void ZoomTo(Envelope extent);

    SelectionCollection Selection { get; }

    ILayer LayerById(string id);
    ILayer LayerByName(string name);
    IMapService ServiceByLayer(ILayer layer);
    IMapService FirstServiceByType(Type type);

    IUserData Environment { get; }

    IGraphicsContainer GraphicsContainer { get; }

    SpatialReference SpatialReference
    {
        get;
        set;
    }

    string RequestId { get; set; }

    MapRestrictions MapRestrictions { get; }
    ServiceRestirctions GetServivceRestrictions(IMapService service);

    ServiceDiagnosticsWarningLevel DiagnosticsWaringLevel { get; set; }
}
