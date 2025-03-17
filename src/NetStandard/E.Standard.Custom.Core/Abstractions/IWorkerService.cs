using System.Threading.Tasks;

namespace E.Standard.Custom.Core.Abstractions;

public interface IWorkerService
{
    int DurationSeconds { get; }

    Task<bool> Init();

    Task<bool> DoWork();
}
