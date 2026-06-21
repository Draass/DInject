using VContainer;

namespace DInjectBench
{
    // VERIFY against your installed VContainer version: the Register<TImpl>(Lifetime).As<TContract>()
    // fluent API is stable and documented, but confirm it compiles after install.
    public sealed class VContainerAdapter : IContainerAdapter
    {
        public string Name => "VContainer";

        public string SelfCheck() => null;

        public object Build()
        {
            var b = new ContainerBuilder();
            b.Register<VLeaf>(Lifetime.Transient).As<ILeaf>();
            b.Register<VMid>(Lifetime.Transient).As<IMid>();
            b.Register<VRoot>(Lifetime.Transient).As<IServiceGraphRoot>();
            return b.Build();
        }

        public IServiceGraphRoot ResolveRoot(object container)
        {
            return ((IObjectResolver)container).Resolve<IServiceGraphRoot>();
        }
    }
}
