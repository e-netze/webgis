using E.Standard.CMS.Core;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.Security.Core;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.CMS.UI;

public class SecurityControl : UserControl
{
    public const string CheckBoxUserPrefix = "_chkbox-user-";
    public const string CheckBoxRolePrefix = "_chkbox-role-";
    public const string ButtonRemoveUserName = "_remove-user";
    public const string ButtonRemoveRoleName = "_remove-role";
    public const string ButtonAddUser = "btnAddUser";
    public const string ButtonAddRole = "txtNewRole";

    private readonly IHttpService _http;
    private readonly ICryptoService _crypto;
    private readonly IEnumerable<ICustomCmsSecurityService> _customCmsSecurityServices;


    private SecurityControl(IHttpService http,
                            ICryptoService crypto,
                            IEnumerable<ICustomCmsSecurityService> customCmsSecurityServices)
    {
        _http = http;
        _crypto = crypto;
        _customCmsSecurityServices = customCmsSecurityServices;

        this.Name = "cms-security-control";
        this.AddControls(new IUIControl[]
        {
            txtNewUser,txtNewRole,cmbSchemeUser,cmbSchemeRole, txtWebgis
        });

        AddEvents();
    }

    public SecurityControl(IHttpService http,
                           ICryptoService crypto,
                           IEnumerable<ICustomCmsSecurityService> customCmsSecurityServices,
                           CMSManager.NodeAuthorization nodeAuth,
                           string propertyTagName = "")
    {
        _http = http;
        _crypto = crypto;
        _customCmsSecurityServices = customCmsSecurityServices;

        this.Name = "cms-security-control";

        var hasExclusiveRestrictions = nodeAuth.HasExclusiveRestriction;

        if (nodeAuth.Users != null)
        {
            foreach (var user in nodeAuth.Users)
            {
                var row = new Table.Row(user.Name);

                row.Cells.AddRange(new Table.Cell[]
                {
                    new Table.Cell(new CheckBox(CheckBoxUserPrefix + user.Name) { Value = user.Allowed.ToString().ToLower(), IsClickable=true } ),
                    new Table.Cell(new Label() { Label = $"{user.Name}" }),
                    new Table.Cell(new Label() { Label = user.InheritFrom })
                });
                if (String.IsNullOrWhiteSpace(user.InheritFrom))
                {
                    var deleteButton = new ImageButton(ButtonRemoveUserName)
                    {
                        Width = 26,
                        Height = 26,
                        Image = "/images/remove-26.png"
                    };
                    deleteButton.OnClick += DeleteButton_OnClick;
                    row.Cells.Add(new Table.Cell(deleteButton));
                }
                else
                {
                    row.Cells.Add(new Table.Cell(new Label()));
                }

                tblUsers.Rows.Add(row);
            }
        }
        var addRow = new Table.Row();
        addRow.Cells.AddRange(new Table.Cell[]
        {
            new Table.Cell(),
            new Table.Cell(cmbSchemeUser),
            new Table.Cell(txtNewUser),
            new Table.Cell(btnAddUser)
        });
        tblUsers.Rows.Add(addRow);

        if (nodeAuth.Roles != null)
        {
            foreach (var role in nodeAuth.Roles)
            {
                var row = new Table.Row(role.Name);

                row.Cells.AddRange(new Table.Cell[]
                {
                    new Table.Cell(new CheckBox(CheckBoxRolePrefix + role.Name) { Value = role.Allowed.ToString().ToLower(), IsClickable=true }),
                    new Table.Cell(new Label() { Label = role.Title.OrTake(role.Name) }, new SubLabel() { Label = role.Description }),
                    new Table.Cell(new Label() { Label = role.InheritFrom })
                });

                if (String.IsNullOrWhiteSpace(role.InheritFrom))
                {
                    var deleteButton = new ImageButton(ButtonRemoveRoleName)
                    {
                        Width = 26,
                        Height = 26,
                        Image = "/images/remove-26.png"
                    };
                    deleteButton.OnClick += DeleteButton_OnClick;
                    row.Cells.Add(new Table.Cell(deleteButton));
                }
                else
                {
                    row.Cells.Add(new Table.Cell(new Label()));
                }

                tblRoles.Rows.Add(row);
            }
        }
        addRow = new Table.Row();
        addRow.Cells.AddRange(new Table.Cell[]
        {
            new Table.Cell(),
            new Table.Cell(cmbSchemeRole),
            new Table.Cell(txtNewRole),
            new Table.Cell(btnAddRole)
        });
        tblRoles.Rows.Add(addRow);

        this.AddControl(new Heading() { Label = "Benutzer" });
        if (tblUsers.Rows.Count > 0)
        {
            tblUsers.Headers = new string[] { "", "Name", "Geerbt von", "" };
            tblUsers.ColumnWidths = new string[] { "38px", "230px", "auto", "38px" };
            this.AddControl(tblUsers);
        }

        this.AddControl(new Heading() { Label = "Rolen" });
        if (tblRoles.Rows.Count > 0)
        {
            tblRoles.Headers = new string[] { "", "Name", "Geerbt von", "" };
            tblRoles.ColumnWidths = new string[] { "38px", "230px", "auto", "38px" };
            this.AddControl(tblRoles);
        }

        this.AddControl(advices);
        foreach (var customCmsSecurity in _customCmsSecurityServices)
        {
            if (!String.IsNullOrEmpty(customCmsSecurity.AdviceMessage))
            {
                advices.AddControl(new InfoText()
                {
                    Text = $"{customCmsSecurity.SecurityMethod}: {customCmsSecurity.AdviceMessage}",
                    BgColor = "#ffe"
                }); ;
            }
        }

        this.AddControl(gbWebgis);
        gbWebgis.AddControl(txtWebgis);
        gbWebgis.AddControl(btnRefreshSchemes);

        AddEvents();
    }

