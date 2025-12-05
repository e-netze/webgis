using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class EditingFieldCategory : NameUrl, IUI, ICreatable, IDisplayName, IEditable
{
    public EditingFieldCategory()
    {
        base.StoreUrl = false;
        base.ValidateUrl = false;
    }

    #region Properties

    public bool IsDefault { get; set; }

    [Category("~Erweiterte Collector Eigenschaften (Schnellsuche)")]
    [DisplayName("Schnellsuche Service")]
    public string QuickSearchService { get; set; }

    [Category("~Erweiterte Collector Eigenschaften (Schnellsuche)")]
    [DisplayName("Schnellsuche Categorie")]
    public string QuickSearchCategory { get; set; }

    [Category("~Erweiterte Collector Eigenschaften (Schnellsuche)")]
    [DisplayName("Schnellsuche Platzhalter")]
    public string QuickSearchPlaceholder { get; set; }

    [Category("~Erweiterte Collector Eigenschaften (Schnellsuche)")]
    [DisplayName("Schnellsuche setzt Geometrie")]
    public bool QuickSearchSetGeometry { get; set; }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NameUrlControl();
        ((NameUrlControl)ip).UrlIsVisible = false;

        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            return Crypto.GetID() + @"/.general";
        }
        else
        {
            return ".general";
        }
    }

    public Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name; }
    }

    #endregion

    #region IPersistable
    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        this.IsDefault = (bool)stream.Load("is_default", false);

        this.QuickSearchService = (string)stream.Load("quick_search_service", null);
        this.QuickSearchCategory = (string)stream.Load("quick_search_category", null);
        this.QuickSearchPlaceholder = (string)stream.Load("quick_search_placeholder", null);
        this.QuickSearchSetGeometry = (bool)stream.Load("quick_search_setgeometry", false);
    }
    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("is_default", this.IsDefault);

        if (!String.IsNullOrEmpty(this.QuickSearchService))
        {
            stream.Save("quick_search_service", this.QuickSearchService);
        }

        if (!String.IsNullOrEmpty(this.QuickSearchCategory))
        {
            stream.Save("quick_search_category", this.QuickSearchCategory);
        }

        if (!String.IsNullOrEmpty(this.QuickSearchPlaceholder))
        {
            stream.Save("quick_search_placeholder", this.QuickSearchPlaceholder);
        }

        stream.Save("quick_search_setgeometry", this.QuickSearchSetGeometry);
    }
    #endregion
}
