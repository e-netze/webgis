namespace E.Standard.Api.App.Models;

public class Binary
{
    public string ContentType { get; set; }
    public string FileName { get; set; }
    public byte[] Data { get; set; }
}