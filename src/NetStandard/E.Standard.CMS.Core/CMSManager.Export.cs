using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace E.Standard.CMS.Core;

public partial class CMSManager
{
    /// <summary>
    /// Timings and counters of an export (deploy). Times are accumulated per phase.
    /// </summary>
    public class ExportStatistics
    {
        internal readonly Stopwatch Total = new Stopwatch();
        internal readonly Stopwatch FileSystem = new Stopwatch();
        internal readonly Stopwatch Acl = new Stopwatch();
        internal readonly Stopwatch Read = new Stopwatch();
        internal readonly Stopwatch Build = new Stopwatch();
        internal readonly Stopwatch Links = new Stopwatch();

        public int Directories { get; internal set; }
        public int Nodes { get; internal set; }

        public TimeSpan TotalTime => Total.Elapsed;
        public TimeSpan FileSystemTime => FileSystem.Elapsed;
        public TimeSpan AclTime => Acl.Elapsed;
        public TimeSpan ReadTime => Read.Elapsed;
        public TimeSpan BuildTime => Build.Elapsed;
        public TimeSpan LinksTime => Links.Elapsed;

        public override string ToString()
            => $"{Nodes} nodes in {Directories} folders: {Ms(Total.Elapsed)} total " +
               $"(file system {Ms(FileSystem.Elapsed)}, acl {Ms(Acl.Elapsed)}, read {Ms(Read.Elapsed)}, build {Ms(Build.Elapsed)}, link check {Ms(Links.Elapsed)}, " +
               $"schema/other {Ms(Total.Elapsed - FileSystem.Elapsed - Acl.Elapsed - Read.Elapsed - Build.Elapsed - Links.Elapsed)})";

        private static string Ms(TimeSpan ts) => $"{Math.Max(0, (long)ts.TotalMilliseconds)}ms";
    }

    private class ExportContext
    {
        public ExportContext(List<Warning> warnings, ExportStatistics statistics)
        {
            Warnings = warnings;
            Statistics = statistics;
        }

        // null => no link checks
        public List<Warning> Warnings { get; }
        public ExportStatistics Statistics { get; }
    }
}
