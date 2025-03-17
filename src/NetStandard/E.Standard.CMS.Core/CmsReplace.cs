using E.Standard.CMS.Core.Extensions;
using System;
using System.Collections.Specialized;
using System.Data;
using System.Xml;

namespace E.Standard.CMS.Core;

public class CmsReplace
{
    private DataTable _globalReplace = null;

    public CmsReplace(DataTable replaceTable)
    {
        _globalReplace = replaceTable;
    }

    public CmsReplace()
    {
        ApplyEmptyTable();
    }

    public void Add(string from, string to)
    {
        if (from == null || to == null)
        {
            return;
        }

        var row = _globalReplace.NewRow();
        row["From"] = from;
        row["To"] = to;
        _globalReplace.Rows.Add(row);
    }

    public void ReplaceInXmlDocument(XmlDocument doc)
    {
        if (_globalReplace == null || doc == null)
        {
            return;
        }

        foreach (XmlNode node in doc.ChildNodes)
        {
            ReplaceInXmlDocument(node);
        }
    }

    public string ReplaceSecrets(string str)
    {
        if (_globalReplace != null)
        {
            foreach (DataRow row in _globalReplace.Rows)
            {
                string from = row["From"]?.ToString(), to = row["To"]?.ToString();
                if (from.IsSecretPlaceholder())
                {
                    str = str.Replace(from, to);
                }
            }
        }

        return str;
    }

    public bool HasItems => _globalReplace?.Rows != null && _globalReplace.Rows.Count > 0;

    public NameValueCollection ToCollection()
    {
        var coll = new NameValueCollection();

        if (HasItems)
        {
            foreach (DataRow row in _globalReplace.Rows)
            {
                coll[row["From"].ToString()] = row["To"].ToString();
            }
        }

        return coll;
    }

    #region Helper

    private void ApplyEmptyTable()
    {
        _globalReplace = CreateEmptySchemaTable();
    }

    private void ReplaceInXmlDocument(XmlNode node)
    {
        if (node == null || _globalReplace == null || node.ChildNodes == null)
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

                foreach (DataRow row in _globalReplace.Rows)
                {
                    if (row["From"] == DBNull.Value || row["To"] == DBNull.Value)
                    {
                        continue;
                    }

                    string from = (string)row["From"];
                    string to = (string)row["To"];
                    if (String.IsNullOrEmpty(from))
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

    #endregion

    #region Static 

    public static DataTable CreateEmptySchemaTable()
    {
        var table = new DataTable("Replace");

        table.Columns.Add(new DataColumn("From", typeof(string)));
        table.Columns.Add(new DataColumn("To", typeof(string)));

        return table;
    }

    #endregion
}