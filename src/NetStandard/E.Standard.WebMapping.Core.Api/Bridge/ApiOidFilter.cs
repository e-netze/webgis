namespace E.Standard.WebMapping.Core.Api.Bridge;

public class ApiOidFilter : ApiQueryFilter
{
    public ApiOidFilter(long oid)
    {
        this.Oid = oid;
        QueryItems.Add("#oid#", oid.ToString());
    }

    public long Oid { get; private set; }

}
