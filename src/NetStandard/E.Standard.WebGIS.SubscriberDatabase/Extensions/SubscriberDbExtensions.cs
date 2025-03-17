namespace E.Standard.WebGIS.SubscriberDatabase.Extensions;

public static class SubscriberDbExtensions
{
    public static bool IsRegularSubscriber(this SubscriberDb.Subscriber subscriber)
    {
        // Some actions are not allowed for Cloud Subscriber (Endpoint owners: "owner:....")
        return subscriber.FullName.IsRegularSubscriber();
    }

    public static bool IsRegularSubscriber(this string subscriberName)
    {
        // Some actions are not allowed for Cloud Subscriber (Endpoint owners: "owner:....")
        return subscriberName.StartsWith("subscriber:");
    }

    public static bool IsRegularOwner(this SubscriberDb.Subscriber subscriber)
    {
        // Some actions are not allowed for Cloud Subscriber (Endpoint owners: "owner:....")
        return subscriber.FullName.IsRegularOwner();
    }

    public static bool IsRegularOwner(this string subscriberName)
    {
        // Some actions are not allowed for Cloud Subscriber (Endpoint owners: "owner:....")
        return subscriberName.StartsWith("owner:");
    }
}
