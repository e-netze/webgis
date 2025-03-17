namespace E.Standard.WebMapping.Core.ServiceResponses;

public class SuccessResponse : ServiceResponse
{
    private readonly bool _succeeded;

    public SuccessResponse(int index, string serviceID, bool succeeded)
        : base(index, serviceID)
    {
        _succeeded = succeeded;
    }

    public bool Succeeded
    {
        get { return _succeeded; }
    }
}
