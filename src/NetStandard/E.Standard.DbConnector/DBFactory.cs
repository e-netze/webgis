using E.Standard.DbConnector.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace E.Standard.DbConnector;

public class DBFactory : IDisposable
{
    private readonly DBConnection _connection = new DBConnection();
    Dictionary<Type, DbType> _typeMap = null;

    public DBFactory(string connectionString)
    {
        _connection.OleDbConnectionMDB = connectionString;
    }

    private void InitTypeMap()
    {
        if (_typeMap == null)
        {
            _typeMap = new Dictionary<Type, DbType>();
        }
        else
        {
            return;
        }

        _typeMap[typeof(byte)] = DbType.Byte;
        _typeMap[typeof(sbyte)] = DbType.SByte;
        _typeMap[typeof(short)] = DbType.Int16;
        _typeMap[typeof(ushort)] = DbType.UInt16;
        _typeMap[typeof(int)] = DbType.Int32;
        _typeMap[typeof(uint)] = DbType.UInt32;
        _typeMap[typeof(long)] = DbType.Int64;
        _typeMap[typeof(ulong)] = DbType.UInt64;
        _typeMap[typeof(float)] = DbType.Single;
        _typeMap[typeof(double)] = DbType.Double;
        _typeMap[typeof(decimal)] = DbType.Decimal;
        _typeMap[typeof(bool)] = DbType.Boolean;
        _typeMap[typeof(string)] = DbType.String;
        _typeMap[typeof(char)] = DbType.StringFixedLength;
        _typeMap[typeof(Guid)] = DbType.Guid;
        _typeMap[typeof(DateTime)] = DbType.DateTime;
        _typeMap[typeof(DateTimeOffset)] = DbType.DateTimeOffset;
        _typeMap[typeof(byte[])] = DbType.Binary;
        _typeMap[typeof(byte?)] = DbType.Byte;
        _typeMap[typeof(sbyte?)] = DbType.SByte;
        _typeMap[typeof(short?)] = DbType.Int16;
        _typeMap[typeof(ushort?)] = DbType.UInt16;
        _typeMap[typeof(int?)] = DbType.Int32;
        _typeMap[typeof(uint?)] = DbType.UInt32;
        _typeMap[typeof(long?)] = DbType.Int64;
        _typeMap[typeof(ulong?)] = DbType.UInt64;
        _typeMap[typeof(float?)] = DbType.Single;
        _typeMap[typeof(double?)] = DbType.Double;
        _typeMap[typeof(decimal?)] = DbType.Decimal;
        _typeMap[typeof(bool?)] = DbType.Boolean;
        _typeMap[typeof(char?)] = DbType.StringFixedLength;
        _typeMap[typeof(Guid?)] = DbType.Guid;
        _typeMap[typeof(DateTime?)] = DbType.DateTime;
        _typeMap[typeof(DateTimeOffset?)] = DbType.DateTimeOffset;
    }

    public DbType GetDbType(Type type)
    {
        if (type == null)
        {
            return DbType.String;
        }

        InitTypeMap();

        try
        {
            return _typeMap[type];
        }
        catch { return DbType.String; }
    }

    public DbConnection GetConnection()
    {
        return _connection.GetConnection();
    }
    public DbCommand GetCommand(DbConnection connection = null)
    {
        DbCommand command = _connection.GetCommand();
        if (command != null && connection != null)
        {
            command.Connection = connection;
        }

        return command;
    }
    public DbParameter GetParameter(string key, object val, Type type = null)
    {
        var parameter = _connection.GetParameter(key, val);

        if (val == null || val == DBNull.Value)
        {
            parameter.DbType = GetDbType(type);
        }

        return parameter;
    }

    public string ParaName(string key)
    {
        return _connection.ParaName(key);
    }

    public string ParaNames(string keys)
    {
        return _connection.ParaNames(keys);
    }

    public string TableName(string key)
    {
        return _connection.TableName2(key);
    }

    public string ColumnNames(string columns)
    {
        return _connection.ColumnNames2(columns);
    }

    #region Schema

