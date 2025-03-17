using E.Standard.OGC.Schema;
using E.Standard.OGC.Schema.WMS_C_1_4_0;
using E.Standard.Platform;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Tiling;
using System;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.OGC.WMSC;

public class WmscService : TileService
{
    private readonly string _url, _tiledLayer, _tiledCrs, _tileUrl;

    public WmscService(string url, string tiledLayer, string tiledCrs, string tileUrl)
        : base(false)
    {
        _url = url;
        _tiledLayer = tiledLayer;
        _tiledCrs = tiledCrs;
        _tileUrl = tileUrl;

        base.Server = _tileUrl;
    }

    #region IService Member

    public override bool PreInit(string serviceID, string server, string url, string authUser, string authPwd, string token, string appConfigPath, ServiceTheme[] serviceThemes)
    {

        base.PreInit(serviceID, server, url, authUser, authPwd, token, appConfigPath, serviceThemes);

        return true;
    }

    async public override Task<bool> InitAsync(IMap map, IRequestContext requestContext)
    {
        try
        {
            this.Map = map;
            var httpService = requestContext.Http;

            //string xml = await WebHelper.DownloadStringAsync(_url + "&REQUEST=DescripeTiles&VERSION=1.1.1&SERVICE=WMS", _proxy);
            string xml = await httpService.GetStringAsync(_url + "&REQUEST=DescripeTiles&VERSION=1.1.1&SERVICE=WMS");

            Serializer<WMS_DescribeTilesResponse> ser = new Serializer<WMS_DescribeTilesResponse>();
            WMS_DescribeTilesResponse response = ser.FromString(xml, Encoding.UTF8);

            //response.TiledLayer[0].TiledCrs[0].TileMatrixSet.TileMatrix.Length;

            if (response == null || response.TiledLayer.Length == 0 || response.TiledLayer[0].TiledCrs.Length == 0 ||
                response.TiledLayer[0].TiledCrs[0].TileMatrixSet == null || response.TiledLayer[0].TiledCrs[0].TileMatrixSet.TileMatrix.Length == 0)
            {
                return false;
            }

            foreach (TiledLayer tiledLayer in response.TiledLayer)
            {
                bool found = false;
                if (tiledLayer.name == _tiledLayer && tiledLayer.TiledCrs != null)
                {
                    foreach (TiledCrs tiledCrs in tiledLayer.TiledCrs)
                    {
                        if (tiledCrs.name == _tiledCrs)
                        {
                            TileMatrixSet matrixSet = tiledCrs.TileMatrixSet;

                            this.TileGrid = new TileGrid(FromPointType(matrixSet.TileMatrix[0].Point),
                                int.Parse(matrixSet.TileWidth), int.Parse(matrixSet.TileHeight),
                                96.0, TileGridOrientation.UpperLeft);

                            foreach (TileMatrix matrix in matrixSet.TileMatrix)
                            {
                                this.TileGrid.AddLevel(FromPointType(matrix.Point), (int)matrix.scale, matrix.scale / (this.TileGrid.Dpi / 0.0254));
                            }

                            found = true;
                        }
                    }
                }
                if (found == true)
                {
                    break;
                }
            }

            WMS.OgcWmsLayer layer = new WMS.OgcWmsLayer("_alllayers", "0", this, queryable: false);
            this.Layers.SetItems(new ILayer[] { layer });

            return true;
        }
        catch (Exception /*ex*/)
        {
            return false;
        }
    }

    #endregion

    #region IClone Member

    override public IMapService Clone(IMap parent)
    {
        WmscService clone = new WmscService(_url, _tiledLayer, _tiledCrs, _tileUrl);

        base.Clone(clone, parent);

        return clone;
    }

    #endregion

    #region TileService Overrides

    public override string ImageUrl(IRequestContext requestContext, IMap map)
    {
        string url = _tileUrl.Replace("[LAYER]", _tiledLayer);
        return url;
    }

    public override string[] ImageUrls(IRequestContext requestContext, IMap map) => [this.ImageUrl(requestContext, map)];

    public override (string tileUrl, string[] domains) ImageUrlPro(IRequestContext requestContext, IMap map) => (ImageUrl(requestContext, map), null);

    #endregion

    #region Helper
    private Point FromPointType(PointType pt)
    {
        if (pt == null || pt.Item == null)
        {
            return null;
        }

        if (pt.Item is CoordType)
        {
            string[] xy = ((CodeType)pt.Item).Value.Trim().Split(' ');
            if (xy.Length < 2)
            {
                return null;
            }

            return new Point(xy[0].ToPlatformDouble(),
                             xy[1].ToPlatformDouble());
        }
        else if (pt.Item is CoordinatesType)
        {
            string[] xy = ((CoordinatesType)pt.Item).Value.Trim().Split(' ');
            if (xy.Length < 2)
            {
                return null;
            }

            return new Point(xy[0].ToPlatformDouble(),
                             xy[1].ToPlatformDouble());
        }
        else if (pt.Item is DirectPositionType)
        {
            string[] xy = ((DirectPositionType)pt.Item).Text.Trim().Split(' ');
            if (xy.Length < 2)
            {
                return null;
            }

            return new Point(xy[0].ToPlatformDouble(),
                             xy[1].ToPlatformDouble());
        }

        return null;
    }

    #endregion
}
