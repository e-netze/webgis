using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.UI;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class Chainage : NameUrl, ICreatable, IUI, IDisplayName, IEditable, IPersistable
{
    private LengthUnit _unit = LengthUnit.m;
    private string _expression = "{0}";
    private string _pointlinerelation = String.Empty;
    private string _pointstatfield = String.Empty;

    public Chainage()
    {
        this.ValidateUrl = false;
        this.StoreUrl = false;
    }

    #region Properties
    [Browsable(true)]
    [DisplayName("Längeneinheit")]
    [Category("Darstellung")]
    public LengthUnit Unit
    {
        get { return _unit; }
        set { _unit = value; }
    }
    [Browsable(true)]
    [DisplayName("Ausdruck")]
    [Category("Darstellung")]
    public string Expression
    {
        get { return _expression; }
        set { _expression = value; }
    }
    
    [Browsable(true)]
    [DisplayName("Punkt-Linien Beziehung (SQL)")]
    [Category("~Verknüfung mit Punkt-Linienthema")]
    public string PointLineRelation
    {
        get { return _pointlinerelation; }
        set { _pointlinerelation = value; }
    }
    [Browsable(true)]
    [DisplayName("Stationierungsfeld des Punktthemas")]
    [Category("~Verknüfung mit Punkt-Linienthema")]
    public string PointStatField
    {
        get { return _pointstatfield; }
        set { _pointstatfield = value; }
    }

    [Browsable(true)]
    [DisplayName("Service URL")]
    [Category("~Oder Abfrage-Service-API")]
    public string ServiceUrl { get; set; } = String.Empty; 

    [Browsable(true)]
    [Category("Berechnung")]
    [DisplayName("Koordinatensystem, in dem gerechnet werden soll (EPSG-Code)")]
    [Description("Um die Genauigkeit der Ergebnisse zu gewährleisten, sollte in einer Abbildungsebene mit möglichst geringer Längenverzerrung gerechnet werden. Wird hier kein Wert oder 0 angeführt, wird in der Kartenprojektion rechnert. Das kann bei WebMercator oder geographischen Projektionen zu Verzerrungen führen. Hier ist beispielsweise ein Projeziertes Koordinatensystem wie Gauß-Krüger ideal.")]
    public int CalcSrefId { get; set; }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            return Crypto.GetID() + @"\.general";
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

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new TocGroupControl();
        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name; }
    }

    #endregion

    #region IPersistable Member

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _unit = (LengthUnit)stream.Load("unit", (int)LengthUnit.m);
        _expression = (string)stream.Load("expression", "{0}");
        _pointlinerelation = (string)stream.Load("pointlinerelation", String.Empty);
        _pointstatfield = (string)stream.Load("pointstatfield", String.Empty);
        this.CalcSrefId = (int)stream.Load("calcsrefid", 0);
        this.ServiceUrl = (string)stream.Load("serviceurl", String.Empty);
    }
    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("unit", (int)_unit);
        stream.Save("expression", _expression);
        stream.Save("pointlinerelation", _pointlinerelation);
        stream.Save("pointstatfield", _pointstatfield);
        stream.Save("calcsrefid", this.CalcSrefId);
        stream.Save("serviceurl", this.ServiceUrl);
    }

    #endregion
}

public class ChainageLineTheme : Link
{
}

public class ChainagePointTheme : Link
{
}

public class ChainageLink : SchemaNodeLink, IEditable
{
    public ChainageLink()
    {
    }

    [Browsable(true)]
    public string Url
    {
        get
        {
            string url = this.RelativePath.Replace("\\", "/");
            if (url.LastIndexOf("/") != -1)
            {
                return url.Substring(url.LastIndexOf("/") + 1, url.Length - url.LastIndexOf("/") - 1);
            }
            return String.Empty;
        }
    }
    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);
    }
    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);
    }
}
