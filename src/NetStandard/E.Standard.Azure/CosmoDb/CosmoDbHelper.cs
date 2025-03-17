//using Microsoft.Azure.Documents;
using Microsoft.Azure.Cosmos;
using System;
using System.Threading.Tasks;

namespace E.Standard.Azure.CosmoDb;

public class CosmoDbHelper
{
    public static async Task<V> ExecuteWithRetriesAsync<V>(Func<Task<V>> function, int maxSeconds = 300)
    {
        TimeSpan sleepTime = TimeSpan.Zero;
        var startTime = DateTime.UtcNow;

        while (true)
        {
            try
            {
                return await function();
            }
            catch (CosmosException de)
            {
                if ((int)de.StatusCode != 429)
                {
                    throw;
                }
                sleepTime = de.RetryAfter ?? TimeSpan.FromSeconds(1);
            }
            catch (AggregateException ae)
            {
                if (!(ae.InnerException is CosmosException))
                {
                    throw;
                }

                var de = (CosmosException)ae.InnerException;
                if ((int)de.StatusCode != 429)
                {
                    throw;
                }
                sleepTime = de.RetryAfter ?? TimeSpan.FromSeconds(1);
            }
            catch (Exception ex)
            {
                if ((DateTime.UtcNow - startTime).TotalSeconds > maxSeconds)
                {
                    throw;
                }

                Console.Write(ex.Message);
                if (ex.Message.ToLower().Contains("request rate is large") || ex.Message.ToLower().Contains("statuscode: 429"))
                {
                    sleepTime = new TimeSpan(0, 0, 0, 0, new Random(Guid.NewGuid().GetHashCode()).Next(100, 3000));
                }
                else
                {
                    throw;
                }
            }

            Console.WriteLine($"sleep: {sleepTime.TotalMilliseconds}ms");
            await Task.Delay(sleepTime);
            Console.WriteLine("retry...");
        }
    }

    public static V ExecuteWithRetries<V>(Func<V> function, int maxSeconds = 300)
    {
        TimeSpan sleepTime = TimeSpan.Zero;
        var startTime = DateTime.UtcNow;

        while (true)
        {
            try
            {
                return function();
            }
            catch (CosmosException de)
            {
                if ((int)de.StatusCode != 429)
                {
                    throw;
                }
                sleepTime = de.RetryAfter ?? TimeSpan.FromSeconds(1);
            }
            catch (AggregateException ae)
            {
                if (!(ae.InnerException is CosmosException))
                {
                    throw;
                }

                CosmosException de = (CosmosException)ae.InnerException;
                if ((int)de.StatusCode != 429)
                {
                    throw;
                }
                sleepTime = de.RetryAfter ?? TimeSpan.FromSeconds(1);
            }
            catch (Exception ex)
            {
                if ((DateTime.UtcNow - startTime).TotalSeconds > maxSeconds)
                {
                    throw;
                }

                Console.Write(ex.Message);
                if (ex.Message.ToLower().Contains("request rate is large") || ex.Message.ToLower().Contains("statuscode: 429"))
                {
                    sleepTime = new TimeSpan(0, 0, 0, 0, new Random(Guid.NewGuid().GetHashCode()).Next(100, 3000));
                }
                else
                {
                    throw;
                }
            }

            Task.Delay(sleepTime).Wait();
        }
    }
}
