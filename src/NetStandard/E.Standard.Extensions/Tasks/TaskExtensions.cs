using System.Threading.Tasks;

namespace E.Standard.Extensions.Tasks;

static public class TaskExtensions
{
    static public T AwaitResult<T>(this Task<T> task)
    {
        return task.GetAwaiter().GetResult();
    }
}
