namespace E.Standard.WebMapping.Core.Abstraction;

public interface IAttachmentContainer
{
    bool HasAttachments { get; }

    object GetAttachmentsFor(int id);
}