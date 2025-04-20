#nullable enable

using E.Standard.Extensions.RegEx;
using E.Standard.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static E.Standard.WebGIS.SubscriberDatabase.SubscriberDb;

namespace E.Standard.WebGIS.SubscriberDatabase;

public class SubscriberFileDb : ISubscriberDb
{
    private readonly string _rootPath;
    private readonly ILogger? _logger;

    internal SubscriberFileDb(string rootPath, ILogger? logger)
    {
        _rootPath = rootPath;
        _logger = logger;
    }

    #region ISubscriberDb

    #region Subsciber

    public bool CreateApiSubscriber(SubscriberDb.Subscriber subscriber)
    {
        subscriber.Id = CreatePseudoId();
        subscriber.Name = subscriber.Name.ToLower();
        subscriber.Email = subscriber.Email.ToLower();
        subscriber.LastLogin = DateTime.UtcNow;
        subscriber.Created = DateTime.UtcNow;

        string path = Path.Combine(SubscribersRootPath, $"{subscriber.Id}.json");
        File.WriteAllText(path, JSerializer.Serialize(subscriber));

        return true;
    }

    public SubscriberDb.Subscriber? GetSubscriberById(string id)
    {
        string path = Path.Combine(SubscribersRootPath, $"{id}.json");

        if (File.Exists(path))
        {
            return JSerializer.Deserialize<SubscriberDb.Subscriber>(File.ReadAllText(path));
        }

        return null;
    }

    public SubscriberDb.Subscriber? GetSubscriberByName(string name)
    {
        _logger?.LogInformation("GetSubscriberByName: {name} from {path}", name, SubscribersRootPath);

        foreach (var fileInfo in new DirectoryInfo(SubscribersRootPath).GetFiles("*.json"))
        {
            try
            {
                var subscriber = JSerializer.Deserialize<SubscriberDb.Subscriber>(File.ReadAllText(fileInfo.FullName));

                _logger?.LogInformation("Loaded Subscriber: {name}, File: {filename}", subscriber?.Name, fileInfo.FullName);

                if (name.Equals(subscriber?.Name,  StringComparison.OrdinalIgnoreCase))
                {
                    return subscriber;
                }
            }
            catch(Exception ex) 
            {
                // Log exception if needed
                _logger?.LogError(ex, "Error deserializing subscriber file: {name}, File: {filename}", name, fileInfo.FullName);
            }
        }

        return null;
    }

    public SubscriberDb.Subscriber[] GetSubscribers()
    {
        List<SubscriberDb.Subscriber> subscribers = new List<SubscriberDb.Subscriber>();

        foreach (var fileInfo in new DirectoryInfo(SubscribersRootPath).GetFiles("*.json"))
        {
            try
            {
                var subscriber = JSerializer.Deserialize<SubscriberDb.Subscriber>(File.ReadAllText(fileInfo.FullName));

                if (subscriber is not null)
                {
                    subscribers.Add(subscriber);
                }
            }
            catch { }
        }

        return subscribers.ToArray();
    }

    public bool SetApiSubscriberLastLogin(string id)
    {
        var subscriber = GetSubscriberById(id);
        if (subscriber == null)
        {
            return false;
        }

        subscriber.LastLogin = DateTime.UtcNow;
        return UpdateApiSubscriberSettings(subscriber);
    }

    public string[] SubscriberNames(string term)
    {
        return GetSubscribers()
            .Where(s => s.Name.MatchWildcard(term))
            .Select(s => s.Name)
            .ToArray();
    }

    public bool UpdateApiSubscriberPassword(SubscriberDb.Subscriber subscriber, string newPassword)
    {
        subscriber.Password = newPassword;

        return UpdateApiSubscriberSettings(subscriber);
    }

    public bool UpdateApiSubscriberSettings(SubscriberDb.Subscriber subscriber)
    {
        string path = Path.Combine(SubscribersRootPath, $"{subscriber.Id}.json");

        subscriber.Name = subscriber.Name.ToLower();
        subscriber.Email = subscriber.Email.ToLower();

        File.Delete(path);
        File.WriteAllText(path, JSerializer.Serialize(subscriber));

        return true;
    }

