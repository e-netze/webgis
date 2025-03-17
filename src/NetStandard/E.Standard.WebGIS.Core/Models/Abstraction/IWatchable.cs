namespace E.Standard.WebGIS.Core.Models.Abstraction;

public interface IWatchable
{
    int milliseconds { get; set; }

    string WatchId { get; set; }
}
