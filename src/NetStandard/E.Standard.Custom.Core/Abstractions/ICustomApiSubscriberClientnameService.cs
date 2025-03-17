using E.Standard.Custom.Core.Models;
using E.Standard.WebGIS.SubscriberDatabase;

namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomApiSubscriberClientnameService
{
    CustomSubscriberClientname GetCustomClientname(SubscriberDb.Client client);
}
