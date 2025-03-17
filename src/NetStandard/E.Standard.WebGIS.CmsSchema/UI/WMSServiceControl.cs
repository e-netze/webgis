using E.Standard.CMS.Core;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.Extensions.ErrorHandling;
using E.Standard.OGC.Schema;
using E.Standard.OGC.Schema.wms;
using E.Standard.OGC.Schema.wms_1_1_1;
using E.Standard.OGC.Schema.wms_1_3_0;
using E.Standard.Web.Models;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core.Proxy;
using System;
using System.Collections.Specialized;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class WMSServiceControl : NameUrlUserConrol, IInitParameter, ISubmit
{
    private WMSService _service = null;

    private GroupBox gbService = new GroupBox() { Label = "WMS Dienst" };
    private ClientCertificateControl certControl = new ClientCertificateControl("certControl");
    private AuthentificationControl authentificationControl = new AuthentificationControl();
    private NameUrlControl nameUrlControl = new NameUrlControl();
    private Input txtServer = new Input("txtServer") { Label = "Server/Url", Placeholder = "Url eingeben..." };
    private ComboBox cmbVersion = new ComboBox("cmbVersion") { Label = "Version" };
    private ComboBox cmbImageFormat = new ComboBox("cmbImageFormat") { Label = "Image Format" };
    private ComboBox cmbGetFeatureInfoFormat = new ComboBox("cmbGetFeatureInfoFormat") { Label = "GetFeatureInfo Format" };
    private Input txtTicketService = new Input("txtTicketService") { Label = "WebGIS Instanz für Ticket Service" };
    private Button btnRefresh = new Button("btnRefresh") { Label = "Aktualisieren" };
    private ListBox lstLayers = new ListBox("lstLayers") { Label = "Layers", Height = 250 };

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public WMSServiceControl(CmsItemTransistantInjectionServicePack servicePack, bool copyMode)
    {
        _servicePack = servicePack;

        gbService.Enabled = !copyMode;

        this.AddControl(gbService);
        gbService.AddControl(txtServer);
        gbService.AddControl(cmbVersion);
        cmbVersion.Options.AddRange(new ComboBox.Option[] { new ComboBox.Option("1.1.1"), new ComboBox.Option("1.3.0") });

        var gbProperties = new GroupBox() { Label = "Dienst Eigenschaften" };
        this.AddControl(gbProperties);
        gbProperties.AddControl(btnRefresh);
        gbProperties.AddControl(cmbImageFormat);
        gbProperties.AddControl(cmbGetFeatureInfoFormat);
        gbProperties.AddControl(lstLayers);

        this.AddControl(authentificationControl);
        var gbTicket = new GroupBox() { Label = "TicketService (optional)", Collapsed = true };
        this.AddControl(gbTicket);
        gbTicket.AddControl(txtTicketService);
        this.AddControl(certControl);

        this.AddControl(nameUrlControl);

        txtServer.OnChange += txtServer_TextChanged;
        certControl.OnChange += certControl_CertificateChanged;
        btnRefresh.OnClick += btnRefresh_Click;
    }

    public override NameUrlControl NameUrlControlInstance => nameUrlControl;

    #region IInitParameter Member

    public object InitParameter
    {
        set
        {
            _service = value as WMSService;

            if (_service != null)
            {
                authentificationControl.Authentification = _service;
                nameUrlControl.InitParameter = _service;

                txtServer.Value = _service.Server;
                switch (_service.Version)
                {
                    case WMS_Version.version_1_1_1:
                        cmbVersion.SelectedIndex = 0;
                        break;
                    case WMS_Version.version_1_3_0:
                        cmbVersion.SelectedIndex = 1;
                        break;
                }
            }
        }
    }

    #endregion

    #region ISubmit Member

    public void Submit(NameValueCollection secrets)
    {
        if (_service != null)
        {
            _service.Server = txtServer.Value;
            if (cmbImageFormat.SelectedIndex >= 0)
            {
                _service.ImageFormat = (cmbImageFormat.SelectedItem != null ? cmbImageFormat.SelectedItem.ToString() : String.Empty);
            }

            if (cmbGetFeatureInfoFormat.SelectedIndex >= 0)
            {
                _service.GetFeatureInfoFormat = (cmbGetFeatureInfoFormat.SelectedItem != null ? cmbGetFeatureInfoFormat.SelectedItem.ToString() : String.Empty);
            }

            _service.ImportLayers = lstLayers.SelectedItems;

            switch (cmbVersion.Value)
            {
                case "1.1.1":
                    _service.Version = WMS_Version.version_1_1_1;
                    break;
                case "1.3.0":
                    _service.Version = WMS_Version.version_1_3_0;
                    break;
            }
            _service.TicketServer = txtTicketService.Value.Trim();
        }
    }

    #endregion

    private void btnRefresh_Click(object sender, EventArgs e)
    {
        TicketClient ticketClient = null;
        String ticket = String.Empty;

        try
        {
            cmbImageFormat.Options.Clear();

            string user = string.Empty, pwd = string.Empty;
            if (this.authentificationControl.Authentification != null)
            {
                user = this.authentificationControl.Authentification.Username;
                pwd = this.authentificationControl.Authentification.Password;
            }

            RequestAuthorization requestAuthorization = null;

            if ((!String.IsNullOrEmpty(user) &&
                 !String.IsNullOrEmpty(pwd)) ||
                certControl.X509Certificate != null)
            {
                requestAuthorization = new RequestAuthorization()
                {
                    Username = user,
                    Password = pwd,
                    ClientCerticate = certControl.X509Certificate
                };
            }

            string url = txtServer.Value;
            //ticketClient = !String.IsNullOrEmpty(txtTicketService.Text.Trim()) ? new TicketClient(txtTicketService.Text.Trim()) : null;
            //if (ticketClient != null) url = WMSService.AppendToUrl(txtServer.Text, "ogc_ticket=" + (ticket = ticketClient.Login(user, pwd, conn.GetProxy(txtServer.Text))));

            if (!String.IsNullOrEmpty(txtTicketService.Value.Trim()))
            {
                var ticketType = TicketClient.GetTicketType(
                            _servicePack.HttpService,
                            txtTicketService.Value.Trim(),
                            user, pwd,
                            _servicePack.HttpService.GetProxy(txtServer.Value), 300)
                                .GetAwaiter().GetResult();
                url = WMSService.AppendToUrl(txtServer.Value, "ogc_ticket=" + ticketType.Token);
            }

            CapabilitiesHelper caps = null;
            if (cmbVersion.Value == "1.1.1")
            {
                Serializer<WMT_MS_Capabilities> ser = new Serializer<WMT_MS_Capabilities>();

                caps = new CapabilitiesHelper(
                    ser.FromUrlAsync(WMSService.AppendToUrl(url, "REQUEST=GetCapabilities&VERSION=1.1.1&SERVICE=WMS"),
                    _servicePack.HttpService, requestAuthorization).Result);
            }

            else if (cmbVersion.Value == "1.3.0")
            {
                Serializer<WMS_Capabilities> ser = new Serializer<WMS_Capabilities>();

                ser.AddReplaceNamespace("https://www.opengis.net/wms", "http://www.opengis.net/wms");
                ser.AddReplaceNamespace("https://www.w3.org/1999/xlink", "http://www.w3.org/1999/xlink");
                ser.AddReplaceNamespace("https://www.w3.org/2001/XMLSchema-instance", "http://www.w3.org/2001/XMLSchema-instance");

                caps = new CapabilitiesHelper(ser.FromUrlAsync(
                    WMSService.AppendToUrl(url, "SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities"),
                    _servicePack.HttpService, requestAuthorization).Result);
            }

            if (caps != null)
            {
                if (caps.ImageFormats != null)
                {
                    foreach (string imageFormat in caps.ImageFormats)
                    {
                        cmbImageFormat.Options.Add(new ComboBox.Option(imageFormat));
                    }
                }

                if (caps.GetFeatureInfoFormats != null)
                {
                    foreach (string gfiFormat in caps.GetFeatureInfoFormats)
                    {
                        cmbGetFeatureInfoFormat.Options.Add(new ComboBox.Option(gfiFormat));
                    }
                }

                if (caps.Layers != null)
                {
                    foreach (var layer in caps.LayersWithStyle)
                    {
                        lstLayers.Options.Add(new ListBox.Option(layer.Name, layer.Title) { Selected = true });
                    }
                }
            }


            if (cmbImageFormat.Options.Count > 0)
            {
                cmbImageFormat.SelectedIndex = 0;
            }

            if (cmbGetFeatureInfoFormat.Options.Count > 0)
            {
                cmbGetFeatureInfoFormat.SelectedIndex = 0;
            }
        }
        catch (System.Exception ex)
        {
            throw ex.ToFullExceptionSummary();
        }
        finally
        {
            if (ticketClient != null && !String.IsNullOrEmpty(ticket))
            {
                ticketClient.Logout(ticket);
            }
        }
    }

    private void txtServer_TextChanged(object sender, EventArgs e)
    {
        //certControl.Enabled = txtServer.Value.ToLower().Trim().StartsWith("https://");
    }

    void certControl_CertificateChanged(object sender, EventArgs e)
    {
        if (_service != null)
        {
            _service.ClientCertificate = certControl.CertificateName;
            _service.ClientCertificatePassword = certControl.CertificatePassword;
        }
    }
}
