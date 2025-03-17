using E.Standard.Web.Abstractions;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.ServiceResponses;

public class ServiceResponse
{
    private readonly int _index = 0;
    private string _serviceID;

    public ServiceResponse(int index, string serviceID)
    {
        _index = index;
        _serviceID = serviceID;
    }

    public int Index
    {
        get { return _index; }
    }

    public string ServiceID
    {
        get { return _serviceID; }
        set { _serviceID = value; }
    }

    virtual public Task<bool> IsEmpty(IHttpService httpService)
    {
        return Task.FromResult(false);
    }

    //virtual public Task CreateDefault(IMapSession session, string txt)
    //{

    //}

    public ErrorResponse InnerErrorResponse { get; set; }
}
