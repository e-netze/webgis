using E.Standard.Web.Abstractions;
using System;

namespace E.Standard.Web.Models;

public class UrlData : IUrlData
{
    public string Url { get; set; } = "";
    public byte[]? Data { get; set; }

    public Exception? Exception { get; set; }
    //public string ContentType { get; set; }
    //public int StatusCode { get; set; }
}
