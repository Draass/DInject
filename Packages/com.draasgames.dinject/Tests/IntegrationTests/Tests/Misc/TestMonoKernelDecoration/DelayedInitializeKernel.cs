using System.Threading.Tasks;
using DInject;

namespace DInject.Tests.TestAnimationStateBehaviourInject
{
    public partial class DelayedInitializeKernel : BaseMonoKernelDecorator
    {
        public async override void Initialize()
        {
            await Task.Delay(5000);
            DecoratedMonoKernel.Initialize();
        }
    }
}