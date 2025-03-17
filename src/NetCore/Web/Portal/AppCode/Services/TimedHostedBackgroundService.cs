using E.Standard.Custom.Core.Abstractions;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Services;

public class TimedHostedBackgroundService : BackgroundService
{
    private int counter = 0;

    private readonly IEnumerable<IWorkerService> _workers;

    public TimedHostedBackgroundService(IEnumerable<IWorkerService> workers = null)
    {
        _workers = workers;
    }

    async protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_workers != null && _workers.Count() > 0)
        {
            foreach (var worker in _workers)
            {
                await worker.Init();
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await DoWork();
            await Task.Delay(1000, stoppingToken);
        }
    }

    async private Task DoWork()
    {
        try
        {
            if (_workers != null)
            {
                foreach (var worker in _workers)
                {
                    if (counter % worker.DurationSeconds == 0)
                    {
                        try
                        {
                            await worker.DoWork();
                        }
                        catch { }
                    }
                }
            }

            counter++;
            if (counter >= 86400)
            {
                counter = 0;
            }
        }
        catch { }
    }
}
