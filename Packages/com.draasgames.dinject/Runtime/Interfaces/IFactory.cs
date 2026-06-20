#if UNITASK_PLUGIN
using Cysharp.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

namespace DInject
{
    public interface IFactory
    {
        bool IsAsync => false;
    }
}
