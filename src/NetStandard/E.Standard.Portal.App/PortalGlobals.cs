using System;

namespace E.Standard.Portal.App;

static public class PortalGlobals
{
    public const string MessageQueuePrefix = "portal-queue-";
    static public string MessageQueueName = $"{MessageQueuePrefix}{Guid.NewGuid().ToString("N").ToLower()}";
}
