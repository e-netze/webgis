using E.Standard.WebMapping.Core.Exceptions;
using System.Threading;

namespace E.Standard.WebMapping.Core.Extensions;

static internal class CancellationTokenSourceExtensions
{
    static public void ThrowExceptionIfCanceled(this CancellationTokenSource cts)
    {
        if (cts != null && cts.IsCancellationRequested)
        {
            throw new CancellationException();
        }
    }
}
