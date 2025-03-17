using E.Standard.DbConnector;
using E.Standard.Security.Exceptions;
using E.Standard.ThreadSafe;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Net;
using System.Text;

namespace E.Standard.Security
{
    [Obsolete("Use E.Standard.Security.Internal assembly")]
    public class TicketDb : IRemoteTicketDb
    {
        static private ThreadSafeDictionary<int, User> _users = new ThreadSafeDictionary<int, User>();
        static private ThreadSafeDictionary<string, Ticket> _tickets = new ThreadSafeDictionary<string, Ticket>();
        static private ThreadSafeDictionary<string, User> _tokenUsers = new ThreadSafeDictionary<string, User>();

        private string _remote_add = String.Empty, _http_x_forwarded_for = String.Empty;
        private string _connectionString = String.Empty;

        public TicketDb(/*HttpContext*/ object context, string connectionString)
        {
            //
            // ToDo:
            // Wird das noch irgendwo verwendet?
            // IP könnte für BotDection vielleicht noch einmal wichtig werden...
            //
            //if (context != null && context.Request != null)
            //{
            //    _remote_add = context.Request.ServerVariables["REMOTE_ADDR"];
            //    _http_x_forwarded_for = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            //    if (_remote_add == null) _remote_add = String.Empty;
            //    if (_http_x_forwarded_for == null) _http_x_forwarded_for = String.Empty;
            //}

            StringBuilder connSb = new StringBuilder();
            string[] connectionStringParamters = connectionString.Split(';');
            foreach (string parameter in connectionStringParamters)
            {
                if (parameter.ToLower() == "user-caching=false")
                {
                    this.UseUserCaching = false;
                    continue;
                }

                if (connSb.Length > 0)
                {
                    connSb.Append(";");
                }

                connSb.Append(parameter);
            }

            _connectionString = connSb.ToString();
        }

        static TicketDb()
        {
            ClearCache();
        }

        #region Caching 

        static public void ClearCache()
        {
            _tickets.Clear();
            _users.Clear();
            _tokenUsers.Clear();
        }

        static public void RemoveUserFromCache(int userId)
        {
            try
            {
                if (_users.ContainsKey(userId))
                {
                    _users.Remove(userId);
                }
            }
            catch { }
        }

        private bool _useUserCaching = true;
        public bool UseUserCaching
        {
            get { return _useUserCaching; }
            set { _useUserCaching = value; }
        }

        #endregion

        public bool HasUsers
        {
            get
            {
                try
                {
                    if (String.IsNullOrEmpty(_connectionString))
                    {
                        return false;
                    }

                    using (DBConnection connection = new DBConnection())
                    {
                        connection.OleDbConnectionMDB = _connectionString;

                        int userCount = Convert.ToInt32(connection.ExecuteScalar("SELECT COUNT(UID) FROM OGC_USERS"));
                        return userCount > 0;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        public string Login(string userName, string password, bool useExistingToken = false, int miniumSecondsValid = 60)
        {
            var ticket = Login2(userName, password, useExistingToken, miniumSecondsValid);
            if (ticket != null)
            {
                return ticket.Token;
            }

            return String.Empty;
        }

        public Ticket Login2(string userName, string password, bool useExistingToken = false, int miniumSecondsValid = 60, bool validatePassword=true)
        {
            #region Parse for SQL Injection

            if (userName.Length > 24)
            {
                throw new TicketDbException(userName, "Bad Login");
            }

            SqlInjection.ParseBlackList(userName);
            SqlInjection.ParseWhiteList(userName);

            #endregion

            using (DBFactory factory = new DBFactory(_connectionString))
            {
                using (DbConnection connection = factory.GetConnection())
                {
                    DbCommand command = factory.GetCommand(connection);

                    #region Validate Password

                    command.CommandText = "select * from " + factory.TableName("OGC_USERS") + " where " + factory.ColumnNames("USERNAME") + "=" + factory.ParaName("username");
                    command.Parameters.Add(factory.GetParameter("username", userName));

                    connection.Open();

                    User user = null;

                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            user = new User(reader);
                            if (validatePassword == true && !user.ValidatePassword(password))
                            {
                                throw new TicketDbException(userName, "Unknown user or password");
                            }
                        }
                        if (user == null)
                        {
                            throw new TicketDbException(userName, "Unknown user or password");
                        }
                    }

                    #endregion

                    #region Update Lastlogin

                    command.Parameters.Clear();
                    command.CommandText = "update " + factory.TableName("OGC_USERS") + " set " + factory.ColumnNames("LASTLOGIN") + "=" + factory.ParaName("lastlogin") + " where " + factory.ColumnNames("USERNAME") + "=" + factory.ParaName("username");
                    command.Parameters.Add(factory.GetParameter("username", userName));
                    command.Parameters.Add(factory.GetParameter("lastlogin", DateTime.UtcNow));
                    command.ExecuteNonQuery();

                    #endregion

                    if (useExistingToken && miniumSecondsValid > 0)
                    {
                        command.Parameters.Clear();

                        command.CommandText = "select " + factory.ColumnNames("TOKEN,EXPIRE_DATE,EXPIRE_DATE_LONG") + " from " + factory.TableName("OGC_TICKETS") + " where " +
                            factory.ColumnNames("UID") + "=" + factory.ParaName("uid") + " and ("
                            + factory.ColumnNames("EXPIRE_DATE") + ">" + factory.ParaName("date_now") + " or "
                            + factory.ColumnNames("EXPIRE_DATE_LONG") + ">" + factory.ParaName("date_now") +
                            ") order by " +
                            factory.ColumnNames("EXPIRE_DATE") + " desc";
                        command.Parameters.Add(factory.GetParameter("uid", user.Id));
                        command.Parameters.Add(factory.GetParameter("date_now", DateTime.UtcNow.AddSeconds(miniumSecondsValid)));  // At Minimum still 1 minute valid

                        Ticket ticket = null;
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ticket = new Ticket()
                                {
                                    Token = reader["TOKEN"].ToString(),
                                    ExpireDate = Convert.ToDateTime(reader["EXPIRE_DATE"]),
                                    LongLivingToken = TicketDb.ToLongLivingToken(reader["TOKEN"].ToString())
                                };
                                if (reader["EXPIRE_DATE_LONG"] != null && reader["EXPIRE_DATE_LONG"]!=DBNull.Value)
                                {
                                    ticket.ExpireDateLong = Convert.ToDateTime(reader["EXPIRE_DATE_LONG"]);
                                }

                                break;
                            }
                        }

                        if (ticket != null)
                        {
                            if (ticket.ExpireDate < DateTime.UtcNow)
                            {
                                ticket.ExpireDate = DateTime.UtcNow.AddDays(user.ExpireDays);

                                command.Parameters.Clear();
                                command.CommandText = "update " + factory.TableName("OGC_TICKETS") + " set " + factory.ColumnNames("EXPIRE_DATE") + "=" + factory.ParaName("expire_date") + " where " + factory.ColumnNames("TOKEN") + "=" + factory.ParaName("token");
                                command.Parameters.Add(factory.GetParameter("expire_date", ticket.ExpireDate));
                                command.Parameters.Add(factory.GetParameter("token", ticket.Token));

                                command.ExecuteNonQuery();
                            }

                            return ticket;
                        }
                    }

                    #region Insert new Token

                    string token = Guid.NewGuid().ToString("N").ToLower();
                    DateTime now = DateTime.UtcNow;
                    DateTime expireDate = now.AddDays(user.ExpireDays);
                    DateTime expireDateLong = now.AddDays(Math.Max(user.ExpireDaysLong, user.ExpireDays));

                    command.Parameters.Clear();
                    command.CommandText = "insert into " + factory.TableName("OGC_TICKETS") + " (" + factory.ColumnNames("UID,TOKEN,EXPIRE_DATE,EXPIRE_DATE_LONG,REMOTE_ADDR,HTTP_X_FORWARDED_FOR") + ") values (" + factory.ParaName("UID,TOKEN,EXPIRE_DATE,EXPIRE_DATE_LONG,REMOTE_ADDR,HTTP_X_FORWARDED_FOR") + ")";
                    command.Parameters.Add(factory.GetParameter("UID", user.Id));
                    command.Parameters.Add(factory.GetParameter("TOKEN", token));
                    command.Parameters.Add(factory.GetParameter("EXPIRE_DATE", expireDate));
                    command.Parameters.Add(factory.GetParameter("EXPIRE_DATE_LONG", expireDateLong));
                    command.Parameters.Add(factory.GetParameter("REMOTE_ADDR", _remote_add));
                    command.Parameters.Add(factory.GetParameter("HTTP_X_FORWARDED_FOR", _http_x_forwarded_for));

                    command.ExecuteNonQuery();

                    #endregion

                    return new Ticket()
                    {
                        Token = token,
                        LongLivingToken = TicketDb.ToLongLivingToken(token),
                        ExpireDate = expireDate,
                        ExpireDateLong = expireDateLong,
                        CreateFromCurrentLogin = true
                    };
                }
            }
        }

