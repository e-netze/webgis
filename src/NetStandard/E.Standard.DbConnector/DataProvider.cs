using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Text;
using System.Xml;

namespace E.Standard.DbConnector;

/// <summary>
/// Zusammenfassung für DataProvider.
/// </summary>
public class DataProvider
{
    private SqlConnection _sqlConnection = null;
    private OracleConnection _oracleConnection = null;
    private Npgsql.NpgsqlConnection _npgsqlConnection = null;
    private SQLiteConnection _sqliteConnection = null;
    private string _lastErrorMessage = "";

    public DataProvider()
    {
    }

    public void Dispose()
    {
        Close();
    }

    public string LastErrorMessage
    {
        get { return _lastErrorMessage; }
    }
    public bool Open(string connectionString)
    {
        return Open(connectionString, false);
    }
    public bool Open(string connectionString, bool testIt)
    {
        int pos = connectionString.IndexOf(":");
        if (pos == -1)
        {
            return false;
        }

        try
        {
            Close();

            string dbType = connectionString.Substring(0, pos);
            connectionString = connectionString
                .Substring(pos + 1, connectionString.Length - pos - 1)
                .AddRequiredConnectionStringParameters(dbType);

            switch (dbType.ToLower())
            {
                case "oracle":
                    _oracleConnection = new OracleConnection(connectionString);
                    if (testIt)
                    {
                        _oracleConnection.Open();
                        _oracleConnection.Close();
                    }
                    break;
                case "sql":
                    _sqlConnection = new SqlConnection(connectionString);
                    if (testIt)
                    {
                        _sqlConnection.Open();
                        _sqlConnection.Close();
                    }
                    break;
                case "postgres":
                    _npgsqlConnection = new Npgsql.NpgsqlConnection(connectionString);
                    if (testIt)
                    {
                        _npgsqlConnection.Open();
                        _npgsqlConnection.Close();
                    }
                    break;
                case "sqlite":
                    _sqliteConnection = new SQLiteConnection(connectionString);
                    if (testIt)
                    {
                        _sqliteConnection.Open();
                        _sqliteConnection.Close();
                    }
                    break;
            }

            return true;
        }
        catch (Exception ex)
        {
            _lastErrorMessage = ex.Message;
            return false;
        }
    }
    public void Close()
    {
        if (_oracleConnection != null)
        {
            _oracleConnection.Close();
            _oracleConnection.Dispose();
        }
        if (_sqlConnection != null)
        {
            _sqlConnection.Close();
            _sqlConnection.Dispose();
        }
        if (_npgsqlConnection != null)
        {
            _npgsqlConnection.Close();
            _npgsqlConnection.Dispose();
        }
        if (_sqliteConnection != null)
        {
            _sqliteConnection.Close();
            _sqliteConnection.Dispose();
        }
        _oracleConnection = null;
        _sqlConnection = null;
        _npgsqlConnection = null;
        _sqliteConnection = null;
    }