    private void AddEvents()
    {
        btnRefreshSchemes.OnClick += BtnRefreshSchemes_OnClick;
        txtNewUser.OnAutoComplete += TxtNewUser_OnAutoComplete;
        txtNewRole.OnAutoComplete += TxtNewRole_OnAutoComplete;
    }

    private void TxtNewRole_OnAutoComplete(object sender, EventArgs e)
    {
        var url = txtWebgis.Value;
        var term = txtNewRole.Value;
        var prefix = cmbSchemeRole.Value;

        var ev = (InputAutoComplete.AutoCompleteEventArgs)e;

        foreach (var customSecurity in _customCmsSecurityServices)
        {
            var items = customSecurity.AutoCompleteValues(term, prefix, ev.CmsId, ev.UserId).Result;
            if (items != null && items.Count() > 0)
            {
                ev.Items.AddRange(items.Select(item => new InputAutoComplete.AutoCompleteItems.Item()
                {
                    Value = item.Value.OrTake(item.SuggestedText),
                    SuggestedText = item.SuggestedText,
                    SubText = item.SubText,
                    Thumbnail = item.Thumbnail
                }));

                return;
            }
        }

        if (!String.IsNullOrWhiteSpace(url) && !String.IsNullOrWhiteSpace(prefix))
        {
            var items = QueryPortalValue<string[]>(url, $"security-autocomplete?prefix={prefix}&term={term}&cmsid={ev.CmsId}&userid={ev.UserId}");
            if (items != null)
            {
                var itemsList = new List<string>(items);
                if (CmsDocument.UseAuthExclusives)
                {
                    itemsList.AddRange(items.Select(i => $"{i}{CmsDocument.AuthExclusivePostfix}"));
                }

                ev.Items.AddRange(itemsList.Select(item => new InputAutoComplete.AutoCompleteItems.Item()
                {
                    SuggestedText = item
                }));
            }
        }
    }

    private void TxtNewUser_OnAutoComplete(object sender, EventArgs e)
    {
        var url = txtWebgis.Value;
        var term = txtNewUser.Value;
        var prefix = cmbSchemeUser.Value;

        var ev = (InputAutoComplete.AutoCompleteEventArgs)e;

        if (!String.IsNullOrWhiteSpace(url) && !String.IsNullOrWhiteSpace(prefix))
        {
            var items = QueryPortalValue<string[]>(url, $"security-autocomplete?prefix={prefix}&term={term}&cmsid={ev.CmsId}&userid={ev.UserId}");
            if (items != null)
            {
                var itemsList = new List<string>(items);
                if (CmsDocument.UseAuthExclusives)
                {
                    itemsList.AddRange(items.Select(i => $"{i}{CmsDocument.AuthExclusivePostfix}"));
                }

                ev.Items.AddRange(itemsList.Select(i => new InputAutoComplete.AutoCompleteItems.Item()
                {
                    SuggestedText = i
                }));
            }
        }
    }

    public void SetWebgisInstance(string url)
    {
        try
        {
            txtWebgis.Value = url;
            BtnRefreshSchemes_OnClick(btnRefreshSchemes, new EventArgs());
        }
        catch
        {
            // Do nothing => sonst geht Dialog nicht auf, wenn ein Fehler auftritt
        }
    }

