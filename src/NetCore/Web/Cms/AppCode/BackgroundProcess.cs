using E.Standard.CMS.Core.Abstractions;
using E.Standard.CMS.Core.IO;
using E.Standard.ThreadSafe;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Cms.AppCode;

public class BackgroundProcess : IConsoleOutputStream
{
    static public IEnumerable<BackgroundProcess> CurrentProcesses = new ThreadSafeList<BackgroundProcess>();

    private ThreadSafeList<string> _console = new ThreadSafeList<string>();
    private Thread _processThread;

    private object locker = new object();

    public BackgroundProcess(string cmsId,
                             string userName,
                             ParameterizedThreadStart start,
                             object userData = null)
    {
        this.ProcId = Guid.NewGuid().ToString();
        this.UserName = userName;
        this.CmsId = cmsId;
        this.UserData = userData;

        ((ThreadSafeList<BackgroundProcess>)CurrentProcesses).Add(this);

        _processThread = new Thread(start);
        _processThread.Start(this);
    }

    public delegate void ProcessHander();

    public string ProcId { get; private set; }

    public string CmsId { get; private set; }

    public string UserName { get; private set; }

    public object UserData { get; set; }

    public bool IsCanceled { get; private set; }
    public void Cancel()
    {
        try
        {
            this.IsCanceled = true;
            WriteLine("-------------------------------------------------------------");
            WriteLine("Try to cancel process");
            WriteLine("-------------------------------------------------------------");
        }
        catch (Exception ex)
        {
            WriteLine("ERROR: Can't cancel process - " + ex.Message);
        }
    }

    public void WriteLine(string line)
    {
        _console.Add(line);
    }

    public void WriteLines(string multilineText)
    {
        if (!String.IsNullOrEmpty(multilineText))
        {
            foreach (var line in multilineText.Split('\n'))
            {
                WriteLine(line);
            }
        }
    }

    public void WriteLines(string[] lines)
    {
        if (lines != null)
        {
            foreach (var line in lines)
            {
                WriteLine(line);
            }
        }
    }

    private ConsoleStreamFileResult _fileResult = null;
    public ConsoleStreamFileResult TakeFileAndReleaseProcess()
    {
        var result = _fileResult;
        _fileResult = null;

        ((ThreadSafeList<BackgroundProcess>)CurrentProcesses).Remove(this);

        return result;
    }
    public void PutFile(ConsoleStreamFileResult file)
    {
        _fileResult = file;
    }
    public bool HasFile => _fileResult != null;
    public IEnumerable<ConsoleStreamFileResult> GetFiles() => HasFile ? [_fileResult] : [];

    private int _lineIndex = 0;
    public IEnumerable<string> ReadLines()
    {
        //return new string[]
        //{
        //    "Line1",
        //    "Line2",
        //    "Line3"
        //};

        lock (locker)
        {
            if (_processThread.IsAlive == false && _console.Count == _lineIndex)
            {
                if (_fileResult == null)
                {
                    ((ThreadSafeList<BackgroundProcess>)CurrentProcesses).Remove(this);
                }
                return null;
            }

            int count = _console.Count - _lineIndex;

            List<string> lines = new List<string>();
            for (int i = 0; i < count; i++)
            {
                lines.Add(">" + (_lineIndex + i + 1).ToString().PadLeft(5, '0') + ": " + _console.GetAt(_lineIndex + i));
            }
            _lineIndex = _console.Count;

            return lines;
        }
    }

    public object ProcDefinition(string title)
    {
        return new
        {
            procId = this.ProcId,
            title = title,
            cmsId = this.CmsId
        };
    }
}
