namespace E.Standard.Api.App;

public interface ILookupConnection
{
    string ConnectionString { get; set; }
    string SqlClause { get; set; }
}
