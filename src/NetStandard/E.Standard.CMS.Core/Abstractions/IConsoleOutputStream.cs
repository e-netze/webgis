using E.Standard.CMS.Core.IO;
using System.Collections.Generic;

namespace E.Standard.CMS.Core.Abstractions;

public interface IConsoleOutputStream
{
    void WriteLine(string line);
    void WriteLines(string multilineText);
    void WriteLines(string[] lines);

    bool IsCanceled { get; }
    void Cancel();

    void PutFile(ConsoleStreamFileResult file);
    bool HasFile { get; }
    IEnumerable<ConsoleStreamFileResult> GetFiles();
}
