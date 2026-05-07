using System.Threading;

using E.Standard.WebMapping.Core.Exceptions;

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
