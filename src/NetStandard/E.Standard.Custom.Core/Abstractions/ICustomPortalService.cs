using System.Threading.Tasks;

namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomPortalService
{
    Task LogMapRequest(string id, string category, string map, string username);
}
