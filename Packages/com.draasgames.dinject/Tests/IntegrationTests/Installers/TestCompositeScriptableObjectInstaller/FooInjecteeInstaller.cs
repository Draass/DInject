using UnityEngine;
using DInject;

namespace DInject.Tests.Installers.CompositeScriptableObjectInstallers
{
    // [CreateAssetMenu(fileName = "FooInjecteeInstaller", menuName = "Installers/FooInjecteeInstaller")]
    public partial class FooInjecteeInstaller : ScriptableObjectInstaller<FooInjecteeInstaller>
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