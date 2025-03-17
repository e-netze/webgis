using Microsoft.Extensions.Options;
using System.Threading;

namespace Api.Core.AppCode.Services;

public class CancellationTokenService
{
    private readonly CancellationTokenServiceOptions _options;

    public CancellationTokenService(IOptionsMonitor<CancellationTokenServiceOptions> optionsMonitor)
    {
        _options = optionsMonitor.CurrentValue;
    }

    public CancellationTokenSource CreateTimeoutCancellationToken() =>
        _options.TimeoutMillisecnds > 0 ?
            new CancellationTokenSource(_options.TimeoutMillisecnds) :
            new CancellationTokenSource();
}
