using E.Standard.CMS.Core;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.Tiling;

public class GeneralTileService : TileService
{
    private TileGridOrientation _origin = TileGridOrientation.UpperLeft;
    private Envelope _extent = new Envelope();
    private double[] _resolutions;
    private string _tileUrl = String.Empty;
    private int _tileWidth = 256, _tileHeight = 256;
    private readonly string _layerName = "_tilecache";
    private string[] _domains = null;

    private GeneralTileService(bool hideBeyondMaxLevel) : base(hideBeyondMaxLevel) { }

    public GeneralTileService(CmsNode propNode, CmsNode layerNode)
        : base((bool?)propNode?.Load("hide_beyond_maxlevel", false) == true)
    {
        if (propNode != null)
        {
            _origin = (TileGridOrientation)propNode.Load("origin", (int)TileGridOrientation.UpperLeft);
            _tileUrl = (string)propNode.Load("tileurl", String.Empty);
            _tileWidth = (int)propNode.Load("tilewidth", 256);
            _tileHeight = (int)propNode.Load("tileheight", 256);
            base.TilePath = propNode.LoadString("tilepath");

            _extent.MinX = (double)propNode.Load("minx", 0.0);
            _extent.MinY = (double)propNode.Load("miny", 0.0);
            _extent.MaxX = (double)propNode.Load("maxx", 0.0);
            _extent.MaxY = (double)propNode.Load("maxy", 0.0);

            int projId = (int)propNode.Load("projid", -1);
            if (projId > 0)
            {
                base.SupportedCrs = new int[] { projId };
            }

            double? res = null;
            List<double> resList = new List<double>();
            int i = 0;
            while ((res = (double?)propNode.Load("res" + i, null)) != null)
            {
                resList.Add((double)res);
                i++;
            }
            _resolutions = resList.ToArray();

            base.Server = _tileUrl;

            if (layerNode != null)
            {
                _layerName = layerNode.Name;
            }

            string domains = (string)propNode.Load("domains", String.Empty);
            if (!String.IsNullOrEmpty(domains))
            {
                _domains = domains.Split('|');
            }
        }
    }

    #region IService Member

    public override bool PreInit(string serviceID, string server, string url, string authUser, string authPwd, string token, string appConfigPath, ServiceTheme[] serviceThemes)
    {

        base.PreInit(serviceID, server, url, authUser, authPwd, token, appConfigPath, serviceThemes);

        return true;
    }

    public override Task<bool> InitAsync(IMap map, IRequestContext requestContext)
    {
        try
        {
            this.Map = map;

            TileGrid = new TileGrid(
                new Point(_extent.MinX, (_origin == TileGridOrientation.UpperLeft ? _extent.MaxY : _extent.MinY)),
                _tileWidth, _tileHeight, 96.0,
                _origin)
            {
                GridRendering = this.GridRendering
            };

            int level = 0;
            foreach (double res in _resolutions)
            {
                TileGrid.AddLevel(level++, res);
            }

            OGC.WMS.OgcWmsLayer layer = new OGC.WMS.OgcWmsLayer(_layerName, "0", this, queryable: false);
            this.Layers.SetItems(new ILayer[] { layer });

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    #endregion

    #region IClone Member

    override public IMapService Clone(IMap parent)
    {
        GeneralTileService clone = new GeneralTileService(base.HideBeyondMaxLevel);

        base.Clone(clone, parent);
        clone._origin = _origin;
        clone._extent = new Envelope(_extent);
        clone._resolutions = _resolutions;
        clone._tileUrl = _tileUrl;
        clone._tileWidth = _tileWidth;
        clone._tileHeight = _tileHeight;
        clone._domains = _domains != null ? new List<string>(_domains).ToArray() : null;

        return clone;
    }

    #endregion

    public override string ImageUrl(IRequestContext requestContext, IMap map)
    {
        if (_domains != null && _domains.Length > 0)
        {
            return String.Format(_tileUrl, _domains[0]);
        }
        return _tileUrl;
    }

    public override string[] ImageUrls(IRequestContext requestContext, IMap map)
    {
        if (_domains != null && _domains.Length > 0)
        {
            var urls = new string[_domains.Length];

            for (int i = 0; i < urls.Length; i++)
            {
                urls[i] = String.Format(_tileUrl, _domains[i]);
            }

            return urls;
        }

        return new string[] { _tileUrl };
    }

    public override (string tileUrl, string[] domains) ImageUrlPro(IRequestContext requestContext, IMap map)
    {
        if (_domains != null && _domains.Length > 0)
        {
            return (_tileUrl.Replace("{0}", "{s}"), _domains);
        }
        return (_tileUrl, null);
    }
}
