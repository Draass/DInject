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

        public object Build(BindMode mode)
        {
            var lt = mode == BindMode.Singleton ? Lifetime.Singleton : Lifetime.Transient;
            var b = new ContainerBuilder();
            b.RegisterType(typeof(RLeaf), new[] { typeof(ILeaf) }, lt, Resolution.Lazy);
            b.RegisterType(typeof(RMid), new[] { typeof(IMid) }, lt, Resolution.Lazy);
            b.RegisterType(typeof(RRoot), new[] { typeof(IServiceGraphRoot) }, lt, Resolution.Lazy);
            return b.Build();
        }

        public IServiceGraphRoot ResolveRoot(object container)
        {
            return ((Container)container).Resolve<IServiceGraphRoot>();
        }

        public ILeaf ResolveLeaf(object container)
        {
            return ((Container)container).Resolve<ILeaf>();
        }
    }
}
