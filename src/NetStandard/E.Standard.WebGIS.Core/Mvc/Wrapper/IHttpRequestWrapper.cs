using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace E.Standard.WebGIS.Core.Mvc.Wrapper;

public interface IHttpRequestWrapper
{
    Uri Url { get; }

    NameValueCollection QueryString { get; }

    NameValueCollection Form { get; }

    NameValueCollection Cookies { get; }

    NameValueCollection Headers { get; }

    NameValueCollection ServerVariables { get; }

    IDictionary<string, IFormFileWrapper> Files { get; }

    Uri UrlReferrer { get; }

    string FormOrQuery(string key);
}
