using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using System.Collections.Generic;
using System.Xml;

namespace E.Standard.CMS.Core.Schema;

public class ItemOrder
{
    private IPathInfo _di;
    private IDocumentInfo _fi;
    private OrderedItems _orderedItems = new OrderedItems();
    private bool _fast = false;

    public ItemOrder(string folder)
    {
        _di = DocumentFactory.PathInfo(folder);
        _fi = DocumentFactory.DocumentInfo(folder + @"/.itemorder.xml");
        Load();
    }
    public ItemOrder(string folder, bool fast)
    {
        _di = DocumentFactory.PathInfo(folder);
        _fi = DocumentFactory.DocumentInfo(folder + @"/.itemorder.xml");
        _fast = fast;
        Load();
    }

    public ItemOrder(string folder, string[] items)
    {
        _di = DocumentFactory.PathInfo(folder);
        _fi = DocumentFactory.DocumentInfo(folder + @"/.itemorder.xml");
        Items = items;
    }
    public bool Exists
    {
        get
        {
            if (_fi == null)
            {
                return false;
            }

            return _fi.Exists;
        }
    }
    private void Load()
    {
        try
        {
            if (_fi.Exists)
            {
                var doc = new XmlDocumentWrapper();
                doc.Load(_fi.FullName);

                foreach (XmlNode itemNode in doc.SelectNodes("items/item[@name]"))
                {
                    string name = itemNode.Attributes["name"].Value;
                    string path = _di.FullName + @"\" + name;
                    if (_fast)
                    {
                        _orderedItems.Add(name.ToLower());
                    }
                    else
                    {
                        var fi = DocumentFactory.DocumentInfo(path);
                        if (fi.Exists)
                        {
                            _orderedItems.Add(name.ToLower());
                            continue;
                        }
                        var di = DocumentFactory.PathInfo(path);
                        if (di.Exists)
                        {
                            _orderedItems.Add(name.ToLower());
                            continue;
                        }
                    }
                }
                //if (!_fast)
                Refresh();
            }
            else
            {
                Refresh();
            }
        }
        catch { }
    }
    private void Refresh()
    {
        foreach (var di in _di.GetDirectories())
        {
            _orderedItems.Add(di.Name.ToLower());
        }
        foreach (var fi in _di.GetFiles())
        {
            _orderedItems.Add(fi.Name.ToLower());
        }
    }

    public string[] Items
    {
        get
        {
            return _orderedItems.ToArray();
        }
        set
        {
            _orderedItems = new OrderedItems();
            foreach (string item in value)
            {
                _orderedItems.Add(item.ToLower());
            }
            Refresh();
        }
    }
    public void Save()
    {
        var doc = new XmlDocumentWrapper();
        XmlNode itemsNode = doc.CreateElement("items");
        doc.AppendChild(itemsNode);

        foreach (string item in _orderedItems)
        {
            XmlNode itemNode = doc.CreateElement("item");
            XmlAttribute attr = doc.CreateAttribute("name");
            attr.Value = item;
            itemNode.Attributes.Append(attr);
            itemsNode.AppendChild(itemNode);
        }

        doc.Save(_fi.FullName);
    }

    private class OrderedItems : List<string>
    {
        public new void Add(string item)
        {
            if (Contains(item) ||
                item == null ||
                item.StartsWith(".") && item.ToLower() != ".linktemplate.xml" ||
                item.ToLower().EndsWith(".acl"))
            {
                return;
            }

            base.Add(item);
        }

        public void Add(List<string> items)
        {
            foreach (string item in items)
            {
                Add(item);
            }
        }
    }
}
