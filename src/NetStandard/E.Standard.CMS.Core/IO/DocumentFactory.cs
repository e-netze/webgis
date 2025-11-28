using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.Platform;
using E.Standard.Security.Cryptography.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Xml;

namespace E.Standard.CMS.Core.IO;

static public class DocumentFactory
{
    static public Dictionary<string, Type> DocumentTypes = new Dictionary<string, Type>() { { "", typeof(XmlFileStreamDocument) } };
    static public Dictionary<string, Type> DocumentInfoTypes = new Dictionary<string, Type>() { { "", typeof(FileSystemDocumentInfo) } };
    static public Dictionary<string, Type> PathInfoTypes = new Dictionary<string, Type>() { { "", typeof(FileSystemPathInfo) } };
    static public Dictionary<string, bool> CanImportValues = new Dictionary<string, bool>() { { "", true } };
    static public Dictionary<string, bool> CanClearValues = new Dictionary<string, bool>() { { "", true } };

    static internal List<string> TypeNames = new List<string>();

    private static ICryptoService _crypto = null;

    static public void Init(ICryptoService cryptoService)
    {
        _crypto = cryptoService;

        TypeNames.Clear();
        TypeNames.AddRange(DocumentInfoTypes.Keys.Where(k => !string.IsNullOrWhiteSpace(k)));
    }

    static public IStreamDocument Open(string path, NameValueCollection stringReplace = null)
    {
        Type documentType = null;
        foreach (var typeName in TypeNames)
        {
            if (path.Contains(typeName))
            {
                documentType = DocumentTypes[typeName];
                break;
            }
        }
        if (documentType == null)
        {
            documentType = DocumentTypes[""];
        }

        IStreamDocument document = (IStreamDocument)Activator.CreateInstance(documentType);
        document.Init(path, stringReplace);
        return document;
    }

    static public IStreamDocument New(string connectionString)
    {
        Type documentType = null;
        foreach (var typeName in TypeNames)
        {
            if (connectionString.Contains(typeName))
            {
                documentType = DocumentTypes[typeName];
                break;
            }
        }
        if (documentType == null)
        {
            documentType = DocumentTypes[""];
        }

        IStreamDocument document = (IStreamDocument)Activator.CreateInstance(documentType);
        document.Init(string.Empty);
        return document;
    }

    static public IDocumentInfo DocumentInfo(string path)
    {
        Type documentInfoType = null;
        foreach (var typeName in TypeNames)
        {
            if (path.Contains(typeName))
            {
                documentInfoType = DocumentInfoTypes[typeName];
                break;
            }
        }
        if (documentInfoType == null)
        {
            documentInfoType = DocumentInfoTypes[""];
        }

        IDocumentInfo documentInfo = (IDocumentInfo)Activator.CreateInstance(documentInfoType, new object[] { path });
        return documentInfo;
    }

    static public IPathInfo PathInfo(string path)
    {
        Type pathInfoType = null;
        foreach (var typeName in TypeNames)
        {
            if (path.Contains(typeName))
            {
                pathInfoType = PathInfoTypes[typeName];
                break;
            }
        }
        if (pathInfoType == null)
        {
            pathInfoType = PathInfoTypes[""];
        }

        IPathInfo pathInfo = (IPathInfo)Activator.CreateInstance(pathInfoType, new object[] { path });
        return pathInfo;
    }

    static public bool CanImport(string path)
    {
        foreach (var key in CanImportValues.Keys)
        {
            if (!string.IsNullOrWhiteSpace(key) && path.Contains(key))
            {
                return CanImportValues[key];
            }
        }

        return CanImportValues[""];
    }

    static public bool CanClear(string path)
    {
        foreach (var key in CanClearValues.Keys)
        {
            if (!string.IsNullOrWhiteSpace(key) && path.Contains(key))
            {
                return CanClearValues[key];
            }
        }

        return CanClearValues[""];
    }

    static public string EncryptString(string input) => _crypto == null ? input : _crypto.EncryptTextDefault(input, E.Standard.Security.Cryptography.CryptoResultStringType.Hex);
    static public string DecryptString(string cypherText) => _crypto == null ? cypherText : _crypto.DecryptTextDefault(cypherText);

    static public void ModifySchema(string root, XmlDocument schema)
    {
        if (SystemInfo.IsLinux)
        {
            // Linux schema nodes are case-sensitive,
            // so we need to convert all schema-node names and urifilterlinks
            // to lowercase
            var pathInfo = PathInfo(root);
            if (typeof(FileSystemPathInfo).Equals(pathInfo?.GetType()))
            {
                // all schema-node[names, urifilterlinks] to lowsercase

                foreach (var nodeName in new string[] { "schema-node", "schema-link" })
                {
                    foreach (var node in schema.SelectNodes($"//{nodeName}"))
                    {
                        var schemaNode = node as XmlNode;
                        if (schemaNode != null)
                        {
                            foreach (var attributeName in new string[] { "name", "urifilterlinks" })
                            {
                                if (!string.IsNullOrEmpty(schemaNode.Attributes[attributeName]?.Value))
                                {
                                    schemaNode.Attributes[attributeName].Value = schemaNode.Attributes[attributeName].Value.ToLowerInvariant();
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public static object PathInfo(object p)
    {
        throw new NotImplementedException();
    }
}
