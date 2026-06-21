namespace DInject.Tests.Installers.MonoInstallers
{
    public partial class Foo
    {
    }

    public partial class FooInstaller : MonoInstaller<FooInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind<Foo>().AsSingle().NonLazy();
        }
    }
}
