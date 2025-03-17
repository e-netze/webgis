using E.Standard.CMS.Core;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.OGC.Schema;
using E.Standard.OGC.Schema.wfs;
using E.Standard.Web.Models;
using E.Standard.WebGIS.CMS;
using System;
using System.Collections.Specialized;

namespace E.Standard.WebGIS.CmsSchema.UI;

internal class WFSServiceControl : NameUrlUserConrol, IInitParameter, ISubmit
{
    private bool isInCopyMode;
    private WFSService _service = null;

    AuthentificationControl authentificationControl = new AuthentificationControl();
    NameUrlControl nameUrlControl = new NameUrlControl();
    GroupBox gbService = new GroupBox() { Label = "WFS Dienst" };
    Input txtServer = new Input("txtServer") { Label = "WFS Service Url" };
    ComboBox cmbVersion = new ComboBox("cmbVersion") { Label = "Version" };
    ClientCertificateControl certControl = new ClientCertificateControl("certControl");

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public WFSServiceControl(CmsItemTransistantInjectionServicePack servicePack,
                             bool isInCopyMode)
    {
        _servicePack = servicePack;

        this.isInCopyMode = isInCopyMode;

        this.AddControl(nameUrlControl);

        this.AddControl(authentificationControl);
        this.AddControl(certControl);

        this.AddControl(gbService);
        gbService.AddControl(txtServer);
        gbService.AddControl(cmbVersion);
        cmbVersion.Options.AddRange(new ComboBox.Option[] { new ComboBox.Option("1.0.0"), new ComboBox.Option("1.1.0") });

        txtServer.OnChange += txtServer_TextChanged;
        certControl.OnChange += certControl_CertificateChanged;
    }

    public override NameUrlControl NameUrlControlInstance => nameUrlControl;

    #region IInitParameter Member

    public object InitParameter
    {
        set
        {
            _service = value as WFSService;

            if (_service != null)
            {
                authentificationControl.Authentification = _service;
                nameUrlControl.InitParameter = _service;

                txtServer.Value = _service.Server;
                switch (_service.Version)
                {
                    case WFS_Version.version_1_0_0:
                        cmbVersion.SelectedIndex = 0;
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

            switch (cmbVersion.Value)
            {
                case "1.0.0":
                    _service.Version = WFS_Version.version_1_0_0;
                    break;
                case "1.1.0":
                    _service.Version = WFS_Version.version_1_1_0;
                    break;
            }
        }
    }

    #endregion

    private void btnRefresh_Click(object sender, EventArgs e)
    {
        try
        {
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

            CapabilitiesHelper caps = null;
            if (cmbVersion.SelectedIndex == 0)
            {
                Serializer<OGC.Schema.wfs_1_0_0.WFS_CapabilitiesType> ser = new Serializer<OGC.Schema.wfs_1_0_0.WFS_CapabilitiesType>();
                OGC.Schema.wfs_1_0_0.WFS_CapabilitiesType wfsCaps = ser.FromUrlAsync(
                    WMSService.AppendToUrl(txtServer.Value, "REQUEST=GetCapabilities&VERSION=1.0.0&SERVICE=WFS"),
                    _servicePack.HttpService,
                    requestAuthorization).Result;

                caps = new CapabilitiesHelper(wfsCaps);
            }
            else if (cmbVersion.SelectedIndex == 1)
            {
                Serializer<OGC.Schema.wfs_1_1_0.WFS_CapabilitiesType> ser = new Serializer<OGC.Schema.wfs_1_1_0.WFS_CapabilitiesType>();
                OGC.Schema.wfs_1_1_0.WFS_CapabilitiesType wfsCaps = ser.FromUrlAsync(
                    WMSService.AppendToUrl(txtServer.Value, "REQUEST=GetCapabilities&VERSION=1.1.0&SERVICE=WFS"),
                    _servicePack.HttpService,
                    requestAuthorization).Result;

                caps = new CapabilitiesHelper(wfsCaps);
            }
        }
        catch (System.Exception)
        {
            //StringBuilder msg = new StringBuilder();
            //msg.Append(ex.Message);
            //while (ex.InnerException != null)
            //{
            //    msg.Append("\r\n" + ex.InnerException.Message);
            //    ex = ex.InnerException;
            //}
            //MessageBox.Show(msg.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw;
        }
    }

    private void txtServer_TextChanged(object sender, EventArgs e)
    {
        //certControl.Enabled = txtServer.Value!=null && txtServer.Value.ToLower().Trim().StartsWith("https://");
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