using E.Standard.OGC.Schema;
using E.Standard.OGC.Schema.wmts_1_0_0;
using E.Standard.Platform;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.Tiling;

public abstract class BaseWmtsService : TileService, IMapServiceDescription, IServiceSecuredDownload
{
    protected readonly string _url;
    protected readonly string _layer;
    protected readonly string _tileMatrixSet;
    protected readonly string _style;
    protected readonly string _imageFormat;
    protected readonly int _maxLevel;
    protected readonly string[] _tileUrls;

    public BaseWmtsService(
                string url,
                string layer,
                string tileMatrixSet,
                string style,
                string imageFormat,
                string[] resourceUrls,
                int maxLevel,
                bool hideBeyondMaxLevel)
        : base(hideBeyondMaxLevel)
    {
        _url = url;

        _layer = layer;
        _tileMatrixSet = tileMatrixSet;
        _style = style;
        _imageFormat = imageFormat;

        _tileUrls = resourceUrls;
        if (_tileUrls != null && _tileUrls.Length == 1 && String.IsNullOrWhiteSpace(_tileUrls[0]))
        {
            _tileUrls = null;
        }

        if ((_tileUrls == null || _tileUrls.Length == 0) && _url.ToLower().EndsWith("/mapserver/wmts/1.0.0/wmtscapabilities.xml")) // ESRI AGS
        {
            _tileUrls = new string[] { _url.Substring(0, _url.Length - "/1.0.0/wmtscapabilities.xml".Length) + "?" };
        }
        if ((_tileUrls == null || _tileUrls.Length == 0) && (_url.EndsWith("?") || _url.EndsWith("&")))
        {
            _tileUrls = new string[] {
                _url+"SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=" + _layer + "&STYLE=" + _style + "&FORMAT=" + _imageFormat + "&TILEMATRIXSET=" + _tileMatrixSet + "&TILEMATRIX=[LEVEL]&TILEROW=[ROW]&TILECOL=[COL]"
            };
        }
        if (_tileUrls == null || _tileUrls.Length == 0)
        {
            _tileUrls = new string[1];  // ??
        }

        for (int i = 0; i < _tileUrls.Length; i++)
        {
            _tileUrls[i] = _tileUrls[i]
                .Replace("{Style}", _style)
                .Replace("{TileMatrixSet}", _tileMatrixSet)
                .Replace("{TileMatrix}", "[LEVEL]")
                .Replace("{TileRow}", "[ROW]")
                .Replace("{TileCol}", "[COL]");
        }

        _maxLevel = maxLevel;

        base.Server = _url;
    }

    #region IService Member

    async public override Task<bool> InitAsync(IMap map, IRequestContext requestContext)
    {
        var httpService = requestContext.Http;

        _initErrorResponse = null;

        try
        {
            this.Map = map;

            string url = _url;

            if (!url.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                url += (url.EndsWith("?") || url.EndsWith("&") ? "" : "?") + "REQUEST=GetCapabilities&VERSION=1.0.0&SERVICE=WMTS";
            }

            string xml = await DownloadAsync(requestContext, url);

            xml = xml.Replace("<Layer>", @"<ows:DatasetDescriptionSummary xsi:type=""LayerType"">");
            xml = xml.Replace("</Layer>", @"</ows:DatasetDescriptionSummary>");

            Serializer<Capabilities> ser = new Serializer<Capabilities>();
            var response = ser.FromString(xml, Encoding.UTF8);

            this.ServiceDescription = this.CopyrightText = String.Empty;

            #region Description

            if (response?.ServiceIdentification?.Abstract != null && response.ServiceIdentification.Abstract.Length > 0)
            {
                this.ServiceDescription += $"{response.ServiceIdentification.Abstract[0].Value?.Replace("\n", "").Replace("\r", "")}\n";
            }
            if (response?.ServiceProvider != null)
            {
                if (!String.IsNullOrWhiteSpace(response.ServiceProvider.ProviderName))
                {
                    this.ServiceDescription += $"Provider: {response.ServiceProvider.ProviderName}\n";
                }
                if (response.ServiceProvider.ProviderSite?.href != null)
                {
                    this.ServiceDescription += $"Site: {response.ServiceProvider.ProviderSite.href}\n";
                }
            }
            if (response?.ServiceProvider?.ServiceContact != null)
            {
                // ToDo: eg: https://www.basemap.at/wmts/1.0.0/WMTSCapabilities.xml
            }

            #endregion

            SpatialReference sRef = null;
            if (response != null && response.Contents != null)
            {
                if (response.Contents.TileMatrixSet.Length > 0)
                {
                    var matrixSet = response.Contents.TileMatrixSet.Where(m => m.Identifier != null && m.Identifier.Value == _tileMatrixSet).FirstOrDefault();
                    if (matrixSet != null && matrixSet.TileMatrix != null && matrixSet.TileMatrix.Length > 0)
                    {
                        int srs = int.Parse(matrixSet.SupportedCRS.Substring(matrixSet.SupportedCRS.LastIndexOf(":") + 1));
                        sRef = CoreApiGlobals.SRefStore.SpatialReferences.ById(srs);

                        this.TileGrid = new TileGrid(FromPointString(matrixSet.TileMatrix[0].TopLeftCorner, sRef, false),
                                        int.Parse(matrixSet.TileMatrix[0].TileWidth), int.Parse(matrixSet.TileMatrix[0].TileHeight),
                                        25.4D / 0.28D,  // WMTS -> 1Pixel is 0.28mm;
                                        TileGridOrientation.UpperLeft)
                        {
                            GridRendering = this.GridRendering
                        };
                    }

                    foreach (var tileMatrix in matrixSet.TileMatrix)
                    {
                        var level = int.Parse(tileMatrix.Identifier.Value);
                        if (_maxLevel >= 0 && level > _maxLevel)
                        {
                            continue;
                        }

                        this.TileGrid.AddLevel(level, tileMatrix.ScaleDenominator / (this.TileGrid.Dpi / 0.0254));
                    }
                }

                if (response.Contents.DatasetDescriptionSummary != null)
                {
                    var layerType = (E.Standard.OGC.Schema.wmts_1_0_0.LayerType)response.Contents.DatasetDescriptionSummary.Where(s =>
                        s is E.Standard.OGC.Schema.wmts_1_0_0.LayerType && s.Identifier?.Value == _layer).FirstOrDefault();

                    if (layerType?.Abstract != null && layerType.Abstract.Length > 0)
                    {
                        var layerDescription = layerType.Title != null && layerType.Title.Length > 0 ? layerType.Title[0].Value : layerType.Identifier.Value;
                        this.ServiceDescription += $"\n{layerDescription}:\n{layerType.Abstract[0].Value}";
                    }
                }
            }

            OGC.WMS.OgcWmsLayer layer = new OGC.WMS.OgcWmsLayer("_alllayers", "0", this, queryable: false);
            this.Layers.SetItems(new ILayer[] { layer });

            return true;
        }
        catch (Exception ex)
        {
            base._initErrorResponse = new ExceptionResponse(this.Map.Services.IndexOf(this), this.ID, ex, Const.InitServiceExceptionPreMessage);
            return false;
        }
    }

