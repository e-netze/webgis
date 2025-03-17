namespace E.Standard.WebMapping.Core.ServiceResponses;

public class JavaScriptResponse : ServiceResponse
{
    public string JavaScript;

    public JavaScriptResponse(int index, string serviceID, string jScript)
        : base(index, serviceID)
    {
        JavaScript = jScript;
    }
}
