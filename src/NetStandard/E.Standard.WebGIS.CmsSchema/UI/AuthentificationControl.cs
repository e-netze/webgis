using E.Standard.CMS.UI.Controls;
using Newtonsoft.Json;
using System;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class AuthentificationControl : UserControl
{
    private IAuthentification _authentification = null;
    private Input _txtUser = new Input("txtUser") { Label = "Benutzername", Placeholder = "Optional" };
    private Input _txtPwd = new InputPassword("txtPwd") { Label = "Password", Placeholder = "Optional" };
    private Input _txtToken = new Input("txtToken") { Label = "oder Token", Placeholder = "Optional (fixer Token für Krako Reverse Proxy)" };

    public AuthentificationControl(string name = "")
        : base(name)
    {
        var groupBox = new GroupBox() { Label = "Security (optional)", Collapsed = true };
        groupBox.AddControls(new Control[] { _txtUser, _txtPwd, _txtToken });
        this.AddControl(groupBox);

        _txtUser.OnChange += txtUser_TextChanged;
        _txtPwd.OnChange += txtPwd_TextChanged;
        _txtToken.OnChange += txtToken_TextChanged;
    }


    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public IAuthentification Authentification
    {
        get { return _authentification; }
        set
        {
            if (value != null)
            {
                _authentification = value;
                if (String.IsNullOrWhiteSpace(_authentification.Username))
                {
                    _authentification.Username = String.Empty;
                }

                if (String.IsNullOrWhiteSpace(_authentification.Password))
                {
                    _authentification.Password = String.Empty;
                }

                if (String.IsNullOrWhiteSpace(_authentification.Token))
                {
                    _authentification.Token = String.Empty;
                }

                _txtUser.Value = _authentification.Username;
                _txtPwd.Value = _authentification.Password;
                _txtToken.Value = _authentification.Token;
            }
        }
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool ShowUser
    {
        set
        {
            _txtUser.Visible = _txtPwd.Visible = value;
        }
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool ShowToken
    {
        set
        {
            _txtToken.Visible = value;
        }
    }

    private void txtUser_TextChanged(object sender, EventArgs e)
    {
        if (_authentification != null)
        {
            _authentification.Username = _txtUser.Value;
        }
    }

    private void txtPwd_TextChanged(object sender, EventArgs e)
    {
        if (_authentification != null)
        {
            _authentification.Password = _txtPwd.Value;
        }
    }

    private void txtToken_TextChanged(object sender, EventArgs e)
    {
        if (_authentification != null)
        {
            _authentification.Token = _txtToken.Value;
        }
    }
}
