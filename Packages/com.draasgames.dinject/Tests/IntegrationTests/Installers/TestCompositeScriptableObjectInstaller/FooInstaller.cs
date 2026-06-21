using UnityEngine;
using DInject;

namespace DInject.Tests.Installers.CompositeScriptableObjectInstallers
{
    // [CreateAssetMenu(fileName = "FooInstaller", menuName = "Installers/FooInstaller")]
    public partial class FooInstaller : ScriptableObjectInstaller<FooInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind<Foo>().AsSingle().NonLazy();
        }
    }
}