    public string DbTypeName(Type type, int size = 0, bool primaryKey = false, bool isSerial = false, bool notNull = false)
    {
        InitTypeMap();

        if (!_typeMap.ContainsKey(type))
        {
            throw new DatabaseException("Unknown DbType: " + type.ToString());
        }

        var dbType = _typeMap[type];

        string fieldType = String.Empty;

        switch (_connection.DatabaseType)
        {
            case DBType.sql:
                switch (dbType)
                {
                    case DbType.Byte:
                        fieldType = "tinyint";
                        break;
                    case DbType.SByte:
                        fieldType = "tinyint";
                        break;
                    case DbType.Int16:
                        fieldType = "smallint";
                        break;
                    case DbType.UInt16:
                        fieldType = "smallint";
                        break;
                    case DbType.Int32:
                        fieldType = "int";
                        break;
                    case DbType.UInt32:
                        fieldType = "int";
                        break;
                    case DbType.Int64:
                        fieldType = "bigint";
                        break;
                    case DbType.UInt64:
                        fieldType = "bigint";
                        break;
                    case DbType.Single:
                        fieldType = "float";
                        break;
                    case DbType.Double:
                        fieldType = "real";
                        break;
                    case DbType.Decimal:
                        fieldType = "real";
                        break;
                    case DbType.Boolean:
                        fieldType = "bit";
                        break;
                    case DbType.String:
                        fieldType = "nvarchar";
                        break;
                    case DbType.StringFixedLength:
                        fieldType = "nvarchar";
                        break;
                    case DbType.Guid:
                        fieldType = "uniqueidentifier";
                        break;
                    case DbType.DateTime:
                        fieldType = "datetime";
                        break;
                    case DbType.DateTimeOffset:
                        fieldType = "datetimeoffset";
                        break;
                    case DbType.Binary:
                        fieldType = "binary";
                        break;
                }
                break;
            case DBType.oracle:
                switch (dbType)
                {
                    case DbType.Byte:
                        fieldType = "NUMBER(3)";
                        break;
                    case DbType.SByte:
                        fieldType = "NUMBER(3)";
                        break;
                    case DbType.Int16:
                        fieldType = "NUMBER(5)";
                        break;
                    case DbType.UInt16:
                        fieldType = "NUMBER(5)";
                        break;
                    case DbType.Int32:
                        fieldType = "NUMBER(10)";
                        break;
                    case DbType.UInt32:
                        fieldType = "NUMBER(10)";
                        break;
                    case DbType.Int64:
                        fieldType = "NUMBER(19)";
                        break;
                    case DbType.UInt64:
                        fieldType = "NUMBER(19)";
                        break;
                    case DbType.Single:
                        fieldType = "BINARY_FLOAT";
                        break;
                    case DbType.Double:
                        fieldType = "BINARY_DOUBLE";
                        break;
                    case DbType.Decimal:
                        fieldType = "BINARY_DOUBLE";
                        break;
                    case DbType.Boolean:
                        fieldType = "NUMBER(1)"; //"BOOLEAN";
                        break;
                    case DbType.String:
                        if (size > 2000)
                        {
                            size = 0;
                            fieldType = "CLOB";
                        }
                        else
                        {
                            fieldType = "NVARCHAR2";
                        }
                        break;
                    case DbType.StringFixedLength:
                        fieldType = "CHAR";
                        break;
                    case DbType.Guid:
                        fieldType = "RAW(16)";
                        break;
                    case DbType.DateTime:
                        fieldType = "TIMESTAMP(3)";
                        break;
                    case DbType.DateTimeOffset:
                        fieldType = "TIMESTAMP WITH TIME ZONE";
                        break;
                    case DbType.Binary:
                        fieldType = "LONG RAW";
                        break;
                }
                break;
            case DBType.postgres:
                switch (dbType)
                {
                    case DbType.Byte:
                        fieldType = "smallint";
                        break;
                    case DbType.SByte:
                        fieldType = "smallint";
                        break;
                    case DbType.Int16:
                        fieldType = "smallint";
                        break;
                    case DbType.UInt16:
                        fieldType = "smallint";
                        break;
                    case DbType.Int32:
                        fieldType = "integer";
                        break;
                    case DbType.UInt32:
                        fieldType = "integer";
                        break;
                    case DbType.Int64:
                        fieldType = "bigint";
                        break;
                    case DbType.UInt64:
                        fieldType = "bigint";
                        break;
                    case DbType.Single:
                        fieldType = "real";
                        break;
                    case DbType.Double:
                        fieldType = "double precision";
                        break;
                    case DbType.Decimal:
                        fieldType = "decimal";
                        break;
                    case DbType.Boolean:
                        fieldType = "boolean";
                        break;
                    case DbType.String:
                        fieldType = "varchar";
                        break;
                    case DbType.StringFixedLength:
                        fieldType = "char";
                        break;
                    case DbType.Guid:
                        fieldType = "uuid";
                        break;
                    case DbType.DateTime:
                        fieldType = "timestamp";
                        break;
                    case DbType.DateTimeOffset:
                        fieldType = "timestamp with time zone";
                        break;
                    case DbType.Binary:
                        fieldType = "bytea";
                        break;
                }
                break;
            case DBType.sqlite:
                switch (dbType)
                {
                    case DbType.Byte:
                        fieldType = "INTEGER";
                        break;
                    case DbType.SByte:
                        fieldType = "INTEGER";
                        break;
                    case DbType.Int16:
                        fieldType = "INTEGER";
                        break;
                    case DbType.UInt16:
                        fieldType = "INTEGER";
                        break;
                    case DbType.Int32:
                        fieldType = "INTEGER";
                        break;
                    case DbType.UInt32:
                        fieldType = "INTEGER";
                        break;
                    case DbType.Int64:
                        fieldType = "INTEGER";
                        break;
                    case DbType.UInt64:
                        fieldType = "INTEGER";
                        break;
                    case DbType.Single:
                        fieldType = "REAL";
                        break;
                    case DbType.Double:
                        fieldType = "REAL";
                        break;
                    case DbType.Decimal:
                        fieldType = "REAL";
                        break;
                    case DbType.Boolean:
                        fieldType = "INTEGER";
                        break;
                    case DbType.String:
                        fieldType = "TEXT";
                        break;
                    case DbType.StringFixedLength:
                        fieldType = "TEXT";
                        break;
                    case DbType.Guid:
                        fieldType = "TEXT";
                        break;
                    case DbType.DateTime:
                        fieldType = "TEXT";
                        break;
                    case DbType.DateTimeOffset:
                        fieldType = "TEXT";
                        break;
                    case DbType.Binary:
                        fieldType = "BLOB";
                        break;
                }
                break;
        }

        if (size > 0)
        {
            fieldType += "(" + size + ")";
        }

        if (isSerial)
        {
            switch (_connection.DatabaseType)
            {
                case DBType.sql:
                    fieldType += " IDENTITY(1,1)";
                    break;
                case DBType.oracle:
                    //throw new NotImplementedException("Serial not implemented");
                    break;
                case DBType.postgres:
                    fieldType = "serial";
                    break;
                case DBType.sqlite:
                    fieldType += " AUTOINCREMENT";
                    break;
            }
        }

        if (primaryKey == true)
        {
            fieldType += " PRIMARY KEY";
        }

        if (notNull && !fieldType.ToLower().Contains("not null"))
        {
            fieldType += " not null";
        }

        return fieldType;
    }

