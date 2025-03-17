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
    private SqlConnection sqlConnection = null;
    private OracleConnection oracleConnection = null;
    private Npgsql.NpgsqlConnection npgsqlConnection = null;
    private SQLiteConnection sqliteConnection = null;
    private string _errMsg = "";

    public DataProvider()
    {
    }

    public void Dispose()
    {
        Close();
    }

    public string lastErrorMessage
    {
        get { return _errMsg; }
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

            string type = connectionString.Substring(0, pos);
            string connStr = connectionString.Substring(pos + 1, connectionString.Length - pos - 1);

            switch (type.ToLower())
            {
                case "oracle":
                    oracleConnection = new OracleConnection(connStr);
                    if (testIt)
                    {
                        oracleConnection.Open();
                        oracleConnection.Close();
                    }
                    break;
                case "sql":
                    sqlConnection = new SqlConnection(connStr);
                    if (testIt)
                    {
                        sqlConnection.Open();
                        sqlConnection.Close();
                    }
                    break;
                case "postgres":
                    npgsqlConnection = new Npgsql.NpgsqlConnection(connStr);
                    if (testIt)
                    {
                        npgsqlConnection.Open();
                        npgsqlConnection.Close();
                    }
                    break;
                case "sqlite":
                    sqliteConnection = new SQLiteConnection(connStr);
                    if (testIt)
                    {
                        sqliteConnection.Open();
                        sqliteConnection.Close();
                    }
                    break;
            }

            return true;
        }
        catch (Exception ex)
        {
            _errMsg = ex.Message;
            return false;
        }
    }
    public void Close()
    {
        if (oracleConnection != null)
        {
            oracleConnection.Close();
            oracleConnection.Dispose();
        }
        if (sqlConnection != null)
        {
            sqlConnection.Close();
            sqlConnection.Dispose();
        }
        if (npgsqlConnection != null)
        {
            npgsqlConnection.Close();
            npgsqlConnection.Dispose();
        }
        if (sqliteConnection != null)
        {
            sqliteConnection.Close();
            sqliteConnection.Dispose();
        }
        oracleConnection = null;
        sqlConnection = null;
        npgsqlConnection = null;
        sqliteConnection = null;
    }

    public DataTable ExecuteQuery(string sql)
    {
        try
        {
            DataSet ds = new DataSet();

            if (oracleConnection != null)
            {
                oracleConnection.Open();
                OracleDataAdapter adapter = new OracleDataAdapter(sql, oracleConnection);
                adapter.Fill(ds);
                oracleConnection.Close();
            }
            else if (sqlConnection != null)
            {
                sqlConnection.Open();
                SqlDataAdapter adapter = new SqlDataAdapter(sql, sqlConnection);
                adapter.Fill(ds);
                sqlConnection.Close();
            }
            else if (npgsqlConnection != null)
            {
                npgsqlConnection.Open();
                Npgsql.NpgsqlDataAdapter adapter = new Npgsql.NpgsqlDataAdapter(sql, npgsqlConnection);
                adapter.Fill(ds);
                npgsqlConnection.Close();
            }
            else if (sqliteConnection != null)
            {
                //sqliteConnection.Open();
                //SqliteDataAdapter adapter = new System.Data.SQLite.SQLiteDataAdapter(sql, sqliteConnection);
                //adapter.Fill(ds);
                //sqliteConnection.Close();

                sqliteConnection.Open();
                var command = new SQLiteCommand(sql, sqliteConnection);
                ds = ToDataset(command);
                sqliteConnection.Close();
            }
            if (ds.Tables.Count == 0)
            {
                return null;
            }

            return ds.Tables[0];
        }
        catch (Exception ex)
        {
            if (oracleConnection != null)
            {
                oracleConnection.Close();
            }

            if (sqlConnection != null)
            {
                sqlConnection.Close();
            }

            if (npgsqlConnection != null)
            {
                npgsqlConnection.Close();
            }

            if (sqliteConnection != null)
            {
                sqliteConnection.Close();
            }

            _errMsg = ex.Message;
            return null;
        }
    }
    public bool Join(string sql, XmlNode feature)
    {
        return Join(sql, feature, true);
    }
    public bool Join(string sql, XmlNode feature, bool one2n)
    {
        string field = DBConnection.getFieldPlacehoder(sql);
        while (field != "")
        {
            string val = DBConnection.getFieldValue(feature, field);
            if (val == "" && field == "BFL")
            {
                val = " "; // return false;  nur für die Schulung
            }

            sql = sql.Replace("[" + field + "]", val);
            field = DBConnection.getFieldPlacehoder(sql);
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
