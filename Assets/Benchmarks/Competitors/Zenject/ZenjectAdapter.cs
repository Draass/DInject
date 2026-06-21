using Zenject;

namespace DInjectBench
{
    // Extenject uses the same bind DSL as DInject (DInject is a Zenject fork). 'using Zenject;'
    // here resolves DiContainer to Zenject.DiContainer; the DInject adapter lives in a separate
    // file/assembly with 'using DInject;' so the identically-named types never collide.
    //
    // NOTE: runs on Extenject's REFLECTION path unless Zenject Reflection Baking is configured for
    // this assembly (Edit > Zenject > Reflection Baking). Label results accordingly.
    public sealed class ZenjectAdapter : IContainerAdapter
    {
        public string Name => "Extenject";

        public string SelfCheck() => null;

        public object Build(BindMode mode)
        {
            var c = new DiContainer();
            if (mode == BindMode.Singleton)
            {
                c.Bind<ILeaf>().To<ZLeaf>().AsSingle();
                c.Bind<IMid>().To<ZMid>().AsSingle();
                c.Bind<IServiceGraphRoot>().To<ZRoot>().AsSingle();
            }
            else
            {
                c.Bind<ILeaf>().To<ZLeaf>().AsTransient();
                c.Bind<IMid>().To<ZMid>().AsTransient();
                c.Bind<IServiceGraphRoot>().To<ZRoot>().AsTransient();
            }
            return c;
        }

        public IServiceGraphRoot ResolveRoot(object container)
        {
            return ((DiContainer)container).Resolve<IServiceGraphRoot>();
        }

        public ILeaf ResolveLeaf(object container)
        {
            return ((DiContainer)container).Resolve<ILeaf>();
        }
    }
}
