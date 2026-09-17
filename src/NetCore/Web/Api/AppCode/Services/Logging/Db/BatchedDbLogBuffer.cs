#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;

namespace Api.Core.AppCode.Services.Logging.Db;

/// <summary>
/// Buffers log rows in memory and writes them to the database in batches, instead of opening a
/// new DB connection/transaction for every single logged request - important under high
/// concurrent request volume, where a connection-per-row approach would otherwise dominate the
/// cost of logging itself.
///
/// A batch is written out either once <paramref name="batchSize"/> rows have accumulated, on the
/// periodic background timer (bounds how long rows can sit unwritten under low traffic, when the
/// count threshold would rarely be reached), or on an explicit <see cref="Flush"/> call (wired up
/// to <c>IGeoServicePerformanceLogger.Flush()</c>/<c>IExceptionLogger.Flush()</c> - see
/// <see cref="DbGeoServicePerformanceLogger"/>/<see cref="DbExceptionLogger"/>).
/// </summary>
internal sealed class BatchedDbLogBuffer<TRow> : IDisposable
{
    private readonly Action<IReadOnlyList<TRow>> _writeBatch;
    private readonly int _batchSize;
    private readonly int _maxQueueLength;
    private readonly Queue<TRow> _queue = new();
    private readonly object _flushLock = new();
    private int _count;
    private readonly Timer _flushTimer;

    public BatchedDbLogBuffer(Action<IReadOnlyList<TRow>> writeBatch, int batchSize = 200, TimeSpan? flushInterval = null)
    {
        _writeBatch = writeBatch;
        _batchSize = batchSize;
        _maxQueueLength = batchSize * 50;

        var interval = flushInterval ?? TimeSpan.FromSeconds(5);
        _flushTimer = new Timer(_ => Flush(), null, interval, interval);
    }

    public void Enqueue(TRow row)
    {
        int count = Interlocked.Increment(ref _count);
        if (count > _maxQueueLength)
        {
            // Backpressure safety valve - e.g. the database is unreachable for a while. Better to
            // drop entries than let the queue grow unbounded and risk the API process itself
            // running out of memory because of its own logging.
            Interlocked.Decrement(ref _count);
            return;
        }

        lock (_queue)
        {
            _queue.Enqueue(row);
        }

        if (count >= _batchSize)
        {
            Flush();
        }
    }

    public void Flush()
    {
        // Only one flush actually runs at a time - a thread that loses the race just returns;
        // whatever it enqueued is picked up by the flush that is currently running (drains
        // everything present at the time, not just one batch) or the next one.
        if (!Monitor.TryEnter(_flushLock))
        {
            return;
        }

        try
        {
            List<TRow> batch;
            lock (_queue)
            {
                if (_queue.Count == 0)
                {
                    return;
                }

                batch = new List<TRow>(_queue.Count);
                while (_queue.TryDequeue(out var row))
                {
                    batch.Add(row);
                }
            }

            Interlocked.Add(ref _count, -batch.Count);

            try
            {
                _writeBatch(batch);
            }
            catch
            {
                // Never let a logging failure break the application - the batch is lost, matching
                // the "best effort" semantics of the other performance/exception loggers.
            }
        }
        finally
        {
            Monitor.Exit(_flushLock);
        }
    }

    public void Dispose()
    {
        _flushTimer.Dispose();
        Flush();
    }
}
