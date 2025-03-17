using E.Standard.ArcXml;
using E.Standard.ArcXml.Extensions;
using E.Standard.Web.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace E.Standard.WebGIS.CmsSchema.Legacy;

public class IMSServerInfo
{
    private string _server;
    private List<string> _layers = null, _ids = null, _aliasnames = null, _groups = null, _fields = null;
    private List<int> _fieldtypes = null;
    private List<bool> _visibleLayers;

    private string _lastErr;
    private bool _useShortNames = false;
    private Encoding _encoding = Encoding.UTF8;

    private readonly IHttpService _httpService;
    private readonly ArcAxlConnectionProperties _connectionProperties;

    public IMSServerInfo(IHttpService httpService,
                         ArcAxlConnectionProperties connectionProperties,
                         string server,
                         Encoding encoding)
    {
        _httpService = httpService;
        _connectionProperties = connectionProperties;

        _server = server;

        _encoding = encoding;
    }

    // Bei SDE Feldern... Namen ohne die SDE-Bezeichnugen (SDE.ADO.LAYER...) verwenden
    public bool UseShortNames
    {
        get { return _useShortNames; }
        set { _useShortNames = value; }
    }

    async public Task<bool> GetLayerInfoAsync(string service)
    {
        _layers = null;
        _visibleLayers = null;

        XmlDocument xmldoc = null;
        _layers = new List<string>();
        _ids = new List<string>();
        _visibleLayers = new List<bool>();
        _aliasnames = new List<string>();
        _groups = new List<string>();

        if (xmldoc == null)
        {
            string requestAxl = "<?xml version='1.0' encoding='" + _encoding.BodyName.ToUpper() + "'?><ARCXML version='1.1'><REQUEST><GET_SERVICE_INFO fields='false' envelope='false' renderer='false' extensions='false' /></REQUEST></ARCXML>";
            string resp = await _httpService.SendAxlRequestAsync(_connectionProperties, requestAxl, _server, service);

            if (resp.IndexOf("Error") == 0)
            {
                _lastErr = "Unbekannter Fehler";
                return false;
            }

            XmlTextReader xml = new XmlTextReader(resp.ToString(), XmlNodeType.Element, null);
            xmldoc = new XmlDocument();
            xmldoc.Load(xml);
            XmlNode errNode = xmldoc.SelectSingleNode("//ERROR");
            if (errNode != null)
            {
                _lastErr = errNode.InnerText;
                return false;
            }
        }
        XmlNodeList layerinfos = xmldoc.GetElementsByTagName("LAYERINFO");
        for (int i = 0; i < layerinfos.Count; i++)
        {
            if (layerinfos[i].Attributes["name"] == null)
            {
                continue;
            }

            if (layerinfos[i].Attributes["id"] == null)
            {
                continue;
            }

            _layers.Add(layerinfos[i].Attributes["name"].Value.ToString());
            _ids.Add(layerinfos[i].Attributes["id"].Value.ToString());

            if (layerinfos[i].Attributes["visible"] != null)
            {
                _visibleLayers.Add(Convert.ToBoolean(layerinfos[i].Attributes["visible"].Value.ToString()));
            }
            else
            {
                _visibleLayers.Add(true);
            }

            if (layerinfos[i].Attributes["aliasname"] != null)
            {
                _aliasnames.Add(layerinfos[i].Attributes["aliasname"].Value);
            }
            else
            {
                _aliasnames.Add("");
            }

            if (layerinfos[i].Attributes["group"] != null)
            {
                _groups.Add(layerinfos[i].Attributes["group"].Value);
            }
            else
            {
                _groups.Add("");
            }
        }
        return true;
    }
    public int LayerCount { get { return (_layers == null) ? 0 : _layers.Count; } }

    public bool GetLayer(int i, ref string name, ref string id, ref bool visible)
    {
        string aliasname = "", group = "";
        return GetLayer(i, ref name, ref id, ref visible, ref aliasname, ref group);
    }

    public bool GetLayer(int i, ref string name, ref string id, ref bool visible, ref string aliasname, ref string group)
    {
        if (_layers == null)
        {
            return false;
        }

        if (i < 0 || i >= _layers.Count)
        {
            return false;
        }

        name = _layers[i].ToString();
        id = _ids[i].ToString();
        visible = Convert.ToBoolean(_visibleLayers[i]);
        aliasname = _aliasnames[i].ToString();
        group = _groups[i].ToString();
        return true;
    }

    async public Task<bool> GetFieldInfoByIdAsync(string service, string layer)
    {
        _fields = null;
        _fieldtypes = null;
        XmlDocument xmldoc = null;

        if (xmldoc == null)
        {
            string requestAxl = "<?xml version='1.0' encoding='" + _encoding.BodyName + "'?><ARCXML version='1.1'><REQUEST><GET_SERVICE_INFO fields='true' envelope='false' renderer='false' extensions='false' /></REQUEST></ARCXML>";
            string resp = await _httpService.SendAxlRequestAsync(_connectionProperties, requestAxl, _server, service);
            if (resp == null)
            {
                return false;
            }

            if (resp.IndexOf("Error") == 0)
            {
                return false;
            }

            XmlTextReader xml = new XmlTextReader(resp.ToString(), XmlNodeType.Element, null);
            xmldoc = new XmlDocument();
            xmldoc.Load(xml);
        }

        _fields = new List<string>();
        _fieldtypes = new List<int>();


        XmlNodeList layerinfos = xmldoc.GetElementsByTagName("LAYERINFO");
        for (int i = 0; i < layerinfos.Count; i++)
        {
            if (layerinfos[i].Attributes["id"] == null)
            {
                continue;
            }

            if (layerinfos[i].Attributes["id"].Value.ToString() == layer)
            {
                XmlNodeList fields = layerinfos[i].SelectNodes("FCLASS/FIELD");
                for (int j = 0; j < fields.Count; j++)
                {
                    if (fields[j].Attributes["name"] == null)
                    {
                        continue;
                    }

                    _fields.Add(ShortName(fields[j].Attributes["name"].Value));
                    if (fields[j].Attributes["type"] == null)
                    {
                        _fieldtypes.Add(-999);
                    }
                    else
                    {
                        _fieldtypes.Add(Convert.ToInt32(fields[j].Attributes["type"].Value));
                    }
                }
                return true;
            }
        }
        return false;
    }

    public int FieldCount { get { return (_fields == null) ? 0 : _fields.Count; } }
    public bool GetField(int i, ref string name, ref int type)
    {
        if (_fields == null)
        {
            return false;
        }

        if (i < 0 || i >= _fields.Count)
        {
            return false;
        }

        name = _fields[i].ToString();
        type = Convert.ToInt32(_fieldtypes[i]);
        return true;
    }

    public string ErrorMessage
    {
        get { return _lastErr; }
    }

    #region Helper

    private string ShortName(string name)
    {
        if (!UseShortNames)
        {
            return name;
        }

        return ToShortName(name);
    }

    private string ToShortName(string fieldname)
    {
        int pos = 0;
        string[] fieldnames = fieldname.Split(';');
        fieldname = "";
        for (int i = 0; i < fieldnames.Length; i++)
        {
            while ((pos = fieldnames[i].IndexOf(".")) != -1)
            {
                fieldnames[i] = fieldnames[i].Substring(pos + 1, fieldnames[i].Length - pos - 1);
            }
            if (fieldname != "")
            {
                fieldname += ";";
            }

            fieldname += fieldnames[i];
        }

        return fieldname;
    }

    #endregion
}
