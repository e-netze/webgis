using System.Data;

namespace E.Standard.DbConnector.Schema;

public interface IDbSchemaProvider
{
    DataSet DbSchema { get; }

    string DbConnectionString { get; }
}
