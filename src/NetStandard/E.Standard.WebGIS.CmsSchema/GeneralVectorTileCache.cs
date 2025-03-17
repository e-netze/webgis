using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.WebGIS.CmsSchema.UI;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class GeneralVectorTileCache :
    CopyableNode, ICreatable, IEditable, IUI, IDisplayName
{
    private string _guid;
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public GeneralVectorTileCache(CmsItemTransistantInjectionServicePack servicePack)
    {
        base.StoreUrl = false;
        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());

        _servicePack = servicePack;
    }

    #region IDisplayName

    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name; }
    }

    #endregion

    #region ICreatable

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            return this.Url + @"\.general";
        }
        else
        {
            return ".general";
        }
    }

    public Task<bool> CreatedAsync(string FullName)
    {
        BuildServiceInfo(FullName);
        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IEditable/IUI

    public IUIControl GetUIControl(bool create)
    {
        this.Create = create;

        IInitParameter ip = new FormGeneralVectorTileCache();
        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region Helper

    public void BuildServiceInfo(string fullName)
    {
        var di = (DocumentFactory.DocumentInfo(fullName)).Directory;

        var di_themes = DocumentFactory.PathInfo(di.FullName + @"\themes");
        if (di_themes.Exists == false)
        {
            di_themes.Create();
        }

        ServiceLayer layer = new ServiceLayer();
        layer.Name = "_tilecache";

        layer.Url = Crypto.GetID();
        layer.Id = "0";

        layer.Visible = true;

        var xmlStream = DocumentFactory.New(di.FullName);
        layer.Save(xmlStream);
        xmlStream.SaveDocument(di.FullName + @"\themes\" + layer.CreateAs(true) + ".xml");

        ItemOrder itemOrder = new ItemOrder(di.FullName + @"\themes");
        itemOrder.Save();
    }

    private string ThemeExists(string fullName, ServiceLayer layer)
    {
        var di = (DocumentFactory.DocumentInfo(fullName)).Directory;
        di = DocumentFactory.PathInfo(di.FullName + @"\themes");
        if (!di.Exists)
        {
            return String.Empty;
        }

        foreach (var fi in di.GetFiles("*.xml"))
        {
            if (fi.Name.StartsWith("."))
            {
                continue;
            }

            ServiceLayer l = new ServiceLayer();
            IStreamDocument xmlStream = DocumentFactory.Open(fi.FullName);
            l.Load(xmlStream);

            if (l.Name == layer.Name)
            {
                return "services/miscellaneous/generalvectortilecache/" + this.Url + "/themes/" + l.Url;
            }
        }

        return String.Empty;
    }

    #endregion

    protected override void BeforeCopy()
    {
        base.BeforeCopy();
        _guid = Guid.NewGuid().ToString("N").ToLower();
    }

    public override string NodeTitle => "Allgemeiner Vector Tile Cache";
}

public class GeneralVectorTileCacheProperties : SchemaNode, IEditable
{
    #region Properties

    [DisplayName("Styles Json Url")]
    public string StylesJsonUrl { get; set; } = "";

    public string PreviewImageUrl { get; set; } = "";

    #endregion

    #region IPersistable Member

    public void Load(IStreamDocument stream)
    {
        StylesJsonUrl = (string)stream.Load("styles_json", "");
        PreviewImageUrl = (string)stream.Load("preview_image_url", "");
    }

    public void Save(IStreamDocument stream)
    {
        stream.Save("styles_json", StylesJsonUrl ?? "");
        stream.Save("preview_image_url", PreviewImageUrl ?? "");
    }

    #endregion
}
