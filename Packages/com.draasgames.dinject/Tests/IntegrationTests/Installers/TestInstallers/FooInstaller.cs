namespace DInject.Tests.Installers.Installers
{
    public partial class Foo
    {
    }

    public partial class FooInstaller : Installer<FooInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind<Foo>().AsSingle().NonLazy();
        }
    }
}
