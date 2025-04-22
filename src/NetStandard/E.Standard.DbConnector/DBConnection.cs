using E.Standard.DbConnector.Exceptions;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Xml;

namespace E.Standard.DbConnector;

public enum DBType { sql, oracle, postgres, sqlite, microsoftsql }
public enum dataType { integer, real, date, text, boolean, currency, unknown }

/// <summary>
/// Zusammenfassung für DBConnection.
/// </summary>
public class DBConnection : IDisposable
{
    protected string _connectionString = "";
    protected string _lastErrorMessage = "";
    protected DBType _dbType = DBType.sql;
    protected DataTable _schemaTable = null;
    protected DbDataAdapter _updateAdapter = null;

    public DBConnection()
    {
    }

    public void Dispose()
    {
        if (_updateAdapter != null)
        {
            _updateAdapter.Dispose();
            _updateAdapter = null;
        }
        if (_schemaTable != null)
        {
            _schemaTable.Dispose();
            _schemaTable = null;
        }
        //GC.Collect(0);
    }
    public string OleDbConnectionMDB
    {
        set
        {
            if (value.ToLower().IndexOf("sql:") == 0)
            {
                _connectionString = value.Substring(4, value.Length - 4);
                _dbType = DBType.sql;
            }
            else if (value.ToLower().IndexOf("mssql:") == 0)
            {
                _connectionString = value.Substring(6, value.Length - 6);
                _dbType = DBType.microsoftsql;
            }
            else if (value.ToLower().IndexOf("oracle:") == 0)
            {
                _connectionString = value.Substring(7, value.Length - 7);
                _dbType = DBType.oracle;
            }
            else if (value.ToLower().IndexOf("postgres:") == 0)
            {
                _connectionString = value.Substring(9, value.Length - 9);
                _dbType = DBType.postgres;
            }
            else if (value.ToLower().IndexOf("sqlite:") == 0)
            {
                _connectionString = value.Substring(7, value.Length - 7);
                _dbType = DBType.sqlite;
            }
            else
            {
                throw new DatabaseException($"Unsupported DBType: {value.Substring(0, Math.Min(value.Length, 5))}");
            }

            _connectionString = _connectionString.AddRequiredConnectionStringParameters(_dbType);
        }
    }

    public void ParseConnectionString(string connectionString)
    {
        this.OleDbConnectionMDB = connectionString;
    }

    public DBType DatabaseType
    {
        get { return _dbType; }
        set { _dbType = value; }
    }

    public string errorMessage { get { return _lastErrorMessage; } }

    public DataTable Select(string sql)
    {
        DataSet ds = new DataSet();
        if (SQLQuery(ref ds, sql, "TAB1"))
        {
            return ds.Tables["TAB1"];
        }
        return null;
    }
    public bool SQLQuery(ref DataSet ds, string sql, string table)
    {
        DbDataAdapter adapter = null;
        DbConnection connection = null;
        try
        {
            switch (_dbType)
            {
                case DBType.sql:
                    connection = new SqlConnection(_connectionString);
                    adapter = new SqlDataAdapter(sql, (SqlConnection)connection);
                    break;
                case DBType.microsoftsql:
                    connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                    adapter = new Microsoft.Data.SqlClient.SqlDataAdapter(sql, (Microsoft.Data.SqlClient.SqlConnection)connection);
                    break;
                case DBType.oracle:
                    connection = new OracleConnection(_connectionString);
                    adapter = new OracleDataAdapter(sql, (OracleConnection)connection);
                    break;
                case DBType.postgres:
                    connection = new Npgsql.NpgsqlConnection(_connectionString);
                    adapter = new Npgsql.NpgsqlDataAdapter(sql, (Npgsql.NpgsqlConnection)connection);
                    break;
                case DBType.sqlite:
                    using (var sqliteConnection = new SQLiteConnection(_connectionString))
                    {
                        sqliteConnection.Open();
                        var sqliteCommand = new SQLiteCommand(sql, sqliteConnection);
                        var dataset = DataProvider.ToDataset(sqliteCommand, ds: ds);
                    }
                    //connection = new SqliteConnection(m_connStr);
                    //adapter = new SqliteDataAdapter(sql, (SQLiteConnection)connection);
                    break;
            }
            if (adapter != null)
            {
                adapter.Fill(ds, table);
            }
        }
        catch (Exception e)
        {
            //if (adapter != null) adapter.Dispose();
            _lastErrorMessage = "QUERY (" + sql + "): " + e.Message;
            WriteEventlogMessage(e);
            return false;
        }
        finally
        {
            if (connection != null)
            {
                try
                {
                    connection.Close();
                    connection.Dispose();
                }
                catch { }
                connection = null;
            }
            if (adapter != null)
            {
                try
                {
                    adapter.Dispose();
                }
                catch { }
                adapter = null;
            }
        }
        return true;
    }

