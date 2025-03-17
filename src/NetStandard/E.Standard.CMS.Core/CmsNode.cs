using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace E.Standard.CMS.Core;

public class CmsNode
{
    private XmlNode _xmlNode;
    private CmsDocument.UserIdentification _ui;
    private CmsDocument _cms;

    internal CmsNode(CmsDocument cms, CmsDocument.UserIdentification ui, XmlNode xmlNode)
    {
        _cms = cms;
        _ui = ui;
        _xmlNode = xmlNode;
    }

    public string NodeName
    {
        get
        {
            if (_xmlNode == null)
            {
                return String.Empty;
            }

            return _xmlNode.Name;
        }
    }
    public string Name
    {
        get
        {
            return (string)Load("name", this.NodeName);
        }
    }
    public string Url
    {
        get
        {
            return (string)Load("url", this.NodeName);
        }
    }
    public bool Visible
    {
        get
        {
            return (bool)Load("visible", true);
        }
    }
    public string AliasName
    {
        get
        {
            return (string)Load("aliasname", this.Name);
        }
    }
    public string Id
    {
        get
        {
            return (string)Load("id", String.Empty);
        }
    }

    public string this[string attr]
    {
        get
        {
            if (_xmlNode == null ||
                _xmlNode.Attributes[attr] == null)
            {
                return String.Empty;
            }

            return _xmlNode.Attributes[attr].Value;
        }
    }

    public bool HasElement(string element)
    {
        return _xmlNode == null || _xmlNode.SelectSingleNode(element + "[@type]") == null;
    }

    public object Load(string element, object defaultValue)
    {
        if (_xmlNode == null)
        {
            return defaultValue;
        }

        XmlNode elementNode = _xmlNode.SelectSingleNode(element + "[@type]");
        if (elementNode == null)
        {
            return defaultValue;
        }

        #region AuthCheck
        if (elementNode.Attributes["unauthorizeddefault"] != null &&
            _cms != null && _ui != null)
        {
            if (!_cms.CheckAuth(_ui, this.NodeXPath + "@" + element))
            {
                if (elementNode.Attributes["type"].Value != "System.String")
                {
                    Type type = System.Type.GetType(elementNode.Attributes["type"].Value, false, true);
                    object obj = Activator.CreateInstance(type);
                    if (obj == null)
                    {
                        return defaultValue;
                    }

                    obj = Convert.ChangeType(elementNode.Attributes["unauthorizeddefault"].Value, type, CmsDocument.nhi);
                    return obj;
                }
                else
                {
                    return elementNode.Attributes["unauthorizeddefault"].Value;
                }
            }
        }
        #endregion

        if (elementNode.Attributes["type"].Value != "System.String")
        {
            Type type = System.Type.GetType(elementNode.Attributes["type"].Value, false, true);
            object obj = Activator.CreateInstance(type);
            if (obj == null)
            {
                return defaultValue;
            }

            obj = Convert.ChangeType(elementNode.InnerText, type, CmsDocument.nhi);
            return obj;
        }
        else
        {
            return elementNode.InnerText;
        }
    }

    public string LoadString(string element)
    {
        string str = Load(element, String.Empty).ToString();

        if (!String.IsNullOrWhiteSpace(_cms?.WebAppPath) && str.StartsWith("$string:"))
        {
            string id = str.Substring(8, str.Length - 8);
            string filename = (_cms?.WebEtcPath) + @"/res/strings.xml";
            System.IO.FileInfo fi = new System.IO.FileInfo(filename);
            if (fi.Exists)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filename);

                XmlNode stringNode = doc.SelectSingleNode("strings/string[@id='" + id + "' and @value]");
                if (stringNode != null)
                {
                    str = stringNode.Attributes["value"].Value;
                }
            }
        }

        return str;
    }

    public CmsNode ParentNode
    {
        get
        {
            if (_xmlNode == null || _xmlNode.ParentNode == null)
            {
                return null;
            }

            return new CmsNode(_cms, _ui, _xmlNode.ParentNode);
        }
    }

    public string NodeXPath
    {
        get
        {
            string xPath = this.NodeName;

            CmsNode parent = this.ParentNode;
            while (parent != null)
            {
                if (parent.NodeName == "config" ||
                    parent.NodeName == "#document")
                {
                    break;
                }

                xPath = parent.NodeName + "/" + xPath;
                parent = parent.ParentNode;
            }

            return xPath;
        }
    }

    internal XmlNode XmlNode
    {
        get { return _xmlNode; }
    }

    public CmsDocument Cms { get { return _cms; } }
}

public class CmsLink : CmsNode
{
    CmsNode _target;

    internal CmsLink(CmsDocument cms, CmsDocument.UserIdentification ui, XmlNode xmlNode, CmsNode target)
        : base(cms, ui, xmlNode)
    {
        _target = target;
    }

    public CmsNode Target
    {
        get { return _target; }
    }

    static public CmsLink Create(CmsDocument cms, CmsDocument.UserIdentification ui, CmsNode xmlNode, CmsNode target)
    {
        return new CmsLink(cms, ui, xmlNode.XmlNode, target);
    }
}

public class CmsNodeCollection : List<CmsNode>
{
    public IEnumerable<CmsLink> Links => this.Where(n => n is CmsLink).Select(n => (CmsLink)n);
    public IEnumerable<CmsNode> LinkTargets => this.Where(n => (n as CmsLink)?.Target != null).Select(n => ((CmsLink)n).Target);
}

public class CmsArrayItem
{
    private CmsNode _node;
    private string _arrayName, _itemGuid;

    public CmsArrayItem(CmsNode node, string arrayName, string itemGuid)
    {
        _node = node;
        _arrayName = arrayName;
        _itemGuid = itemGuid;
    }

    public string ItemGuid
    {
        get { return _itemGuid; }
    }

    public object Load(string element, object defaultValue)
    {
        return _node.Load(_arrayName + "_" + _itemGuid + "_" + element, defaultValue);
    }

    public string LoadString(string element)
    {
        return _node.LoadString(_arrayName + "_" + _itemGuid + "_" + element);
    }
}

public class CmsArrayItems : List<CmsArrayItem>
{
}