    private void BtnRefreshSchemes_OnClick(object sender, EventArgs e)
    {
        cmbSchemeUser.Options.Clear();
        cmbSchemeRole.Options.Clear();

        #region Custom Prefixes managed from CMS

        foreach (var customSecurityService in _customCmsSecurityServices)
        {
            foreach (var securityPrefix in customSecurityService.GetCustomSecurityPrefixes())
            {
                if (securityPrefix.type == SecurityTypes.User)
                {
                    cmbSchemeUser.Options.Add(new ComboBox.Option(securityPrefix.name));
                }
                else if (securityPrefix.type == SecurityTypes.Group)
                {
                    cmbSchemeRole.Options.Add(new ComboBox.Option(securityPrefix.name));
                }
            }
        }

        #endregion

        #region Prefixes managed in WebGIS Portal 

        if (!String.IsNullOrWhiteSpace(txtWebgis.Value))
        {
            var prefixes = QueryPortalValue<JsonSecurityPrefix[]>(txtWebgis.Value, "security-prefixes");

            foreach (var prefix in prefixes)
            {
                if (prefix.type == "user")
                {
                    cmbSchemeUser.Options.Add(new ComboBox.Option(prefix.name));
                }
                else if (prefix.type == "group")
                {
                    cmbSchemeRole.Options.Add(new ComboBox.Option(prefix.name));
                }
            }
        }

        #endregion

        cmbSchemeUser.Options.Add(new ComboBox.Option("(custom/no-scheme)"));
        cmbSchemeRole.Options.Add(new ComboBox.Option("(custom/no-scheme)"));
    }

    #region WebGis

    private T QueryPortalValue<T>(string url, string method)
    {
        string jsonStringEncrypted = _http.GetStringAsync($"{url}/proxy/query/{method}",
            new RequestAuthorization()
            {
                UseDefaultCredentials = true
            }).Result;

        string jsonString = _crypto.DecryptTextDefault(jsonStringEncrypted.Trim());
        var jsonObject = JSerializer.Deserialize<JsonValueResponse<T>>(jsonString);

        return jsonObject.value;
    }

    #region Helper Classes

    private class JsonValueResponse<T>
    {
        public T value { get; set; }
    }

    private class UserPrefix
    {
        public UserPrefix(string prefix)
        {
            this.Prefix = prefix;
        }

        public string Prefix { get; set; }

        public override string ToString()
        {
            return Prefix.ToString();
        }
    }

    private class JsonSecurityPrefix
    {
        public JsonSecurityPrefix() { }
        public JsonSecurityPrefix(string n, string t)
        {
            this.name = n;
            this.type = t;
        }

        public string name { get; set; }
        public string type { get; set; }
    }

    #endregion

    #endregion

    #region Static Members

    static public SecurityControl Empty(IHttpService http, ICryptoService crypto, IEnumerable<ICustomCmsSecurityService> customCmsSecurityServices)
    {
        var control = new SecurityControl(http, crypto, customCmsSecurityServices);
        return control;
    }

    #endregion

    private void DeleteButton_OnClick(object sender, EventArgs e)
    {

    }

    #region Properties

    public string NewUserName
    {
        get
        {
            var scheme = cmbSchemeUser.Value ?? String.Empty;
            if (scheme.StartsWith("(") && scheme.EndsWith(")")) // eg. (custom/no-scheme)
            {
                scheme = String.Empty;
            }

            return scheme + txtNewUser.Value;
        }
    }
    public string NewRoleName
    {
        get
        {
            var scheme = cmbSchemeRole.Value ?? String.Empty;
            if (scheme.StartsWith("(") && scheme.EndsWith(")")) // eg. (custom/no-scheme)
            {
                scheme = String.Empty;
            }

            return (scheme) + txtNewRole.Value;
        }
    }

    #endregion

    #region UIElements

    private readonly Table tblUsers = new Table("tblUser");
    private readonly Table tblRoles = new Table("tblRoles");

    private readonly ImageButton btnAddUser = new ImageButton(ButtonAddUser) { Width = 26, Height = 26, Image = "/images/add-26.png" };
    private readonly ImageButton btnAddRole = new ImageButton(ButtonAddRole) { Width = 26, Height = 26, Image = "/images/add-26.png" };

    private readonly InputAutoComplete txtNewUser = new InputAutoComplete("txtNewUser") { Label = "Neuer Benutzer" };
    private readonly InputAutoComplete txtNewRole = new InputAutoComplete("txtNewRole") { Label = "Neue Rolle" };

    private readonly ComboBox cmbSchemeUser = new ComboBox("cmbSchemeUser") { Label = "Schema" };
    private readonly ComboBox cmbSchemeRole = new ComboBox("cmbSchemeRole") { Label = "Schema" };

    private readonly GroupBox advices = new GroupBox() { Label = "Hinweise", Collapsed = false };

    private readonly GroupBox gbWebgis = new GroupBox() { Label = "WebGIS", Collapsed = true };
    private readonly Input txtWebgis = new Input("txtWebgis") { Label = "WebGIS Portal 5 Instance" };
    private readonly Button btnRefreshSchemes = new Button("btnRefreshSchemes") { Label = "Aktualisieren" };

    #endregion
}
