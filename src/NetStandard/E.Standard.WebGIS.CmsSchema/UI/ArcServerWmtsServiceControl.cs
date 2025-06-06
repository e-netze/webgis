﻿using E.Standard.CMS.Core;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.Extensions.Text;
using E.Standard.OGC.Schema;
using E.Standard.OGC.Schema.wmts_1_0_0;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class ArcServerWmtsServiceControl : NameUrlUserConrol, IInitParameter, ISubmit
{
    private ArcServerWmtsService _service = null;
    private Capabilities _response = null;

    GroupBox gbService = new GroupBox() { Label = "WMTS Dienst" };
    GroupBox gbProps = new GroupBox() { Label = "Dienst Eigenschaften" };
    NameUrlControl nameUrlControl = new NameUrlControl();
    Input txtServer = new Input("txtServer") { Label = "Server/Url", Placeholder = "Url eingeben..." };
    ComboBox cmbTiledLayer = new ComboBox("cmbTiledLayer") { Label = "Tiled-Layer" };
    ComboBox cmbTiledMatrixSet = new ComboBox("cmbTiledMatrixSet") { Label = "MatrixSet" };
    ComboBox cmbStyle = new ComboBox("cmbStyle") { Label = "Style" };
    ComboBox cmbImageFormat = new ComboBox("cmbImageFormat") { Label = "Image Format" };
    AuthentificationControl authentificationControl = new AuthentificationControl() { ShowToken = false };
    Button btnRefresh = new Button("btnRefresh") { Label = "Aktualisieren" };

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public ArcServerWmtsServiceControl(CmsItemTransistantInjectionServicePack servicePack,
                              bool copyMode)
    {
        _servicePack = servicePack;

        gbService.Enabled = gbProps.Enabled = !copyMode;

        this.AddControl(gbService);
        gbService.AddControl(txtServer);

        this.AddControl(gbProps);
        gbProps.AddControl(btnRefresh);
        gbProps.AddControl(cmbTiledLayer);
        gbProps.AddControl(cmbTiledMatrixSet);
        gbProps.AddControl(cmbImageFormat);
        gbProps.AddControl(cmbStyle);

        this.AddControl(authentificationControl);

        this.AddControl(nameUrlControl);

        btnRefresh.OnClick += btnRefresh_Click;

        cmbTiledLayer.OnClick += cmbTiledLayer_SelectedIndexChanged;
        //cmbTiledLayer.OnChange += cmbTiledLayer_SelectedIndexChanged;
    }

    public override NameUrlControl NameUrlControlInstance => nameUrlControl;

    #region IInitParameter Member

    public object InitParameter
    {
        set
        {
            _service = value as ArcServerWmtsService;

            if (_service != null)
            {
                nameUrlControl.InitParameter = _service;
                authentificationControl.Authentification = _service;
            }
        }
    }

    #endregion

    #region ISubmit Member

    public void Submit(NameValueCollection secrets)
    {
        if (_service != null)
        {
            GetCapabilites(secrets);

            if (_response == null)
            {
                throw new Exception("Can't query Capabilites");
            }

            _service.Server = txtServer.Value;

            var layer = (LayerType)_response.Contents.DatasetDescriptionSummary.Where(s =>
                s is LayerType && s.Identifier?.Value == cmbTiledLayer.SelectedItem?.ToString()).FirstOrDefault();

            if (layer == null)
            {
                throw new Exception($"Can't determine layer: {cmbTiledLayer.SelectedItem?.ToString()}");
            }


            if (cmbTiledLayer.SelectedItem != null)
            {
                _service.TileLayer = cmbTiledLayer.SelectedItem.ToString();
            }

            if (cmbTiledMatrixSet.SelectedItem != null)
            {
                _service.TileMatrixSet = cmbTiledMatrixSet.SelectedItem.ToString();
            }

            if (cmbStyle.SelectedItem != null)
            {
                _service.TileStyle = cmbStyle.SelectedItem.ToString();
            }

            if (cmbImageFormat.SelectedItem != null)
            {
                _service.ImageFormat = cmbImageFormat.SelectedItem.ToString();
            }

            List<string> resourceUrls = new List<string>();
            if (layer.ResourceURL != null)
            {
                foreach (var resourceUrl in layer.ResourceURL)
                {
                    if (resourceUrl.resourceType == URLTemplateTypeResourceType.tile && !String.IsNullOrWhiteSpace(resourceUrl.template))
                    {
                        // Use the MapServer/tile/.... url form AGS. Works also if Service needs a token
                        int mapServerIndex = _service.Server.IndexOf("/MapServer", StringComparison.OrdinalIgnoreCase);
                        if (mapServerIndex > 0
                            && !string.IsNullOrEmpty(authentificationControl.Authentification.Username))
                        {
                            string mapServerUrl = _service.Server.Substring(0, mapServerIndex + "/MapServer".Length); ;
                            resourceUrls.Add($"{mapServerUrl}/tile/{{TileMatrix}}/{{TileRow}}/{{TileCol}}");
                        }
                        else
                        {
                            resourceUrls.Add(resourceUrl.template);
                        }
                    }
                }
            }

            _service.ResourceURLs = resourceUrls.ToArray();
        }
    }

    #endregion

    #region Events

    private void btnRefresh_Click(object sender, EventArgs e)
    {
        try
        {
            _response = null;
            cmbTiledLayer.Options.Clear();
            cmbImageFormat.Options.Clear();
            cmbStyle.Options.Clear();
            cmbTiledMatrixSet.Options.Clear();

            GetCapabilites();
            if (_response == null)
            {
                throw new Exception("Can't query Capabilites");
            }

            if (_response.Contents != null && _response.Contents.DatasetDescriptionSummary != null)
            {
                foreach (var layerSummery in _response.Contents.DatasetDescriptionSummary)
                {
                    if (!(layerSummery is LayerType))
                    {
                        continue;
                    }

                    LayerType layer = (LayerType)layerSummery;
                    if (layer?.Identifier != null)
                    {
                        cmbTiledLayer.Options.Add(new ComboBox.Option(layer.Identifier.Value, layer.Title != null && layer.Title.Length > 0 ? layer.Title[0].Value : layer.Identifier.Value));
                    }
                }
            }
            if (cmbTiledLayer.Options.Count > 0)
            {
                cmbTiledLayer.SelectedIndex = 0;
            }
        }
        catch (Exception)
        {
            _response = null;
            throw;
        }
    }

    private void cmbTiledLayer_SelectedIndexChanged(object sender, EventArgs e)
    {
        GetCapabilites();
        if (_response == null)
        {
            throw new Exception("Can't query Capabilites");
        }

        cmbImageFormat.Options.Clear();
        cmbStyle.Options.Clear();
        cmbTiledMatrixSet.Options.Clear();

        var layer = (LayerType)_response.Contents.DatasetDescriptionSummary.Where(s =>
          s is LayerType && s.Identifier?.Value == cmbTiledLayer.SelectedItem?.ToString()).FirstOrDefault();

        if (layer == null)
        {
            return;
        }

        if (layer.Format != null)
        {
            foreach (string format in layer.Format)
            {
                cmbImageFormat.Options.Add(new ComboBox.Option(format));
            }
        }

        if (layer.TileMatrixSetLink != null)
        {
            foreach (var link in layer.TileMatrixSetLink)
            {
                string setId = link.TileMatrixSet;
                var matrixSet = _response.Contents.TileMatrixSet.Where(m => m.Identifier != null && m.Identifier.Value == setId).FirstOrDefault();
                if (matrixSet?.Identifier != null)
                {
                    cmbTiledMatrixSet.Options.Add(new ComboBox.Option(matrixSet.Identifier.Value, matrixSet.Title != null && matrixSet.Title.Length > 0 ? matrixSet.Title[0].Value : matrixSet.Identifier.Value));
                }
            }
        }

        if (layer.Style != null)
        {
            foreach (var style in layer.Style)
            {
                if (style?.Identifier != null)
                {
                    cmbStyle.Options.Add(new ComboBox.Option(style.Identifier.Value, style.Title != null && style.Title.Length > 0 ? style.Title[0].Value : style.Identifier.Value));
                }
            }
        }

        if (cmbImageFormat.Options.Count > 0)
        {
            cmbImageFormat.SelectedIndex = 0;
        }

        if (cmbTiledMatrixSet.Options.Count > 0)
        {
            cmbTiledMatrixSet.SelectedIndex = 0;
        }

        if (cmbStyle.Options.Count > 0)
        {
            cmbStyle.SelectedIndex = 0;
        }

        nameUrlControl.SetName(layer.Title != null && layer.Title.Length > 0 ? layer.Title[0].Value : layer.Identifier.Value, true);
    }

    #endregion

    #region Helper

    private void GetCapabilites(NameValueCollection secrets = null)
    {
        if (_response == null)
        {
            string url = txtServer.Value;
            if (url.EndsWith("?") || url.EndsWith("&"))
            {
                url += "SERVICE=WMTS&VERSION=1.0.0&REQUEST=GetCapabilities";
            }
            else if (!url.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                url += "/1.0.0/wmtscapabilities.xml";
            }

            if (!String.IsNullOrEmpty(_service?.Username) &&
                !String.IsNullOrEmpty(_service?.Password))
            {
                var aService = new ArcServerService(_servicePack);

                aService.Server = url;
                aService.Username = _service.Username.Replace(secrets);
                aService.Password = _service.Password.Replace(secrets);

                var token = aService.RefreshTokenAsync().Result;

                url = $"{url}{(url.Contains("?") ? "&" : "?")}token={token}";
            }


            Serializer<Capabilities> ser = new Serializer<Capabilities>();
            string xml = _servicePack.HttpService.GetStringAsync(url).Result;

            xml = xml.Replace("<Layer>", @"<ows:DatasetDescriptionSummary xsi:type=""LayerType"">");
            xml = xml.Replace("</Layer>", @"</ows:DatasetDescriptionSummary>");

            _response = ser.FromString(xml, Encoding.UTF8);
        }
    }

    #endregion

    #region ItemClasses

    #endregion
}
