using System.Reflection;
using DInject;

namespace DInjectBench
{
    public sealed class DInjectAdapter : IContainerAdapter
    {
        public string Name => "DInject";

        public string SelfCheck()
        {
            // The generator emits a static __zenCreateInjectTypeInfo per covered type. Its absence
            // means the analyzer DLL is not imported with the 'RoslynAnalyzer' label, and - since
            // DInject is codegen-only - resolution would fail rather than fall back to reflection.
            var generated = typeof(DRoot).GetMethod(
                "__zenCreateInjectTypeInfo",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) != null;

            return generated
                ? null
                : "DInject codegen not active: import Packages/com.draasgames.dinject/CodeGen/DInject.CodeGen.dll with the 'RoslynAnalyzer' asset label.";
        }

        public object Build(BindMode mode)
        {
            var c = new DiContainer();
            if (mode == BindMode.Singleton)
            {
                c.Bind<ILeaf>().To<DLeaf>().AsSingle();
                c.Bind<IMid>().To<DMid>().AsSingle();
                c.Bind<IServiceGraphRoot>().To<DRoot>().AsSingle();
            }
            else
            {
                c.Bind<ILeaf>().To<DLeaf>().AsTransient();
                c.Bind<IMid>().To<DMid>().AsTransient();
                c.Bind<IServiceGraphRoot>().To<DRoot>().AsTransient();
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
