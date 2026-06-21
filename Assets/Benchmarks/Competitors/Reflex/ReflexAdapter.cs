using Reflex.Core;
using Reflex.Enums;

namespace DInjectBench
{
    // Verified against Reflex 14.3.1: ContainerBuilder.RegisterType(type, contracts, Lifetime, Resolution)
    // (Transient must pair with Resolution.Lazy - Transient+Eager is asserted against), Build() -> Container,
    // Container.Resolve<T>().
    public sealed class ReflexAdapter : IContainerAdapter
    {
        public string Name => "Reflex";

        public string SelfCheck() => null;

        public object Build()
        {
            var b = new ContainerBuilder();
            b.RegisterType(typeof(RLeaf), new[] { typeof(ILeaf) }, Lifetime.Transient, Resolution.Lazy);
            b.RegisterType(typeof(RMid), new[] { typeof(IMid) }, Lifetime.Transient, Resolution.Lazy);
            b.RegisterType(typeof(RRoot), new[] { typeof(IServiceGraphRoot) }, Lifetime.Transient, Resolution.Lazy);
            return b.Build();
        }

        public IServiceGraphRoot ResolveRoot(object container)
        {
            return ((Container)container).Resolve<IServiceGraphRoot>();
        }
    }
}
