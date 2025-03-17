using E.Standard.Platform;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Tiling;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest;

public class TileGridService : TileService
{
    private readonly string _configUrl, _tileUrl;
    private string _layerName = "_alllayers";

    public TileGridService(string configUrl, string tileUrl, CMS.Core.CmsNode layerNode, bool hideBeyondMaxLevel)
        : base(hideBeyondMaxLevel)
    {
        _configUrl = configUrl;
        _tileUrl = tileUrl;

        base.Server = _tileUrl;

        if (layerNode != null)
        {
            _layerName = !String.IsNullOrEmpty(layerNode.Name) ? layerNode.Name : _layerName;
        }
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

            string config = String.Empty;
            if (_configUrl.ToLower().StartsWith("http://") || _configUrl.ToLower().StartsWith("https://"))
            {
                //config = await WebHelper.DownloadStringAsync(_configUrl, _proxy);
                config = await requestContext.Http.GetStringAsync(_configUrl);
            }
            else
            {
                StreamReader sr = new StreamReader(_configUrl);
                config = sr.ReadToEnd();
                sr.Close();
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(config);

            XmlNode TileCacheInfo = doc.SelectSingleNode("CacheInfo/TileCacheInfo");
            if (TileCacheInfo == null)
            {
                return false;
            }

            XmlNode X = TileCacheInfo.SelectSingleNode("TileOrigin/X");
            XmlNode Y = TileCacheInfo.SelectSingleNode("TileOrigin/Y");
            if (X == null || Y == null)
            {
                return false;
            }

            XmlNode TileCols = TileCacheInfo.SelectSingleNode("TileCols");
            XmlNode TileRows = TileCacheInfo.SelectSingleNode("TileRows");
            if (TileCols == null || TileRows == null)
            {
                return false;
            }

            XmlNode DPI = TileCacheInfo.SelectSingleNode("DPI");
            if (DPI == null)
            {
                return false;
            }

            TileGrid = new TileGrid(
                new Point(X.InnerText.ToPlatformDouble(), Y.InnerText.ToPlatformDouble()),
                int.Parse(TileCols.InnerText), int.Parse(TileRows.InnerText),
                DPI.InnerText.ToPlatformDouble(),
                TileGridOrientation.UpperLeft)
            {
                GridRendering = this.GridRendering
            };

            foreach (XmlNode LODInfo in TileCacheInfo.SelectNodes("LODInfos/LODInfo"))
            {
                XmlNode LevelID = LODInfo.SelectSingleNode("LevelID");
                XmlNode Resolution = LODInfo.SelectSingleNode("Resolution");
                if (LevelID == null || Resolution == null)
                {
                    continue;
                }

                TileGrid.AddLevel(
                    int.Parse(LevelID.InnerText),
                    Resolution.InnerText.ToPlatformDouble());
            }

            var layer = new TileLayer(_layerName, "0", this);
            this.Layers.SetItems(new ILayer[] { layer });

            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region IClone Member

    override public IMapService Clone(IMap parent)
    {
        TileGridService clone = new TileGridService(_configUrl, _tileUrl, null, base.HideBeyondMaxLevel);
        clone._layerName = _layerName;

        base.Clone(clone, parent);

        return clone;
    }

    #endregion

    public override string ImageUrl(IRequestContext requestContext, IMap map)
    {
        return _tileUrl;
    }

    public override string[] ImageUrls(IRequestContext requestContext, IMap map)
    {
        return new string[] { ImageUrl(requestContext, map) };
    }

    public override (string tileUrl, string[] domains) ImageUrlPro(IRequestContext requestContext, IMap map)
    {
        return (ImageUrl(requestContext, map), null);
    }
}
