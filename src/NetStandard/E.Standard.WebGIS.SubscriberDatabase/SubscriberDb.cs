using E.Standard.DbConnector;
using E.Standard.DbConnector.Schema;
using E.Standard.Security.Cryptography.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.SubscriberDatabase;

public class SubscriberDb : ISubscriberDb, IDbSchemaProvider
{
    private readonly string _connectionString;

    private SubscriberDb(string connectionString)
    {
        _connectionString = connectionString;
    }

    #region IDbSchemaProvider

    public DataSet DbSchema
    {
        get
        {
            DataSet dataset = new DataSet();

            DataTable table = new DataTable("webgis_api_subscribers");
            table.Columns.Add(new DataColumn("@id", typeof(int)));
            table.Columns.Add(new DataColumn("!name:32", typeof(string)));
            table.Columns.Add(new DataColumn("firstname:32", typeof(string)));
            table.Columns.Add(new DataColumn("lastname:32", typeof(string)));
            table.Columns.Add(new DataColumn("passwordhash:128", typeof(string)));
            table.Columns.Add(new DataColumn("email:32", typeof(string)));
            table.Columns.Add(new DataColumn("created", typeof(DateTime)));
            table.Columns.Add(new DataColumn("lastlogin", typeof(DateTime)));
            table.Columns.Add(new DataColumn("is_administrator", typeof(bool)));
            dataset.Tables.Add(table);

            table = new DataTable("webgis_api_clients");
            table.Columns.Add(new DataColumn("@id", typeof(int)));
            table.Columns.Add(new DataColumn("!clientname:255", typeof(string)));
            table.Columns.Add(new DataColumn("clientid:40", typeof(string)));
            table.Columns.Add(new DataColumn("clientreferer:255", typeof(string)));
            table.Columns.Add(new DataColumn("locked", typeof(bool)));
            table.Columns.Add(new DataColumn("subscriber", typeof(int)));
            table.Columns.Add(new DataColumn("clientsecret:80", typeof(string)));
            table.Columns.Add(new DataColumn("created", typeof(DateTime)));
            table.Columns.Add(new DataColumn("expires", typeof(DateTime)));
            dataset.Tables.Add(table);

            return dataset;
        }
    }

    public string DbConnectionString { get { return _connectionString; } }

    #endregion

    #region Subscribers

    public bool CreateApiSubscriber(Subscriber subscriber)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection dbconn = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.Connection = dbconn;
                command.CommandText = "insert into webgis_api_subscribers (name,firstname,lastname,passwordhash,email,created,lastlogin,is_administrator) values (" + conn.ParaName("name,firstname,lastname,passwordhash,email,created,lastlogin,is_administrator") + ")";

                command.Parameters.Add(conn.GetParameter("name", subscriber.Name.ToLower()));
                command.Parameters.Add(conn.GetParameter("firstname", subscriber.FirstName));
                command.Parameters.Add(conn.GetParameter("lastname", subscriber.LastName));
                command.Parameters.Add(conn.GetParameter("passwordhash", subscriber.PasswordHash));
                command.Parameters.Add(conn.GetParameter("email", subscriber.Email.ToLower()));
                command.Parameters.Add(conn.GetParameter("created", DateTime.UtcNow));
                command.Parameters.Add(conn.GetParameter("lastlogin", DateTime.UtcNow));
                command.Parameters.Add(conn.GetParameter("is_administrator", subscriber.IsAdministrator));