    public DataTable ExecuteQuery(string selectCommandText)
    {
        try
        {
            DataSet ds = new DataSet();

            if (_oracleConnection != null)
            {
                _oracleConnection.Open();
                OracleDataAdapter adapter = new OracleDataAdapter(selectCommandText, _oracleConnection);
                adapter.Fill(ds);
                _oracleConnection.Close();
            }
            else if (_sqlConnection != null)
            {
                _sqlConnection.Open();
                SqlDataAdapter adapter = new SqlDataAdapter(selectCommandText, _sqlConnection);
                adapter.Fill(ds);
                _sqlConnection.Close();
            }
            else if (_npgsqlConnection != null)
            {
                _npgsqlConnection.Open();
                Npgsql.NpgsqlDataAdapter adapter = new Npgsql.NpgsqlDataAdapter(selectCommandText, _npgsqlConnection);
                adapter.Fill(ds);
                _npgsqlConnection.Close();
            }
            else if (_sqliteConnection != null)
            {
                //sqliteConnection.Open();
                //SqliteDataAdapter adapter = new System.Data.SQLite.SQLiteDataAdapter(sql, sqliteConnection);
                //adapter.Fill(ds);
                //sqliteConnection.Close();

                _sqliteConnection.Open();
                var command = new SQLiteCommand(selectCommandText, _sqliteConnection);
                ds = ToDataset(command);
                _sqliteConnection.Close();
            }
            if (ds.Tables.Count == 0)
            {
                return null;
            }

            return ds.Tables[0];
        }
        catch (Exception ex)
        {
            if (_oracleConnection != null)
            {
                _oracleConnection.Close();
            }

            if (_sqlConnection != null)
            {
                _sqlConnection.Close();
            }

            if (_npgsqlConnection != null)
            {
                _npgsqlConnection.Close();
            }

            if (_sqliteConnection != null)
            {
                _sqliteConnection.Close();
            }

            _lastErrorMessage = ex.Message;
            return null;
        }
    }
    public bool Join(string sql, XmlNode feature)
    {
        return Join(sql, feature, true);
    }
    public bool Join(string sql, XmlNode feature, bool one2n)
    {
        string field = DBConnection.GetFieldPlacehoder(sql);
        while (field != "")
        {
            string val = DBConnection.GetFieldValue(feature, field);
            if (val == "" && field == "BFL")
            {
                val = " "; // return false;  nur für die Schulung
            }

            sql = sql.Replace("[" + field + "]", val);
            field = DBConnection.GetFieldPlacehoder(sql);
        }

        DataTable table = ExecuteQuery(sql);
        if (table == null)
        {
            return false;
        }

        if (table.Rows.Count == 0)
        {
            return false;
        }

        DataRow row = table.Rows[0];

        XmlNodeList fields = feature.SelectNodes("FIELDS");
        if (fields.Count == 0)
        {
            return false;
        }

        int rowCount = table.Rows.Count;
        XmlAttribute attr;
        foreach (DataColumn col in table.Columns)
        {
            XmlNode fieldNode = feature.OwnerDocument.CreateNode(XmlNodeType.Element, "FIELD", "");

            attr = feature.OwnerDocument.CreateAttribute("name");
            attr.Value = col.ColumnName;
            fieldNode.Attributes.Append(attr);
            attr = feature.OwnerDocument.CreateAttribute("value");
            if (!one2n)
            {
                attr.Value = row[col.ColumnName].ToString();
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("<table width='100%' cellpadding='0' cellspacing='0' border='0'>");

                int count = 1;
                foreach (DataRow row2 in table.Rows)
                {
                    if (count < rowCount)
                    {
                        sb.Append("<tr><td nowrap style='border-bottom: gray 1px solid'>");
                    }
                    else
                    {
                        sb.Append("<tr><td nowrap>");
                    }

                    string val = row2[col.ColumnName].ToString().Trim();
                    if (val == "")
                    {
                        val = "&nbsp;";
                    }

                    sb.Append(val + "</td></tr>");
                    count++;
                }
                sb.Append("</table>");
                attr.Value = sb.ToString();
            }
            fieldNode.Attributes.Append(attr);
            attr = feature.OwnerDocument.CreateAttribute("type");
            attr.Value = "12";
            fieldNode.Attributes.Append(attr);
            fields[0].AppendChild(fieldNode);
        }
        return true;
    }

    #region Static Helpers

    static internal DataSet ToDataset(DbCommand command, string tableName = null, DataSet ds = null)
    {
        var dataset = ds ?? new DataSet();
        var table = new DataTable(tableName ?? "TAB1");

        dataset.Tables.Add(table);

        using (var reader = command.ExecuteReader())
        {
            int fieldCount = reader.FieldCount;

            for (int i = 0; i < fieldCount; i++)
            {
                var fieldName = reader.GetName(i);
                var fieldType = reader.GetFieldType(i);

                table.Columns.Add(new DataColumn(fieldName, fieldType));
            }

            while (reader.Read())
            {
                var dataRow = table.NewRow();
                for (int i = 0; i < fieldCount; i++)
                {
                    dataRow[i] = reader.GetValue(i);
                }
                table.Rows.Add(dataRow);
            }
        }

        return dataset;
    }

    #endregion
}
