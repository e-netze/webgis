using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.OGC.Schema;
using E.Standard.OGC.Schema.wms;
using E.Standard.OGC.Schema.wms_1_1_1;
using E.Standard.OGC.Schema.wms_1_3_0;
using E.Standard.Web.Models;
using E.Standard.WebGIS.CMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class AddServiceLayersControl : UserControl, IInitParameter
{
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    private readonly ISchemaNode _node = null;
    private readonly string _relPath = String.Empty;
    private readonly CMSManager _cms = null;

    private InfoText _infoText = new InfoText("txtVanishedLayers")
    {
        BgColor = "#ffaaaa"
    };
    private ListBox _lstLayers = new ListBox("lstLayers") { Label = "Layer" };

    public AddServiceLayersControl(CmsItemTransistantInjectionServicePack servicePack,
                                   ISchemaNode schemaNode)
    {
        _servicePack = servicePack;

        if (schemaNode != null)
        {
            _node = schemaNode;
            _cms = schemaNode.CmsManager;
            _relPath = schemaNode.RelativePath;

            if (!_relPath.EndsWith("/") && !_relPath.EndsWith(@"\"))
            {
                _relPath += "/";
            }
        }

        this.PerformQuery().Wait();

        if (!String.IsNullOrEmpty(_infoText.Text))
        {
            this.AddControl(_infoText);
        }

        this.AddControl(_lstLayers);

    }

    #region IInitParameter

    public object InitParameter
    {
        set
        {
        }
    }

    #endregion

    public string[] SelectedLayerIds => _lstLayers.SelectedItems.ToArray();

    #region Helper

    async internal Task PerformQuery()
    {
        if (_cms == null || String.IsNullOrEmpty(_relPath))
        {
            return;
        }

        ServiceLayer[] currentLayers = Array.Empty<ServiceLayer>();

        currentLayers = _cms.SchemaNodeInstances(_servicePack, _relPath, true)
                            .Select(l => l as ServiceLayer)
                            .Where(l => l != null)
                            .ToArray();


        var options = new List<ListBox.Option>();

        if (_relPath.StartsWith("services/arcgisserver/mapserver"))
        {
            #region ArcGIS Dienst abfragen

            ArcServerService service = _cms.SchemaNodeInstance(_servicePack, Helper.TrimPathRight(_relPath, 2), true) as ArcServerService;
            try
            {
                foreach (var jsonLayer in await service.GetLayersWithGroupLayernamesAsync())
                {
                    options.Add(new ListBox.Option(jsonLayer.Id.ToString(), jsonLayer.Name)
                    {
                        Selected = currentLayers.Where(l => l.Id == jsonLayer.Id.ToString()).Count() > 0
                    });
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"{service.ServiceUrl}\n{ex.Message}");
            }

            #endregion
        }
        else if (_relPath.StartsWith("services/ogc/wms"))
        {
            #region WMS Dienst abfragen

            WMSService service = _cms.SchemaNodeInstance(_servicePack, Helper.TrimPathRight(_relPath, 2), true) as WMSService;

            RequestAuthorization requestAuthorization = null;

            if ((!String.IsNullOrEmpty(service.Username) &&
                 !String.IsNullOrEmpty(service.Password)) ||
                 !String.IsNullOrEmpty(service.Token))
            {
                requestAuthorization = new RequestAuthorization()
                {
                    Username = service.Username,
                    Password = service.Password,
                    UrlToken = service.Token
                };
            }

            string url = service.Server;
            CapabilitiesHelper capsHelper = null;

            try
            {
                if (service.Version == WMS_Version.version_1_1_1)
                {
                    Serializer<WMT_MS_Capabilities> ser = new Serializer<WMT_MS_Capabilities>();
                    url = WMSService.AppendToUrl(url, "VERSION=1.1.1&SERVICE=WMS&REQUEST=GetCapabilities");
                    WMT_MS_Capabilities caps = await ser.FromUrlAsync(url, _servicePack.HttpService, requestAuthorization);
                    capsHelper = new CapabilitiesHelper(caps);
                }
                else if (service.Version == WMS_Version.version_1_3_0)
                {
                    Serializer<WMS_Capabilities> ser = new Serializer<WMS_Capabilities>();

                    ser.AddReplaceNamespace("https://www.opengis.net/wms", "http://www.opengis.net/wms");
                    ser.AddReplaceNamespace("https://www.w3.org/1999/xlink", "http://www.w3.org/1999/xlink");
                    ser.AddReplaceNamespace("https://www.w3.org/2001/XMLSchema-instance", "http://www.w3.org/2001/XMLSchema-instance");

                    url = WMSService.AppendToUrl(url, "VERSION=1.3.0&SERVICE=WMS&REQUEST=GetCapabilities");
                    WMS_Capabilities caps = await ser.FromUrlAsync(url, _servicePack.HttpService, requestAuthorization);
                    capsHelper = new CapabilitiesHelper(caps);
                }

                foreach (var wmsLayer in capsHelper.LayersWithStyle)
                {
                    options.Add(new ListBox.Option(wmsLayer.Name, wmsLayer.Title)
                    {
                        Selected = currentLayers.Where(l => l.Id == wmsLayer.Name).Count() > 0
                    });
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"{service.Server}\n{ex.Message}");
            }

            #endregion
        }

        _lstLayers.Options.Clear();
        _lstLayers.Options.AddRange(options.OrderBy(o => o.Label.ToLower()));

        #region Check for vanished layers

        var vanishedLayers = new List<ServiceLayer>();

        foreach (var currentLayer in currentLayers)
        {
            if (options.Where(o => o.Value == currentLayer.Id).Count() == 0)
            {
                vanishedLayers.Add(currentLayer);
            }
        }

        if (vanishedLayers.Count > 0)
        {
            _infoText.Text = $"Vanished Layers:\n{String.Join("\n", vanishedLayers.Select(l => $"- {l.Name}"))}";
        }

        #endregion
    }

    #endregion
}