    #region Favorites

    public Task<UserFavoriteStatus> GetFavUserStatusAsync(string username) => Task.FromResult(UserFavoriteStatus.Inactive);

    public Task<bool> SetFavUserSatusAsync(string username, UserFavoriteStatus status) => Task.FromResult(false);

    public Task<bool> SetFavItemAsync(string username, string task, string tool, string toolItem) => Task.FromResult(false);

    public Task<IEnumerable<string>> GetFavItemsAsync(string username, string task, string tool) => Task.FromResult<IEnumerable<string>>(null!);

    public Task<bool> DeteleUserFavorites(string username, string? task = null, string? tool = null) => Task.FromResult(false);

    #endregion

    #endregion

    #region Clients

    public bool CreateApiClient(SubscriberDb.Client client)
    {
        client.Id = CreatePseudoId();
        client.ClientName = client.ClientName.ToLower();

        File.WriteAllText(Path.Combine(ClientsRootPath, $"{client.Subscriber}.{client.ClientId}.json"), JSerializer.Serialize(client));

        return true;
    }

    public SubscriberDb.Client[] GetSubriptionClients(string subscriber)
    {
        string path = ClientsRootPath;

        List<SubscriberDb.Client> clients = new List<SubscriberDb.Client>();
        foreach (var fileInfo in new DirectoryInfo(path).GetFiles(subscriber.ToString() + ".*.json"))
        {
            try
            {
                var client = JSerializer.Deserialize<SubscriberDb.Client>(File.ReadAllText(fileInfo.FullName));
                if (client?.Subscriber == subscriber)
                {
                    clients.Add(client);
                }
            }
            catch { }
        }

        return clients.ToArray();
    }

    public bool UpdateApiClient(SubscriberDb.Client client)
    {
        client.ClientName = client.ClientName.ToLower();

        string path = Path.Combine(ClientsRootPath, $"{client.Subscriber}.{client.ClientId}.json");

        File.Delete(path);
        File.WriteAllText(path, JSerializer.Serialize(client));

        return true;
    }

    public bool DeleteApiClient(SubscriberDb.Client client)
    {
        client.ClientName = client.ClientName.ToLower();

        string path = Path.Combine(ClientsRootPath, $"{client.Subscriber}.{client.ClientId}.json");

        File.Delete(path);

        return true;
    }

    public SubscriberDb.Client? GetClientByClientId(string clientId)
    {
        string path = ClientsRootPath;

        foreach (var fileInfo in new DirectoryInfo(path).GetFiles($"*.{clientId}.json"))
        {
            try
            {
                var client = JSerializer.Deserialize<SubscriberDb.Client>(File.ReadAllText(fileInfo.FullName));
                if (client?.ClientId == clientId)
                {
                    return client;
                }
            }
            catch { }
        }

        return null;
    }

    public SubscriberDb.Client? GetClientByName(SubscriberDb.Subscriber subscriber, string clientName)
    {
        return GetSubriptionClients(subscriber.Id).Where(c => c.ClientName == clientName.ToLower()).FirstOrDefault();
    }

    #endregion

    #endregion

    #region Helper

    private void CreateIfNoExist(string path)
    {
        DirectoryInfo di = new DirectoryInfo(path);
        if (!di.Exists)
        {
            di.Create();
        }
    }

    private string SubscribersRootPath
    {
        get
        {
            string path = Path.Combine(_rootPath, "subscribers");
            CreateIfNoExist(path);
            return path;
        }
    }

    private string ClientsRootPath
    {
        get
        {
            string path = Path.Combine(_rootPath, "clients");
            CreateIfNoExist(path);
            return path;
        }
    }

    private string CreatePseudoId()
    {
        return Math.Abs((new object()).GetHashCode()).ToString();
    }

    #endregion

}