    #endregion

    #region IClone Member

    //override public IMapService Clone(IMap parent)
    //{
    //    WmtsService clone = new WmtsService(_url, _layer, _tileMatrixSet, _style, _imageFormat, _tileUrls, _maxLevel, base.HideBeyondMaxLevel);

    //    base.Clone(clone, parent);

    //    clone._ticketServer = _ticketServer;
    //    clone._ticketServiceProxy = _ticketServiceProxy;

    //    clone._authUser = _authUser;
    //    clone._authPassword = _authPassword;
    //    clone._token = _token;
    //    clone._ticket = _ticket;

    //    clone.ServiceDescription = this.ServiceDescription;
    //    clone.CopyrightText = this.CopyrightText;

    //    return clone;
    //}

    protected override void Clone(TileService clone, IMap parent)
    {
        base.Clone(clone, parent);

        if (clone is BaseWmtsService baseWmtsService)
        {
            baseWmtsService.ServiceDescription = this.ServiceDescription;
            baseWmtsService.CopyrightText = this.CopyrightText;
        }
    }

    #endregion

    #region IServiceDescription

    public string ServiceDescription { get; set; }
    public string CopyrightText { get; set; }

    #endregion

    #region TileService Overrides

    public override string ImageUrl(IRequestContext requestContext, IMap map)
    {
        return ImageUrl(requestContext, map, 0);
    }

    public override string[] ImageUrls(IRequestContext requestContext, IMap map)
    {
        var urls = new string[_tileUrls.Length];
        for (int i = 0; i < _tileUrls.Length; i++)
        {
            urls[i] = ImageUrl(requestContext, map, i);
        }
        return urls;
    }

    public override (string tileUrl, string[] domains) ImageUrlPro(IRequestContext requestContext, IMap map)
    {
        var result = base.CreateImageUrlTemplate(_tileUrls);

        return result;
    }

    protected virtual string ImageUrl(IRequestContext requestContext, IMap map, int index)
    {
        if (index > _tileUrls.Length)
        {
            index = 0;
        }

        return _tileUrls[index];
    }

    #endregion

    protected abstract Task<string> DownloadAsync(IRequestContext requestContext, string url);

    #region Helper

    private Point FromPointString(string pointString, SpatialReference sRef, bool ignoreAxes = false)
    {
        pointString = pointString.Trim();
        while (pointString.Contains("  "))
        {
            pointString.Replace("  ", " ");
        }

        double[] coords = pointString.Trim().Split(' ').Select(m => m.ToPlatformDouble()).ToArray();
        if (coords.Length != 2)
        {
            throw new ArgumentException("Wrong Koordinate Format: " + pointString);
        }

        if (ignoreAxes == false &&
            sRef != null &&
            (sRef.AxisX == AxisDirection.North || sRef.AxisX == AxisDirection.South) &&
            (sRef.AxisY == AxisDirection.West || sRef.AxisY == AxisDirection.East))
        {
            return new Point(coords[1], coords[0]);
        }
        else
        {
            return new Point(coords[0], coords[1]);
        }
    }

    #endregion

    #region IServiceTokenProvider

    abstract public Task<byte[]> GetSecuredData(IRequestContext context, string url);

    #endregion
}
