using E.Standard.CMS.Core;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using System;
using System.Collections.Specialized;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class ArcServerTilingServiceControl : NameUrlUserConrol, IInitParameter, ISubmit
{
    private ArcServerTileService _atService = null;

    GroupBox gbConnection = new GroupBox() { Label = "Verbindung" };
    ComboBox cmbType = new ComboBox("cmbType") { Label = "Verbndingstype" };
    Input txtMapName = new Input("txtMapName") { Label = "Map Name" };
    Input txtLayer = new Input("txtLayer") { Label = "Layer" };
    GroupBox gbTiles = new GroupBox() { Label = "Tiles" };
    Input txtConfUrl = new Input("txtConfUrl") { Label = "Conf.xml" };
    Input txtTileUrl = new Input("txtTileUrl") { Label = "Tile Url" };

    private readonly EsriServiceControl imsServiceControl = null;

    public ArcServerTilingServiceControl(CmsItemTransistantInjectionServicePack servicePack,
                                         bool copyMode)
    {
        imsServiceControl = new EsriServiceControl(servicePack);
        imsServiceControl.CopyMode = copyMode;
        imsServiceControl.OnChanged += new EventHandler(imsServiceControl1_OnChanged);
        cmbType.SelectedIndex = 0;

        this.AddControl(imsServiceControl);

        this.AddControl(gbConnection);
        gbConnection.AddControl(cmbType);
        gbConnection.AddControl(txtMapName);
        gbConnection.AddControl(txtLayer);
        cmbType.Options.Add(new ComboBox.Option("REST"));
        cmbType.Options.Add(new ComboBox.Option("MapServer (on demand tiles)"));

        this.AddControl(gbTiles);
        gbTiles.AddControl(txtConfUrl);
        gbTiles.AddControl(txtTileUrl);
    }

    public override NameUrlControl NameUrlControlInstance => imsServiceControl.NameUrlControlInstance;

    #region Events
    void imsServiceControl1_OnChanged(object sender, EventArgs e)
    {
        RefreshGUI();
    }

    private void txtMapName_TextChanged(object sender, EventArgs e)
    {
        RefreshGUI();
        if (_atService != null)
        {
            _atService.MapName = txtMapName.Value;
        }
    }

    private void txtLayer_TextChanged(object sender, EventArgs e)
    {
        RefreshGUI();
        if (_atService.LayerName != null)
        {
            _atService.LayerName = txtLayer.Value;
        }
    }

    private void cmbType_SelectedIndexChanged(object sender, EventArgs e)
    {
        RefreshGUI();
    }

    private void txtConfUrl_TextChanged(object sender, EventArgs e)
    {
        if (_atService != null)
        {
            _atService.ServiceConfigUrl = txtConfUrl.Value;
        }
    }

    private void txtTileUrl_TextChanged(object sender, EventArgs e)
    {
        if (_atService != null)
        {
            _atService.TileUrl = txtTileUrl.Value;
        }
    }
    #endregion

    #region IInitParameter Member

    public object InitParameter
    {
        set
        {
            imsServiceControl.InitParameter = value;
            _atService = value as ArcServerTileService;
        }
    }

    #endregion

    #region ISubmit Member

    public void Submit(NameValueCollection secrets)
    {
        imsServiceControl.Submit(secrets);
    }

    #endregion

    #region GUI
    private void RefreshGUI()
    {
        if (_atService == null || String.IsNullOrEmpty(_atService.Server) || string.IsNullOrEmpty(_atService.Service))
        {
            return;
        }

        if (String.IsNullOrEmpty(txtConfUrl.Value))
        {
            txtConfUrl.Value = "http://" + ServerHost(_atService.Server) + "/arcgiscache/" + _atService.Service + "/" + txtMapName.Value + "/conf.xml";
        }

        string s = (_atService.Server.StartsWith("http://") || _atService.Server.StartsWith("https://")) ? _atService.Server : "http://" + ServerHost(_atService.Server) + "/arcgis/services";
        switch (cmbType.SelectedIndex)
        {
            case 0:
                txtTileUrl.Value = s.Replace("/services", "/rest/services") + "/" + _atService.Service + "/MapServer/tile/[LEVEL]/[ROW]/[COL]";
                break;
            case 1:
                txtTileUrl.Value = s + "/" + _atService.Service + "/MapServer?mapname=" + txtMapName.Value + "&layer=" + txtLayer.Value + "&level=[LEVEL]&row=[ROW]&column=[COL]&format=JPG";
                break;
        }
    }

    #region Helper
    private string ServerHost(string server)
    {
        try
        {
            Uri uri = new Uri(server);
            server = uri.Host;
        }
        catch
        {
        }
        return server;
    }
    #endregion



    #endregion


}
