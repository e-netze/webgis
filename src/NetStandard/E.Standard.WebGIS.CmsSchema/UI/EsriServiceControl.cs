using E.Standard.ArcXml;
using E.Standard.ArcXml.Extensions;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.Extensions.Compare;
using E.Standard.Localization.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class EsriServiceControl : NameUrlUserConrol, IInitParameter, ISubmit
{
    private IMSService _imsService = null;
    private ArcServerService _agsService = null;
    private ArcServerTileService _agsTileService = null;
    private ArcServerImageServerService _agsImageService = null;
    public event EventHandler OnChanged = null;

    private GroupBox _gbService;
    private AuthentificationControl _authentificationControl;
    private Input _txtServer;
    private ComboBox _cmbService;
    private TextArea _serviceDescription;
    private ListBox _lstLayers;

    private NameUrlControl _nameUrlControl;

    private GroupBox _gbAgs;
    private ComboBox _cmbAgsAccessMethod;

    private CheckBox _chkAutoImportQueries = new CheckBox("chkAutoImportQueries") { Label = "Autoimport: alle vorhanden Feature Layer als Abfragen einfügen" };
    private CheckBox _chkAutoImportPresentations = new CheckBox("chkAutoImportPresentations") { Label = "Autoimport: Layerschaltungen/Darstellungsvariaten" };
    private CheckBox _chkAutoImportEditing = new CheckBox("chkAutoImportEditing") { Label = "Autoimport: alle editierbaren Feature Layer als Editthema einfügen" };

    private Button _bthRefreshServiceCombo = new Button("btnRefreshServiceCombo") { Label = "Services Aktualisieren" };

    private readonly CmsItemTransistantInjectionServicePack _servicePack;
    private readonly ILocalizer _localizer;

    public EsriServiceControl(CmsItemTransistantInjectionServicePack servicePack)
        : this(servicePack, false)
    {
    }

    public EsriServiceControl(CmsItemTransistantInjectionServicePack servicePack,
                             bool copyMode)
    {
        _servicePack = servicePack;
        _localizer = _servicePack?.GetLocalizer(typeof(EsriServiceControl));

        InitializeControls();

        _gbService.Enabled = !copyMode;

        #region UI

        _gbService.AddControls(new IUIControl[] {
            _txtServer,
            _bthRefreshServiceCombo,
            _cmbService,
            _serviceDescription
        });

        this.AddControl(_gbService);

        _gbAgs.AddControls(new IUIControl[] { _cmbAgsAccessMethod/*, _chkAutoImportPresentations, _chkAutoImportQueries, _chkAutoImportEditing*/ });
        this.AddControl(_gbAgs);

        _cmbAgsAccessMethod.Options.Add(new ComboBox.Option("Rest"));
        _cmbAgsAccessMethod.Options.Add(new ComboBox.Option("Soap"));

        this.AddControl(_authentificationControl);

        _bthRefreshServiceCombo.OnClick += bthRefreshServiceCombo_OnClick;
        _cmbService.OnClick += cmbService_OnClick;

        this.AddControl(_nameUrlControl);

        #region Events

        _txtServer.OnChange += txtServer_TextChanged;
        _cmbService.OnChange += cmbService_TextUpdate;

        #endregion

        #endregion
    }


    private void InitializeControls()
    {
        _gbService = new GroupBox() { Label = _localizer.Localize("ims-service") };
        _authentificationControl = new AuthentificationControl();
        _txtServer = new Input("txtServer") { Label = _localizer.Localize("server"), Required = true };
        _cmbService = new ComboBox("cmbService") { Label = _localizer.Localize("service"), Required = true };
        _serviceDescription = new TextArea("txtServiceDescription") { Label = "", Enabled = false, Rows = 2 };
        _lstLayers = new ListBox("lstLayers") { Label = _localizer.Localize("layers"), Height = 250 };

        _nameUrlControl = new NameUrlControl(_servicePack, "nameUrlControl");

        _gbAgs = new GroupBox() { Label = "ArcGIS Server Eingenschaften" };
        _cmbAgsAccessMethod = new ComboBox("cmbAgsAccessMethod") { Label = "Zugriffsmethode" };
    }

    public override NameUrlControl NameUrlControlInstance => _nameUrlControl;


    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool CopyMode
    {
        get { return !_gbService.Enabled; }
        set { _gbService.Enabled = !value; }
    }

    #region IInitParameter Member

    public object InitParameter
    {
        set
        {
            if (value is IMSService)
            {
                _authentificationControl.Visible = true;

                _agsService = null;
                _imsService = value as IMSService;

                _txtServer.Value = _imsService.Server;
                _cmbService.Value = _imsService.Service;

                _authentificationControl.Authentification = _imsService;

                _nameUrlControl.InitParameter = _imsService;

                _gbService.Label = _localizer.Localize("ims-service");
                _gbAgs.Visible = false;
            }
            else if (value is ArcServerService)
            {
                _authentificationControl.Visible = true;

                _imsService = null;
                _agsService = value as ArcServerService;

                _gbService.AddControl(_lstLayers);

                _authentificationControl.Authentification = _agsService;

                _txtServer.Value = _agsService.Server;
                _cmbService.Value = _agsService.Service;

                _nameUrlControl.InitParameter = _agsService;

                _gbService.Label = "ArcGis Server Dienst";

                _gbAgs.Visible = true;
                _cmbAgsAccessMethod.SelectedIndex = 0;

                _txtServer.Placeholder = "Server oder http(s)://server/arcgis";
            }
            else if (value is ArcServerTileService)
            {
                _authentificationControl.Visible = false;

                _imsService = null;
                _agsTileService = value as ArcServerTileService;

                //authentificationControl1.Authentification = _atService;

                _txtServer.Value = _agsTileService.Server;
                _cmbService.Value = _agsTileService.Service;

                _nameUrlControl.InitParameter = _agsTileService;

                _gbService.Label = "ArcGis Server Tiling Dienst";
                _gbAgs.Visible = false;
            }
            else if (value is ArcServerImageServerService)
            {
                _authentificationControl.Visible = true;

                _imsService = null;
                _agsImageService = value as ArcServerImageServerService;

                _authentificationControl.Authentification = _agsImageService;

                _txtServer.Value = _agsImageService.Server;
                _txtServer.Placeholder = "Server oder http(s)://server/arcgis";

                _cmbService.Value = _agsImageService.Service;

                _nameUrlControl.InitParameter = _agsImageService;

                _gbService.Label = "ArcGis Image Server Dienst";
                _gbAgs.Visible = false;
            }
        }
    }

    #endregion

    #region ISubmit Member

    public void Submit(NameValueCollection secrets)
    {
        if (_agsService != null)
        {
            var server = _agsService.GetServerName(_txtServer.Value);

            if (server.ToLower().EndsWith("/mapserver"))
            {
                _agsService.ServiceUrl = server;
            }
            else
            {
                string serviceUrl = _agsService.GetServerName(_txtServer.Value) + "/services/" + _cmbService.Value + "/MapServer";

                if (_cmbAgsAccessMethod.Value.ToLower() == "rest" && !serviceUrl.Contains("/rest/services/"))
                {
                    serviceUrl = serviceUrl.Replace("/services/", "/rest/services/");
                }

                _agsService.ServiceUrl = serviceUrl;
            }

            _agsService.AutoImportPresentations = Convert.ToBoolean(_chkAutoImportPresentations.Value);
            _agsService.AutoImportQueries = Convert.ToBoolean(_chkAutoImportQueries.Value);
            _agsService.AutoImportEditing = Convert.ToBoolean(_chkAutoImportEditing.Value);

            _agsService.ImportLayers = _lstLayers.SelectedItems;
        }
        if (_agsImageService != null && String.IsNullOrEmpty(_agsImageService.ServiceUrl))
        {
            string serviceUrl = new ArcServerService(_servicePack).GetServerName(_txtServer.Value) + "/services/" + _cmbService.Value + "/ImageServer";

            if (!serviceUrl.ToLower().Contains("/rest/services/"))
            {
                serviceUrl = serviceUrl.Replace("/services/", "/rest/services/");
            }

            _agsImageService.ServiceUrl = serviceUrl;
        }
    }

    #endregion

    private void bthRefreshServiceCombo_OnClick(object sender, EventArgs e)
    {
        _cmbService.Options.Clear();
        try
        {
            if (_imsService != null)
            {
                var connectionProperties = new ArcAxlConnectionProperties()
                {
                    AuthUsername = _imsService.Username,
                    AuthPassword = _imsService.Password,
                    Token = _imsService.Token,
                    Timeout = 25
                };

                foreach (string service in _servicePack.HttpService.GetServiceNamesAsync(connectionProperties, _txtServer.Value).Result)
                {
                    _cmbService.Options.Add(new ComboBox.Option(service));
                }
            }
            else if (_agsService != null)
            {
                _agsService.Server = _txtServer.Value;
                _agsService.Username = _authentificationControl.Authentification != null ? _authentificationControl.Authentification.Username : String.Empty;
                _agsService.Password = _authentificationControl.Authentification != null ? _authentificationControl.Authentification.Password : String.Empty;
                _agsService.Token = _authentificationControl.Authentification != null ? _authentificationControl.Authentification.Token : String.Empty;

                if (!String.IsNullOrWhiteSpace(_agsService.Username) && !String.IsNullOrWhiteSpace(_agsService.Password))
                {
                    var _ = _agsService.RefreshTokenAsync().Result;
                }

                foreach (string service in _agsService.GetServicesAsync("MapServer").Result)
                {
                    _cmbService.Options.Add(new ComboBox.Option(service));
                }
            }
            else if (_agsImageService != null)
            {
                var aService = new ArcServerService(_servicePack);
                aService.Server = _txtServer.Value;
                aService.Username = _authentificationControl.Authentification != null ? _authentificationControl.Authentification.Username : String.Empty;
                aService.Password = _authentificationControl.Authentification != null ? _authentificationControl.Authentification.Password : String.Empty;

                if (!String.IsNullOrWhiteSpace(aService.Username) && !String.IsNullOrWhiteSpace(aService.Password))
                {
                    var _ = aService.RefreshTokenAsync().Result;
                }

                foreach (string service in aService.GetServicesAsync("ImageServer").Result)
                {
                    _cmbService.Options.Add(new ComboBox.Option(service));
                }
            }
            else if (_agsTileService != null)
            {
                var aService = new ArcServerService(_servicePack);
                aService.Server = _txtServer.Value;
                aService.Username = _authentificationControl.Authentification != null ? _authentificationControl.Authentification.Username : String.Empty;
                aService.Password = _authentificationControl.Authentification != null ? _authentificationControl.Authentification.Password : String.Empty;

                if (!String.IsNullOrWhiteSpace(aService.Username) && !String.IsNullOrWhiteSpace(aService.Password))
                {
                    var _ = aService.RefreshTokenAsync().Result;
                }

                foreach (string service in aService.GetServicesAsync("MapServer").Result)
                {
                    _cmbService.Options.Add(new ComboBox.Option(service));
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private void cmbService_OnClick(object sender, EventArgs e)
    {
        try
        {
            if (String.IsNullOrWhiteSpace(_cmbService.Value))
            {
                _serviceDescription.Value = "Kein Service gefunden";
                return;
            }

            if (_imsService != null)
            {
                #region IMS Service

                _nameUrlControl.SetName(_cmbService.Value, true);

                #endregion
            }
            else if (_agsService != null)
            {
                #region AGS MapServer Service

                string serviceUrl = _agsService.GetServerName(_txtServer.Value) + "/services/" + _cmbService.Value + "/MapServer";

                _agsService.Server = _txtServer.Value;
                _agsService.Username = _authentificationControl.Authentification != null ? _authentificationControl.Authentification.Username : String.Empty;
                _agsService.Password = _authentificationControl.Authentification != null ? _authentificationControl.Authentification.Password : String.Empty;
                _agsService.ServiceUrl = serviceUrl;

                if (!String.IsNullOrWhiteSpace(_agsService.Username) && !String.IsNullOrWhiteSpace(_agsService.Password))
                {
                    var _ = _agsService.RefreshTokenAsync().Result;
                }

                var descriptionResult = _agsService.GetServiceDescriptionAsync().Result;

                if (!String.IsNullOrWhiteSpace(descriptionResult.description))
                {
                    _serviceDescription.Value = descriptionResult.description;
                }
                else
                {
                    _serviceDescription.Value = "Keine weiteren Informationen zu diesem Dienst";
                }

                string mapName = (
                    descriptionResult.mapName?.ToLower() == "layers" || descriptionResult.mapName?.ToLower() == "layer"
                        ? String.Empty
                        : descriptionResult.mapName
                    ) ?? String.Empty;
                _nameUrlControl.SetName(mapName.OrTake(_agsService.Service?.Split("/").Last()), true);

                var jsonLayers = _agsService.GetLayersWithGroupLayernamesAsync().Result;
                _lstLayers.Options.Clear();

                foreach (var jsonLayer in jsonLayers)
                {
                    _lstLayers.Options.Add(new ListBox.Option(jsonLayer.Id.ToString(), jsonLayer.Name) { Selected = true });
                }

                #endregion
            }
            else if (_agsImageService != null)
            {
                #region Image Server Service

                var aService = new ArcServerService(_servicePack);
                string serviceUrl = aService.GetServerName(_txtServer.Value) + "/services/" + _cmbService.Value + "/ImageServer";

                aService.Server = _txtServer.Value;
                aService.Username = _authentificationControl.Authentification != null ? _authentificationControl.Authentification.Username : String.Empty;
                aService.Password = _authentificationControl.Authentification != null ? _authentificationControl.Authentification.Password : String.Empty;
                aService.ServiceUrl = serviceUrl;

                if (!String.IsNullOrWhiteSpace(aService.Username) && !String.IsNullOrWhiteSpace(aService.Password))
                {
                    var _ = aService.RefreshTokenAsync().Result;
                }

                var descriptionResult = aService.GetServiceDescriptionAsync().Result;

                if (!String.IsNullOrWhiteSpace(descriptionResult.description))
                {
                    _serviceDescription.Value = descriptionResult.description;
                }
                else
                {
                    _serviceDescription.Value = "Keine weiteren Informationen zu diesem Dienst";
                }

                string mapName = (descriptionResult.mapName?.ToLower() == "layers" || descriptionResult.mapName?.ToLower() == "layer" ? String.Empty : descriptionResult.mapName) ?? String.Empty;
                _nameUrlControl.SetName(mapName, true);

                #endregion
            }
            else if (_agsTileService != null)
            {
                #region Tiling Server Service

                var aService = new ArcServerService(_servicePack);
                string serviceUrl = _agsService.GetServerName(_txtServer.Value) + "/services/" + _cmbService.Value + "/MapServer";

                aService.Server = _txtServer.Value;
                aService.Username = _authentificationControl.Authentification != null ? _authentificationControl.Authentification.Username : String.Empty;
                aService.Password = _authentificationControl.Authentification != null ? _authentificationControl.Authentification.Password : String.Empty;
                aService.ServiceUrl = serviceUrl;

                if (!String.IsNullOrWhiteSpace(aService.Username) && !String.IsNullOrWhiteSpace(aService.Password))
                {
                    var _ = aService.RefreshTokenAsync().Result;
                }

                var descriptionResult = aService.GetServiceDescriptionAsync().Result;

                if (!String.IsNullOrWhiteSpace(descriptionResult.description))
                {
                    _serviceDescription.Value = descriptionResult.description;
                }
                else
                {
                    _serviceDescription.Value = "Keine weiteren Informationen zu diesem Dienst";
                }

                string mapName = (descriptionResult.mapName?.ToLower() == "layers" || descriptionResult.mapName?.ToLower() == "layer" ? String.Empty : descriptionResult.mapName) ?? String.Empty;
                _nameUrlControl.SetName(mapName, true);

                #endregion
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private void txtServer_TextChanged(object sender, EventArgs e)
    {
        if (_imsService != null)
        {
            _imsService.Server = _txtServer.Value;
        }
        else if (_agsService != null)
        {
            _agsService.Server = _txtServer.Value;
        }
        else if (_agsTileService != null)
        {
            _agsTileService.Server = _txtServer.Value;
        }
        else if (_agsImageService != null)
        {
            _agsImageService.Server = _txtServer.Value;
        }

        //if (!String.IsNullOrEmpty(_cmbService.Value))
        //{
        //    _nameUrlControl.SetName(_cmbService.Value, true);
        //    _nameUrlControl.SetDirty(true); 
        //}

        OnChanged?.Invoke(this, new EventArgs());
    }

    private void cmbService_TextUpdate(object sender, EventArgs e)
    {
        if (_imsService != null)
        {
            _imsService.Service = _cmbService.Value;
        }
        else if (_agsService != null)
        {
            _agsService.Service = _cmbService.Value;
        }
        else if (_agsTileService != null)
        {
            _agsTileService.Service = _cmbService.Value;
        }
        else if (_agsImageService != null)
        {
            _agsImageService.Service = _cmbService.Value;
        }

        //if(!String.IsNullOrEmpty(_cmbService.Value))
        //{
        //    _nameUrlControl.SetName(_cmbService.Value, true);
        //    _nameUrlControl.SetDirty(true);
        //}

        OnChanged?.Invoke(this, new EventArgs());
    }

    private void cmbService_SelectedIndexChanged(object sender, EventArgs e)
    {
        cmbService_TextUpdate(sender, e);
    }
}
