#if UNITASK_PLUGIN
using Cysharp.Threading.Tasks;
using Task = Cysharp.Threading.Tasks.UniTask;
#else
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
#endif

namespace DInject
{
    public interface IAsyncInject
    {
        bool HasResult { get; }
        bool IsCancelled { get; }
        bool IsFaulted { get; }
        bool IsCompleted { get; }
        object Result { get; }

        Task Task { get; }

#if !UNITASK_PLUGIN
        TaskAwaiter GetAwaiter();
#endif
    }
}
