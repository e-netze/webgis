using E.Standard.CMS.Core.IO.Abstractions;
using System;
using System.Xml;

namespace E.Standard.CMS.Core.IO;

public class XmlDocumentWrapper
{
    private XmlDocument _doc = new XmlDocument();

    public XmlDocumentWrapper()
    {

    }

    public XmlElement CreateElement(string name)
    {
        return _doc.CreateElement(name);
    }

    public XmlAttribute CreateAttribute(string name)
    {
        return _doc.CreateAttribute(name);
    }

    public XmlNode AppendChild(XmlNode newChild)
    {
        return _doc.AppendChild(newChild);
    }

    public XmlNodeList SelectNodes(string xpath)
    {
        return _doc.SelectNodes(xpath);
    }

    public XmlNode SelectSingleNode(string xpath)
    {
        return _doc.SelectSingleNode(xpath);
    }

    public void Save(string filename)
    {
        Type documentInfoType = null;
        foreach (var typeName in DocumentFactory.TypeNames)
        {
            if (filename.Contains(typeName))
            {
                documentInfoType = DocumentFactory.DocumentInfoTypes[typeName];
                break;
            }
        }
        if (documentInfoType == null)
        {
            _doc.Save(filename);
        }
        else
        {
            IDocumentInfo documentInfo = (IDocumentInfo)Activator.CreateInstance(documentInfoType, new object[] { filename });
            documentInfo.Write(_doc.OuterXml);
        }
    }

    public void Load(string filename)
    {
        Type documentInfoType = null;
        foreach (var typeName in DocumentFactory.TypeNames)
        {
            if (filename.Contains(typeName))
            {
                documentInfoType = DocumentFactory.DocumentInfoTypes[typeName];
                break;
            }
        }
        if (documentInfoType == null)
        {
            _doc.Load(filename);
        }
        else
        {
            IDocumentInfo documentInfo = (IDocumentInfo)Activator.CreateInstance(documentInfoType, new object[] { filename });
            string xml = documentInfo is IXmlConverter ? ((IXmlConverter)documentInfo).ReadAllAsXmlString() : documentInfo.ReadAll();

            if (!string.IsNullOrWhiteSpace(xml))
            {
                _doc.LoadXml(xml);
            }
        }

    }
}
