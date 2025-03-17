namespace E.Standard.WebMapping.Core.ServiceResponses;

public class TimeoutResponse : ErrorResponse
{
    public TimeoutResponse(int index, string serviceID, string message, string message2)
        : base(index, serviceID, message, message2)
    {
    }
}
