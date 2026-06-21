namespace DInject.Tests.DecoratorTests
{
    public partial class Scene1Installer : MonoInstaller<Scene1Installer>
    {
        public override void InstallBindings()
        {
            Container.Bind<Bar>().AsSingle();
        }
    }
}
