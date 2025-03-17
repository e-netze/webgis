using E.Standard.Azure.CosmoDb;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Plattform;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace E.Standard.CMS.MongoDB;

public class MongoDocumentInfo : IDocumentInfo, IXmlConverter
{
    public MongoDocumentInfo(string path)
    {
        this.PathString = path.ToPlattformPath();
    }

    private string PathString { get; set; }

    #region IDocumentInfo

    private bool? _exists = null;
    public bool Exists
    {
        get
        {
            var cmsItem = MongoStreamDocument.CachedItemds[this.PathString];
            if (cmsItem != null)
            {
                return cmsItem.ItemType == CmsItemType.File;
            }

            if (!_exists.HasValue)
            {
                //var client = new MongoClient(PathString.GetConnetion());
                //var database = client.GetDatabase(PathString.GetDatabase());
                //var collection = database.GetCollection<MongoCmsItem>(PathString.GetCollectionName());

                //string itemPath = PathString.GetNodePath();
                //string cmsId = itemPath.PathToCmsId();

                //var query = collection.AsQueryable<MongoCmsItem>()
                //    .Where(i => i.Path == itemPath && i.CmsId == cmsId);
                //cmsItem = query.FirstOrDefault();

                cmsItem = MongoPathInfo.QueryMongoItem(PathString);

                //var filter = Builders<MongoCmsItem>.Filter.Eq("Path", itemPath);
                //cmsItem = collection.Find(filter).FirstOrDefault();

                _exists = cmsItem != null && cmsItem.ItemType == CmsItemType.File;

                if (cmsItem != null)
                {
                    MongoStreamDocument.CachedItemds.Add(this.PathString, cmsItem);
                }
                else
                {
                    MongoStreamDocument.CachedItemds.Add(this.PathString, new MongoCmsItem(this.PathString) { ItemType = CmsItemType.Unknown });   // Cachen, damit beim nächsten Exists nicht wieder abgefragt wird
                }

                //Console.WriteLine("Exits: " + itemPath);
            }

            return _exists.Value;
        }
    }

    public string Name
    {
        get
        {
            var name = PathString.GetNodePath();
            if (name.Contains("/"))
            {
                name = name.Substring(name.LastIndexOf("/") + 1);
            }

            // Bei FileInfo ist Extension beim Namen dabei!?
            //if (name.Contains("."))
            //    name = name.Substring(0, name.LastIndexOf("."));

            return name;
        }
    }

    public string Title
    {
        get
        {
            var name = this.Name;

            if (/*name.Contains(".") && */name.LastIndexOf(".") > 0)
            {
                name = name.Substring(0, name.LastIndexOf(".") - 1);
            }

            return name;
        }
    }

    public string FullName => this.PathString;

    public string Extension
    {
        get
        {
            var extension = PathString.GetNodePath();
            if (extension.Contains("/"))
            {
                extension = extension.Substring(extension.LastIndexOf("/") + 1);
            }

            if (extension.Contains("."))
            {
                return extension.Substring(extension.LastIndexOf("."));
            }
            else
            {
                return String.Empty;
            }
        }
    }

