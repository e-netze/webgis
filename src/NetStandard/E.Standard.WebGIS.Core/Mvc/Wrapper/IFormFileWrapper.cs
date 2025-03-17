namespace E.Standard.WebGIS.Core.Mvc.Wrapper;

public interface IFormFileWrapper
{
    int ContentLength { get; }
    string ContentDisposition { get; }
    string ContentType { get; }
    string FileName { get; }
    byte[] Data { get; }
}
