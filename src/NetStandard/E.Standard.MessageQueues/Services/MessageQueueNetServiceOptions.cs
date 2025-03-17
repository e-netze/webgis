namespace E.Standard.MessageQueues.Services;

public class MessageQueueNetServiceOptions
{
    public string QueueServiceUrl { get; set; }
    public string Namespace { get; set; }
    public string QueueName { get; set; }
    public string ApiClient { get; set; }
    public string ApiClientSecret { get; set; }
}
