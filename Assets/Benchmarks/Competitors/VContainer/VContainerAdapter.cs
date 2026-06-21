using VContainer;

namespace DInjectBench
{
    // VContainer: Register<TImpl>(Lifetime).As<TContract>(); Build() -> IObjectResolver; Resolve<T>().
    // Ensure VContainer.SourceGenerator is active (default since 1.13) for the codegen fast path.
    public sealed class VContainerAdapter : IContainerAdapter
    {
        public string Name => "VContainer";

        public string SelfCheck() => null;

        public object Build(BindMode mode)
        {
            var lt = mode == BindMode.Singleton ? Lifetime.Singleton : Lifetime.Transient;
            var b = new ContainerBuilder();
            b.Register<VLeaf>(lt).As<ILeaf>();
            b.Register<VMid>(lt).As<IMid>();
            b.Register<VRoot>(lt).As<IServiceGraphRoot>();
            return b.Build();
        }

        public IServiceGraphRoot ResolveRoot(object container)
        {
            return ((IObjectResolver)container).Resolve<IServiceGraphRoot>();
        }

        public ILeaf ResolveLeaf(object container)
        {
            return ((IObjectResolver)container).Resolve<ILeaf>();
        }
    }
}
