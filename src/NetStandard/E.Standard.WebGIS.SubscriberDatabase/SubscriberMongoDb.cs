using E.Standard.Caching.Extensions;
using E.Standard.WebGIS.SubscriberDatabase.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.SubscriberDatabase;

public class SubscriberMongoDb : ISubscriberDb2
{
    private string _connectionString;

    internal SubscriberMongoDb(string connectionString)
    {
        _connectionString = connectionString;

        //var client = new MongoClient(_connectionString.GetMongoConnection());
        //var database = client.GetDatabase(_connectionString.GetMongoDatabase());
        //database.DropCollection("Subscribers");
    }

    #region ISubscriberDb

    #region Subscriber

    public bool CreateApiSubscriber(SubscriberDb.Subscriber subscriber, bool migrate = false)
    {
        if (migrate == true)
        {
            throw new NotSupportedException("Migration: Identity insert is not supported in MongoDB");
        }

        subscriber.Name = subscriber.Name.ToLower();
        subscriber.Created = DateTime.UtcNow;
        subscriber.LastLogin = DateTime.UtcNow;

        var collection = SubscriberCollection();

        var filter = Builders<SubscriberDocument>.Filter.Eq("Name", subscriber.Name);
        var item = collection.Find(filter).FirstOrDefault();
        if (item != null)
        {
            throw new Exception("Subscriber allready exists");
        }

        item = new SubscriberDocument(subscriber);
        collection.InsertOne(item);

        return true;
    }

    public bool UpdateApiSubscriberPassword(SubscriberDb.Subscriber subscriber, string newPassword)
    {
        var collection = SubscriberCollection();

        subscriber.Password = newPassword;
        var item = collection.FindOneAndUpdate(
            filter: Builders<SubscriberDocument>.Filter.EqId(subscriber.Id),
            update: Builders<SubscriberDocument>.Update.Set(i => i.PasswordHash, subscriber.PasswordHash)
            );

        return item != null;
    }

    public bool UpdateApiSubscriberSettings(SubscriberDb.Subscriber subscriber)
    {
        var collection = SubscriberCollection();


        var item = collection.FindOneAndUpdate<SubscriberDocument>(
            filter: Builders<SubscriberDocument>.Filter.EqId(subscriber.Id),
            update: Builders<SubscriberDocument>.Update
                        .Set(i => i.FirstName, subscriber.FirstName)
                        .Set(i => i.LastName, subscriber.LastName)
                        .Set(i => i.Email, subscriber.Email)
            );

        return item != null;
    }

    public SubscriberDb.Subscriber GetSubscriberById(string id)
    {
        var collection = SubscriberCollection();

        var filter = Builders<SubscriberDocument>.Filter.EqId(id);
        var item = collection.Find(filter).FirstOrDefault();

        return item?.ToSubscriber();
    }

    public SubscriberDb.Subscriber GetSubscriberByName(string name)
    {
        name = name.ToLower();

        var collection = SubscriberCollection();

        var filter = Builders<SubscriberDocument>.Filter.Eq("Name", name);
        var item = collection.Find(filter).FirstOrDefault();

        return item?.ToSubscriber();
    }

    public bool SetApiSubscriberLastLogin(string id)
    {
        var collection = SubscriberCollection();

        var item = collection.FindOneAndUpdate(
            filter: Builders<SubscriberDocument>.Filter.EqId(id),
            update: Builders<SubscriberDocument>.Update.Set(i => i.LastLogin, DateTime.UtcNow)
            );

        return item != null;
    }

    public SubscriberDb.Subscriber[] GetSubscribers()
    {
        var collection = SubscriberCollection();

        return collection
            .Find(_ => true)
            .ToList()
            .Select(i => i.ToSubscriber())
            .ToArray();
    }

    public string[] SubscriberNames(string term)
    {
        return GetSubscribers()
                    .Select(s => s.Name)
                    .ToArray();
    }

    #endregion

    #region Favorites

    public Task<bool> DeleteUserFavorites(string username, string task = null, string tool = null) => Task.FromResult(false);

    public Task<bool> SetFavItemAsync(string username, string task, string tool, string toolItem) => Task.FromResult(false);

    public Task<bool> SetFavUserStatusAsync(string username, UserFavoriteStatus status) => Task.FromResult(false);

