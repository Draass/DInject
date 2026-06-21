using DInject.Internal;

namespace DInject.Tests.AutoLoadSceneTests
{
    public partial class Qux
    {
    }

    public partial class Bar
    {
        public Bar(Qux qux)
        {
        }
    }

    public partial class Foo
    {
        public Foo(Bar bar)
        {
            Log.Trace("Created Foo");
        }
    }

    public partial class Scene3Installer : MonoInstaller<Scene3Installer>
    {
        public override void InstallBindings()
        {
            Container.Bind<Foo>().AsSingle().NonLazy();
        }
    }
}
