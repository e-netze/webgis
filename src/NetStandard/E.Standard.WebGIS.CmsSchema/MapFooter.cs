using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema.Abstraction;
using System;
using System.ComponentModel;
using System.Text;

namespace E.Standard.WebGIS.CmsSchema;

public class MapFooter : IEditable
{
    private string _sources = String.Empty;
    private string _links = String.Empty;
    private string _copyright = String.Empty, _copyrightLink = String.Empty;
    private string _advice = String.Empty;
    private string _logo = String.Empty;
    private int _logoWidth = 30, _logoHeight = 30;

    #region Properties
    [Browsable(true)]
    [DisplayName("Anzeigenamen der Links")]
    [Category("Quellen")]
    public string[] Sources
    {
        get
        {
            return _sources.Split(';');
        }
        set
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in value)
            {
                if (sb.Length > 0)
                {
                    sb.Append(";");
                }

                sb.Append(s.ToString());
            }
            _sources = sb.ToString();
        }
    }
    [Browsable(true)]
    [DisplayName("Zugeh�rige Links")]
    [Category("Quellen")]
    public string[] Links
    {
        get
        {
            return _links.Split(';');
        }
        set
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in value)
            {
                if (sb.Length > 0)
                {
                    sb.Append(";");
                }

                sb.Append(s.ToString());
            }
            _links = sb.ToString();
        }
    }

    [Browsable(true)]
    [DisplayName("#copyright")]
    [Category("#category_copyright")]
    public string Copyright
    {
        get { return _copyright; }
        set { _copyright = value; }
    }
    [Browsable(true)]
    [DisplayName("#copyright_link")]
    [Category("#category_copyright_link")]
    public string CopyrightLink
    {
        get { return _copyrightLink; }
        set { _copyrightLink = value; }
    }
    [Browsable(true)]
    [DisplayName("#advice")]
    [Category("#category_advice")]
    public string Advice
    {
        get { return _advice; }
        set { _advice = value; }
    }
    [Browsable(true)]
    [DisplayName("#logo")]
    [Category("#category_logo")]
    public string Logo
    {
        get { return _logo; }
        set { _logo = value; }
    }
    [Browsable(true)]
    [DisplayName("#logo_width")]
    [Category("#category_logo_width")]
    public int LogoWidth
    {
        get { return _logoWidth; }
        set { _logoWidth = value; }
    }
    [Browsable(true)]
    [DisplayName("#logo_height")]
    [Category("#category_logo_height")]
    public int LogoHeight
    {
        get { return _logoHeight; }
        set { _logoHeight = value; }
    }
    #endregion

    #region IPersistable Member

    public void Load(IStreamDocument stream)
    {
        _sources = (string)stream.Load("sources", String.Empty);
        _links = (string)stream.Load("links", String.Empty);
        _copyright = (string)stream.Load("copyright", String.Empty);
        _copyrightLink = (string)stream.Load("copyrightlink", String.Empty);
        _advice = (string)stream.Load("advice", String.Empty);
        _logo = (string)stream.Load("logo", String.Empty);
        _logoWidth = (int)stream.Load("logowidth", 30);
        _logoHeight = (int)stream.Load("logoheight", 30);
    }

    public void Save(IStreamDocument stream)
    {
        stream.Save("sources", _sources);
        stream.Save("links", _links);
        stream.Save("copyright", _copyright);
        stream.Save("copyrightlink", _copyrightLink);
        stream.Save("advice", _advice);
        stream.Save("logo", _logo);
        stream.Save("logowidth", _logoWidth);
        stream.Save("logoheight", _logoHeight);
    }

    #endregion
}
