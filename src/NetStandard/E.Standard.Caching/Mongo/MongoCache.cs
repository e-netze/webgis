using E.Standard.Caching.Abstraction;
using E.Standard.Caching.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Linq;

namespace E.Standard.Caching.Mongo;

public class MongoCache : IKeyValueCache
{
    private string _connectionString;

    #region IKeyValueCache

    public string Get(string key)
    {
        var collection = KeyValueCollection();

        var item = collection.Find(
            filter: Builders<KeyValueDocument>.Filter.Eq("_id", key)
            ).FirstOrDefault();

        return item?.Value;
    }

    public bool Init(string initalParameter)
    {
        _connectionString = initalParameter;
        return true;
    }

    public void Remove(string key)
    {
        var collection = KeyValueCollection();

        collection.DeleteOne(
            filter: Builders<KeyValueDocument>.Filter.Eq("_id", key)
            );
    }

    public void Set(string key, object o, double expireSeconds)
    {
        var collection = KeyValueCollection();

        var item = new KeyValueDocument(key, o?.ToString(), expireSeconds);
        collection.ReplaceOne(
            filter: Builders<KeyValueDocument>.Filter.Eq("_id", key),
            item,
            new ReplaceOptions() { IsUpsert = true });
    }

    public string[] GetAllKeys()
    {
        var collection = KeyValueCollection();

        var keys = collection
                .Find(FilterDefinition<KeyValueDocument>.Empty)
                .Project(Builders<KeyValueDocument>.Projection.Include("_id"))
                .ToList()
                .Select(doc => doc["_id"]?.ToString())
                .Where(id => !String.IsNullOrEmpty(id))
                .ToArray();

        return keys;
    }

    public int MaxChunkSize => int.MaxValue;

    #endregion

    #region Helper

    private IMongoCollection<KeyValueDocument> KeyValueCollection()
    {
        var client = new MongoClient(_connectionString.GetMongoConnetion());
        var database = client.GetDatabase(_connectionString.GetMongoDatabase());
        var collection = database.GetCollection<KeyValueDocument>("webgis-kv-cache");

        return collection;
    }

    #endregion

    #region Classes

    class KeyValueDocument
    {
        public KeyValueDocument() { }

        public KeyValueDocument(string key, string val, double expireSeconds)
        {
            this.Id = key;
            this.Value = val;
            this.Created = DateTime.UtcNow;
            this.Expires = DateTime.UtcNow.AddSeconds(expireSeconds);
        }

        [BsonId]
        public string Id { get; set; }

        [BsonElement("Value")]
        public string Value { get; set; }

        [BsonElement("Created")]
        public DateTime Created { get; set; }

        [BsonElement("Expires")]
        public DateTime Expires { get; set; }
    }

    #endregion
}
