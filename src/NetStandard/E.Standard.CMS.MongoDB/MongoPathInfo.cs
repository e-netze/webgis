using E.Standard.Azure.CosmoDb;
using E.Standard.CMS.Core.Abstractions;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Plattform;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace E.Standard.CMS.MongoDB;

public class MongoPathInfo : IPathInfo, IDatabasePath, IPathInfo2
{
    private static ConcurrentDictionary<string, IEnumerable<MongoCmsItem>> _cachedItems = new ConcurrentDictionary<string, IEnumerable<MongoCmsItem>>();

    public MongoPathInfo(string path)
    {
        path = path.ToPlattformPath();
        while (path.EndsWith("/"))
        {
            path = path.Substring(0, path.Length - 1);
        }

        this.PathString = path;
    }

    private string PathString { get; set; }

    #region IDatabasePath

    public void CreateDatabase()
    {
        var client = new MongoClient(PathString.GetConnetion());
        var database = client.GetDatabase(PathString.GetDatabase());

        var collectionName = PathString.GetCollectionName();
        if (!database.ListCollectionNames().ToList().Contains(collectionName))
        {
            database.CreateCollection(collectionName);
            var collection = database.GetCollection<MongoCmsItem>(collectionName);

            //var indexPath = Builders<CmsItem>.IndexKeys.Ascending("Path");
            //var indexParemt = Builders<CmsItem>.IndexKeys.Ascending("Parent");

            var notificationLogBuilder = Builders<MongoCmsItem>.IndexKeys;
            var indexModel = new CreateIndexModel<MongoCmsItem>(notificationLogBuilder.Ascending(x => x.Path));
            collection.Indexes.CreateOne(indexModel);
            indexModel = new CreateIndexModel<MongoCmsItem>(notificationLogBuilder.Ascending(x => x.Parent));
            collection.Indexes.CreateOne(indexModel);
        }
    }

    async public Task<bool> DeleteDatabase(IConsoleOutputStream outStream)
    {
        var client = new MongoClient(PathString.GetConnetion());
        var database = client.GetDatabase(PathString.GetDatabase());
        var collection = database.GetCollection<MongoCmsItem>(PathString.GetCollectionName());
        string itemPath = PathString.GetNodePath();
        string cmsId = itemPath.PathToCmsId();




        //var items = collection.Find(i => i.CmsId == cmsId).Limit(1).ToList();
        //if (items.Count > 0)
        //{
        //    var deleteResult = await collection.DeleteManyAsync(i => i.CmsId == cmsId);

        //    return deleteResult.DeletedCount > 0;
        //}

        long counter = 0;
        // Delete Blocks of 10: Avoid => {"Errors":["Request rate is large"]}
        while (true)
        {

            var items = CosmoDbHelper.ExecuteWithRetries(() =>
                collection.Find(i => i.CmsId == cmsId).Limit(25).ToList()
            );

            if (items.Count == 0)
            {
                break;
            }

            var ids = items.Select(i => i.Id);
            //var filter = Builders<MongoCmsItem>.Filter.AnyIn(i => i.Id, ids);

            var deleteResult = await CosmoDbHelper.ExecuteWithRetriesAsync(async () =>
                    await collection.DeleteManyAsync(i => i.CmsId == cmsId && ids.Contains(i.Id))
                );

            if (outStream != null)
            {
                counter += deleteResult.DeletedCount;
                outStream.WriteLine($"... {counter}");
            }
        }

        MongoStreamDocument.CachedItemds.Clear();
        return true;
    }

    #endregion

    #region IPathInfo