                dbconn.Open();
                command.ExecuteNonQuery();
            }
        }

        return true;
    }

    /*
    public bool UpdateApiSubscriber(Subscriber subscriber)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection dbconn = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.Connection = dbconn;
                command.CommandText = "update webgis_api_subscribers set name=" + conn.ParaName("name") + ",firstname=" + conn.ParaName("firstname") + ",lastname=" + conn.ParaName("lastname") + ",passwordhash=" + conn.ParaName("passwordhash") + ",email=" + conn.ParaName("email") + ",is_administrator=" + conn.ParaName("is_administrator") + " where id=" + conn.ParaName("id");

                command.Parameters.Add(conn.GetParameter("id", subscriber.Id));
                command.Parameters.Add(conn.GetParameter("name", subscriber.Name.ToLower()));
                command.Parameters.Add(conn.GetParameter("firstname", subscriber.FirstName));
                command.Parameters.Add(conn.GetParameter("lastname", subscriber.LastName));
                command.Parameters.Add(conn.GetParameter("passwordhash", subscriber.PasswordHash));
                command.Parameters.Add(conn.GetParameter("email", subscriber.Email.ToLower()));
                command.Parameters.Add(conn.GetParameter("is_administrator", subscriber.IsAdministrator));

                dbconn.Open();
                command.ExecuteNonQuery();
            }
        }

        return true;
    }
     * */

    public bool UpdateApiSubscriberSettings(Subscriber subscriber)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection dbconn = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.Connection = dbconn;
                command.CommandText = "update webgis_api_subscribers set firstname=" + conn.ParaName("firstname") + ",lastname=" + conn.ParaName("lastname") + ",email=" + conn.ParaName("email") + " where id=" + conn.ParaName("id");

                command.Parameters.Add(conn.GetParameter("id", int.Parse(subscriber.Id)));
                command.Parameters.Add(conn.GetParameter("firstname", subscriber.FirstName));
                command.Parameters.Add(conn.GetParameter("lastname", subscriber.LastName));
                command.Parameters.Add(conn.GetParameter("email", subscriber.Email.ToLower()));

                dbconn.Open();
                command.ExecuteNonQuery();
            }
        }
        return true;
    }

    public bool UpdateApiSubscriberPassword(Subscriber subscriber, string newPassword)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection dbconn = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.Connection = dbconn;
                command.CommandText = "update webgis_api_subscribers set passwordhash=" + conn.ParaName("passwordhash") + " where id=" + conn.ParaName("id");

                subscriber.Password = newPassword;
                command.Parameters.Add(conn.GetParameter("id", int.Parse(subscriber.Id)));
                command.Parameters.Add(conn.GetParameter("passwordhash", subscriber.PasswordHash));

                dbconn.Open();
                command.ExecuteNonQuery();
            }
        }

        return true;
    }

    public Subscriber GetSubscriberByName(string name)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection dbconn = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.Connection = dbconn;

                /*
                if (name.Contains("@"))
                {
                    command.CommandText = "select * from webgis_api_subscribers where email=" + conn.ParaName("email");
                    command.Parameters.Add(conn.GetParameter("email", name));
                }
                else
                {
                    command.CommandText = "select * from webgis_api_subscribers where name=" + conn.ParaName("name");
                    command.Parameters.Add(conn.GetParameter("name", name));
                }
                 * */

                command.CommandText = "select * from webgis_api_subscribers where name=" + conn.ParaName("name");
                command.Parameters.Add(conn.GetParameter("name", name));

                dbconn.Open();
                var reader = command.ExecuteReader();
                var subscribers = ReadSubscribers(reader);
                if (subscribers.Length == 1)
                {
                    return subscribers[0];
                }

                return null;
            }
        }
    }

    public Subscriber GetSubscriberById(string id)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection dbconn = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.Connection = dbconn;

                command.CommandText = "select * from webgis_api_subscribers where id=" + conn.ParaName("id");
                command.Parameters.Add(conn.GetParameter("id", int.Parse(id)));

                dbconn.Open();
                var reader = command.ExecuteReader();
                var subscribers = ReadSubscribers(reader);
                if (subscribers.Length == 1)
                {
                    return subscribers[0];
                }

                return null;
            }
        }
    }

    public bool SetApiSubscriberLastLogin(string id)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection dbconn = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.Connection = dbconn;
                command.CommandText = "update webgis_api_clients set lastlogin=" + conn.ParaName("lastlogin") + " where id=" + conn.ParaName("id");

                command.Parameters.Add(conn.GetParameter("id", int.Parse(id)));
                command.Parameters.Add(conn.GetParameter("lastlogin", DateTime.UtcNow));

                dbconn.Open();
                command.ExecuteNonQuery();
            }
        }

        return true;
    }

    public string[] SubscriberNames(string term)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection dbconn = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.Connection = dbconn;

                command.CommandText = "select name from webgis_api_subscribers where name  like " + conn.ParaName("term");
                command.Parameters.Add(conn.GetParameter("term", term));

                dbconn.Open();
                var reader = command.ExecuteReader();

                List<string> names = new List<string>();
                while (reader.Read())
                {
                    names.Add((string)reader[0]);
                }

                return names.ToArray();
            }
        }
    }

    public Subscriber[] GetSubscribers()
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection dbconn = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.Connection = dbconn;

                command.CommandText = "select * from webgis_api_subscribers";

                dbconn.Open();
                var reader = command.ExecuteReader();
                return ReadSubscribers(reader);
            }
        }
    }

    #endregion

    #region Clients

    public bool CreateApiClient(Client client)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection dbconn = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.Connection = dbconn;
                command.CommandText = "insert into webgis_api_clients (clientname,clientid,clientsecret,clientreferer,created,expires,locked,subscriber) values (" + conn.ParaName("clientname,clientid,clientsecret,clientreferer,created,expires,locked,subscriber") + ")";

                command.Parameters.Add(conn.GetParameter("clientname", client.ClientName.ToLower()));
                command.Parameters.Add(conn.GetParameter("clientid", client.ClientId));
                command.Parameters.Add(conn.GetParameter("clientsecret", client.ClientSecret));
                command.Parameters.Add(conn.GetParameter("clientreferer", client.ClientReferer));
                command.Parameters.Add(conn.GetParameter("created", DateTime.UtcNow));
                if (client.Expires == null)
                {
                    command.Parameters.Add(conn.GetParameter("expires", DBNull.Value));
                }
                else
                {
                    command.Parameters.Add(conn.GetParameter("expires", client.Expires));
                }

                command.Parameters.Add(conn.GetParameter("locked", client.Locked));
                command.Parameters.Add(conn.GetParameter("subscriber", int.Parse(client.Subscriber)));

                dbconn.Open();
                command.ExecuteNonQuery();
            }
        }

        return true;
    }

    public bool UpdateApiClient(Client client)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection dbconn = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.Connection = dbconn;
                command.CommandText = "update webgis_api_clients set clientname=" + conn.ParaName("clientname") + ",clientsecret=" + conn.ParaName("clientsecret") + ",clientreferer=" + conn.ParaName("clientreferer") + ",expires=" + conn.ParaName("expires") + ",locked=" + conn.ParaName("locked") + ",subscriber=" + conn.ParaName("subscriber") + " where clientid=" + conn.ParaName("clientid");

                command.Parameters.Add(conn.GetParameter("clientname", client.ClientName));
                command.Parameters.Add(conn.GetParameter("clientid", client.ClientId));
                command.Parameters.Add(conn.GetParameter("clientsecret", client.ClientSecret));
                command.Parameters.Add(conn.GetParameter("clientreferer", client.ClientReferer));
                if (client.Expires == null)
                {
                    command.Parameters.Add(conn.GetParameter("expires", DBNull.Value));
                }
                else
                {
                    command.Parameters.Add(conn.GetParameter("expires", client.Expires));
                }

                command.Parameters.Add(conn.GetParameter("locked", client.Locked));
                command.Parameters.Add(conn.GetParameter("subscriber", int.Parse(client.Subscriber)));

                dbconn.Open();
                command.ExecuteNonQuery();
            }
        }

        return true;
    }

    public bool DeleteApiClient(Client client)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection dbconn = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.Connection = dbconn;
                command.CommandText = "delete from webgis_api_clients where clientid=" + conn.ParaName("clientid") + " and subsriber=" + conn.ParaName("subscriber");

                command.Parameters.Add(conn.GetParameter("clientid", client.ClientId));
                command.Parameters.Add(conn.GetParameter("subscriber", int.Parse(client.Subscriber)));

                dbconn.Open();
                command.ExecuteNonQuery();
            }
        }

        return true;
    }

    public Client GetClientByClientId(string clientId)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection dbconn = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.Connection = dbconn;
                command.CommandText = "select * from webgis_api_clients where clientid=" + conn.ParaName("cliendid");
                command.Parameters.Add(conn.GetParameter("cliendid", clientId));

                dbconn.Open();
                var reader = command.ExecuteReader();
                var clients = ReadClients(reader);
                if (clients.Length > 0)
                {
                    return clients[0];
                }

                return null;
            }
        }
    }

    public Client GetClientByName(Subscriber subscriber, string clientName)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection dbconn = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.Connection = dbconn;
                command.CommandText = "select * from webgis_api_clients where subscriber=" + conn.ParaName("subscriber") + " and clientname=" + conn.ParaName("clientname");
                command.Parameters.Add(conn.GetParameter("subscriber", int.Parse(subscriber.Id)));
                command.Parameters.Add(conn.GetParameter("clientname", clientName));

                dbconn.Open();
                var reader = command.ExecuteReader();
                var clients = ReadClients(reader);
                if (clients.Length > 0)
                {
                    return clients[0];
                }

                return null;
            }
        }
    }

    public Client[] GetSubscriptionClients(string subscriber)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection dbconn = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.Connection = dbconn;
                command.CommandText = "select * from webgis_api_clients where subscriber=" + conn.ParaName("subscriber");
                command.Parameters.Add(conn.GetParameter("subscriber", int.Parse(subscriber)));

                dbconn.Open();
                var reader = command.ExecuteReader();
                var clients = ReadClients(reader);
                return clients;
            }
        }
    }

    #endregion

    #region Favorites

    async public Task<UserFavoriteStatus> GetFavUserStatusAsync(string username)
    {
        var usernameHash = ToAnonymFavUsername(username);

        using (var factory = new DBConnection())
        {
            factory.OleDbConnectionMDB = _connectionString;

            using (DbConnection connection = factory.GetConnection())
            {
                DbCommand command = factory.GetCommand();
                command.Connection = connection;

                command.CommandText = "select status from webgis_fav_users where username=" + factory.ParaName("name");
                command.Parameters.Add(factory.GetParameter("name", usernameHash));

                await connection.OpenAsync();
                //var reader = command.ExecuteReader();
                //var subscribers = ReadSubscribers(reader);
                //if (subscribers.Length == 1)
                //    return subscribers[0];

                //return null;
                var status = await command.ExecuteScalarAsync();

                if (status == null)
                {
                    return UserFavoriteStatus.Unknown;
                }

                return (UserFavoriteStatus)Convert.ToInt32(status);
            }
        }
    }

    async public Task<bool> SetFavUserStatusAsync(string username, UserFavoriteStatus status)
    {
        var usernameHash = ToAnonymFavUsername(username);

        using (var factory = new DBConnection())
        {
            factory.OleDbConnectionMDB = _connectionString;

            var existingFavUser = await GetFavUserAsync(username);

            using (DbConnection connection = factory.GetConnection())
            {
                DbCommand command = factory.GetCommand();
                command.Connection = connection;

                if (existingFavUser != null)
                {
                    command.CommandText = "update webgis_fav_users set status=" + factory.ParaName("status") + ",changedate=" + factory.ParaName("changedate") + " where id=" + factory.ParaName("id");
                    command.Parameters.Add(factory.GetParameter("id", existingFavUser.Id));
                }
                else
                {
                    command.CommandText = "insert into webgis_fav_users (username,status,changedate) values (" + factory.ParaName("username") + "," + factory.ParaName("status") + "," + factory.ParaName("changedate") + ")";
                    command.Parameters.Add(factory.GetParameter("username", usernameHash));
                }
                command.Parameters.Add(factory.GetParameter("status", (int)status));
                command.Parameters.Add(factory.GetParameter("changedate", DateTime.UtcNow));

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
        }
    }

    async public Task<bool> SetFavItemAsync(string username, string task, string tool, string toolItem)
    {
        var favUser = await GetFavUserAsync(username);
        if (favUser == null || favUser.Status != (int)UserFavoriteStatus.Active)
        {
            return false;
        }

        task = task.ToLower();
        tool = tool.ToLower();
        //toolItem = toolItem.ToLower();  // Groß u Kleinschreibung belassen. Sonst werden Karten und Kategorien auf der Portalseite immer klein geschreiben!

        var existingFavItem = (await GetFavItemsAsync(favUser.Id, task, tool, toolItem)).FirstOrDefault();

        using (var factory = new DBConnection())
        {
            factory.OleDbConnectionMDB = _connectionString;

            using (DbConnection connection = factory.GetConnection())
            {
                DbCommand command = factory.GetCommand();
                command.Connection = connection;

                if (existingFavItem != null)
                {
                    command.CommandText = "update webgis_fav_items set count=" + factory.ParaName("count") + ",changedate=" + factory.ParaName("changedate") + " where id=" + factory.ParaName("id");
                    command.Parameters.Add(factory.GetParameter("id", existingFavItem.Id));
                }
                else
                {
                    command.CommandText = "insert into webgis_fav_items (userid,task,tool,toolitem,count,changedate) values (" + factory.ParaName("userid") + "," + factory.ParaName("task") + "," + factory.ParaName("tool") + "," + factory.ParaName("toolitem") + "," + factory.ParaName("count") + "," + factory.ParaName("changedate") + ")";
                    command.Parameters.Add(factory.GetParameter("userid", favUser.Id));
                    command.Parameters.Add(factory.GetParameter("task", task));
                    command.Parameters.Add(factory.GetParameter("tool", tool));
                    command.Parameters.Add(factory.GetParameter("toolitem", toolItem));
                }
                command.Parameters.Add(factory.GetParameter("count", existingFavItem != null ? existingFavItem.Count + 1 : 1));
                command.Parameters.Add(factory.GetParameter("changedate", DateTime.UtcNow));

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
        }
    }

    async public Task<IEnumerable<string>> GetFavItemsAsync(string username, string task, string tool)
    {
        var favUser = await GetFavUserAsync(username);
        if (favUser == null || favUser.Status != (int)UserFavoriteStatus.Active)
        {
            return new string[0];
        }

        using (var factory = new DBConnection())
        {
            factory.OleDbConnectionMDB = _connectionString;

            using (DbConnection connection = factory.GetConnection())
            {
                DbCommand command = factory.GetCommand();
                command.Connection = connection;

                command.CommandText = "select toolitem,count,changedate from webgis_fav_items where userid=" + factory.ParaName("userid") + " and task=" + factory.ParaName("task") + " and tool=" + factory.ParaName("tool");
                command.Parameters.Add(factory.GetParameter("userid", favUser.Id));
                command.Parameters.Add(factory.GetParameter("task", task?.ToLower()));
                command.Parameters.Add(factory.GetParameter("tool", tool?.ToLower()));

                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();

                List<FavItem> favItems = new List<FavItem>();
                while (await reader.ReadAsync())
                {
                    var favItem = new FavItem();
                    favItem.ToolItem = reader["toolitem"]?.ToString();
                    favItem.Count = Convert.ToInt32(reader["count"]);
                    favItem.ChangeDate = ToDateTime(reader["changedate"]);

                    favItems.Add(favItem);
                }

                // DoTo: Man könnte beim Sortieren noch mach dem Datum gewichten. 
                // Neuere könnten so weiter nach vorne rücken und alte nach hinten...
                var sortedFavIcons = favItems.OrderByDescending(f => f.Count);

                return sortedFavIcons.Select(s => s.ToolItem);
            }
        }
    }

    async public Task<bool> DeleteUserFavorites(string username, string task = null, string tool = null)
    {
        var favUser = await GetFavUserAsync(username);
        if (favUser == null || favUser.Status != (int)UserFavoriteStatus.Active)
        {
            return false;
        }

        task = task?.ToLower();
        tool = tool?.ToLower();

        using (var factory = new DBConnection())
        {
            factory.OleDbConnectionMDB = _connectionString;

            using (DbConnection connection = factory.GetConnection())
            {
                DbCommand command = factory.GetCommand();
                command.Connection = connection;

                command.CommandText = "delete from webgis_fav_items where userid=" + factory.ParaName("userid");
                command.Parameters.Add(factory.GetParameter("userid", favUser.Id));

                if (task != null)
                {
                    command.CommandText += " and task=" + factory.ParaName("task");
                    command.Parameters.Add(factory.GetParameter("task", task));
                }
                if (tool != null)
                {
                    command.CommandText += " and tool=" + factory.ParaName("tool");
                    command.Parameters.Add(factory.GetParameter("tool", tool));
                }

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
        }
    }

    async private Task<FavUser> GetFavUserAsync(string username)
    {
        var usernameHash = ToAnonymFavUsername(username);

        using (var factory = new DBConnection())
        {
            factory.OleDbConnectionMDB = _connectionString;

            using (DbConnection connection = factory.GetConnection())
            {
                DbCommand command = factory.GetCommand();
                command.Connection = connection;

                command.CommandText = "select * from webgis_fav_users where username=" + factory.ParaName("name");
                command.Parameters.Add(factory.GetParameter("name", usernameHash));

                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();

                FavUser favUser = null;
                while (await reader.ReadAsync())
                {
                    favUser = new FavUser();
                    favUser.Id = Convert.ToInt32(reader["id"]);
                    favUser.Username = reader["username"]?.ToString();
                    favUser.Status = Convert.ToInt32(reader["status"]);
                    favUser.ChangeDate = ToDateTime(reader["changedate"]);
                }

                return favUser;
            }
        }
    }

    async private Task<IEnumerable<FavItem>> GetFavItemsAsync(int userId, string task = null, string tool = null, string toolItem = null)
    {
        using (var factory = new DBConnection())
        {
            factory.OleDbConnectionMDB = _connectionString;

            using (DbConnection connection = factory.GetConnection())
            {
                DbCommand command = factory.GetCommand();
                command.Connection = connection;

                command.CommandText = "select * from webgis_fav_items where userid=" + factory.ParaName("userid");
                command.Parameters.Add(factory.GetParameter("userid", userId));

                if (task != null)
                {
                    command.CommandText += " and task=" + factory.ParaName("task");
                    command.Parameters.Add(factory.GetParameter("task", task));
                }
                if (tool != null)
                {
                    command.CommandText += " and tool=" + factory.ParaName("tool");
                    command.Parameters.Add(factory.GetParameter("tool", tool));
                }
                if (toolItem != null)
                {
                    command.CommandText += " and toolitem=" + factory.ParaName("toolitem");
                    command.Parameters.Add(factory.GetParameter("toolitem", toolItem));
                }

                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();

                List<FavItem> favItems = new List<FavItem>();
                while (await reader.ReadAsync())
                {
                    var favItem = new FavItem();
                    favItem.Id = Convert.ToInt32(reader["id"]);
                    favItem.UserId = Convert.ToInt32(reader["userid"]);
                    favItem.Task = reader["task"]?.ToString();
                    favItem.Tool = reader["tool"]?.ToString();
                    favItem.ToolItem = reader["toolitem"]?.ToString();
                    favItem.Count = Convert.ToInt32(reader["count"]);
                    favItem.ChangeDate = ToDateTime(reader["changedate"]);

                    favItems.Add(favItem);
                }

                return favItems;
            }
        }
    }

    #endregion

    #region Helper

    public Subscriber[] ReadSubscribers(DbDataReader reader)
    {
        List<Subscriber> subscribers = new List<Subscriber>();

        while (reader.Read())
        {
            Subscriber subscriber = new Subscriber();

            subscriber.Id = reader["id"] != null ? reader["id"].ToString() : "0";
            subscriber.Name = Convert.ToString(reader["name"]);
            subscriber.FirstName = Convert.ToString(reader["firstname"]);
            subscriber.LastName = Convert.ToString(reader["lastname"]);
            subscriber.PasswordHash = Convert.ToString(reader["passwordhash"]);
            subscriber.Email = Convert.ToString(reader["email"]);
            subscriber.Created = reader["created"] != null && reader["created"] != DBNull.Value ? Convert.ToDateTime(reader["created"]) : DateTime.UtcNow;
            subscriber.LastLogin = reader["lastlogin"] != null && reader["lastlogin"] != DBNull.Value ? Convert.ToDateTime(reader["created"]) : null;
            subscriber.IsAdministrator = reader["is_administrator"] != null ? Convert.ToBoolean(reader["is_administrator"]) : false;

            subscribers.Add(subscriber);
        }

        return subscribers.ToArray();
    }

    private Client[] ReadClients(DbDataReader reader)
    {
        List<Client> clients = new List<Client>();

        while (reader.Read())
        {
            Client client = new Client();

            client.Id = reader["id"] != null ? reader["id"].ToString() : "0";
            client.ClientName = Convert.ToString(reader["clientname"]);
            client.ClientId = Convert.ToString(reader["clientid"]);
            client.ClientSecret = Convert.ToString(reader["clientsecret"]);
            client.ClientReferer = Convert.ToString(reader["clientreferer"]);
            client.Created = reader["created"] != null && reader["created"] != DBNull.Value ? Convert.ToDateTime(reader["created"]) : DateTime.UtcNow;
            client.Expires = reader["expires"] != null && reader["expires"] != DBNull.Value ? Convert.ToDateTime(reader["expires"]) : null;
            client.Locked = reader["locked"] != null ? Convert.ToBoolean(reader["locked"]) : false;
            client.Subscriber = reader["subscriber"] != null ? reader["subscriber"].ToString() : "0";

            clients.Add(client);
        }

        return clients.ToArray();
    }

    private string ToAnonymFavUsername(string username)
    {
        return $"favuser{username.ToLower()}".Hash64();
    }

    private DateTime ToDateTime(object date)
    {
        if (date is DateTimeOffset)
        {
            return new DateTime(((DateTimeOffset)date).ToUniversalTime().Ticks, DateTimeKind.Utc);
        }

        try
        {
            return Convert.ToDateTime(date);
        }
        catch
        {
            return new DateTime();  // ??
        }
    }

    #endregion

    #region Classes

    public class Subscriber
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
        public DateTime Created { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsAdministrator { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string Password
        {
            set
            {
                this.PasswordHash = HashPassword(value);
            }
        }

        public bool VerifyPassword(string password)
        {
            if (String.IsNullOrEmpty(this.PasswordHash) || this.PasswordHash.Length < 32)
            {
                return false;
            }

            string salt = this.PasswordHash.Substring(0, 64);
            string hash = HashPassword(password, salt);

            return hash.Equals(this.PasswordHash);
        }


        private string _fullName = null;
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string FullName
        {
            get
            {
                return _fullName ?? "subscriber::" + this.Name;
            }
            set
            {
                _fullName = value;
            }
        }

        #region Password Hashing

        private string GenerateSalt()
        {
            byte[] tokenData = RandomNumberGenerator.GetBytes(32);

            string salt = String.Empty;
            foreach (byte x in tokenData)
            {
                salt += String.Format("{0:x2}", x);
            }

            return salt;
        }

        private string HashPassword(string password, string salt = "")
        {
            if (String.IsNullOrEmpty(salt))
            {
                salt = GenerateSalt();
            }

            if (salt.Length != 64)
            {
                throw new ArgumentException("Invalid salt length");
            }

            password += salt;

            byte[] bytes = Encoding.Unicode.GetBytes(password);
            using var hashstring = SHA256.Create();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;

            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }

            return salt + hashString;
        }

        #endregion
    }

    // Never change the values
    public enum SubscriberType
    {
        Default = 0
    }

    public class Client
    {
        public string Id { get; set; }
        public string ClientName { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string ClientReferer { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Expires { get; set; }
        public bool Locked { get; set; }
        public string Subscriber { get; set; }
        public int SubscriberType { get; set; }
        public IEnumerable<string> Roles { get; set; }

        public const string CmsRolePrefix = "cms::";

        public bool IsClientSecret(string secret)
        {
            if (String.IsNullOrWhiteSpace(this.ClientSecret))
            {
                return false;
            }

            return this.ClientSecret.Split(',').Contains(secret);
        }

        public T GetSubscriberType<T>()
            where T : Enum
        {
            var result = new Dictionary<int, T>();
            var values = Enum.GetValues(typeof(T));

            foreach (var v in Enum.GetValues(typeof(T)))
            {
                if ((int)v == this.SubscriberType)
                {
                    return (T)v;
                }
            }

            return default(T);
        }
    }

    public class FavUser
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public int Status { get; set; }
        public DateTime ChangeDate { get; set; }
    }

    public class FavItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Task { get; set; }
        public string Tool { get; set; }
        public string ToolItem { get; set; }
        public int Count { get; set; }
        public DateTime ChangeDate { get; set; }
    }

    #endregion

    #region Static Members

    static public ISubscriberDb Create(string connectionString, ILogger logger = null)
    {
        if (connectionString.ToLower().StartsWith("fs:"))
        {
            return new SubscriberFileDb(connectionString.Substring("fs:".Length), logger);
        }
        else if (connectionString.ToLower().StartsWith("mongo:"))
        {
            return new SubscriberMongoDb(connectionString.Substring("mongo:".Length));
        }
        return new SubscriberDb(connectionString);
    }

    #endregion
}
