namespace E.Standard.WebMapping.Core.ServiceResponses;

public class EmptyImage : ImageLocation
{
    public EmptyImage(int index, string serviceID)
        : base(index, serviceID, string.Empty, "/img/empty.gif")
    {
    }
}
