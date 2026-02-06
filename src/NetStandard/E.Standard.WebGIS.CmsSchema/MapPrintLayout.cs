using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class MapPrintLayout : CopyableXml, IEditable, IUI, IDisplayName, ICreatable
{
    private string _layoutFile = String.Empty;
    private string _parameters = String.Empty;

    public MapPrintLayout()
    {
        this.ValidateUrl = false;
    }

    #region Properties
    [Browsable(true)]
    [DisplayName("#layout_file")]
    [Category("#category_layout_file")]
    [Editor(typeof(TypeEditor.PrintLayoutEditor),
            typeof(TypeEditor.ITypeEditor))]
    public string LayoutFile
    {
        get
        {
            return _layoutFile;
        }
        set
        {
            _layoutFile = value;
        }
    }

    [Browsable(true)]
    [DisplayName("#parameters")]
    [Category("#category_parameters")]
    public string Parameters
    {
        get { return _parameters; }
        set { _parameters = value; }
    }
    #endregion

    #region ICreatable Member

    override public string CreateAs(bool appendRoot)
    {
        if (String.IsNullOrEmpty(this.Url))
        {
            //this.Url = "l" + Guid.NewGuid().ToString("N").ToLower();
            this.Url = "l" + GuidEncoder.Encode(Guid.NewGuid()).ToLower();
        }

        return this.Url;
    }

    override public Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        NameUrlControl ctrl = new NameUrlControl();
        ctrl.InitParameter = this;

        return ctrl;
    }

    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name; }
    }

    #endregion

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _layoutFile = (string)stream.Load("layoutfile", String.Empty);
        _parameters = (string)stream.Load("parameters", String.Empty);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("layoutfile", _layoutFile);
        stream.Save("parameters", _parameters);
    }

    public override string NodeTitle
    {
        get { return "Layout"; }
    }
}