    public bool TableExits(string tableName)
    {
        bool exists;

        using (var connection = this.GetConnection())
        {
            connection.Open();
            try
            {
                // ANSI SQL way.  Works in PostgreSQL, MSSQL, MySQL.  
                var cmd = this.GetCommand(connection);
                cmd.CommandText = "select case when exists((select * from information_schema.tables where table_name = '" + tableName + "')) then 1 else 0 end";

                exists = (int)cmd.ExecuteScalar() == 1;
            }
            catch
            {
                try
                {
                    // Other RDBMS.  Graceful degradation
                    exists = true;
                    var cmdOthers = this.GetCommand(connection);
                    cmdOthers.CommandText = "select 1 from " + tableName + " where 1 = 0";
                    cmdOthers.ExecuteNonQuery();
                }
                catch
                {
                    exists = false;
                }
            }
        }

        return exists;
    }

    public string[] AfterCreateTableCommands(string tableName, string[] serialFieldNames)
    {
        if (_connection.DatabaseType == DBType.oracle)
        {
            List<string> commands = new List<string>();

            for (int i = 0; i < serialFieldNames.Length; i++)
            {
                string serialFieldName = serialFieldNames[i];

                string seqName = tableName + "_seq" + i;
                commands.Add("CREATE SEQUENCE " + seqName + " START WITH 1");
                commands.Add(
@"CREATE OR REPLACE TRIGGER " + tableName + "_tr" + i + @"
BEFORE INSERT ON " + tableName + @"
FOR EACH ROW

BEGIN
   SELECT " + seqName + @".NEXTVAL
   INTO   :new." + serialFieldName + @"  
   FROM dual;
END;
");
            }

            return commands.ToArray();
        }

        return new string[0];
    }

    #endregion

    #region IDisposable Member

    public void Dispose()
    {
        _connection.Dispose();
    }

    #endregion
}
