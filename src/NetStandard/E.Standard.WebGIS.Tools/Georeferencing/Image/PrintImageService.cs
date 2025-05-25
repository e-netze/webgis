using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Extensions;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Models;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using gView.GraphicsEngine;
using System;
using System.IO;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image;

internal class PrintImageService : IStaticOverlayService, IPrintableMapService
{
    private readonly IBridge _bridge;
    private readonly GeorefImageMetadata _georefImageMetadata;
    private readonly string _ower;
    private readonly LayerCollection _layers;

    public PrintImageService(IBridge bridge,
                             string owner,
                             GeorefImageMetadata georefImageMetadata)
    {
        _bridge = bridge;
        _ower = owner;
        _georefImageMetadata = georefImageMetadata;

        this.Url = this.ID = _georefImageMetadata.Id;
        this.Name = _georefImageMetadata.Name;

        _layers = new LayerCollection(this);
        _layers.Add(new ImageLayer(this));
    }

    #region IService

    public string Name { get; set; }
    public string Url { get; set; }

    public string Server => String.Empty;

    public string Service => String.Empty;

    public string ServiceShortname => String.Empty;

    public string ID { get; set; }

    public float InitialOpacity { get; set; }
    public float OpacityFactor { get; set; }

    public bool CanBuffer => false;

    public bool UseToc { get; set; }

    public LayerCollection Layers => _layers;

    public Envelope InitialExtent => null;

    public ServiceResponseType ResponseType => ServiceResponseType.StaticOverlay;

    public ServiceDiagnostic Diagnostics => null;

    public ServiceDiagnosticsWarningLevel DiagnosticsWaringLevel { get; set; }
    public bool IsDirty { get; set; }
    public int Timeout { get; set; }

    public IMap Map { get; set; }

    public double MinScale { get; set; }
    public double MaxScale { get; set; }
    public bool ShowInToc { get; set; }
    public string CollectionId { get; set; }
    public bool CheckSpatialConstraints { get; set; }
    public bool IsBaseMap { get; set; }
    public BasemapType BasemapType { get; set; }
    public string BasemapPreviewImage { get; set; }

    public IMapService Clone(IMap parent)
    {
        return this;
    }

    public Task<ServiceResponse> GetMapAsync(IRequestContext requestContext)
    {
        return Task.FromResult<ServiceResponse>(null);
    }



    public Task<ServiceResponse> GetSelectionAsync(SelectionCollection collection, IRequestContext requestContext)
    {
        throw new NotImplementedException();
    }

    public Task<bool> InitAsync(IMap map, IRequestContext requestContext)
    {
        this.Map = map;
        return Task.FromResult(true);
    }

    public bool PreInit(string serviceID, string server, string url, string authUser, string authPwd, string token, string appConfigPath, ServiceTheme[] serviceThemes)
    {
        return true;
    }

    #endregion

    #region IPrintableService

    async public Task<ServiceResponse> GetPrintMapAsync(IRequestContext requestContext)
    {
        if (_georefImageMetadata?.TopLeft == null ||
            _georefImageMetadata?.TopRight == null ||
            _georefImageMetadata?.BottomLeft == null)
        {
            return new EmptyImage(-1, this.ID); ;
        }

        string filetitle = $"georefimage_{System.Guid.NewGuid().ToString("N").ToLower()}.png";
        string filename = $"{(string)this.Map.Environment.UserValue(webgisConst.OutputPath, String.Empty)}/{filetitle}";
        string fileurl = $"{(string)this.Map.Environment.UserValue(webgisConst.OutputUrl, String.Empty)}/{filetitle}";

        using (var ms = new MemoryStream(_bridge.GetGeorefImageData(_ower, _georefImageMetadata)))
        using (var sourceBitmap = Current.Engine.CreateBitmap(ms))
        {
            using (var targetBitmap = Current.Engine.CreateBitmap(this.Map.ImageWidth, this.Map.ImageHeight))
            using (var targetGr = targetBitmap.CreateCanvas())
            {
                targetGr.InterpolationMode = InterpolationMode.Bicubic;

                using (var transformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, 4326, this.Map.SpatialReference?.Id ?? 0))
                {
                    _georefImageMetadata.ProjectWorld(transformer);
                }

                var p1 = new Point(_georefImageMetadata.TopLeft.X, _georefImageMetadata.TopLeft.Y);
                var p2 = new Point(_georefImageMetadata.TopRight.X, _georefImageMetadata.TopRight.Y);
                var p3 = new Point(_georefImageMetadata.BottomLeft.X, _georefImageMetadata.BottomLeft.Y);

                p1 = this.Map.WorldToImage(p1);
                p2 = this.Map.WorldToImage(p2);
                p3 = this.Map.WorldToImage(p3);

                //p1.X -= .5f; p1.Y -= .5f;
                //p2.X += .5f; p2.Y -= .5f;
                //p3.X -= .5f; p3.Y += .5f;

                CanvasPointF[] points = new CanvasPointF[]{
                                                            new CanvasPointF((float)p1.X,(float)p1.Y),
                                                            new CanvasPointF((float)p2.X,(float)p2.Y),
                                                            new CanvasPointF((float)p3.X,(float)p3.Y),
                                                        };

                targetGr.DrawBitmap(
                        sourceBitmap,
                        points,
                        new CanvasRectangleF(0f, 0f, sourceBitmap.Width, sourceBitmap.Height)
                    );

                await targetBitmap.SaveOrUpload(filename, ImageFormat.Png);
            }
        }


        return new ImageLocation(-1, this.ID, filename, fileurl);
    }

    #endregion

    #region IStaticOverlayService

    public double[] TopLeftLngLat =>
        _georefImageMetadata?.TopLeft != null ?
            new[] { _georefImageMetadata.TopLeft.Longitude, _georefImageMetadata.TopLeft.Latitude } :
            null;

    public double[] TopRightLngLat =>
        _georefImageMetadata?.TopRight != null ?
            new[] { _georefImageMetadata.TopRight.Longitude, _georefImageMetadata.TopRight.Latitude } :
            null;

    public double[] BottomLeftLngLat =>
        _georefImageMetadata?.BottomLeft != null ?
            new[] { _georefImageMetadata.BottomLeft.Longitude, _georefImageMetadata.BottomLeft.Latitude } :
            null;

    public string OverlayImageUrl => _bridge.GeorefImageUrl(_ower, _georefImageMetadata);

    public float WidthHeightRatio => _georefImageMetadata.CalcWidthHeightRatio();

    #endregion

    #region Classes

    private class ImageLayer : Layer
    {
        public ImageLayer(IMapService service)
            : base(service.Name, "0", service, false)
        {

        }

        override public ILayer Clone(IMapService parent)
        {
            return this;
        }

        public override string IdFieldName => String.Empty;
        public override Task<bool> GetFeaturesAsync(QueryFilter query, FeatureCollection result, IRequestContext requestContext)
        {
            return Task.FromResult(false);
        }
    }

    #endregion
}
