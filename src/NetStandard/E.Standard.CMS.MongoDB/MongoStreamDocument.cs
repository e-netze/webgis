using E.Standard.Azure.CosmoDb;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Plattform;
using E.Standard.Extensions.Text;
using E.Standard.ThreadsafeClasses;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace E.Standard.CMS.MongoDB;

public class MongoStreamDocument : IStreamDocument
{
    private NameValueCollection _stringReplace = null;

    #region Properties

    private Dictionary<string, object> Data = new Dictionary<string, object>();

    private string PathString { get; set; }

    #endregion

    #region IStreamDocument

    public IDocumentInfo ConfigFile
    {
        get
        {
            if (!String.IsNullOrWhiteSpace(this.PathString))
            {
                return new MongoDocumentInfo(this.PathString);
            }

            return null;
        }
    }

    internal static TemporaryCache<MongoCmsItem> CachedItemds = new TemporaryCache<MongoCmsItem>(3000);

    public event ParseEncryptedValue OnParseBeforeEncryptValue = null;
    public string FireParseBoforeEncryptValue(string value)
    {
        string refValue = value;

        OnParseBeforeEncryptValue?.Invoke(ref refValue);

        return refValue;
    }

    public void Init(string path = "", NameValueCollection stringReplace = null)  // connection=mongodb://user:pwd@server:port;db=cms;collection=test1;path=.......
    {
        this.PathString = path.ToPlattformPath();
        _stringReplace = stringReplace;

        if (!String.IsNullOrWhiteSpace(path))
        {
            MongoCmsItem cmsItem = CachedItemds[this.PathString];

            if (cmsItem == null)
            {
                var client = new MongoClient(path.GetConnetion());
                var database = client.GetDatabase(path.GetDatabase());
                var collection = database.GetCollection<MongoCmsItem>(path.GetCollectionName());

                string itemPath = path.GetNodePath();
                string cmsId = itemPath.PathToCmsId();

                cmsItem = CosmoDbHelper.ExecuteWithRetries(() =>
                {
                    var query = collection.AsQueryable<MongoCmsItem>()
                                            .Where(i => i.Path == itemPath && i.CmsId == cmsId);

                    return query.FirstOrDefault();
                });



                //var filter = Builders<MongoCmsItem>.Filter.Eq("Path", itemPath);
                //cmsItem = collection.Find(filter).FirstOrDefault();

                if (cmsItem != null)
                {
                    CachedItemds.Add(this.PathString, cmsItem);
                }

                //Console.WriteLine("Init: " + itemPath);
            }

            if (cmsItem != null)
            {
                this.Data = cmsItem.DataDictionary;
            }
        }
    }

    public NameValueCollection StringReplace => _stringReplace;

    public object Load(string path, object defValue)
    {
        if (Data.ContainsKey(path) && Data[path] != null)
        {
            object value = Data[path];
            if (defValue != null && !defValue.GetType().Equals(Data[path].GetType()))
            {
                value = Convert.ChangeType(value, defValue.GetType());
            }

            if (value is string)
            {
                value = ((string)value).Replace(_stringReplace);
            }

            return value;
        }
        return defValue;
    }

    public bool Remove(string path)
    {
        if (Data.ContainsKey(path))
        {
            Data.Remove(path);
            return true;
        }

        return false;
    }

    public bool Save(string path, object obj)
    {
        if (obj == null)
        {
            Remove(path);
        }
        else
        {
            Data[path] = obj;
        }
        return true;
    }

    public bool Save(string path, object obj, object unauthorizedDefalut)
    {
        Data[path] = obj;
        Data[path + "-unauthorizeddefault"] = unauthorizedDefalut;
        return true;
    }

    public bool SaveOrRemoveIfEmpty(string path, object obj)
    {
        if (obj != null && !String.IsNullOrWhiteSpace(obj.ToString()))
        {
            return Save(path, obj);
        }

        return Remove(path);
    }

    public void SaveDocument()
    {
        throw new NotImplementedException();
    }

    public void SaveDocument(string path)
    {
        MongoDocumentInfo fi = new MongoDocumentInfo(path);
        if (!fi.Directory.Exists)
        {
            fi.Directory.Create();
        }

        var client = new MongoClient(path.GetConnetion());
        var database = client.GetDatabase(path.GetDatabase());
        var collection = database.GetCollection<MongoCmsItem>(path.GetCollectionName());

        string itemPath = path.GetNodePath();
        string cmsId = itemPath.PathToCmsId();

        var query = collection.AsQueryable<MongoCmsItem>()
            .Where(i => i.Path == itemPath && i.CmsId == cmsId);
        var cmsItem = query.FirstOrDefault();

        //var filter = Builders<MongoCmsItem>.Filter.Eq("Path", itemPath);
        //var cmsItem = collection.Find(filter).FirstOrDefault();

        if (cmsItem != null)  // Update
        {
            cmsItem.DataDictionary = this.Data;

            CosmoDbHelper.ExecuteWithRetries(() =>
            {
                var update = Builders<MongoCmsItem>.Update.Set("Data", cmsItem.Data);
                return collection.UpdateOne(i => i.Path == itemPath && i.CmsId == cmsId, update);
            });


            //collection.UpdateOne(filter, update);
        }
        else  // Insert
        {
            CosmoDbHelper.ExecuteWithRetries(() =>
            {
                cmsItem = new MongoCmsItem(itemPath, this.Data);
                collection.InsertOne(cmsItem);
                return true;
            });
        }

        //Console.WriteLine("File Created: " + itemPath);
        CachedItemds.Add(path, cmsItem);
    }

    public bool SetParent(string path)
    {
        throw new NotImplementedException("SetParent is not implementet in MongoStreamDocument");
    }

    #endregion
}
