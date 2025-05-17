using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IFeatureAttachmentProvider
{
    bool HasAttachments { get; }

    Task<IEnumerable<string>> HasAttachmentsFor(IRequestContext requestContext, IEnumerable<string> ids);
    Task<IFeatureAttachments> GetAttachmentsFor(IRequestContext requestContext, string id);
}

public interface IFeatureAttachments
{
    IEnumerable<IFeatureAttachment> Attachements { get; }
}

public interface IFeatureAttachment
{
    string Name { get; }
    string Type { get; }
    byte[] Data { get; }
}