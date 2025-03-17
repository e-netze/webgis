namespace E.Standard.WebGIS.Core.Services;

public interface ITracerService
{
    bool Trace { get; }
    void Log(object source, string msg);
}
