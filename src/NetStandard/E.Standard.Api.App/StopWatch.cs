using E.Standard.WebGIS.Core.Models.Abstraction;
using System;

namespace E.Standard.Api.App;

public class StopWatch
{
    private double _startTicks;

    public StopWatch(string watchId)
    {
        this.WatchId = watchId;
        _startTicks = DateTime.Now.Ticks / 10000.0;
    }

    public string WatchId { get; private set; }

    public int Stop()
    {
        int ret = (int)(DateTime.Now.Ticks / 10000.0 - _startTicks);
        _startTicks = DateTime.Now.Ticks / 10000.0;
        return ret;
    }

    public T Apply<T>(T obj) where T : IWatchable
    {
        obj.milliseconds = this.Stop();
        obj.WatchId = this.WatchId;

        return obj;
    }
}