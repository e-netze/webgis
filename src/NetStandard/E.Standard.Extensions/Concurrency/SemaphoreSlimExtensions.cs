using System;
using System.Threading;
using System.Threading.Tasks;

namespace E.Standard.Extensions.Concurrency;

public static class SemaphoreSlimExtensions
{
    public static async Task<bool> RunExclusiveOrAwaitFinishingAsync(this SemaphoreSlim semaphore, Func<Task> action)
    {
        bool lockTaken = await semaphore.WaitAsync(0);

        try
        {
            if (lockTaken)
            {
                await action();
            }
            else
            {
                await semaphore.WaitAsync(); // wait until all calls of this method ends
            }

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            semaphore.Release();
        }
    }
}
