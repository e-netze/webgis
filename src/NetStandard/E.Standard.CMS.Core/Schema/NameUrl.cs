using E.Standard.CMS.Core.Extensions;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema.Abstraction;
using Newtonsoft.Json;
using System.ComponentModel;

namespace E.Standard.CMS.Core.Schema;

public class NameUrl : Persistable, IUrlNode
{
    private string _name = string.Empty, _url = string.Empty;
    private bool _storeUrl = true, _nameAndUrlIdent = false;
    private bool _validateUrl = true;

    public NameUrl()
    {
    }

    public NameUrl(NameUrl nameUrl)
    {
        _name = nameUrl._name;
        _url = nameUrl._url;
        _storeUrl = nameUrl._storeUrl;
        _nameAndUrlIdent = nameUrl._nameAndUrlIdent;
        _validateUrl = nameUrl._validateUrl;
    }

    #region Properties

    [Category("Bezeichnung")]
    virtual public string Name
    {
        get { return _name; }
        set
        {
            _name = value;
            if (_nameAndUrlIdent && string.IsNullOrWhiteSpace(_url))  // Nur schreiben wenn leer ist (sonst funktionieren TOC und TOCGroup nicht mehr -> Namen lassen sich nicht mehr �nderen...)!!!
            {
                _url = value.ToValidNodeUrl();
            }
        }
    }

    [ReadOnly(true)]
    [Category("Bezeichnung")]
    virtual public string Url
    {
        get { return _url; }
        set
        {
            _url = value.ToValidNodeUrl();

            if (_nameAndUrlIdent && string.IsNullOrWhiteSpace(_name))    // Nur schreiben wenn leer ist (sonst funktionieren TOC und TOCGroup nicht mehr -> Namen lassen sich nicht mehr �nderen...)!!!!
            {
                _name = value;
            }
        }
    }

    [Browsable(false)]
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool StoreUrl
    {
        get { return _storeUrl; }
        set { _storeUrl = value; }
    }

    [Browsable(false)]
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool NameUrlIdentically
    {
        get { return _nameAndUrlIdent; }
        set { _nameAndUrlIdent = value; }
    }
    #endregion

    #region IPersistable Member

    override public void Load(IStreamDocument stream)
    {
        base.Load(stream);

        string urlDefault = string.Empty;
        var fi = stream.ConfigFile;

        if (fi != null)
        {
            switch (fi.Name.ToLower())
            {
                case ".general.xml":
                case "general.xml":
                    urlDefault = fi.Directory.Name;
                    break;
                default:
                    urlDefault = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
                    break;
            }
        }

        _name = (string)stream.Load("name", string.Empty);
        _url = (string)stream.Load("url", urlDefault);
    }

    override public void Save(IStreamDocument stream)
    {
        base.Save(stream);

        //
        // DoTo: Irgendwo sollte man abfrage, ob Name und Url im Formular bef�llt sind (beim erstellen eines neuen Features)
        // Sollte nicht hier sein, weil sonst bestehende Rechtecksausdehnung nicht gehen...
        //

        //if (String.IsNullOrWhiteSpace(_name))
        //    throw new Exception("Property 'name' is empty!");

        //if (String.IsNullOrWhiteSpace(_url))
        //    throw new Exception("Property 'url' is empty!");

        stream.Save("name", _name);

        if (_storeUrl)
        {
            stream.Save("url", _url);
        }
    }

    #endregion

    #region IUrlNode Member
    [Browsable(false)]
    public bool ValidateUrl
    {
        get { return _validateUrl; }
        set { _validateUrl = value; }
    }

    #endregion
}