        public bool Logout(string token)
        {
            if (_connectionString.StartsWith("https://"))
            {
                var nvc = new NameValueCollection();
                nvc.Add("token", token);

                var info = GetRemoteDbItem<Info>("Logout", nvc, token);

                return info.Succeeded;
            }

            string cacheTokenName = token;

            token = TicketDb.ToToken(token);

            using (DBFactory factory = new DBFactory(_connectionString))
            {
                using (DbConnection connection = factory.GetConnection())
                {
                    DbCommand command = factory.GetCommand(connection);

                    command.CommandText = "delete from " + factory.TableName("OGC_TICKETS") + " where " + factory.ColumnNames("TOKEN") + "=" + factory.ParaName("token");
                    command.Parameters.Add(factory.GetParameter("token", token));

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            if (_tickets.ContainsKey(cacheTokenName))
            {
                _tickets.Remove(cacheTokenName);
            }

            return true;
        }

        public string LoginOrUseExistingTicket(string user, string password)
        {
            return Login(user, password, true);
        }

        public Ticket TicketByToken(string token, Uri requestUrl)
        {
            if (String.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            try
            {
                if (_tickets.ContainsKey(token))
                {
                    var cachedTicket = _tickets[token];
                    if (TicketDb.IsLongLivingToken(token) && (requestUrl == null || requestUrl.Scheme.ToLower() != "https"))
                    {
                        throw new WrongRequestSchemeException();
                    }
                }

                bool isLongLiving = TicketDb.IsLongLivingToken(token);
                if (isLongLiving)
                {
                    if (requestUrl == null || requestUrl.Scheme.ToLower() != "https")
                    {
                        throw new WrongRequestSchemeException();
                    }

                    token = TicketDb.ToToken(token);
                }

                string cacheTokenName = token;

                if (_connectionString.StartsWith("https://"))
                {
                    var nvc = new NameValueCollection();
                    nvc.Add("token", token);

                    var ticketInfo = GetRemoteDbItem<TicketInfo>("TicketByToken", nvc, token);
                    var ticket= ticketInfo.ToTicket();

                    _tickets.Add(cacheTokenName, ticket);
                    return ticket;
                }

                using (DBFactory factory = new DBFactory(_connectionString))
                {
                    using (DbConnection connection = factory.GetConnection())
                    {
                        DbCommand command = factory.GetCommand(connection);

                        command.CommandText = "select * from " + factory.TableName("OGC_TICKETS") + " where " + factory.ColumnNames("TOKEN") + "=" + factory.ParaName("token");
                        command.Parameters.Add(factory.GetParameter("token", token));

                        connection.Open();
                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Ticket ticket = new Ticket(reader);
                                ticket.IsFromLongLivingTokenRequest = isLongLiving;

                                _tickets.Add(cacheTokenName, ticket);
                                return ticket;
                            }
                            else
                            {
                                throw new Exception("Invalid token");
                            }
                        }
                    }
                }
            }
            catch (WrongRequestSchemeException)
            {

                Logout(token);
                throw new Exception("Token is deleted because of invalid request scheme for long-living-token. Always use https with long-living-tokens. You must login again and request another token.");
            }
        }

        public User UserRowByUid(int uid)
        {
            if (this.UseUserCaching && _users.ContainsKey(uid))
            {
                return _users[uid];
            }

            using (DBFactory factory = new DBFactory(_connectionString))
            {
                using (DbConnection connection = factory.GetConnection())
                {
                    DbCommand command = factory.GetCommand(connection);

                    command.CommandText = "select * from " + factory.TableName("OGC_USERS") + " where " + factory.ColumnNames("UID") + "=" + factory.ParaName("uid");
                    command.Parameters.Add(factory.GetParameter("uid", uid));

                    connection.Open();
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            User user = new User(reader);

                            if (this.UseUserCaching)
                            {
                                _users.Add(uid, user);
                            }

                            return user;
                        }
                        else
                        {
                            throw new Exception("Unknown uid");
                        }
                    }
                }
            }

