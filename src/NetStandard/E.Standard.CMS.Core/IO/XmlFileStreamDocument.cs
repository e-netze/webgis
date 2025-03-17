using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.Extensions.Text;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Xml;

namespace E.Standard.CMS.Core.IO;

public class XmlFileStreamDocument : IStreamDocument
{
    private NumberFormatInfo _nhi = CultureInfo.InvariantCulture.NumberFormat;
    private XmlDocument _doc;
    private XmlNode _parent;
    private NameValueCollection _stringReplace = null;
    private string _filename = string.Empty;

    public XmlFileStreamDocument()
    {

    }

    //public XmlStreamDocument()
    //{
    //    _doc = new XmlDocument();
    //    _parent = _doc.CreateElement("config");
    //    _doc.AppendChild(_parent);
    //}
    //public XmlStreamDocument(string filename)
    //{
    //    try
    //    {
    //        _doc = new XmlDocument();
    //        _doc.Load(_filename = filename);
    //        _parent = _doc.SelectSingleNode("config");
    //    }
    //    catch (Exception ex)
    //    {
    //        throw new FileLoadException("Datei '" + filename + "' kann nicht gelesen werden!", filename, ex);
    //    }
    //}

    public event ParseEncryptedValue OnParseBeforeEncryptValue = null;

    public string FireParseBoforeEncryptValue(string value)
    {
        string refValue = value;

        OnParseBeforeEncryptValue?.Invoke(ref refValue);

        return refValue;
    }

    public void Init(string path = "", NameValueCollection stringReplace = null)
    {
        _doc = new XmlDocument();
        _stringReplace = stringReplace;

        if (string.IsNullOrWhiteSpace(path))
        {
            _parent = _doc.CreateElement("config");
            _doc.AppendChild(_parent);
        }
        else
        {
            try
            {
                _doc.Load(_filename = path);
                _parent = _doc.SelectSingleNode("config");
            }
            catch (Exception ex)
            {
                throw new FileLoadException("Datei '" + path + "' kann nicht gelesen werden!", path, ex);
            }
        }
    }

    public NameValueCollection StringReplace => _stringReplace;

    public void SaveDocument()
    {
        if (string.IsNullOrEmpty(_filename))
        {
            throw new ArgumentException("No filename...");
        }

        SaveDocument(_filename);
    }
    public void SaveDocument(string filename)
    {
        DirectoryInfo di = new DirectoryInfo(
            Path.GetDirectoryName(filename));
        if (!di.Exists)
        {
            di.Create();
        }

        _doc.Save(filename);
    }
    public void SaveDocument(Stream stream)
    {
        _doc.Save(stream);
    }

    public XmlDocument XmlDocument()
    {
        return _doc.Clone() as XmlDocument;
    }

    private XmlNode CreateNode(string path)
    {
        return CreateNode(path, false);
    }
    private XmlNode CreateNode(string path, bool appendNodeAttribute)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        string[] parts = path.Replace(@"\", "/").Split('/');

        XmlNode parent = _parent; // _doc.SelectSingleNode("config");
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parts.Length; i++)
        {
            XmlNode node = parent.SelectSingleNode(parts[i]);
            if (node == null)
            {
                node = _doc.CreateElement(parts[i]);

                parent.AppendChild(node);
                if (appendNodeAttribute)
                {
                    XmlAttribute attr = _doc.CreateAttribute("schemanode");
                    attr.Value = "1";
                    node.Attributes.Append(attr);
                }
            }
            parent = node;
        }

        parent.Attributes.RemoveAll();
        if (appendNodeAttribute)
        {
            XmlAttribute attr = _doc.CreateAttribute("schemanode");
            attr.Value = "1";
            parent.Attributes.Append(attr);
        }
        return parent;
    }

    public override bool Equals(object obj)
    {
        if (obj is XmlFileStreamDocument)
        {
            XmlFileStreamDocument xmlStream = (XmlFileStreamDocument)obj;
            return xmlStream._doc.InnerXml.Equals(_doc.InnerXml);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public bool SetParent(string path)
    {
        path = path.Replace(@"\", "/");
        _parent = _doc.SelectSingleNode("config" + (string.IsNullOrEmpty(path) ? "" : "/" + path));
        if (_parent == null)
        {
            _parent = _doc.SelectSingleNode("config");
            _parent = CreateNode(path, true);
        }
        return _parent != null;
    }

    #region IStream Member

    public IDocumentInfo ConfigFile
    {
        get
        {
            if (_filename == string.Empty)
            {
                return null;
            }

            return new FileSystemDocumentInfo(_filename);
        }
    }

    public bool Save(string path, object obj)
    {
        return SaveProperty(path, obj) != null;
    }

    public bool Save(string path, object obj, object unauthorizedDefalut)
    {
        XmlNode node = SaveProperty(path, obj);
        if (node == null)
        {
            return false;
        }

        XmlAttribute attr = _doc.CreateAttribute("unauthorizeddefault");
        if (unauthorizedDefalut.GetType() == typeof(double))
        {
            attr.Value = ((double)unauthorizedDefalut).ToString(_nhi);
        }
        else if (unauthorizedDefalut.GetType() == typeof(float))
        {
            attr.Value = ((float)unauthorizedDefalut).ToString(_nhi);
        }
        else if (unauthorizedDefalut.GetType() == typeof(decimal))
        {
            attr.Value = ((decimal)unauthorizedDefalut).ToString(_nhi);
        }
        else
        {
            attr.Value = unauthorizedDefalut.ToString();
        }

        node.Attributes.Append(attr);

        return true;
    }

    public bool SaveOrRemoveIfEmpty(string path, object obj)
    {
        if (obj != null && !string.IsNullOrWhiteSpace(obj.ToString()))
        {
            return Save(path, obj);
        }

        return Remove(path);
    }

    public object Load(string path, object defValue)
    {
        XmlNode node = _doc.SelectSingleNode("config/" + path.Replace(@"\", "/") + "[@type]");
        if (node == null)
        {
            return defValue;
        }

        if (node.Attributes["type"].Value != "System.String")
        {
            Type type = Type.GetType(node.Attributes["type"].Value, false, true);
            object obj = Activator.CreateInstance(type);
            if (obj == null)
            {
                return defValue;
            }

            obj = Convert.ChangeType(node.InnerText, type, _nhi);
            return obj;
        }
        else
        {
            return node.InnerText.Replace(_stringReplace);
        }
    }

    public bool Remove(string path)
    {
        XmlNode node = _doc.SelectSingleNode("config/" + path.Replace(@"\", "/") + "[@type]");
        if (node == null)
        {
            return false;
        }

        return node.ParentNode.RemoveChild(node) != null;
    }
    #endregion

    private XmlNode SaveProperty(string path, object obj)
    {
        XmlNode node = CreateNode(path);
        if (node == null)
        {
            return node;
        }

        XmlAttribute attr = _doc.CreateAttribute("type");
        attr.Value = obj.GetType().ToString();
        node.Attributes.Append(attr);

        if (obj.GetType() == typeof(double))
        {
            node.InnerText = ((double)obj).ToString(_nhi);
        }
        else if (obj.GetType() == typeof(float))
        {
            node.InnerText = ((float)obj).ToString(_nhi);
        }
        else if (obj.GetType() == typeof(decimal))
        {
            node.InnerText = ((decimal)obj).ToString(_nhi);
        }
        else
        {
            node.InnerText = obj.ToString();
        }

        return node;
    }
    internal XmlNode ParentXmlNode
    {
        get { return _doc.SelectSingleNode("config"); }
    }
}
