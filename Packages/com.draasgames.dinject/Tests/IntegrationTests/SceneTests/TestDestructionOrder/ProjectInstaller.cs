namespace DInject.Tests.TestDestructionOrder
{
    public partial class ProjectInstaller : MonoInstaller<ProjectInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<FooDisposable3>().AsSingle();
        }
    }
}
