namespace DInject.Tests.Installers.ScriptableObjectInstallers
{
    public partial class Foo
    {
    }

    //[CreateAssetMenu(fileName = "FooInstaller", menuName = "Installers/FooInstaller")]
    public partial class FooInstaller : ScriptableObjectInstaller<FooInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind<Foo>().AsSingle().NonLazy();
        }
    }
}
