namespace DInject.Tests.AutoLoadSceneTests
{
    public partial class Scene1Installer : MonoInstaller<Scene1Installer>
    {
        public override void InstallBindings()
        {
            Container.Bind<Qux>().AsSingle();
        }
    }
}