    public IPathInfo Directory
    {
        get
        {
            var path = this.PathString;
            path = path.Replace(@"\", "/");

            if (path.Contains("/"))
            {
                return new MongoPathInfo(path.Substring(0, path.LastIndexOf("/")));
            }
            else
            {
                throw new Exception("Document has no root/parent element: " + path);
            }
        }
    }

    public IDocumentInfo CopyTo(string path)
    {
        return CopyTo(path, false);
    }

    public IDocumentInfo CopyTo(string path, bool overwrite)
    {
        var client = new MongoClient(PathString.GetConnetion());
        var database = client.GetDatabase(PathString.GetDatabase());
        var collection = database.GetCollection<MongoCmsItem>(PathString.GetCollectionName());

        string itemPath = PathString.GetNodePath();
        string cmsId = itemPath.PathToCmsId();

        var query = collection.AsQueryable<MongoCmsItem>()
                    .Where(i => i.Path == itemPath && i.CmsId == cmsId);
        var cmsItem = CosmoDbHelper.ExecuteWithRetries(() => query.FirstOrDefault());

        //var filter = Builders<MongoCmsItem>.Filter.Eq("Path", itemPath);
        //var cmsItem = collection.Find(filter).FirstOrDefault();

        if (cmsItem == null)
        {
            throw new Exception("Unknown item: " + itemPath);
        }

        var targetPath = path.GetNodePath();
        var targetCmsId = targetPath.PathToCmsId();

        var targetQuery = collection.AsQueryable<MongoCmsItem>()
                    .Where(i => i.Path == targetPath && i.CmsId == targetCmsId);
        var targetItem = targetQuery.FirstOrDefault();

        //var targetFilter = Builders<MongoCmsItem>.Filter.Eq("Path", targetPath);
        //var targetItem = collection.Find(filter).FirstOrDefault();

        if (targetPath != null && overwrite == false)
        {
            throw new Exception("Item already exists: " + targetPath);
        }

        if (overwrite && targetItem != null)
        {
            targetItem.Data = cmsItem.Data;
            var result = CosmoDbHelper.ExecuteWithRetries(() => collection.ReplaceOne(i => i.Path == targetPath && i.CmsId == targetCmsId, targetItem));

            if (result.ModifiedCount == 0)
            {
                throw new Exception("Can't replace item: " + targetItem);
            }
        }
        else
        {
            targetItem = new MongoCmsItem(targetPath)
            {
                Data = cmsItem.Data
            };

            CosmoDbHelper.ExecuteWithRetries(() =>
            {
                collection.InsertOne(targetItem);
                return true;
            });
        }

        return new MongoDocumentInfo(Extensions.PathString(PathString.GetConnetion(), PathString.GetDatabase(), PathString.GetConnetion(), targetPath));
    }

    public void Delete()
    {
        var client = new MongoClient(PathString.GetConnetion());
        var database = client.GetDatabase(PathString.GetDatabase());
        var collection = database.GetCollection<MongoCmsItem>(PathString.GetCollectionName());

        string itemPath = PathString.GetNodePath();
        string cmsId = itemPath.PathToCmsId();

        var result = CosmoDbHelper.ExecuteWithRetries(() => collection.DeleteOne(i => i.CmsId == cmsId && i.Path == itemPath));

        //var filter = Builders<MongoCmsItem>.Filter.Eq("Path", itemPath);
        //var result = collection.DeleteOne(filter);

        if (result.DeletedCount == 0)
        {
            throw new Exception("Can't delete item: " + itemPath);
        }

        MongoStreamDocument.CachedItemds.Remove(this.PathString);
    }

    public void Write(string data)
    {
        var client = new MongoClient(PathString.GetConnetion());
        var database = client.GetDatabase(PathString.GetDatabase());
        var collection = database.GetCollection<MongoCmsItem>(PathString.GetCollectionName());

        string itemPath = PathString.GetNodePath();
        string cmsId = itemPath.PathToCmsId();

        var cmsItem = CosmoDbHelper.ExecuteWithRetries(() =>
         {
             var query = collection.AsQueryable<MongoCmsItem>()
                         .Where(i => i.Path == itemPath && i.CmsId == cmsId);
             return query.FirstOrDefault();
         });

        //var filter = Builders<MongoCmsItem>.Filter.Eq("Path", itemPath);
        //var item = collection.Find(filter).FirstOrDefault();

        if (cmsItem != null)
        {
            cmsItem.DataString = data;
            if (CosmoDbHelper.ExecuteWithRetries(() => collection.ReplaceOne(i => i.Path == itemPath && i.CmsId == cmsId, cmsItem)).ModifiedCount == 0)
            {
                throw new Exception("Can't replace item: " + cmsItem);
            }
        }
        else
        {
            cmsItem = new MongoCmsItem(itemPath)
            {
                DataString = data
            };

            CosmoDbHelper.ExecuteWithRetries(() =>
            {
                collection.InsertOne(cmsItem);
                return true;
            });
        }

        MongoStreamDocument.CachedItemds.Remove(this.PathString);
    }
    public string ReadAll()
    {
        //var client = new MongoClient(PathString.GetConnetion());
        //var database = client.GetDatabase(PathString.GetDatabase());
        //var collection = database.GetCollection<MongoCmsItem>(PathString.GetCollectionName());

        //string itemPath = PathString.GetNodePath();
        //string cmsId = itemPath.PathToCmsId();

        //var query = collection.AsQueryable<MongoCmsItem>()
        //            .Where(i => i.Path == itemPath && i.CmsId == cmsId);
        //var cmsItem = query.FirstOrDefault();

        var cmsItem = MongoPathInfo.QueryMongoItem(PathString);

        //var filter = Builders<MongoCmsItem>.Filter.Eq("Path", itemPath);
        //var item = collection.Find(filter).FirstOrDefault();

        if (cmsItem == null)
        {
            return String.Empty;
        }

        return cmsItem.DataString;
    }

    #endregion

    #region IXmlConverter

    public string ReadAllAsXmlString()
    {
        //var client = new MongoClient(PathString.GetConnetion());
        //var database = client.GetDatabase(PathString.GetDatabase());
        //var collection = database.GetCollection<MongoCmsItem>(PathString.GetCollectionName());

        //string itemPath = PathString.GetNodePath();
        //string cmsId = itemPath.PathToCmsId();

        //var query = collection.AsQueryable<MongoCmsItem>()
        //            .Where(i => i.Path == itemPath && i.CmsId == cmsId);
        //var cmsItem = query.FirstOrDefault();

        var cmsItem = MongoPathInfo.QueryMongoItem(PathString);

        //var filter = Builders<MongoCmsItem>.Filter.Eq("Path", itemPath);
        //var item = collection.Find(filter).FirstOrDefault();

        if (cmsItem == null)
        {
            return String.Empty;
        }

        if (cmsItem.DataString.Trim().StartsWith("<"))
        {
            return cmsItem.DataString;
        }

        MemoryStream ms = new MemoryStream();
        using (XmlTextWriter xWriter = new XmlTextWriter(ms, Encoding.UTF8))
        {

            xWriter.WriteStartDocument();
            xWriter.WriteStartElement("config");

            var dict = cmsItem.DataDictionary;
            if (dict != null)
            {
                foreach (var key in dict.Keys)
                {
                    if (dict[key] == null)
                    {
                        continue;
                    }

                    xWriter.WriteStartElement(key);
                    var type = dict[key].GetType();
                    var val = dict[key].ToString();

                    if (type.Equals(typeof(System.Int64)))
                    {
                        long longVal = Convert.ToInt64(dict[key]);
                        if (longVal >= int.MinValue && longVal <= int.MaxValue)
                        {
                            type = typeof(System.Int32);
                        }
                    }
                    if (type.Equals(typeof(System.Double)) || type.Equals(typeof(System.Single)))
                    {
                        val = val?.Replace(",", ".");
                    }

                    xWriter.WriteAttributeString("type", type.ToString());
                    xWriter.WriteString(val);
                    xWriter.WriteEndElement();
                }
            }

            xWriter.WriteEndElement();
            xWriter.WriteEndDocument();
            xWriter.Flush();

            ms.Position = 0;
            using (StreamReader sr = new StreamReader(ms))
            {
                return sr.ReadToEnd();
            }
        }
    }

    public bool WriteXmlData(string xml, bool overrideExisting = true)
    {
        var client = new MongoClient(PathString.GetConnetion());
        var database = client.GetDatabase(PathString.GetDatabase());
        var collection = database.GetCollection<MongoCmsItem>(PathString.GetCollectionName());

        string itemPath = PathString.GetNodePath();
        string cmsId = itemPath.PathToCmsId();

        var cmsItem = CosmoDbHelper.ExecuteWithRetries(() =>
          {
              var query = collection.AsQueryable<MongoCmsItem>()
                                    .Where(i => i.Path == itemPath && i.CmsId == cmsId);
              return query.FirstOrDefault();
          });


        if (overrideExisting == false && cmsItem != null)
        {
            return false;
        }

        if (!this.PathString.ToLower().EndsWith(".itemorder.xml") &&
            (
            this.PathString.ToLower().EndsWith(".xml") ||
            this.PathString.ToLower().EndsWith(".link")
            ))
        {
            NumberFormatInfo nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            Dictionary<string, object> dict = new Dictionary<string, object>();

            foreach (XmlNode node in doc.SelectNodes("config/*[@type]"))
            {
                if (node.Attributes["type"].Value != "System.String")
                {
                    Type type = System.Type.GetType(node.Attributes["type"].Value, false, true);
                    object obj = Activator.CreateInstance(type);
                    if (obj != null)
                    {
                        obj = Convert.ChangeType(node.InnerText, type, nhi);
                        dict.Add(node.Name, obj);
                    }
                }
                else
                {
                    dict.Add(node.Name, node.InnerText);
                }
            }

            if (cmsItem == null)
            {
                cmsItem = new MongoCmsItem(itemPath, dict);
                CosmoDbHelper.ExecuteWithRetries(() =>
                {
                    collection.InsertOne(cmsItem);
                    return true;
                });
            }
            else
            {
                cmsItem.DataDictionary = dict;
                if (CosmoDbHelper.ExecuteWithRetries(() => collection.ReplaceOne(i => i.Path == itemPath && i.CmsId == cmsId, cmsItem)).ModifiedCount == 0)
                {
                    throw new Exception("Can't replace item: " + cmsItem);
                }
            }
        }
        else
        {
            if (cmsItem == null)
            {
                cmsItem = new MongoCmsItem(itemPath);
                cmsItem.DataString = xml;
                CosmoDbHelper.ExecuteWithRetries(() =>
                {
                    collection.InsertOne(cmsItem);
                    return true;
                });
            }
            else
            {
                cmsItem.DataString = xml;
                if (CosmoDbHelper.ExecuteWithRetries(() => collection.ReplaceOne(i => i.Path == itemPath && i.CmsId == cmsId, cmsItem)).ModifiedCount == 0)
                {
                    throw new Exception("Can't replace item: " + cmsItem);
                }
            }
        }

        MongoStreamDocument.CachedItemds.Remove(this.PathString);

        return true;
    }

    #endregion

    #region Static Members

    //private static ConcurrentDictionary<string, bool> _existsCache = new ConcurrentDictionary<string, bool>();

    //public static void AddToExistsCache(string path, bool exists)
    //{
    //    _existsCache[path] = exists;
    //}

    //private static bool? ExitsFromCache(string path)
    //{
    //    if (_existsCache.ContainsKey(path))
    //        return _existsCache[path];

    //    return null;
    //}

    //public static void ClearExistsCache()
    //{
    //    _existsCache.Clear();
    //}

    #endregion
}