    internal static string GetFieldPlacehoder(string str)
    {
        int pos = str.IndexOf("[");
        if (pos == -1)
        {
            return "";
        }

        int pos2 = str.IndexOf("]");
        if (pos2 == -1)
        {
            return "";
        }

        return str.Substring(pos + 1, pos2 - pos - 1);
    }

    internal static string GetFieldValue(XmlNode feature, string name)
    {
        XmlNodeList fields = feature.SelectNodes("FIELDS/FIELD");
        name = name.ToUpper();
        foreach (XmlNode field in fields)
        {
            string fieldname = field.Attributes["name"].Value.ToString().ToUpper();

            if (fieldname == name || ShortName(fieldname).ToUpper() == name)
            {
                string val = field.Attributes["value"].Value.ToString();
                return val;
            }
        }
        return "";
    }

    private static string ShortName(string name)
    {
        name.Trim();
        int index = name.IndexOf(".");
        while (index != -1)
        {
            name = name.Substring(index + 1, name.Length - index - 1);
            index = name.IndexOf(".");
        }
        return name;
    }

    public object ExecuteScalar(string sql)
    {
        switch (_dbType)
        {
            case DBType.sql:
                return ExecuteScalarSql(sql);
            case DBType.oracle:
                return ExecuteScalarOracle(sql);
            case DBType.postgres:
                return ExecuteScalarNpgsql(sql);
            case DBType.sqlite:
                return ExecuteScalarSQLite(sql);
        }
        _lastErrorMessage = "Not Implemented...";
        WriteEventlogMessage(new Exception(_lastErrorMessage));
        return null;
    }

    private object ExecuteScalarSql(string sql)
    {
        if (_dbType != DBType.sql)
        {
            return false;
        }

        SqlConnection connection = null;
        SqlCommand command = null;
        try
        {
            using (connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                command = new SqlCommand(sql, connection);
                object ret = command.ExecuteScalar();

                connection.Close();
                return ret;
            }

        }
        catch (Exception e)
        {
            if (command != null)
            {
                command.Dispose();
            }

            if (connection != null)
            {
                connection.Close();
                connection.Dispose();
            }
            _lastErrorMessage = e.Message;
            WriteEventlogMessage(e);
            return null;
        }
    }
    private object ExecuteScalarOracle(string sql)
    {
        if (_dbType != DBType.oracle)
        {
            return false;
        }

