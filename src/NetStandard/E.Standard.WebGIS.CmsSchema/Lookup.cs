using System;

namespace E.Standard.WebGIS.CmsSchema;

public class LookUp
{
    private string _connectionString = String.Empty;
    private string _sqlClause = String.Empty;

    public LookUp() { }
    public LookUp(string connectionString, string sqlClause)
    {
        _connectionString = connectionString;
        _sqlClause = sqlClause;
    }

    public string ConnectionString
    {
        get { return _connectionString; }
        set { _connectionString = value; }
    }

    public string SqlClause
    {
        get { return _sqlClause; }
        set { _sqlClause = value; }
    }
}
