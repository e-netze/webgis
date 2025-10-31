//#nullable enable

//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Api.Core.AppCode.Services;

//public class TaskRunnerService
//{
//    private readonly IDictionary<string, Task> _tasks;

//    public TaskRunnerService()
//    {
//        _tasks = new Dictionary<string, Task>();
//    }

//    public void AddTask(string taskName, Func<CancellationToken, Task<object>> taskFunc, CancellationTokenSource? cts)
//    {
//        cts ??= new CancellationTokenSource();
//        _tasks[taskName] = Task.Run(async () =>
//        {
//            try
//            {
//                return await taskFunc(cts.Token);
//            }
//            catch (OperationCanceledException)
//            {
//                // Task was cancelled
//                return null;
//            }
//        }, cts.Token);
//    }

//    public void RemoveTask(string taskName)
//    {
//        if (_tasks.ContainsKey(taskName))
//        {
//            _tasks.Remove(taskName);
//        }
//    }

//    public IEnumerable<string> GetRunningTasks()
//    {
//        return _tasks.Keys;
//    }

//    public T GetTaskResul<T>(string taskName)
//    {
//        if (_tasks.TryGetValue(taskName, out var task))
//        {
//            for (int i = 0; i < 100; i++)
//            {
//                if (task.IsCompleted)
//                {
//                    if (task is Task<T> typedTask)
//                    {
//                        return typedTask.Result;
//                    }
//                    return default(T);
//                }
//                Thread.Sleep(100);
//            }
//        }

//        return default(T);
//    }

//}
