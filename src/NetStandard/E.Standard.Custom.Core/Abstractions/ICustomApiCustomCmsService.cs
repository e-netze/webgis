namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomApiCustomCmsService
{
    string GetCustomCmsDocumentPath(string cmsId);
    string GetCustomCmsAccountName(string cmsId);
}
