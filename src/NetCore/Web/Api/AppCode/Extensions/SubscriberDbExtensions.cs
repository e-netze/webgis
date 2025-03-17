using E.Standard.Api.App.Extensions;
using E.Standard.Configuration.Services;
using E.Standard.WebGIS.SubscriberDatabase;
using System.Data;
using System.Linq;

namespace Api.Core.AppCode.Extensions;

static public class SubscriberDbExtensions
{
    static public bool IsAdminSubscriber(this SubscriberDb.Subscriber subscriber, ConfigurationService config)
    {
        if (subscriber == null)
        {
            return false;
        }

        return config.AdminSubscribers()
                     .Where(m => m.ToLower() == subscriber.Name.ToLower())
                     .Count() > 0;
    }
}