    public Task<IEnumerable<string>> GetFavItemsAsync(string username, string task, string tool) => Task.FromResult<IEnumerable<string>>(null);

    public Task<UserFavoriteStatus> GetFavUserStatusAsync(string username) => Task.FromResult(UserFavoriteStatus.Inactive);

    #endregion

    #region Clients

    public string GenerateNewClientId()
    {
        return ObjectId.GenerateNewId(DateTime.UtcNow).ToString();
    }

    public bool CreateApiClient(SubscriberDb.Client client, bool migrate = false)
    {
        if (migrate == true)
        {
            throw new NotSupportedException("Migration: Identity insert is not supported in MongoDB");
        }

        client.ClientName = client.ClientName.ToLower();
        client.Created = DateTime.UtcNow;

        var collection = ClientCollection();

        var filter = Builders<ClientDocument>.Filter.Eq("Subscriber", client.Subscriber) &
                     Builders<ClientDocument>.Filter.Eq("ClientName", client.ClientName);
        var item = collection.Find(filter).FirstOrDefault();
        if (item != null)
        {
            throw new Exception("Already exists");
        }

        item = new ClientDocument(client)
        {
            Id = ClientDocument.ToId(client.ClientId)
        };
        collection.InsertOne(item);

        return true;
    }

    public SubscriberDb.Client GetClientByClientId(string clientId)
    {
        var collection = ClientCollection();

        var filter = Builders<ClientDocument>.Filter.EqId(clientId);
        var item = collection.Find(filter).FirstOrDefault();

        return item?.ToClient();
    }

    public SubscriberDb.Client GetClientByName(SubscriberDb.Subscriber subscriber, string clientName)
    {
        clientName = clientName.ToLower();

        var collection = ClientCollection();

        var filter = Builders<ClientDocument>.Filter.Eq("Subscriber", subscriber.Id) &
                     Builders<ClientDocument>.Filter.Eq("ClientName", clientName);
        var item = collection.Find(filter).FirstOrDefault();

        return item?.ToClient();
    }

    public SubscriberDb.Client[] GetSubscriptionClients(string subscriber)
    {
        var collection = ClientCollection();

        var filter = Builders<ClientDocument>.Filter.Eq("Subscriber", subscriber);
        return collection
            .Find(filter)
            .ToList()
            .Select(c => c.ToClient())
            .ToArray();
    }

    public bool UpdateApiClient(SubscriberDb.Client client)
    {
        var collection = ClientCollection();

        var item = collection.FindOneAndUpdate<ClientDocument>(
            filter: Builders<ClientDocument>.Filter.EqId(client.ClientId),
            update: Builders<ClientDocument>.Update
                        .Set(i => i.ClientName, client.ClientName)
                        .Set(i => i.ClientId, client.ClientId)
                        .Set(i => i.ClientSecret, client.ClientSecret)
                        .Set(i => i.ClientReferer, client.ClientReferer)
            );

        return item != null;
    }

    public bool DeleteApiClient(SubscriberDb.Client client)
    {
        var collection = ClientCollection();

        var documentId = ClientDocument.ToId(client.ClientId);

        var result = collection.DeleteOne<ClientDocument>(c => c.Id == documentId);
        return result.DeletedCount == 1;
    }

    async public Task<bool> ApplyClientCmsId(SubscriberDb.Client client, string cmsId, bool add)
    {
        var collection = ClientCollection();

        var roles = new List<string>();
        if (client.Roles != null)
        {
            roles.AddRange(client.Roles);
        }

        bool store = false;

        if (add == true && !roles.Contains(SubscriberDb.Client.CmsRolePrefix + cmsId))
        {
            roles.Add(SubscriberDb.Client.CmsRolePrefix + cmsId);
            store = true;
        }
        else if (add == false && roles.Contains(SubscriberDb.Client.CmsRolePrefix + cmsId))
        {
            roles.Remove(SubscriberDb.Client.CmsRolePrefix + cmsId);
            store = true;
        }

        if (store)
        {
            var item = await collection.FindOneAndUpdateAsync<ClientDocument>(
                filter: Builders<ClientDocument>.Filter.EqId(client.ClientId),
                update: Builders<ClientDocument>.Update
                            .Set(i => i.Roles, roles.ToArray())
                );

            return item != null;
        }

        return true;
    }

