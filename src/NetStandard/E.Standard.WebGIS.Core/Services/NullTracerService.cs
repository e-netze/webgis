namespace E.Standard.WebGIS.Core.Services;

public class NullTracerService : ITracerService
{
    public bool Trace => false;

    public void Log(object source, string msg)
    {

    }
}
