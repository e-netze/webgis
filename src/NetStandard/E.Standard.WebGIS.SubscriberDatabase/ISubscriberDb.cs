using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.SubscriberDatabase;

public interface ISubscriberDb
{
    bool CreateApiSubscriber(SubscriberDb.Subscriber subscriber);

    bool UpdateApiSubscriberSettings(SubscriberDb.Subscriber subscriber);

    bool UpdateApiSubscriberPassword(SubscriberDb.Subscriber subscriber, string newPassword);

    SubscriberDb.Subscriber GetSubscriberByName(string name);

    SubscriberDb.Subscriber GetSubscriberById(string id);

    bool SetApiSubscriberLastLogin(string id);

    string[] SubscriberNames(string term);

    SubscriberDb.Subscriber[] GetSubscribers();

    bool CreateApiClient(SubscriberDb.Client client);

    bool UpdateApiClient(SubscriberDb.Client client);

    SubscriberDb.Client GetClientByClientId(string clientId);

    SubscriberDb.Client GetClientByName(SubscriberDb.Subscriber subscriber, string clientName);

    SubscriberDb.Client[] GetSubscriptionClients(string subscriber);

    bool DeleteApiClient(SubscriberDb.Client client);

    #region Favorites

    Task<UserFavoriteStatus> GetFavUserStatusAsync(string username);

    Task<bool> SetFavUserStatusAsync(string username, UserFavoriteStatus status);

    Task<bool> SetFavItemAsync(string username, string task, string tool, string toolItem);

    Task<IEnumerable<string>> GetFavItemsAsync(string username, string task, string tool);

    Task<bool> DeleteUserFavorites(string username, string task = null, string tool = null);

    #endregion
}

public interface ISubscriberDb2 : ISubscriberDb
{
    string GenerateNewClientId();

    Task<bool> ApplyClientCmsId(SubscriberDb.Client client, string cmsId, bool add);
}
