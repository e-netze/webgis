using E.Standard.Extensions.Collections;
using E.Standard.WebGIS.Core.Mvc.Wrapper;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Portal.AppCode.Mvc.Wrapper;

public class HttpRequestWrapper : IHttpRequestWrapper
{
    public HttpRequestWrapper(HttpRequest request)
    {
        this.Request = request;
    }

    private HttpRequest Request { get; set; }

    public Uri Url => new Uri(Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(this.Request));

    private NameValueCollection _queryString = null;
    public NameValueCollection QueryString
    {
        get
        {
            if (_queryString == null)
            {
                _queryString = Request.Query.ToNameValueCollection();
            }

            return _queryString;
        }
    }

    private NameValueCollection _form = null;
    public NameValueCollection Form
    {
        get
        {
            if (_form == null)
            {
                if (Request.HasFormContentType)
                {
                    _form = Request.Form.ToNameValueCollection();
                }
                else
                {
                    _form = new NameValueCollection();
                }
            }

            return _form;
        }
    }

    private NameValueCollection _cookies = null;
    public NameValueCollection Cookies
    {
        get
        {
            if (_cookies == null)
            {
                _cookies = new NameValueCollection();
                foreach (var cookie in this.Request.Cookies)
                {
                    _cookies[cookie.Key] = cookie.Value;
                }
            }

            return _cookies;
        }
    }

    private IDictionary<string, IFormFileWrapper> _files = null;
    public IDictionary<string, IFormFileWrapper> Files
    {
        get
        {
            if (_files != null)
            {
                return _files;
            }

            _files = new Dictionary<string, IFormFileWrapper>();
            try
            {
                foreach (var formFile in Request.Form.Files)
                {
                    byte[] data = new byte[formFile.Length];

                    formFile.OpenReadStream().ReadExactly(data, 0, data.Length);
                    _files.Add(formFile.Name,
                        new FormFile()
                        {
                            Data = data,
                            FileName = formFile.FileName,
                            ContentDisposition = formFile.ContentDisposition,
                            ContentType = formFile.ContentType
                        });
                }
            }
            catch { }
            return _files;
        }
    }

    private NameValueCollection _headers = null;
    public NameValueCollection Headers
    {
        get
        {
            if (_headers == null)
            {
                _headers = new NameValueCollection();

                foreach (var headerKey in this.Request.Headers.Keys)
                {
                    _headers[headerKey] = this.Request.Headers[headerKey];
                }
            }

            return _headers;
        }
    }

    private NameValueCollection _serverVariables = null;
    public NameValueCollection ServerVariables
    {
        get
        {
            if (_serverVariables == null)
            {
                _serverVariables = new NameValueCollection();

                _serverVariables["LOGON_USER"] = this.Request.HttpContext?.User?.Identity?.Name;

                // ToDo:
            }

            return _serverVariables;
        }
    }

    public Uri UrlReferrer
    {
        get
        {
            if (String.IsNullOrWhiteSpace(Request.Headers["Referer"]))
            {
                return null;
            }

            return new Uri(Request.Headers["Referer"].ToString());
        }
    }

    public string FormOrQuery(string key)
    {
        try
        {
            return Request.HasFormContentType &&
                   Request.Form != null &&
                   !String.IsNullOrWhiteSpace(Request.Form[key]) ? Request.Form[key] : Request.Query[key];
        }
        catch
        {
            return String.Empty;
        }
    }

    #region Classes

    public class FormFile : IFormFileWrapper
    {
        public int ContentLength
        {
            get
            {
                return this.Data != null ? this.Data.Length : 0;
            }
        }

        public string ContentDisposition { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public byte[] Data { get; set; }
    }

    #endregion
}