    private bool? _exists = null;
    public bool Exists
    {
        get
        {
            var cmsItem = MongoStreamDocument.CachedItemds[this.PathString];
            if (cmsItem != null)
            {
                return cmsItem.ItemType == CmsItemType.Directory;
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

                string itemPath = PathString.GetNodePath();
                cmsItem = MongoPathInfo.QueryMongoItem(PathString);

                //var filter = Builders<MongoCmsItem>.Filter.Eq("Path", itemPath);
                //cmsItem = collection.Find(filter).FirstOrDefault();

                _exists = cmsItem != null && cmsItem.ItemType == CmsItemType.Directory;

                if (cmsItem != null)
                {
                    MongoStreamDocument.CachedItemds.Add(this.PathString, cmsItem);
                }
                else
                {
                    MongoStreamDocument.CachedItemds.Add(this.PathString, new MongoCmsItem(/*this.PathString*/itemPath) { ItemType = CmsItemType.Unknown });   // Cachen, damit beim nächsten Exists nicht wieder abgefragt wird
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

            return name;
        }
    }

    public string FullName => this.PathString;

    public IPathInfo Parent
    {
        get
        {
            if (this.PathString.Contains("/") && this.PathString.LastIndexOf("/") > this.PathString.IndexOf("path="))
            {
                string path = this.PathString.Substring(0, this.PathString.LastIndexOf("/"));
                return new MongoPathInfo(path);
            }

            return null;
        }
    }


    public void Create()
    {
        while (this.Parent != null)
        {
            if (!this.Parent.Exists)
            {
                this.Parent.Create();
            }
            else
            {
                break;
            }
        }

        var client = new MongoClient(PathString.GetConnetion());
        var database = client.GetDatabase(PathString.GetDatabase());
        var collection = database.GetCollection<MongoCmsItem>(PathString.GetCollectionName());
        string itemPath = PathString.GetNodePath();

        var cmsItem = new MongoCmsItem(itemPath)
        {
            ItemType = CmsItemType.Directory
        };

        CosmoDbHelper.ExecuteWithRetries(() => { collection.InsertOne(cmsItem); return true; });

        //Console.WriteLine("Created: " + cmsItem.Path);

        MongoStreamDocument.CachedItemds.Add(PathString, cmsItem);
    }

    public IPathInfo CreateSubdirectory(string path)
    {
        var pathInfo = new MongoPathInfo(this.PathString + (String.IsNullOrWhiteSpace(this.PathString) ? "" : "/") + path);
        pathInfo.Create();

        return pathInfo;
    }

    public void Delete()
    {
        Delete(false);
    }

    public void Delete(bool recursive)
    {
        var client = new MongoClient(PathString.GetConnetion());
        var database = client.GetDatabase(PathString.GetDatabase());
        var collection = database.GetCollection<MongoCmsItem>(PathString.GetCollectionName());

        string itemPath = PathString.GetNodePath();
        string cmsId = itemPath.PathToCmsId();

        var items = CosmoDbHelper.ExecuteWithRetries(() =>
         {
             //var query = collection.AsQueryable<MongoCmsItem>()
             //    .Where(u => u.CmsId == cmsId && u.Parent.StartsWith(itemPath));

             //return query.ToArray();
             var cursor = collection
                              .Find(u => u.CmsId == cmsId && u.Parent.StartsWith(itemPath))
                              .Limit(int.MaxValue)
                              .ToCursor();

             return cursor.GetAll().ToArray();
         });

        if (items.Length > 0 && recursive == false)
        {
            throw new Exception("Path is not empty: " + itemPath);
        }

        #region Delete Items

        var itemsResult = CosmoDbHelper.ExecuteWithRetries(() =>
                collection.DeleteMany<MongoCmsItem>(u => u.CmsId == cmsId && u.Parent.StartsWith(itemPath))
            );

        Console.WriteLine($"Deleted {itemsResult.DeletedCount} of {items.Count()} items...");
        if (itemsResult.DeletedCount != items.Count())
        {
            throw new Exception("Can't delete all child items, try again");
        }

        #endregion

        #region Delete Path

        var result = collection.DeleteOne(i => i.CmsId == cmsId && i.Path == itemPath);

        //var filter = Builders<MongoCmsItem>.Filter.Eq("Path", itemPath);
        //var result = collection.DeleteOne(filter);

        if (result.DeletedCount == 0)
        {
            throw new Exception("Can't delete item: " + itemPath);
        }

        MongoStreamDocument.CachedItemds.RemoveResursive(PathString);

        #endregion
    }

    public IEnumerable<IPathInfo> GetDirectories()
    {
        string itemPath = PathString.GetNodePath();
        string cmsId = itemPath.PathToCmsId();

        IEnumerable<MongoCmsItem> items = null;

        if (_cachedItems.ContainsKey(cmsId))
        {
            #region Caching

            var cachedItems = _cachedItems[cmsId];
            items = cachedItems.Where(i => i.CmsId == cmsId && i.Parent == itemPath && i.ItemTypeValue == (int)CmsItemType.Directory);

            #endregion
        }
        else
        {
            #region Querying

            var client = new MongoClient(PathString.GetConnetion());
            var database = client.GetDatabase(PathString.GetDatabase());
            var collection = database.GetCollection<MongoCmsItem>(PathString.GetCollectionName());

            items = CosmoDbHelper.ExecuteWithRetries(() =>
            {
                var cursor = collection
                .Find(i => i.CmsId == cmsId && i.Parent == itemPath && i.ItemTypeValue == (int)CmsItemType.Directory)
                .Limit(int.MaxValue)
                .ToCursor();

                return cursor.GetAll();
            });


            //var query = collection.AsQueryable<MongoCmsItem>()
            //    .Where(i => i.CmsId == cmsId && i.Parent == itemPath && i.ItemTypeValue == (int)CmsItemType.Directory);

            //items = query.GetAll();

            #endregion
        }

        return items.Select(i =>
        {
            var pathString = Extensions.PathString(PathString.GetConnetion(), PathString.GetDatabase(), PathString.GetCollectionName(), i.Path);
            MongoStreamDocument.CachedItemds.Add(pathString, i);
            return new MongoPathInfo(pathString);
        });
    }

    public IEnumerable<IDocumentInfo> GetFiles(string pattern)
    {
        // Nicht ganz effizent

        pattern = pattern.Replace(@"\", @"\\");
        pattern = pattern.Replace(".", @"\.");
        pattern = pattern.Replace("?", ".");
        pattern = pattern.Replace("*", ".*?");
        pattern = pattern.Replace(" ", @"\s");

        var documentInfos = GetFiles().Where(d => Regex.IsMatch(d.Name, pattern));

        return documentInfos;
    }

    public IEnumerable<IDocumentInfo> GetFiles()
    {
        string itemPath = PathString.GetNodePath();
        string cmsId = itemPath.PathToCmsId();

        var client = new MongoClient(PathString.GetConnetion());
        var database = client.GetDatabase(PathString.GetDatabase());
        var collection = database.GetCollection<MongoCmsItem>(PathString.GetCollectionName());

        IEnumerable<MongoCmsItem> items = null;

        if (_cachedItems.ContainsKey(cmsId))
        {
            #region Caching

            var cachedItems = _cachedItems[cmsId];
            items = cachedItems.Where(i => i.CmsId == cmsId && i.Parent == itemPath && i.ItemTypeValue == (int)CmsItemType.File);

            #endregion
        }
        else
        {
            #region Quering

            items = CosmoDbHelper.ExecuteWithRetries(() =>
            {
                var cursor = collection
                                    .Find(i => i.CmsId == cmsId && i.Parent == itemPath && i.ItemTypeValue == (int)CmsItemType.File)
                                    .Limit(int.MaxValue)
                                    .ToCursor();

                return cursor.GetAll();
            });

            //var query = collection.AsQueryable<MongoCmsItem>()
            //    .Where(i => i.CmsId == cmsId && i.Parent == itemPath && i.ItemTypeValue == (int)CmsItemType.File);
            //items = query.GetAll();

            #endregion
        }

        return items.Select(i =>
        {
            var pathString = Extensions.PathString(PathString.GetConnetion(), PathString.GetDatabase(), PathString.GetCollectionName(), i.Path);
            MongoStreamDocument.CachedItemds.Add(pathString, i);
            return new MongoDocumentInfo(pathString);
        });
    }

    #endregion

    #region IPathInfo2

    public int CacheAllRecursive()
    {
        var client = new MongoClient(PathString.GetConnetion());
        var database = client.GetDatabase(PathString.GetDatabase());
        var collection = database.GetCollection<MongoCmsItem>(PathString.GetCollectionName());

        string itemPath = PathString.GetNodePath();
        string cmsId = itemPath.PathToCmsId();

        var cursor = collection.Find(x => x.CmsId == cmsId)
            .Limit(int.MaxValue)
            .ToCursor();

        var items = cursor.GetAll();

        ReleaseCacheRecursive();
        _cachedItems.TryAdd(cmsId, items);

        return items.Count();
    }

    public bool ReleaseCacheRecursive()
    {
        string itemPath = PathString.GetNodePath();
        string cmsId = itemPath.PathToCmsId();

        if (_cachedItems.ContainsKey(cmsId))
        {
            return _cachedItems.TryRemove(cmsId, out IEnumerable<MongoCmsItem> items);
        }

        return false;
    }

    #endregion

    #region Helper



    #endregion

    #region Static Members

    static internal MongoCmsItem QueryMongoItem(string path)
    {
        string itemPath = path.GetNodePath();
        string cmsId = itemPath.PathToCmsId();

        if (_cachedItems.ContainsKey(cmsId))
        {
            #region Caching

            var cachedItems = _cachedItems[cmsId];
            return cachedItems
                .Where(i => i.Path == itemPath && i.CmsId == cmsId)
                .FirstOrDefault();

            #endregion
        }
        else
        {
            var client = new MongoClient(path.GetConnetion());
            var database = client.GetDatabase(path.GetDatabase());
            var collection = database.GetCollection<MongoCmsItem>(path.GetCollectionName());

            return CosmoDbHelper.ExecuteWithRetries(() =>
            {
                var query = collection.AsQueryable<MongoCmsItem>()
                        .Where(i => i.Path == itemPath && i.CmsId == cmsId);
                return query.FirstOrDefault();
            });

        }
    }

    #endregion
}
