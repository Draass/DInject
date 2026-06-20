using UnityEngine;
using DInject;

namespace DInject.Tests.Installers.CompositeMonoInstallers
{
    public class FooInstaller : MonoInstaller<FooInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind<Foo>().AsSingle().NonLazy();
        }
    }
}