using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace E.Standard.WebGIS.CmsSchema;

public class EditThemeLink : SchemaNodeLink, IEditable
{
    private string _aliasname = String.Empty;
    private string _editThemeId = String.Empty;

    #region Properties
    [DisplayName("Alias Name")]
    public string AliasName
    {
        get { return _aliasname; }
        set { _aliasname = value; }
    }
    [Editor(typeof(TypeEditor.EditThemeIdEditor), typeof(TypeEditor.ITypeEditor))]
    public string EditThemeId
    {
        get { return _editThemeId; }
        set { _editThemeId = value; }
    }
    #endregion

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _aliasname = (string)stream.Load("aliasname", String.Empty);
        _editThemeId = (string)stream.Load("editthemeid", String.Empty);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("aliasname", _aliasname);
        stream.Save("editthemeid", _editThemeId);
    }
}

public class GdiPropertiesEditTheme : AuthorizableArrayItem
{
    private Link _themeLink = null;
    private string _aliasname = String.Empty;
    private string _editThemeId = String.Empty;
    private bool _editService = false;

    public GdiPropertiesEditTheme()
    {
    }

    #region Properties

    [DisplayName("Alias Name")]
    public string AliasName
    {
        get { return _aliasname; }
        set { _aliasname = value; }
    }

    [Editor(typeof(TypeEditor.EditThemeIdEditor), typeof(TypeEditor.ITypeEditor))]
    public string EditThemeId
    {
        get { return _editThemeId; }
        set { _editThemeId = value; }
    }

    [DisplayName("(webGIS 5) Edit Service")]
    public bool EditService
    {
        get { return _editService; }
        set { _editService = value; }
    }

    // Falls "false" wird dieses EditThema im WebGIS5 nicht angezeigt. Wurde bspw. bei St�rungspools verwendet: 2 Themen bei St�rfallen => eines f�r Collector, eines f�r WebGIS
    [DisplayName("(webGIS 5) Visible")]
    public bool Visible
    {
        get; set;
    }

    #endregion

    [Browsable(false)]
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public Link ThemeLink
    {
        get { return _themeLink; }
        internal set { _themeLink = value; }
    }

    public override string ToString()
    {
        return "Gdi-EditThemeId: " + _editThemeId;
    }

    public override void Load()
    {
        _aliasname = (string)this.StreamLoad("gdi_aliasname", String.Empty);
        _editThemeId = (string)this.StreamLoad("gdi_editthemeid", String.Empty);
        _editService = (bool)this.StreamLoad("gdi_editservice", false);
        this.Visible = (bool)this.StreamLoad("gdi_visible", true);
    }

    public override void Save()
    {
        this.StreamSave("gdi_aliasname", _aliasname);
        this.StreamSave("gdi_editthemeid", _editThemeId);
        this.StreamSave("gdi_editservice", _editService);
        this.StreamSave("gdi_visible", this.Visible);
    }
}

public class GdiPropertiesEditThemeArray : List<GdiPropertiesEditTheme>
{
    private Link _themeLink;

    public GdiPropertiesEditThemeArray(Link themeLink)
    {
        _themeLink = themeLink;
    }

    public GdiPropertiesEditThemeArray(Link themeLink, IEnumerable<GdiPropertiesEditTheme> collection)
        : base(collection)
    {
        _themeLink = themeLink;

        foreach (GdiPropertiesEditTheme element in this)
        {
            element.ThemeLink = _themeLink;
        }
    }

    new public void Add(GdiPropertiesEditTheme element)
    {
        if (element != null)
        {
            element.ThemeLink = _themeLink;
            base.Add(element);
        }
    }

    public static GdiPropertiesEditThemeArray Load(Link themeLink, IStreamDocument stream, string propertyName)
    {
        AuthorizableArray a = new AuthorizableArray(propertyName, null, typeof(GdiPropertiesEditTheme));
        a.Load(stream);

        if (a.Array != null && a.Array.Length > 0)
        {
            GdiPropertiesEditThemeArray array = new GdiPropertiesEditThemeArray(themeLink);
            foreach (object o in a.Array)
            {
                if (o is GdiPropertiesEditTheme)
                {
                    array.Add((GdiPropertiesEditTheme)o);
                }
            }
            return array;
        }

        return null;
    }

    public static void Save(IStreamDocument stream, string propertyName, GdiPropertiesEditThemeArray array)
    {
        AuthorizableArray a = new AuthorizableArray(propertyName,
            (array != null ? array.ToArray() : null),
            typeof(GdiPropertiesEditTheme));

        a.Save(stream);
    }
}
