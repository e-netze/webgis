using System;
using System.Xml;

namespace E.Standard.Configuration;

public class CmsReplaceConfig
{
    private readonly XmlDocument _replaceDoc;

    public CmsReplaceConfig(string filename)
    {
        _replaceDoc = new XmlDocument();
        _replaceDoc.Load(filename);
    }

    public void ReplaceInXmlDocument(XmlDocument doc)
    {
        if (_replaceDoc == null || doc == null)
        {
            return;
        }

        foreach (XmlNode node in doc.ChildNodes)
        {
            ReplaceInXmlDocument(node);
        }
    }

    private void ReplaceInXmlDocument(XmlNode node)
    {
        if (node == null || _replaceDoc == null || node.ChildNodes == null)
        {
            return;
        }

        if (node.ChildNodes.Count == 0)
        {
            if (node.Name == "_linkuri" ||
                (node is XmlText && node.ParentNode != null && node.ParentNode.Name == "_linkuri"))
            {
                // Bei Links nicht ersetzen!!!
            }
            else
            {
                #region Replace

                foreach (XmlElement row in _replaceDoc.SelectNodes("DocumentElement/Replace"))
                {
                    var from = _replaceDoc.SelectSingleNode("From")?.InnerText;
                    var to = _replaceDoc.SelectSingleNode("From")?.InnerText;

                    if (String.IsNullOrEmpty(from) || String.IsNullOrEmpty(to))
                    {
                        continue;
                    }

                    node.InnerText = node.InnerText.Replace(from, to);
                }

                #endregion Replace
            }
        }
        else
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                ReplaceInXmlDocument(child);
            }
        }
    }

    #region Static Members

    static public CmsReplaceConfig TryLoad(string configFile)
    {
        try
        {
            return new CmsReplaceConfig(configFile);
        }
        catch { }

        return null;
    }

    #endregion
}