    public SubscriberDb.Client[] GetAllClients()
    {
        var collection = ClientCollection();

        return collection
            .Find(_ => true)
            .ToList()
            .Select(i => i.ToClient())
            .ToArray();
    }

    #endregion

    #endregion

    #region Helper

    private IMongoCollection<SubscriberDocument> SubscriberCollection()
    {
        var client = new MongoClient(_connectionString.GetMongoConnetion());
        var database = client.GetDatabase(_connectionString.GetMongoDatabase());
        var collection = database.GetCollection<SubscriberDocument>("webgis-subscribers");

        return collection;
    }

    private IMongoCollection<ClientDocument> ClientCollection()
    {
        var client = new MongoClient(_connectionString.GetMongoConnetion());
        var database = client.GetDatabase(_connectionString.GetMongoDatabase());
        var collection = database.GetCollection<ClientDocument>("webgis-clients");

        return collection;
    }

    #endregion

    #region Classes

    class SubscriberDocument
    {
        public SubscriberDocument() { }

        public SubscriberDocument(SubscriberDb.Subscriber subscriber)
        {
            this.Name = subscriber.Name;
            this.FirstName = subscriber.FirstName;
            this.LastName = subscriber.LastName;
            this.PasswordHash = subscriber.PasswordHash;
            this.Email = subscriber.Email;
            this.LastLogin = subscriber.LastLogin;
            this.Created = subscriber.Created;
            this.IsAdmin = subscriber.IsAdministrator;
        }

        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("FirstName")]
        public string FirstName { get; set; }

        [BsonElement("LastName")]
        public string LastName { get; set; }

        [BsonElement("PasswordHash")]
        public string PasswordHash { get; set; }

        [BsonElement("Email")]
        public string Email { get; set; }

        [BsonElement("LastLogin")]
        public DateTime? LastLogin { get; set; }

        [BsonElement("Created")]
        public DateTime Created { get; set; }

        [BsonElement("IsAdmin")]
        public bool IsAdmin { get; set; }

        public SubscriberDb.Subscriber ToSubscriber()
        {
            return new SubscriberDb.Subscriber()
            {
                Id = this.Id.ToString(),
                Name = this.Name,
                FirstName = this.FirstName,
                LastName = this.LastName,
                PasswordHash = this.PasswordHash,
                Email = this.Email,
                Created = this.Created,
                LastLogin = this.LastLogin,
                IsAdministrator = this.IsAdmin
            };
        }
    }

    class ClientDocument
    {
        public ClientDocument() { }

        public ClientDocument(SubscriberDb.Client client)
        {
            this.ClientName = client.ClientName;
            this.ClientId = client.ClientId;
            this.ClientSecret = client.ClientSecret;
            this.ClientReferer = client.ClientReferer;
            this.Created = client.Created;
            this.Expires = client.Expires;
            this.Locked = client.Locked;
            this.Subscriber = client.Subscriber;
            this.SubscriberType = client.SubscriberType;
            this.Roles = client.Roles?.ToArray();
        }

        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("ClientName")]
        public string ClientName { get; set; }

        [BsonElement("ClientId")]
        public string ClientId { get; set; }

        [BsonElement("ClientSecret")]
        public string ClientSecret { get; set; }

        [BsonElement("ClientReferer")]
        public string ClientReferer { get; set; }

        [BsonElement("Created")]
        public DateTime Created { get; set; }

        [BsonElement("Expires")]
        public DateTime? Expires { get; set; }

        [BsonElement("Locked")]
        public bool Locked { get; set; }

        [BsonElement("Subscriber")]
        public string Subscriber { get; set; }

        [BsonElement("SubscriberType")]
        public int SubscriberType { get; set; }

        [BsonElement("Roles")]
        public string[] Roles { get; set; }

        public SubscriberDb.Client ToClient()
        {
            return new SubscriberDb.Client()
            {
                Id = this.Id.ToString(),
                ClientName = this.ClientName,
                ClientId = this.ClientId,
                ClientSecret = this.ClientSecret,
                ClientReferer = this.ClientReferer,
                Created = this.Created,
                Expires = this.Expires,
                Locked = this.Locked,
                Subscriber = this.Subscriber,
                SubscriberType = this.SubscriberType,
                Roles = this.Roles
            };
        }

        #region Static Members

        static public ObjectId ToId(string id)
        {
            return ObjectId.Parse(id);
        }

        #endregion
    }

    #endregion
}
