using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IServiceAttachmentProvider 
{
    Task<byte[]> GetServiceAttachementData(IRequestContext requestContext, string attachmentUri);
}
