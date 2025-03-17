using E.Standard.CMS.Core.Abstractions;
using E.Standard.CMS.Core.IO;

namespace cms.tools;
internal class ConsoleOutput : IConsoleOutputStream
{
    private readonly List<ConsoleStreamFileResult> _resultFiles = new();

    public void WriteLine(string message)
        => Console.WriteLine(message);

    public void WriteLines(string multilineText)
        => WriteLines(multilineText.Split('\n'));

    public void WriteLines(string[] lines)
    {
        foreach (var line in lines ?? [])
        {
            WriteLine(line);
        }
    }

    public bool IsCanceled { get; private set; }



    public void Cancel()
    {

        this.IsCanceled = true;
        WriteLine("-------------------------------------------------------------");
        WriteLine("Try to cancel process");
        WriteLine("-------------------------------------------------------------");
    }

    public void PutFile(ConsoleStreamFileResult file)
    {
        _resultFiles.Add(file);
    }

    public bool HasFile => _resultFiles.Any();

    public IEnumerable<ConsoleStreamFileResult> GetFiles() => _resultFiles.ToArray();
}
