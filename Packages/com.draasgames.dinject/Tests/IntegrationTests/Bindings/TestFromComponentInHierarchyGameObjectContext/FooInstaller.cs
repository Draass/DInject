using UnityEngine;

#pragma warning disable 649

namespace DInject.Tests.Bindings.FromComponentInHierarchyGameObjectContext
{
    public partial class FooInstaller : MonoInstaller
    {
        [SerializeField]
        Foo _foo;

        public override void InstallBindings()
        {
            Container.Bind<Foo>().FromInstance(_foo).AsSingle();
            Container.Bind<Gorp>().FromComponentInHierarchy().AsSingle();
        }
    }
}
