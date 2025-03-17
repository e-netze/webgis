using System;
using System.Collections.Generic;

namespace E.Standard.WebGIS.Core.Services;

public interface IRazorCompileEngineService : IHubService
{
    string RunCompile(string code, string razorCacheId, Type modelType, object model = null);

    object RawString(string str);
}

public class RazorCompileException : Exception
{
    public IEnumerable<RazorCompileError> CompilerErrors { get; set; }

    public class RazorCompileError
    {
        public bool IsWarning { get; set; }
        public string ErrorText { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}
