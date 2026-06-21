using UnityEngine;
using DInject;

namespace DInject.Tests.Installers.CompositeMonoInstallers
{
    public partial class FooInjecteeInstaller : MonoInstaller<FooInjecteeInstaller>
    {
        public override void InstallBindings()
        {
            Container
                .Bind<FooInjectee>()
                .AsSingle()
                .NonLazy();
        }
    }
}