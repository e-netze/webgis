using System;

namespace E.Standard.DbConnector;

public class DBTableNameAttribute : Attribute
{
    public DBTableNameAttribute(string tableName)
    {
        this.TableName = tableName;
    }

    public string TableName { get; set; }
}

public class DBFieldNameAttribute : Attribute
{
    public DBFieldNameAttribute(string fieldName, bool isOid = false, bool updatable = true)
    {
        this.FieldName = fieldName;
        this.IsOid = isOid;
        this.Updatable = updatable;
    }

    public string FieldName { get; set; }

    public bool IsOid { get; set; }
    public bool Updatable { get; set; }
}