        try
        {
            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                OracleCommand command = new OracleCommand(sql, connection);
                connection.Open();

                object ret = command.ExecuteScalar();
                connection.Close();
                return ret;
            }
        }
        catch (Exception e)
        {
            _lastErrorMessage = e.Message;
            WriteEventlogMessage(e);
            return null;
        }
    }

    private object ExecuteScalarNpgsql(string sql)
    {
        if (_dbType != DBType.postgres)
        {
            return false;
        }

        Npgsql.NpgsqlConnection connection = null;
        Npgsql.NpgsqlCommand command = null;
        try
        {
            using (connection = new Npgsql.NpgsqlConnection(_connectionString))
            {
                connection.Open();

                command = new Npgsql.NpgsqlCommand(sql, connection);
                object ret = command.ExecuteScalar();

                connection.Close();
                return ret;
            }

        }
        catch (Exception e)
        {
            if (command != null)
            {
                command.Dispose();
            }

            if (connection != null)
            {
                connection.Close();
                connection.Dispose();
            }
            _lastErrorMessage = e.Message;
            WriteEventlogMessage(e);
            return null;
        }
    }
    private object ExecuteScalarSQLite(string sql)
    {
        if (_dbType != DBType.sqlite)
        {
            return false;
        }

        SQLiteConnection connection = null;
        SQLiteCommand command = null;
        try
        {
            using (connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                command = new SQLiteCommand(sql, connection);
                object ret = command.ExecuteScalar();

                connection.Close();
                return ret;
            }

        }
        catch (Exception e)
        {
            if (command != null)
            {
                command.Dispose();
            }

            if (connection != null)
            {
                connection.Close();
                connection.Dispose();
            }
            _lastErrorMessage = e.Message;
            WriteEventlogMessage(e);
            return null;
        }
    }

    public static string LogPath = String.Empty;
    private static object _thisLock = new object();
    public void WriteEventlogMessage(Exception ex)
    {
        if (String.IsNullOrEmpty(LogPath))
        {
            return;
        }

        try
        {
            lock (_thisLock)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Exception Type: " + ex.GetType().ToString() + " \r\n");
                sb.Append("    Message   : " + ex.Message + "\r\n");
                sb.Append("    Source    : " + ex.Source + "\r\n");
                sb.Append("    Stacktrace: " + ex.StackTrace);

                FileInfo fi = new FileInfo(LogPath + @"\dbconnector_exceptions.log");

                StreamWriter sw = null;
                try
                {
                    if (!fi.Directory.Exists)
                    {
                        fi.Directory.Create();
                    }

                    if (fi.Exists && fi.Length > 10 * 1024 * 1024)
                    {
                        try { fi.Delete(); }
                        catch { }
                    }

                    sw = new StreamWriter(fi.FullName, true);
                    sw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + " " + sb.ToString());
                }
                catch { }
                finally
                {
                    if (sw != null)
                    {
                        sw.Close();
                    }
                }
            }
        }
        catch
        {
        }
    }

    #region Factory
    public DbConnection GetConnection()
    {
        switch (_dbType)
        {
            case DBType.sql:
                return new SqlConnection(_connectionString);
            case DBType.microsoftsql:
                return new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
            case DBType.oracle:
                return new OracleConnection(_connectionString);
            case DBType.postgres:
                return new Npgsql.NpgsqlConnection(_connectionString);
            case DBType.sqlite:
                return new SQLiteConnection(_connectionString);
        }
        return null;
    }

    public DbCommand GetCommand()
    {
        switch (_dbType)
        {
            case DBType.sql:
                return new SqlCommand();
            case DBType.microsoftsql:
                return new Microsoft.Data.SqlClient.SqlCommand();
            case DBType.oracle:
                return new OracleCommand();
            case DBType.postgres:
                return new Npgsql.NpgsqlCommand();
            case DBType.sqlite:
                return new SQLiteCommand();
        }
        return null;
    }

    public DbParameter GetParameter(string name, object value)
    {
        switch (_dbType)
        {
            case DBType.sql:
                return new SqlParameter(ParaName(name), value);
            case DBType.microsoftsql:
                return new Microsoft.Data.SqlClient.SqlParameter(ParaName(name), value);
            case DBType.oracle:
                if (value is bool)  // Bool ist bei Oracle immer NUMBER(1)
                {
                    value = (bool)value == true ? 1 : 0;
                }
                //else if(value is int)
                //{
                //    return new OracleParameter(name, value)
                //    {
                //        OracleDbType = OracleDbType.Int32
                //    };
                //}
                return new OracleParameter(ParaName(name), value);
            case DBType.postgres:
                return new Npgsql.NpgsqlParameter(ParaName(name), value);
            case DBType.sqlite:
                return new SQLiteParameter(ParaName(name), value);
        }
        return null;
    }

    public string ParaName(string name)
    {
        name = name.Trim().Replace(" ", "");
        if (_dbType == DBType.oracle)
        {
            return ":" + name.Replace(":", "").Replace(",", ",:");
        }
        return "@" + name.Replace("@", "").Replace(",", ",@");
    }

    public string ParaNames(string names)
    {
        StringBuilder sb = new StringBuilder();
        foreach (string name in names.Split(','))
        {
            if (sb.Length > 0)
            {
                sb.Append(",");
            }

            sb.Append(ParaName(name));
        }
        return sb.ToString();
    }

    #endregion

    public string TableName2(string table)
    {
        StringBuilder sb = new StringBuilder();

        foreach (string t in table.Split('.'))
        {
            if (sb.Length > 0)
            {
                sb.Append(".");
            }

            switch (_dbType)
            {
                case DBType.postgres:
                    sb.Append("\"" + t/*.ToLower()*/ + "\"");
                    break;
                case DBType.microsoftsql:
                case DBType.sql:
                    sb.Append("[" + t + "]");
                    break;
                default:
                    sb.Append(t);
                    break;
            }
        }
        return sb.ToString();
    }

    public string ColumnNames2(string columns)
    {
        switch (_dbType)
        {
            case DBType.postgres:
                return "\"" + columns/*.ToLower()*/.Replace(" ", "").Replace(",", "\",\"") + "\"";
            case DBType.microsoftsql:
            case DBType.sql:
                return "[" + columns.Replace(" ", "").Replace(",", "],[") + "]";

        }
        return columns;
    }
}
