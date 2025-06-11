using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace E.Standard.DbConnector.Schema;

public class SchemaHelper
{
    public bool Create(IDbSchemaProvider schemaProvider)
    {
        var dataset = schemaProvider?.DbSchema;

        if (dataset == null)
        {
            throw new Exception("No schema definied");
        }

        var dbFactory = new DBFactory(schemaProvider.DbConnectionString);

        foreach (DataTable table in dataset.Tables)
        {
            if (dbFactory.TableExits(table.TableName))
            {
                continue;
            }

            StringBuilder createTable = new StringBuilder();
            createTable.Append("create table " + table.TableName + " (");
            List<string> serialFields = new List<string>();

            bool firstColumn = true;
            foreach (DataColumn column in table.Columns)
            {
                string name = column.ColumnName;
                int size = 0;
                bool isPrimaryKey = false;
                bool isSerial = false;
                bool notNull = false;

                if (name.Contains(":"))
                {
                    size = int.Parse(name.Substring(name.IndexOf(":") + 1));
                    name = name.Substring(0, name.IndexOf(":"));
                }
                if (name.StartsWith("#"))
                {
                    name = name.Substring(1);
                    isPrimaryKey = true;
                }
                if (name.StartsWith("@"))
                {
                    name = name.Substring(1);
                    isPrimaryKey = isSerial = notNull = true;
                    serialFields.Add(name);
                }
                if (name.StartsWith("!"))
                {
                    name = name.Substring(1);
                    notNull = true;
                }

                if (!firstColumn)
                {
                    createTable.Append(",");
                }

                createTable.Append(name + " " + dbFactory.DbTypeName(column.DataType, size, isPrimaryKey, isSerial, notNull));
                firstColumn = false;
            }

            createTable.Append(")");

            using (var connection = dbFactory.GetConnection())
            {
                connection.Open();

                var command = dbFactory.GetCommand(connection);
                command.CommandText = createTable.ToString();

                command.ExecuteNonQuery();

                foreach (var commandText in dbFactory.AfterCreateTableCommands(table.TableName, serialFields.ToArray()))
                {
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                }

                if (table.Rows.Count > 0)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        command = dbFactory.GetCommand(connection);

                        StringBuilder insert = new StringBuilder();
                        insert.Append("insert into " + table.TableName);

                        StringBuilder fields = new StringBuilder();
                        StringBuilder values = new StringBuilder();

                        foreach (DataColumn column in table.Columns)
                        {
                            string columnName = column.ColumnName;
                            if (columnName.StartsWith("@"))
                            {
                                continue;
                            }

                            if (columnName.StartsWith("#"))
                            {
                                columnName = columnName.Substring(1);
                            }

                            if (columnName.StartsWith("!"))
                            {
                                columnName = columnName.Substring(1);
                            }

                            columnName = columnName.Split(':')[0];

                            if (fields.Length > 0)
                            {
                                fields.Append(",");
                            }

                            fields.Append(columnName);

                            if (values.Length > 0)
                            {
                                values.Append(",");
                            }

                            values.Append(dbFactory.ParaName(columnName));

                            command.Parameters.Add(dbFactory.GetParameter(columnName, row[column.ColumnName]));
                        }

                        insert.Append(" (" + fields.ToString() + ") values (" + values.ToString() + ")");

                        command.CommandText = insert.ToString();
                        command.ExecuteNonQuery();
                    }
                }
            }

        }

        return false;
    }

    //public bool TableExists(IDbSchemaProvider schemaProvider, string tablename)
    //{
    //    var dbFactory = new DBFactory(schemaProvider.DbConnectionString);

    //    return dbFactory.TableExits(tablename);
    //}

    public bool SchemaExists(IDbSchemaProvider schemaProvider)
    {
        var dataset = schemaProvider?.DbSchema;

        if (dataset == null)
        {
            throw new Exception("No schema defined");
        }

        var dbFactory = new DBFactory(schemaProvider.DbConnectionString);

        foreach (DataTable table in dataset.Tables)
        {
            if (!dbFactory.TableExits(table.TableName))
            {
                return false;
            }
        }

        return true;
    }
}