            #region Old & Unsecure
            /*
            using (DBConnection connection = new DBConnection())
            {
                connection.OleDbConnectionMDB = _connectionString;

                DataTable userTab = connection.Select("*", "OGC_USERS", "UID=" + uid);
                if (userTab == null)
                    throw new Exception("Can't open users table");

                if (userTab.Rows.Count == 0)
                    throw new Exception("Unknown uid");

                _users.Add(uid, userTab.Rows[0]);
                return userTab.Rows[0];
            }
            */
            #endregion
        }

        public User UserByUsername(string username, string token="")
        {
            if (this.UseUserCaching)
            {
                var cachedUsers = _users.Copy();
                foreach (int uid in cachedUsers.Keys)
                {
                    if (cachedUsers[uid].Username == username)
                    {
                        return cachedUsers[uid];
                    }
                }
            }

            if (_connectionString.StartsWith("https://"))
            {
                var nvc = new NameValueCollection();
                nvc.Add("username", username);
                nvc.Add("token", token);

                var userInfo = GetRemoteDbItem<UserInfo>("UserByUsername", nvc, token);

                return userInfo.ToUser();
            }

            using (DBFactory factory = new DBFactory(_connectionString))
            {
                using (DbConnection connection = factory.GetConnection())
                {
                    DbCommand command = factory.GetCommand(connection);

                    command.CommandText = "select * from " + factory.TableName("OGC_USERS") + " where " + factory.ColumnNames("USERNAME") + "=" + factory.ParaName("username");
                    command.Parameters.Add(factory.GetParameter("username", username));

                    connection.Open();
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            User user = new User(reader);

                            if (this.UseUserCaching)
                            {
                                _users.Add(user.Id, user);
                            }

                            return user;
                        }
                        else
                        {
                            throw new TicketDbException(username, "Unknown username");
                        }
                    }
                }
            }
        }

        public User[] Users()
        {
            List<User> users = new List<User>();
            using (DBFactory factory = new DBFactory(_connectionString))
            {
                using (DbConnection connection = factory.GetConnection())
                {
                    DbCommand command = factory.GetCommand(connection);

                    command.CommandText = "select " + /*factory.ColumnNames("UID,USERNAME,USERROLES,LASTLOGIN")*/ "*" + " from " + factory.TableName("OGC_USERS") + " order by " + factory.ColumnNames("USERNAME");

                    connection.Open();
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new User(reader));
                        }
                    }
                }
            }
            return users.ToArray();
        }

        public string[] UserNames(string term)
        {
            List<string> users = new List<string>();
            using (DBFactory factory = new DBFactory(_connectionString))
            {
                using (DbConnection connection = factory.GetConnection())
                {
                    DbCommand command = factory.GetCommand(connection);

                    command.CommandText = "select " + factory.ColumnNames("USERNAME") + " from " + factory.TableName("OGC_USERS") + " where " + factory.ColumnNames("USERNAME") + " like " + factory.ParaName("term") + " order by " + factory.ColumnNames("USERNAME");
                    command.Parameters.Add(factory.GetParameter("term", term));

                    connection.Open();
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(reader[0] != null ? reader[0].ToString() : String.Empty);
                        }
                    }
                }
            }
            return users.ToArray();
        }

        public string[] UserRoles(string term)
        {
            string rawTerm = term.Replace("%", "").ToLower();

            List<string> roles = new List<string>();
            using (DBFactory factory = new DBFactory(_connectionString))
            {
                using (DbConnection connection = factory.GetConnection())
                {
                    DbCommand command = factory.GetCommand(connection);

                    command.CommandText = "select " + factory.ColumnNames("USERROLES") + " from " + factory.TableName("OGC_USERS") + " where " + factory.ColumnNames("USERROLES") + " like " + factory.ParaName("term");
                    command.Parameters.Add(factory.GetParameter("term", "%" + term));

                    connection.Open();
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string[] userRoles = reader[0] != null ? reader[0].ToString().Split(';') : new string[0];

                            foreach (string userRole in userRoles)
                            {
                                if (userRole.Contains("="))  // RoleParameter
                                {
                                    continue;
                                }

                                if (userRole.ToLower().Contains(rawTerm) && !roles.Contains(userRole))
                                {
                                    roles.Add(userRole);
                                }
                            }
                        }
                    }
                }
            }
            return roles.ToArray();
        }

        public DateTime TicketExpireDate(string token, Uri requestUrl)
        {
            Ticket ticket = TicketByToken(token, requestUrl);
            return (DateTime)ticket.ExpireDate;
        }

        public bool IsTicketValid(string token, Uri requestUrl)
        {
            Ticket ticket = TicketByToken(token, requestUrl);
            return IsTicketValid(ticket);
        }

        private bool IsTicketValid(Ticket ticket, int addSeconds = 0)
        {
            if (ticket == null)
            {
                return false;
            }

            DateTime expireDate = ticket.IsFromLongLivingTokenRequest ? ticket.ExpireDateLong : ticket.ExpireDate;
            return expireDate >= DateTime.UtcNow.AddSeconds(addSeconds);
        }

        public string UsernameByUid(int uid)
        {
            User user = UserRowByUid(uid);
            return user.Username;
        }

        public string TryGetUsernameByUid(int uid)
        {
            try
            {
                return UsernameByUid(uid);
            }
            catch
            {
                return String.Empty;
            }
        }

        public string TicketUsername(string token, Uri requestUrl)
        {
            User user = TicketUser(token, requestUrl);
            if (user == null)
            {
                return String.Empty;
            }

            return user.Username;
        }

        public User TicketUser(string token, Uri requestUrl)
        {
            if (String.IsNullOrEmpty(_connectionString))
            {
                return null;
            }

            if (this.UseUserCaching)
            {
                var cachedUsers = _tokenUsers.Copy();
                foreach (string t in cachedUsers.Keys)
                {
                    if (t == token)
                    {
                        return cachedUsers[t];
                    }
                }
            }

            if (_connectionString.StartsWith("https://"))
            {
                var nvc = new NameValueCollection();
                nvc.Add("token", token);

                var userInfo = GetRemoteDbItem<UserInfo>("TicketUser", nvc, token);
                User u= userInfo.ToUser();
                if (UseUserCaching)
                {
                    _tokenUsers.Add(token, u);
                }

                return u;
            }

            User user = null;

            if (token.Contains(":"))
            {
                #region client@secret1 or client@secret2

                var userName = token.Split(':')[0];
                var secret = token.Substring(userName.Length + 1);

                if (String.IsNullOrEmpty(secret) || secret.Length < 24)
                {
                    throw new TicketDbException(userName, $"invalid secret. Less than 24 characters.");
                }

                user = UserByUsername(userName);
                if (user == null)
                {
                    throw new TicketDbException(userName, $"unknwon user");
                }

                if (!secret.Equals(user.ClientSecret1) && !secret.Equals(user.ClientSecret2))
                {
                    throw new TicketDbException(userName, $"wrong secret");
                }

                #endregion
            }
            else
            {
                #region Login (long living) Token

                Ticket ticket = TicketByToken(token, requestUrl);
                if (!IsTicketValid(ticket))
                {
                    throw new TicketDbException(TryGetUsernameByUid(ticket.UserId), $"Ticket expired");
                }

                user = UserRowByUid(ticket.UserId);

                if (user != null)
                {
                    int secLevel = user.SecurityLevel;

                    if (secLevel > 0 && _remote_add != ticket.RemoteAddress)
                    {
                        throw new TicketDbException(user.Username, "Level 1 Security: Remove address not match (" + _remote_add + " / " + _http_x_forwarded_for + ")");
                    }
                    if (secLevel > 1 && _http_x_forwarded_for != ticket.HttpXForwardedFor)
                    {
                        throw new TicketDbException(user.Username, "Level 2 Security: Forwarded IP address not match (" + _remote_add + " / " + _http_x_forwarded_for + ")");
                    }
                }

                #endregion
            }

            return user;
        }

        public bool UserExists(string username)
        {
            using (DBConnection connection = new DBConnection())
            {
                connection.OleDbConnectionMDB = _connectionString;

                DataTable ticketsTab = connection.Select("*", "OGC_USERS", "USERNAME='" + username + "'");
                if (ticketsTab == null)
                {
                    throw new TicketDbException(username, "Can't open users table");
                }

                return ticketsTab.Rows.Count > 0;
            }
        }

        public bool DeleteExpiredTickets(int toleranceSeconds = 60)
        {
            using (DBFactory factory = new DBFactory(_connectionString))
            {
                using (DbConnection connection = factory.GetConnection())
                {
                    DbCommand command = factory.GetCommand(connection);

                    command.CommandText = "delete from " + factory.TableName("OGC_TICKETS") + " where " + factory.ColumnNames("EXPIRE_DATE") + "<" + factory.ParaName("date_now") + " and " + factory.ColumnNames("EXPIRE_DATE_LONG") + "<" + factory.ParaName("date_now");
                    command.Parameters.Add(factory.GetParameter("date_now", DateTime.UtcNow.AddSeconds(-toleranceSeconds)));

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            return true;
        }

        public IEnumerable<Ticket> CurrentTickets()
        {
            using (DBFactory factory = new DBFactory(_connectionString))
            {
                using (DbConnection connection = factory.GetConnection())
                {
                    DbCommand command = factory.GetCommand(connection);
                    command.CommandText="select * from " + factory.TableName("OGC_TICKETS") + " where " + factory.ColumnNames("EXPIRE_DATE") + ">" + factory.ParaName("date_now") + " or " + factory.ColumnNames("EXPIRE_DATE_LONG") + ">" + factory.ParaName("date_now");
                    command.Parameters.Add(factory.GetParameter("date_now", DateTime.UtcNow));

                    connection.Open();
                    List<Ticket> tickets = new List<Ticket>();
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tickets.Add(new Ticket(reader));
                        }
                    }

                    return tickets;
                }
            }
        }

        #region Static Members

        private static readonly string _longLivingTokenCryptoPassword = "w43feShDJFdRQz5XNNTf9WxFr2nMmLNM44c4b74NnmS6CYhMYXZXf8bhcMfzUD8ygh7jEA2Wb9nFBV68CXFH29sGhfaravpEquhVR5QJMpn7DNahvyw33yd6JxVnB2BMh7fXGye8w5M7n9CJACM68RV4Wgn9WWH4BXhtZXwFKRYtKdhbjFA9bhLnUYXTG2nrJp2vXM6LZNjAykzMNbftrjVgGAu2beHHr6kbACTb8HGa6mLMFpCnBu8j2Ld8TCpm";

        static public string ToLongLivingToken(string token)
        {
            return new Crypto().EncryptText(token, _longLivingTokenCryptoPassword, Crypto.Strength.AES128, true, Crypto.ResultStringType.Hex);
        }

        static public bool IsLongLivingToken(string token)
        {
            return token.StartsWith("0x");
        }

        static public string ToToken(string longLivingToken)
        {
            if (IsLongLivingToken(longLivingToken))
            {
                return new Crypto().DecryptText(longLivingToken, _longLivingTokenCryptoPassword, Crypto.Strength.AES128, true);
            }

            return longLivingToken;
        }

        #endregion

        #region SubClasses

        public class Ticket
        {
            public Ticket()
            {

            }

            internal Ticket(DbDataReader reader)
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string fieldName = reader.GetName(i).ToUpper();

                    if (fieldName == "UID" && reader["UID"] != null)
                    {
                        this.UserId = Convert.ToInt32(reader["UID"]);
                    }
                    if (fieldName == "TOKEN" && reader["TOKEN"] != null)
                    {
                        this.Token = reader["TOKEN"].ToString();
                    }
                    if (fieldName == "EXPIRE_DATE" && reader["EXPIRE_DATE"] is DateTime)
                    {
                        this.ExpireDate = new DateTime(((DateTime)reader["EXPIRE_DATE"]).Ticks, DateTimeKind.Utc);
                    }
                    if (fieldName == "EXPIRE_DATE_LONG" && reader["EXPIRE_DATE_LONG"] is DateTime)
                    {
                        this.ExpireDateLong = new DateTime(((DateTime)reader["EXPIRE_DATE_LONG"]).Ticks, DateTimeKind.Utc);
                    }
                    if (fieldName == "REMOTE_ADDR" && reader["REMOTE_ADDR"] != null)
                    {
                        this.RemoteAddress = reader["REMOTE_ADDR"].ToString();
                    }
                    if (fieldName == "HTTP_X_FORWARDED_FOR" && reader["HTTP_X_FORWARDED_FOR"] != null)
                    {
                        this.HttpXForwardedFor = reader["HTTP_X_FORWARDED_FOR"].ToString();
                    }
                }

                if (this.ExpireDateLong > this.ExpireDate)
                {
                    this.LongLivingToken = ToLongLivingToken(this.Token);
                }
            }

            public int UserId { get; set; }
            public string Token { get; set; }
            public string LongLivingToken { get; set; }
            public DateTime ExpireDate { get; set; }
            public DateTime ExpireDateLong { get; set; }
            public string RemoteAddress { get; set; }
            public string HttpXForwardedFor { get; set; }

            public bool IsFromLongLivingTokenRequest { get; set; }
            public bool CreateFromCurrentLogin { get; set; }

            public TicketInfo ToTicketInfo()
            {
                return new TicketInfo()
                {
                    Token = this.Token,
                    LongLivingToken = this.LongLivingToken,
                    ExpireDate = this.ExpireDate,
                    ExpireDateLong = this.ExpireDateLong
                };
            }
        }

        public class TicketInfo : Info
        {
            public TicketInfo() 
                :base(true)
            {
            }

            public string Token { get; set; }
            public string LongLivingToken { get; set; }
            public DateTime ExpireDate { get; set; }
            public DateTime ExpireDateLong { get; set; }

            public Ticket ToTicket()
            {
                return new Ticket()
                {
                    Token = this.Token,
                    LongLivingToken = this.LongLivingToken,
                    ExpireDate = this.ExpireDate,
                    ExpireDateLong = this.ExpireDateLong
                };
            }
        }

        public class User
        {
            public User()
            {

            }

            public User(DbDataReader reader)
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string fieldName = reader.GetName(i).ToUpper();

                    if (fieldName == "UID" && reader["UID"] != null)
                    {
                        this.Id = Convert.ToInt32(reader["UID"]);
                    }
                    if (fieldName == "USERNAME" && reader["USERNAME"] != null)
                    {
                        this.Username = reader["USERNAME"].ToString();
                    }
                    if (fieldName == "PASSWORD" && reader["PASSWORD"] != null)
                    {
                        this.EncryptedPassword = reader["PASSWORD"].ToString();
                    }

                    if (fieldName == "EMAIL" && reader["EMAIL"] != null)
                    {
                        this.Email = reader["EMAIL"].ToString();
                    }
                    if (fieldName == "FIRSTNAME" && reader["FIRSTNAME"] != null)
                    {
                        this.FirstName = reader["FIRSTNAME"].ToString();
                    }
                    if (fieldName == "LASTNAME" && reader["LASTNAME"] != null)
                    {
                        this.LastName = reader["LASTNAME"].ToString();
                    }
                    if (fieldName == "ORGANISATION" && reader["ORGANISATION"] != null)
                    {
                        this.Organization = reader["ORGANISATION"].ToString();
                    }
                    if (fieldName == "DESCRIPTION" && reader["DESCRIPTION"] != null)
                    {
                        this.Description = reader["DESCRIPTION"].ToString();
                    }

                    if (fieldName == "LASTLOGIN" && reader["LASTLOGIN"] is DateTime)
                    {
                        this.LastLogin = new DateTime(((DateTime)reader["LASTLOGIN"]).Ticks, DateTimeKind.Utc);
                    }
                    if (fieldName == "EXPIRE" && reader["EXPIRE"] != null)
                    {
                        this.ExpireDays = Convert.ToDouble(reader["EXPIRE"]);
                    }
                    if (fieldName == "EXPIRE_LONG" && reader["EXPIRE_LONG"] != null && reader["EXPIRE_LONG"] != DBNull.Value)
                    {
                        this.ExpireDaysLong = Convert.ToDouble(reader["EXPIRE_LONG"]);
                    }
                    if (fieldName == "SECURITY_LEVEL" && reader["SECURITY_LEVEL"] != null)
                    {
                        this.SecurityLevel = Convert.ToInt32(reader["SECURITY_LEVEL"]);
                    }
                    if (fieldName == "USERROLES" && reader["USERROLES"] != null && reader["USERROLES"] != DBNull.Value)
                    {
                        // Rollen sind an PVP angelehnt. Rollen werden mit ; getrennt angeführt. Enthält die Rolle ein "=" ist es ein Rollen-Parameter...
                        // Rollenparameter sind praktisch, weil sie in Filtern usw im CMS verwendet werden können:
                        //    [role-parameter:parameter_name,filter(,logical_operator)]
                        // zB [role-parameter:gemnr,GEM_NR like '{0}%'(,OR)]
                        //
                        // rolws: gemnr=60902,gemnr=61106,gemnr=61116,gemnr=61114
                        // oder: gemnr=60902,61106,61116,61114
                        // oder: User-Rolle1;User-Rolle2
                        // oder: User-Rolle1;User-Rolle2;gemnr=60902,gemnr=61106,gemnr=61116,gemnr=61114

                        SetRolesAndRoleParameters(((string)reader["USERROLES"]));

                        List<string> allRoles = new List<string>();
                        List<string> allRoleParameters = new List<string>();

                        foreach (string userRole in ((string)reader["USERROLES"]).Split(';'))
                        {
                            string role = userRole.Trim();
                            if (allRoles.Contains(role))
                            {
                                continue;
                            }

                            if (role.Contains("="))  // role-parameter
                            {
                                string paramName = role.Substring(0, role.IndexOf("=")).Trim();
                                string paramValues = role.Substring(role.IndexOf("=") + 1, role.Length - role.IndexOf("=") - 1);
                                foreach (string paramValue in paramValues.Split(','))
                                {
                                    string param = paramName + "=" + paramValue.Trim();
                                    if (!allRoleParameters.Contains(param))
                                    {
                                        allRoleParameters.Add(param);
                                    }
                                }
                            }
                            else
                            {
                                allRoles.Add(role);
                            }
                        }

                        this.Roles = allRoles.ToArray();
                        this.RoleParameters = allRoleParameters.Count > 0 ? allRoleParameters.ToArray() : null;
                    }

                    #region Two Factor Authentication

                    if (fieldName == "TFA_ENABLED" && reader["TFA_ENABLED"] is bool)
                    {
                        this.TfaEnabled = Convert.ToBoolean(reader["TFA_ENABLED"]);
                    }
                    if (fieldName == "TFA_SECRET" && !String.IsNullOrEmpty(reader["TFA_SECRET"]?.ToString()))
                    {
                        this.TfaSecret = new Crypto().DecryptTextDefault(reader["TFA_SECRET"].ToString());
                    }
                    if (fieldName == "TFA_RECOVERYCODES" && !String.IsNullOrEmpty(reader["TFA_RECOVERYCODES"]?.ToString()))
                    {
                        this.TfaRecoveryCodes = new Crypto().DecryptTextDefault(reader["TFA_RECOVERYCODES"].ToString());
                    }

                    #endregion

                    #region Client Secrets

                    if(fieldName=="CLIENT_SECRET1" && !String.IsNullOrEmpty(reader["CLIENT_SECRET1"]?.ToString()))
                    {
                        this.ClientSecret1 = new Crypto().DecryptTextDefault(reader["CLIENT_SECRET1"].ToString());
                    }
                    if (fieldName == "CLIENT_SECRET2" && !String.IsNullOrEmpty(reader["CLIENT_SECRET2"]?.ToString()))
                    {
                        this.ClientSecret2 = new Crypto().DecryptTextDefault(reader["CLIENT_SECRET2"].ToString());
                    }

                    #endregion

                    #region Stamps

                    if (fieldName == "CONCURRENCY_STAMP" && reader["CONCURRENCY_STAMP"] != null)
                    {
                        this.ConcurrencyStamp = reader["CONCURRENCY_STAMP"].ToString();
                    }
                    if (fieldName == "SECURITY_STAMP" && reader["SECURITY_STAMP"] != null)
                    {
                        this.SecurityStamp = reader["SECURITY_STAMP"].ToString();
                    }

                    #endregion
                }
            }

            #region Properties

            public int Id { get; set; }
            public string Username { get; set; }
            public string Password
            {
                get
                {
                    return Crypto.DecryptConfigSettingsValue(this.EncryptedPassword);
                }
                set
                {
                    this.EncryptedPassword = Crypto.EncryptConfigSettingsValue(value, Crypto.ResultStringType.Hex);
                }
            }
            public DateTime LastLogin { get; set; }
            public double ExpireDays { get; set; }
            public double ExpireDaysLong { get; set; }
            public int SecurityLevel { get; set; }

            public string[] Roles { get; internal set; }
            public string[] RoleParameters { get; internal set; }

            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Organization { get; set; }

            public string Description { get; set; }

            public string SecurityStamp { get; set; }
            public string ConcurrencyStamp { get; set; }

            public string ClientSecret1 { get; set; }
            public string ClientSecret2 { get; set; }

            public bool TfaEnabled { get; set; }
            public string TfaSecret { get; set; }
            public string TfaRecoveryCodes { get; set; }

            #endregion

            #region Members

            public bool Insert(TicketDb db)
            {
                using (DBFactory factory = new DBFactory(db._connectionString))
                {
                    using (DbConnection connection = factory.GetConnection())
                    {
                        DbCommand command = factory.GetCommand(connection);

                        string roles = GetRoleString();

                        command.CommandText = "insert into " + factory.TableName("OGC_USERS") + " (" + factory.ColumnNames("USERNAME,PASSWORD,EXPIRE,EXPIRE_LONG,SECURITY_LEVEL,USERROLES,FIRSTNAME,LASTNAME,EMAIL,ORGANISATION") + ") values (" + factory.ParaNames("username,password,expire,expire_long,sec_level,userroles,firstname,lastname,email,organisation") + ")";
                        command.Parameters.Add(factory.GetParameter("username", this.Username));
                        command.Parameters.Add(factory.GetParameter("password", this.EncryptedPassword));
                        command.Parameters.Add(factory.GetParameter("expire", this.ExpireDays));
                        command.Parameters.Add(factory.GetParameter("expire_long", this.ExpireDaysLong));
                        command.Parameters.Add(factory.GetParameter("sec_level", this.SecurityLevel));
                        command.Parameters.Add(factory.GetParameter("userroles", String.IsNullOrWhiteSpace(roles) ? (object)DBNull.Value : (object)roles));
                        command.Parameters.Add(factory.GetParameter("firstname", this.FirstName != null ? this.FirstName : String.Empty));
                        command.Parameters.Add(factory.GetParameter("lastname", this.LastName != null ? this.LastName : String.Empty));
                        command.Parameters.Add(factory.GetParameter("email", this.Email != null ? this.Email : String.Empty));
                        command.Parameters.Add(factory.GetParameter("organisation", this.Organization != null ? this.Organization : String.Empty));

                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }

            public bool Update(TicketDb db)
            {
                using (DBFactory factory = new DBFactory(db._connectionString))
                {
                    using (DbConnection connection = factory.GetConnection())
                    {
                        DbCommand command = factory.GetCommand(connection);

                        string roles = GetRoleString();

                        command.CommandText = "update " + factory.TableName("OGC_USERS") + " set "
                                   + factory.ColumnNames("USERNAME") + "=" + factory.ParaName("username") + ","
                                   + factory.ColumnNames("PASSWORD") + "=" + factory.ParaName("password") + ","
                                   + factory.ColumnNames("FIRSTNAME") + "=" + factory.ParaName("firstname") + ","
                                   + factory.ColumnNames("LASTNAME") + "=" + factory.ParaName("lastname") + ","
                                   + factory.ColumnNames("EMAIL") + "=" + factory.ParaName("email") + ","
                                   + factory.ColumnNames("ORGANISATION") + "=" + factory.ParaName("organisation") + ","
                                   + factory.ColumnNames("EXPIRE") + "=" + factory.ParaName("expire") + ","
                                   + factory.ColumnNames("EXPIRE_LONG") + "=" + factory.ParaName("expire_long") + ","
                                   + factory.ColumnNames("SECURITY_LEVEL") + "=" + factory.ParaName("sec_level") + ","
                                   + factory.ColumnNames("USERROLES") + "=" + factory.ParaName("userroles") + ","
                                   + factory.ColumnNames("TFA_ENABLED") + "=" + factory.ParaName("tfa_enabled") + ","
                                   + factory.ColumnNames("TFA_SECRET") + "=" + factory.ParaName("tfa_secret") + ","
                                   + factory.ColumnNames("TFA_RECOVERYCODES") + "=" + factory.ParaName("tfa_recoverycodes") + ","
                                   + factory.ColumnNames("CLIENT_SECRET1") + "=" + factory.ParaName("client_secret1") + ","
                                   + factory.ColumnNames("CLIENT_SECRET2") + "=" + factory.ParaName("client_secret2") + ","
                                   + factory.ColumnNames("DESCRIPTION") + "=" + factory.ParaName("description") + ","
                                   + factory.ColumnNames("CONCURRENCY_STAMP") + "=" + factory.ParaName("concurrency_stamp") + ","
                                   + factory.ColumnNames("SECURITY_STAMP") + "=" + factory.ParaName("security_stamp")
                                   + " where " + factory.ColumnNames("UID") + "=" + factory.ParaName("id");

                        command.Parameters.Add(factory.GetParameter("username", this.Username));
                        command.Parameters.Add(factory.GetParameter("password", this.EncryptedPassword));
                        command.Parameters.Add(factory.GetParameter("expire", this.ExpireDays));
                        command.Parameters.Add(factory.GetParameter("expire_long", this.ExpireDaysLong));
                        command.Parameters.Add(factory.GetParameter("sec_level", this.SecurityLevel));
                        command.Parameters.Add(factory.GetParameter("userroles", String.IsNullOrWhiteSpace(roles) ? (object)DBNull.Value : (object)roles));
                        command.Parameters.Add(factory.GetParameter("id", this.Id));
                        command.Parameters.Add(factory.GetParameter("firstname", this.FirstName != null ? this.FirstName : String.Empty));
                        command.Parameters.Add(factory.GetParameter("lastname", this.LastName != null ? this.LastName : String.Empty));
                        command.Parameters.Add(factory.GetParameter("email", this.Email != null ? this.Email : String.Empty));
                        command.Parameters.Add(factory.GetParameter("organisation", this.Organization != null ? this.Organization : String.Empty));
                        command.Parameters.Add(factory.GetParameter("tfa_enabled", this.TfaEnabled));
                        command.Parameters.Add(factory.GetParameter("tfa_secret",
                            !String.IsNullOrWhiteSpace(this.TfaSecret) ?
                            new Crypto().EncryptTextDefault(this.TfaSecret) :
                            String.Empty));
                        command.Parameters.Add(factory.GetParameter("tfa_recoverycodes", 
                            !String.IsNullOrWhiteSpace(this.TfaRecoveryCodes) ?
                            new Crypto().EncryptTextDefault(this.TfaRecoveryCodes) :
                            String.Empty));
                        command.Parameters.Add(factory.GetParameter("client_secret1", 
                            !String.IsNullOrWhiteSpace(this.ClientSecret1) ?
                            new Crypto().EncryptTextDefault(this.ClientSecret1) :
                            String.Empty));
                        command.Parameters.Add(factory.GetParameter("client_secret2",
                            !String.IsNullOrWhiteSpace(this.ClientSecret2) ?
                            new Crypto().EncryptTextDefault(this.ClientSecret2) :
                            String.Empty));
                        command.Parameters.Add(factory.GetParameter("description", this.Description ?? String.Empty));
                        command.Parameters.Add(factory.GetParameter("concurrency_stamp", this.ConcurrencyStamp ?? String.Empty));
                        command.Parameters.Add(factory.GetParameter("security_stamp", this.SecurityStamp ?? String.Empty));

                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                TicketDb.RemoveUserFromCache(this.Id);

                return true;
            }

            public bool Delete(TicketDb db)
            {
                using (DBFactory factory = new DBFactory(db._connectionString))
                {
                    using (DbConnection connection = factory.GetConnection())
                    {
                        DbCommand command = factory.GetCommand(connection);

                        string roles = GetRoleString();

                        command.CommandText = "delete from " + factory.TableName("OGC_USERS")
                                   + " where " + factory.ColumnNames("UID") + "=" + factory.ParaName("id");

                        command.Parameters.Add(factory.GetParameter("id", this.Id));

                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }

            public bool CommitToDatabase(TicketDb db)
            {
                if (this.Id > 0)
                {
                    return this.Update(db);
                }

                return this.Insert(db);
            }

            public void SetRolesAndRoleParameters(string roleString, string roleParameterString = "")
            {
                if (!String.IsNullOrWhiteSpace(roleParameterString))
                {
                    roleString = (String.IsNullOrWhiteSpace(roleString) ? roleParameterString : roleString + ";" + roleParameterString);
                }


                List<string> allRoles = new List<string>();
                List<string> allRoleParameters = new List<string>();

                if (!String.IsNullOrWhiteSpace(roleString))
                {
                    foreach (string userRole in roleString.Split(';'))
                    {
                        string role = userRole.Trim();
                        if (String.IsNullOrWhiteSpace(role))
                        {
                            continue;
                        }

                        if (allRoles.Contains(role))
                        {
                            continue;
                        }

                        if (role.Contains("="))  // role-parameter
                        {
                            string paramName = role.Substring(0, role.IndexOf("=")).Trim();
                            string paramValues = role.Substring(role.IndexOf("=") + 1, role.Length - role.IndexOf("=") - 1);
                            foreach (string paramValue in paramValues.Split(','))
                            {
                                string param = paramName + "=" + paramValue.Trim();
                                if (!allRoleParameters.Contains(param))
                                {
                                    allRoleParameters.Add(param);
                                }
                            }
                        }
                        else
                        {
                            allRoles.Add(role);
                        }
                    }
                }

                this.Roles = allRoles.ToArray();
                this.RoleParameters = allRoleParameters.Count > 0 ? allRoleParameters.ToArray() : null;
            }

            public bool ValidatePassword(string password)
            {
                return ValidateUserPassword(this.Username, this.Password, password);
            }

            public UserInfo ToUserInfo()
            {
                return new UserInfo()
                {
                    Username = this.Username,
                    Roles = this.Roles,
                    RoleParameters = this.RoleParameters
                };
            }

            #endregion

            #region Helper

            private string GetRoleString()
            {
                string roles = String.Empty;
                if (this.Roles != null)
                {
                    roles = String.Join(";", this.Roles);
                }

                if (this.RoleParameters != null)
                {
                    roles += (!String.IsNullOrWhiteSpace(roles) ? ";" : "") + String.Join(";", this.RoleParameters);
                }

                roles = roles.Replace(" ", "");
                while (roles.Contains(";;"))
                {
                    roles = roles.Replace(";;", ";");
                }

                if (roles.StartsWith(";"))
                {
                    roles = roles.Substring(1, roles.Length - 1);
                }

                if (roles.EndsWith(";"))
                {
                    roles = roles.Substring(0, roles.Length - 1);
                }

                return roles;
            }

            private string EncryptedPassword
            {
                get;
                set;
            }

            #endregion

            #region Static members

            static public bool ValidateUserPassword(string username, string hasdedPassword, string providedPassword)
            {
                if (hasdedPassword.StartsWith("sha0:"))
                {
                    return hasdedPassword == HashPassword(providedPassword, username.ToLower(), "sha0");
                }
                else
                {
                    return hasdedPassword == providedPassword;  // Old Accounts => Unhashed passwords
                }
            }

            static public string HashPassword(string password, string username, string alg = "sha0")
            {
                switch (alg?.ToLower())
                {
                    case "sha0":
                        return $"sha0:{ new Crypto().Hash64(password, username.ToLower()) }";
                    default:
                        throw new NotImplementedException($"HashPassword algormithm { alg } not implemented");
                }
            }

            #endregion
        }

        public class UserInfo : Info
        {
            public UserInfo()
                : base(true)
            {
            }
            
            public string Username { get; set; }
            public string[] Roles { get; set; }
            public string[] RoleParameters { get; set; }

            public User ToUser()
            {
                return new User()
                {
                    Username = this.Username,
                    Roles = this.Roles,
                    RoleParameters = this.RoleParameters
                };
            }
        }

        public class Info
        {
            public Info(bool succeeded)
            {
                this.Succeeded = succeeded;
            }
            
            [JsonProperty(PropertyName = "succeeded")]
[System.Text.Json.Serialization.JsonPropertyName("succeeded")]
            public bool Succeeded { get; set; }

            [JsonProperty(PropertyName = "error_message")]
[System.Text.Json.Serialization.JsonPropertyName("error_message")]
            public string ErrorMessage { get; set; }
        }

        #endregion

        #region Helper

        private T GetRemoteDbItem<T>(string method, NameValueCollection parameters, string token)
            where T : Info
        {
            using(var client=new WebClient())
            {
                client.UseDefaultCredentials = true;

                StringBuilder sb = new StringBuilder();
                foreach(string parameter in parameters.Keys)
                {
                    sb.Append(sb.Length == 0 ? "?" : "&");
                    sb.Append(parameter + "=" + parameters[parameter]);
                }

                string url = this._connectionString + "/" + method + sb.ToString();
                string response = client.DownloadString(url);
                if (response.StartsWith("enc:"))
                {
                    response = new Crypto().DecryptText(response.Substring(4), new Crypto().ToPasswordString(token), Crypto.Strength.AES256);
                }

                T item = JsonConvert.DeserializeObject<T>(response);
                if (!item.Succeeded)
                {
                    throw new Exception(item.ErrorMessage);
                }

                return item;
            }
        }

        #endregion
    }

    [Obsolete("Use E.Standard.Security.Internal assembly")]
    public interface IRemoteTicketDb
    {
        TicketDb.User TicketUser(string token, Uri requestUrl);
        TicketDb.User UserByUsername(string username, string token = "");
        TicketDb.Ticket TicketByToken(string token, Uri requestUrl);
        bool Logout(string token);
    }

    [Obsolete("Use E.Standard.Security.Internal assembly")]
    public class WrongRequestSchemeException : Exception
    {
        
    }
}