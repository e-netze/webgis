namespace E.Standard.Api.App;

public interface ILookup
{
    ILookupConnection GetLookupConnection(string parameter);
}
