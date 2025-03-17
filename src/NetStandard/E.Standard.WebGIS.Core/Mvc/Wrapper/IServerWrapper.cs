namespace E.Standard.WebGIS.Core.Mvc.Wrapper;

public interface IServerWrapper
{
    string MapPath(string path);

    string AppBinPath { get; }
}
