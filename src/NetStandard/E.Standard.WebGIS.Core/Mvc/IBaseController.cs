using E.Standard.WebGIS.Core.Mvc.Wrapper;

namespace E.Standard.WebGIS.Core.Mvc;

public interface IUrlHelper
{
    string UrlContent(string path);
}

public interface IBaseController : IUrlHelper
{
    IHttpRequestWrapper HttpRequest { get; }
}
