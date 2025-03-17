using E.Standard.CMS.Core.IO.Abstractions;

namespace E.Standard.CMS.Core.Schema.Abstraction;

public interface ICopyable : IPersistable
{
    CMSManager CopyCmsManager { get; set; }
    bool CopyTo(string UriPath);
}
