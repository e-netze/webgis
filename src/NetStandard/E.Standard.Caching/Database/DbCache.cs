using E.Standard.Caching.Abstraction;
using E.Standard.DbConnector;
using E.Standard.DbConnector.Schema;
using E.Standard.Security.Cryptography.Abstractions;
using System;
using System.Data;
using System.Data.Common;

namespace E.Standard.Caching.Database;

public class DbCache : IKeyValueCache, IDbSchemaProvider
{
    private string _connectionString = String.Empty;
    private readonly string _tableName = String.Empty;

    public DbCache(ICryptoService crypto)
    {
        var tablePostfix = crypto
                .StaticDefaultEncrypt("webgis_cache", Security.Cryptography.CryptoResultStringType.Hex)
                .Substring(2, 12)
                .ToLower();

        _tableName = $"{"webgis_cache"}_{tablePostfix}";
    }

    #region IDbSchemaProvider

    public DataSet DbSchema
    {
        get
        {
            DataSet dataset = new DataSet();

            DataTable table = new DataTable(_tableName);

            table.Columns.Add(new DataColumn("#cache_key:256", typeof(string)));
            table.Columns.Add(new DataColumn("cache_val:4000", typeof(string)));
            table.Columns.Add(new DataColumn("created", typeof(DateTime)));
            table.Columns.Add(new DataColumn("expires", typeof(DateTime)));

            dataset.Tables.Add(table);

            return dataset;
        }
    }

    public string DbConnectionString { get { return _connectionString; } }

    #endregion

    #region IKeyValueCache Member

    public bool Init(string initialParameter)
    {
        _connectionString = initialParameter;
        return true;
    }

    public string Get(string key)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection connection = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.CommandText = $"select {conn.ColumnNames2("cache_val")} from {conn.TableName2(_tableName)} where {conn.ColumnNames2("cache_key")}={conn.ParaName("key")}";

                DbParameter keyParam = conn.GetParameter("key", key);
                command.Parameters.Add(keyParam);

                command.Connection = connection;
                connection.Open();

                object ret = command.ExecuteScalar();
                connection.Close();

                return ret != null && ret != DBNull.Value ? ret.ToString() : null;
            }
        }
    }

    public void Set(string key, object o, double expireSeconds)
    {
        if (o == null)
        {
            return;
        }

        // Must be Unique -> Remove old first!!
        Remove(key);

        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection connection = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();

                command.CommandText = $"insert into {conn.TableName2(_tableName)} ({conn.ColumnNames2("cache_key,cache_val,created,expires")}) values ({conn.ParaName("key,val,created,expires")})";

                DbParameter keyParam = conn.GetParameter("key", key);
                DbParameter valParam = conn.GetParameter("val", o.ToString());
                DbParameter crParam = conn.GetParameter("created", DateTime.UtcNow);
                DbParameter exParam = conn.GetParameter("expires", DateTime.UtcNow.AddSeconds(expireSeconds));

                command.Parameters.Add(keyParam);
                command.Parameters.Add(valParam);
                command.Parameters.Add(crParam);
                command.Parameters.Add(exParam);

                command.Connection = connection;
                connection.Open();

                command.ExecuteNonQuery();
                connection.Close();
            }
        }
    }

    public void Remove(string key)
    {
        using (DBConnection conn = new DBConnection())
        {
            conn.OleDbConnectionMDB = _connectionString;

            using (DbConnection connection = conn.GetConnection())
            {
                DbCommand command = conn.GetCommand();
                command.CommandText = $"delete from {conn.TableName2(_tableName)} where {conn.ColumnNames2("cache_key")}={conn.ParaName("key")}";

                DbParameter keyParam = conn.GetParameter("key", key);
                command.Parameters.Add(keyParam);

                command.Connection = connection;
                connection.Open();

                command.ExecuteNonQuery();
                connection.Close();
            }
        }
    }

    public int MaxChunkSize => 4000;

    #endregion
}
