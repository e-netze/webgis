using System;

namespace E.Standard.Web.Abstractions;

public interface IUrlData
{
    string Url { get; }
    byte[]? Data { get; set; }

    Exception? Exception { get; set; }
    //string ContentType { get; set; }
    //int StatusCode { get; set; }
}
