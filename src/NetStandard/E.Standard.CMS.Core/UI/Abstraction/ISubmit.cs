using System.Collections.Specialized;

namespace E.Standard.CMS.Core.UI.Abstraction;

public interface ISubmit
{
    void Submit(NameValueCollection secrets);
}
