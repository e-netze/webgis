using System.IO;

namespace E.Standard.Extensions.IO;

static public class StreamExtensions
{
    // https://stackoverflow.com/questions/221925/creating-a-byte-array-from-a-stream
    static public byte[] ReadFully(this Stream stream)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
