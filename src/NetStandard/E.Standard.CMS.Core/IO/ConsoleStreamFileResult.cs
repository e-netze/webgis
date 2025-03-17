namespace E.Standard.CMS.Core.IO;
public class ConsoleStreamFileResult
{
    public ConsoleStreamFileResult(string filename, string contentType, byte[] data)
    {
        this.FileName = filename;
        this.ContentType = contentType;
        this.Data = data;
    }

    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public byte[] Data { get; private set; }
}
