using System;
using System.IO;
using System.Linq;

namespace E.Standard.Configuration;

public class ConfigFileInfo
{
#if DEBUG || DEBUG_INTERNAL
    static public bool AllowConfigRedirection = true;
#else
    static public bool AllowConfigRedirection = false;
#endif
    public const string TargetFilePrefix = "targetfile:";
    private readonly FileInfo _fileInfo;

    public ConfigFileInfo(string path)
    {
        _fileInfo = new FileInfo(path);

        if (AllowConfigRedirection == true)
        {
            if (_fileInfo.Exists)
            {
                var firstConfigLine = File.ReadAllLines(_fileInfo.FullName).Where(line => !String.IsNullOrWhiteSpace(line)).FirstOrDefault().Trim();
                if (firstConfigLine != null && firstConfigLine.StartsWith(TargetFilePrefix))
                {
                    _fileInfo = new FileInfo(firstConfigLine.Substring(TargetFilePrefix.Length).Trim());
                }
            }
        }
    }

    public bool Exists => _fileInfo.Exists;
    public string FullName => _fileInfo.FullName;
}
